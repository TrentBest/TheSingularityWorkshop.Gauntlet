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

    private static HostEffectiveControls Controls => new(
        Evidence, Evidence, Evidence, Evidence, Evidence, Evidence,
        Evidence, Evidence, Evidence, Evidence, Evidence);

    private static HostExecutionReceipt Receipt => new(
        1, "receipt-1", Request.RequestId, Request.Candidate,
        Request.ProfileId, Request.ProfileVersion, Request.ProfileDigest,
        Request.RunId, Request.Attempt, Now, Now.AddSeconds(2),
        HostExecutionOutcome.Completed, Controls, true, true, true,
        "fixture-provider", "host-fixture-1", "test-key-1", "signed-payload");

    private sealed class AcceptSignature : IHostReceiptSignatureVerifier
    {
        public bool Verify(HostExecutionReceipt receipt) => true;
    }

    private sealed class RejectSignature : IHostReceiptSignatureVerifier
    {
        public bool Verify(HostExecutionReceipt receipt) => false;
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
            Request, Receipt, Now.AddMinutes(1), new AcceptSignature(), replayGuard).IsValid);
        Assert.False(HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt, Now.AddMinutes(1), new AcceptSignature(), replayGuard).IsValid);
    }

    [Fact]
    public void Candidate_or_attempt_mismatch_is_rejected()
    {
        var mismatch = Receipt with { Candidate = Receipt.Candidate with { CommitSha = new string('d', 40) } };
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, mismatch, Now.AddMinutes(1), new AcceptSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("exact requested candidate"));
    }

    [Fact]
    public void Configured_but_unobserved_control_is_rejected()
    {
        var controls = Controls with { NetworkEgressDenied = new HostControlEvidence(true, false) };
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt with { EffectiveControls = controls }, Now.AddMinutes(1),
            new AcceptSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("observed as enforced"));
    }

    [Fact]
    public void Failed_cleanup_or_live_process_tree_is_rejected()
    {
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt with { CleanupSucceeded = false, ProcessTreeTerminated = false },
            Now.AddMinutes(1), new AcceptSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Untrusted_signature_is_rejected_without_consuming_receipt_id()
    {
        var replayGuard = new ReplayGuard();
        Assert.False(HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt, Now.AddMinutes(1), new RejectSignature(), replayGuard).IsValid);
        Assert.True(HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt, Now.AddMinutes(1), new AcceptSignature(), replayGuard).IsValid);
    }

    [Fact]
    public void Incomplete_host_outcome_is_rejected()
    {
        var result = HostIsolationContractValidator.ValidateReceipt(
            Request, Receipt with { Outcome = HostExecutionOutcome.CleanupFailed },
            Now.AddMinutes(1), new AcceptSignature(), new ReplayGuard());
        Assert.False(result.IsValid);
    }
}
