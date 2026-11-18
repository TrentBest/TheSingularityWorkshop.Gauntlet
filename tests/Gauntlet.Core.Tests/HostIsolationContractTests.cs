using System.Text;
using TheSingularityWorkshop.Gauntlet;
using Xunit;

namespace TheSingularityWorkshop.Gauntlet.Tests;

public sealed class HostIsolationContractTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-15T12:00:00Z");

    private static HostExecutionRequest Request => new(
        1, "request-1",
        new CandidateIdentity("TrentBest/Fixture", new string('a', 40), $"sha256:{new string('b', 64)}", null),
        "strict-profile", 1, $"sha256:{new string('c', 64)}",
        "run-1", 1, Now.AddMinutes(-1), Now.AddMinutes(10),
        new HostExecutionLimits(60, 1000, 512, 32, 1024, 1_000_000, 500_000, 10, 3, true));

    private static HostControlEvidence Evidence => new(true, true);
    private static HostControlEvidence Limit(long value) => new(true, true, value, value);

    private static HostEffectiveControls Controls => new(
        Evidence, Evidence, Evidence, Evidence, Evidence, Evidence,
        Limit(60), Limit(1000), Limit(512), Limit(32), Limit(1024),
        Limit(1_000_000), Limit(500_000), Limit(10));

    private static HostExecutionReceipt Receipt => new(
        1, "receipt-1", Request.RequestId, Request.Candidate,
        Request.ProfileId, Request.ProfileVersion, Request.ProfileDigest,
        Request.RunId, Request.Attempt, Now, Now.AddSeconds(2),
        HostExecutionOutcome.Completed, HostTerminalReason.Completed,
        "gauntlet-fixture-image@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        "linux-x64", "fixture-adapter/1.0",
        HostNetworkPolicy.DenyAll, true,
        Request.Candidate.ArtifactDigest, 4096, 1, null, 0, 0,
        Controls, true, true, true,
        "fixture-provider", "host-fixture-1", "test-key-1", "signed-payload");

    private sealed class AcceptSignature : IHostReceiptSignatureVerifier
    {
        public bool Verify(ReadOnlyMemory<byte> canonicalSignedPayload, string signature, string signingKeyId, string hostProvider, string hostIdentity) => !canonicalSignedPayload.IsEmpty && signature == "test-signature" && signingKeyId == "test-key-1" && hostProvider == "fixture-provider" && hostIdentity == "host-fixture-1";
    }

    private sealed class RejectSignature : IHostReceiptSignatureVerifier
    {
        public bool Verify(ReadOnlyMemory<byte> canonicalSignedPayload, string signature, string signingKeyId, string hostProvider, string hostIdentity) => false;
    }

    private sealed class ThrowingSignature : IHostReceiptSignatureVerifier
    {
        public bool Verify(ReadOnlyMemory<byte> canonicalSignedPayload, string signature, string signingKeyId, string hostProvider, string hostIdentity) => throw new InvalidOperationException("adapter failure");
    }

    private sealed class ThrowingReplayGuard : IHostReceiptReplayGuard
    {
        public bool TryRegister(string receiptId) => throw new InvalidOperationException("adapter failure");
    }

    private sealed class ReplayGuard : IHostReceiptReplayGuard
    {
        private readonly HashSet<string> _seen = [];
        public bool TryRegister(string receiptId) => _seen.Add(receiptId);
    }

    [Fact]
    public void Valid_request_is_accepted() =>
        Assert.True(HostIsolationContractValidator.ValidateRequest(Request, Now).IsValid);

    [Fact]
    public void Unsupported_schema_and_missing_limits_are_rejected()
    {
        var invalid = Request with { SchemaVersion = 99, Limits = null! };
        var result = HostIsolationContractValidator.ValidateRequest(invalid, Now);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("schema"));
        Assert.Contains(result.Errors, error => error.Contains("limits"));
    }

    [Fact]
    public void Zero_or_inconsistent_limits_are_rejected()
    {
        var invalid = Request with
        {
            Limits = Request.Limits with { MemoryMiB = 0 }
        };
        Assert.False(HostIsolationContractValidator.ValidateRequest(invalid, Now).IsValid);
    }

    [Fact]
    public void Egress_must_be_denied_and_attempt_must_be_bounded()
    {
        var invalid = Request with
        {
            Attempt = 4,
            Limits = Request.Limits with { DenyNetworkEgress = false, MaxAttempts = 3 }
        };
        Assert.False(HostIsolationContractValidator.ValidateRequest(invalid, Now).IsValid);
    }

    [Fact]
    public void Expired_request_is_rejected()
    {
        Assert.False(HostIsolationContractValidator.ValidateRequest(
            Request with { ExpiresAt = Now.AddSeconds(-1) }, Now).IsValid);
    }

    [Fact]
    public void Exact_signed_receipt_is_accepted_once()
    {
        var replayGuard = new ReplayGuard();
        Assert.True(HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new AcceptSignature(), replayGuard).IsValid);
        Assert.False(HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new AcceptSignature(), replayGuard).IsValid);
    }

    [Fact]
    public void Candidate_or_attempt_mismatch_is_rejected()
    {
        var mismatch = Receipt with { Candidate = Receipt.Candidate with { CommitSha = new string('d', 40) } };
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, mismatch, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new AcceptSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("exact requested candidate"));
    }

    [Fact]
    public void Configured_but_unobserved_control_is_rejected()
    {
        var controls = Controls with { NetworkEgressDenied = new HostControlEvidence(true, false) };
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt with { EffectiveControls = controls }, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1),
            new AcceptSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("observed as enforced"));
    }

    [Fact]
    public void Observed_resource_limit_above_request_is_rejected()
    {
        var controls = Controls with { MemoryLimit = Limit(513) };
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt with { EffectiveControls = controls }, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1),
            new AcceptSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("resource limits exceed"));
    }
    [Fact]
    public void Missing_nested_resource_evidence_is_rejected_without_throwing()
    {
        var controls = Controls with { MemoryLimit = null! };
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt with { EffectiveControls = controls }, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1),
            new AcceptSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("controls were not configured"));
    }

    [Fact]
    public void Signature_verifier_exception_is_rejected()
    {
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new ThrowingSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("verification failed unexpectedly"));
    }

    [Fact]
    public void Replay_guard_exception_is_rejected()
    {
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new AcceptSignature(), new ThrowingReplayGuard());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("replay registration failed unexpectedly"));
    }

    [Fact]
    public void Failed_cleanup_or_live_process_tree_is_rejected()
    {
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt with { CleanupSucceeded = false, ProcessTreeTerminated = false }, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new AcceptSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Untrusted_signature_is_rejected_without_consuming_receipt_id()
    {
        var replayGuard = new ReplayGuard();
        Assert.False(HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new RejectSignature(), replayGuard).IsValid);
        Assert.True(HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new AcceptSignature(), replayGuard).IsValid);
    }


    [Fact]
    public void Receipt_requires_host_platform_adapter_network_and_artifact_evidence()
    {
        var invalid = Receipt with
        {
            HostRunnerImage = "",
            HostPlatform = "",
            IsolationAdapterVersion = "",
            NetworkEgressDeniedObserved = false,
            InputArtifactDigest = $"sha256:{new string('d', 64)}",
            InputArtifactBytes = 0,
            InputArtifactCount = Request.Limits.MaxArtifactCount + 1
        };
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, invalid, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new AcceptSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("runner image/platform"));
        Assert.Contains(result.Errors, error => error.Contains("deny-all network policy"));
        Assert.Contains(result.Errors, error => error.Contains("Input artifact digest"));
    }

    [Fact]
    public void Receipt_rejects_terminal_reason_mismatch_and_oversized_output()
    {
        var invalid = Receipt with
        {
            TerminalReason = HostTerminalReason.WatchdogTimeout,
            OutputArtifactDigest = $"sha256:{new string('d', 64)}",
            OutputArtifactBytes = Request.Limits.MaxArtifactBytes + 1
        };
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, invalid, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new AcceptSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Terminal reason"));
        Assert.Contains(result.Errors, error => error.Contains("Output artifact"));
    }


    [Fact]
    public void Receipt_rejects_artifact_counts_outside_requested_bounds()
    {
        var tooManyInputs = Receipt with { InputArtifactCount = Request.Limits.MaxArtifactCount + 1 };
        var inputResult = HostIsolationContractValidator.ValidateReceipt(
            Request, tooManyInputs, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new AcceptSignature(), new ReplayGuard());
        Assert.False(inputResult.IsValid);
        Assert.Contains(inputResult.Errors, error => error.Contains("Input artifact digest"));

        var tooManyOutputs = Receipt with
        {
            OutputArtifactDigest = $"sha256:{new string('d', 64)}",
            OutputArtifactBytes = 128,
            OutputArtifactCount = Request.Limits.MaxArtifactCount + 1
        };
        var outputResult = HostIsolationContractValidator.ValidateReceipt(
            Request, tooManyOutputs, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new AcceptSignature(), new ReplayGuard());
        Assert.False(outputResult.IsValid);
        Assert.Contains(outputResult.Errors, error => error.Contains("Output artifact"));
    }

    [Fact]
    public void Incomplete_host_outcome_is_rejected()
    {
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt with { Outcome = HostExecutionOutcome.CleanupFailed }, Encoding.UTF8.GetBytes("canonical-test-payload"), Now.AddMinutes(1), new AcceptSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
    }
    [Fact]
    public void Missing_canonical_signed_payload_is_rejected_before_signature_verification()
    {
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt, ReadOnlyMemory<byte>.Empty, Now.AddMinutes(1),
            new AcceptSignature(), new ReplayGuard());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Canonical signed receipt payload is missing"));
    }

}
