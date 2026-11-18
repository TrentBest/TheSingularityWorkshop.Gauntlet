namespace TheSingularityWorkshop.Gauntlet;

/// <summary>Finite resource and capability limits requested for one isolated attempt.</summary>
public sealed record HostExecutionLimits(
    int WallClockSeconds,
    int CpuMillicores,
    int MemoryMiB,
    int ProcessCount,
    int DiskMiB,
    long MaxOutputBytes,
    long MaxArtifactBytes,
    int MaxArtifactCount,
    int MaxAttempts,
    bool DenyNetworkEgress);

/// <summary>Versioned, immutable request to execute one exact candidate attempt.</summary>
public sealed record HostExecutionRequest(
    int SchemaVersion,
    string RequestId,
    CandidateIdentity Candidate,
    string ProfileId,
    int ProfileVersion,
    string ProfileDigest,
    string RunId,
    int Attempt,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    HostExecutionLimits Limits);

/// <summary>Host-observed evidence for one enforced control.</summary>
public sealed record HostControlEvidence(bool Configured, bool ObservedEnforced, long? ConfiguredLimit = null, long? ObservedLimit = null);

/// <summary>Separates configured controls from independently observed enforcement.</summary>
public sealed record HostEffectiveControls(
    HostControlEvidence NoCredentials,
    HostControlEvidence NoHostMounts,
    HostControlEvidence MetadataBlocked,
    HostControlEvidence NetworkEgressDenied,
    HostControlEvidence ReadOnlyCandidateInput,
    HostControlEvidence BoundedScratch,
    HostControlEvidence WallClockLimit,
    HostControlEvidence CpuLimit,
    HostControlEvidence MemoryLimit,
    HostControlEvidence ProcessLimit,
    HostControlEvidence DiskLimit,
    HostControlEvidence OutputLimit,
    HostControlEvidence ArtifactLimit,
    HostControlEvidence ArtifactCountLimit);

/// <summary>The network policy requested and attested for one execution.</summary>
public enum HostNetworkPolicy
{
    /// <summary>All outbound network access, including DNS and metadata endpoints, is denied.</summary>
    DenyAll
}

/// <summary>Why the host execution reached its terminal state.</summary>
public enum HostTerminalReason
{
    /// <summary>The declared workload completed successfully.</summary>
    Completed,
    /// <summary>The declared workload returned a non-zero exit status.</summary>
    CandidateExitNonZero,
    /// <summary>The host watchdog reached the wall-clock deadline.</summary>
    WatchdogTimeout,
    /// <summary>The host accepted a cancellation request.</summary>
    CancellationRequested,
    /// <summary>A host-enforced resource ceiling was reached.</summary>
    ResourceLimitExceeded,
    /// <summary>Host provisioning failed before workload execution.</summary>
    ProvisioningError,
    /// <summary>One or more required isolation controls could not be verified.</summary>
    IsolationControlUnverified,
    /// <summary>The host could not terminate the complete workload process tree.</summary>
    ProcessTreeTerminationFailed,
    /// <summary>The host could not verify successful environment cleanup.</summary>
    CleanupFailed
}

/// <summary>Terminal state attested by the isolation host.</summary>
public enum HostExecutionOutcome
{
    /// <summary>The declared workload completed successfully.</summary>
    Completed,
    /// <summary>The candidate workload returned a failure.</summary>
    CandidateFailed,
    /// <summary>The host watchdog ended the attempt at its deadline.</summary>
    TimedOut,
    /// <summary>The attempt was cancelled.</summary>
    Cancelled,
    /// <summary>A host-enforced resource ceiling ended the attempt.</summary>
    ResourceLimited,
    /// <summary>Host provisioning failed.</summary>
    ProvisioningFailed,
    /// <summary>One or more isolation controls could not be verified.</summary>
    IsolationUnverified,
    /// <summary>The complete workload process tree could not be terminated.</summary>
    TerminationFailed,
    /// <summary>Host cleanup or residual-resource verification failed.</summary>
    CleanupFailed
}

/// <summary>Authenticated host evidence bound to one request and attempt.</summary>
public sealed record HostExecutionReceipt(
    int SchemaVersion,
    string ReceiptId,
    string RequestId,
    CandidateIdentity Candidate,
    string ProfileId,
    int ProfileVersion,
    string ProfileDigest,
    string RunId,
    int Attempt,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    HostExecutionOutcome Outcome,
    HostTerminalReason TerminalReason,
    string HostRunnerImage,
    string HostPlatform,
    string IsolationAdapterVersion,
    HostNetworkPolicy NetworkPolicy,
    bool NetworkEgressDeniedObserved,
    string InputArtifactDigest,
    long InputArtifactBytes,
    int InputArtifactCount,
    string? OutputArtifactDigest,
    long OutputArtifactBytes,
    int OutputArtifactCount,
    HostEffectiveControls EffectiveControls,
    bool ProcessTreeTerminated,
    bool CleanupSucceeded,
    bool ResidualResourcesClear,
    string HostProvider,
    string HostIdentity,
    string SigningKeyId,
    string Signature);

/// <summary>Verifies host receipt signatures using an externally managed trust policy.</summary>
public interface IHostReceiptSignatureVerifier
{
    /// <summary>Returns true only when the supplied signature authenticates the exact canonical payload under the trusted provider key.</summary>
    /// <param name="canonicalSignedPayload">Exact domain-separated RFC 8785 bytes produced by the receipt payload builder.</param>
    /// <param name="signature">The signature value from the receipt wire document.</param>
    /// <param name="signingKeyId">The provider key identifier.</param>
    /// <param name="hostProvider">The attested host provider identity.</param>
    /// <param name="hostIdentity">The attested host/runner identity.</param>
    bool Verify(ReadOnlyMemory<byte> canonicalSignedPayload, string signature, string signingKeyId, string hostProvider, string hostIdentity);
}

/// <summary>Atomically rejects receipt identifiers that have already been accepted.</summary>
public interface IHostReceiptReplayGuard
{
    /// <summary>Registers a receipt identifier once; returns false when it was already registered.</summary>
    bool TryRegister(string receiptId);
}

/// <summary>Result of fail-closed host request or receipt validation.</summary>
public sealed record HostContractValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    /// <summary>Creates a successful validation result.</summary>
    public static HostContractValidationResult Valid() => new(true, Array.Empty<string>());

    /// <summary>Creates a failed validation result.</summary>
    public static HostContractValidationResult Invalid(IEnumerable<string> errors) => new(false, errors.ToArray());
}

/// <summary>
/// Performs schema and binding checks only. It does not provision a host, prove OS isolation,
/// or authorize candidate execution. Signature verification and replay protection are injected.
/// </summary>
public static class HostIsolationContractValidator
{
    /// <summary>Current supported request and receipt schema version.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Validates a request before any host provisioning occurs.</summary>
    public static HostContractValidationResult ValidateRequest(HostExecutionRequest? request, DateTimeOffset now)
    {
        var errors = new List<string>();
        if (request is null)
            return HostContractValidationResult.Invalid(["Request is missing."]);

        if (request.SchemaVersion != CurrentSchemaVersion)
            errors.Add("Request schema version is unsupported.");
        if (string.IsNullOrWhiteSpace(request.RequestId))
            errors.Add("Request ID is required.");
        if (request.Candidate is null ||
            !IsRepository(request.Candidate.Repository) ||
            !IsHex(request.Candidate.CommitSha, 40) ||
            !IsSha256(request.Candidate.ArtifactDigest) ||
            (request.Candidate.ManifestDigest is not null && !IsSha256(request.Candidate.ManifestDigest)))
            errors.Add("Candidate identity is missing or malformed.");
        if (string.IsNullOrWhiteSpace(request.ProfileId) || request.ProfileVersion <= 0 || !IsSha256(request.ProfileDigest))
            errors.Add("Profile identity is missing or malformed.");
        if (string.IsNullOrWhiteSpace(request.RunId) || request.Attempt <= 0)
            errors.Add("Run identity or attempt is invalid.");
        if (request.ExpiresAt <= request.IssuedAt || request.IssuedAt > now || request.ExpiresAt <= now)
            errors.Add("Request validity interval is invalid or expired.");

        var limits = request.Limits;
        if (limits is null ||
            limits.WallClockSeconds <= 0 ||
            limits.CpuMillicores <= 0 ||
            limits.MemoryMiB <= 0 ||
            limits.ProcessCount <= 0 ||
            limits.DiskMiB <= 0 ||
            limits.MaxOutputBytes <= 0 ||
            limits.MaxArtifactBytes <= 0 ||
            limits.MaxArtifactCount <= 0 ||
            limits.MaxAttempts <= 0 ||
            !limits.DenyNetworkEgress)
            errors.Add("All limits must be finite and positive, and network egress must be denied.");
        else if (request.Attempt > limits.MaxAttempts)
            errors.Add("Attempt exceeds the declared maximum attempt count.");

        return errors.Count == 0 ? HostContractValidationResult.Valid() : HostContractValidationResult.Invalid(errors);
    }

    /// <summary>
    /// Validates an authenticated receipt against the exact request. The replay guard is updated
    /// only after all structural, identity, time, outcome, cleanup, and signature checks pass.
    /// </summary>
    public static HostContractValidationResult ValidateReceipt(
        HostExecutionRequest request,
        HostExecutionReceipt? receipt,
        ReadOnlyMemory<byte> canonicalSignedPayload,
        DateTimeOffset now,
        IHostReceiptSignatureVerifier signatureVerifier,
        IHostReceiptReplayGuard replayGuard)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(signatureVerifier);
        ArgumentNullException.ThrowIfNull(replayGuard);

        var requestValidation = ValidateRequest(request, now);
        if (!requestValidation.IsValid)
            return requestValidation;

        var errors = new List<string>();
        if (receipt is null)
            return HostContractValidationResult.Invalid(["Receipt is missing."]);
        if (canonicalSignedPayload.IsEmpty)
            return HostContractValidationResult.Invalid(["Canonical signed receipt payload is missing."]);
        if (receipt.SchemaVersion != CurrentSchemaVersion)
            errors.Add("Receipt schema version is unsupported.");
        if (string.IsNullOrWhiteSpace(receipt.ReceiptId) || string.IsNullOrWhiteSpace(receipt.RequestId))
            errors.Add("Receipt and request identifiers are required.");
        if (receipt.RequestId != request.RequestId ||
            receipt.Candidate != request.Candidate ||
            receipt.ProfileId != request.ProfileId ||
            receipt.ProfileVersion != request.ProfileVersion ||
            receipt.ProfileDigest != request.ProfileDigest ||
            receipt.RunId != request.RunId ||
            receipt.Attempt != request.Attempt)
            errors.Add("Receipt does not bind to the exact requested candidate and attempt.");
        if (receipt.StartedAt < request.IssuedAt || receipt.CompletedAt < receipt.StartedAt ||
            receipt.CompletedAt > request.ExpiresAt || receipt.CompletedAt > now ||
            receipt.StartedAt > request.ExpiresAt ||
            receipt.CompletedAt - receipt.StartedAt > TimeSpan.FromSeconds(request.Limits.WallClockSeconds))
            errors.Add("Receipt execution timestamps are outside the request validity interval.");
        if (string.IsNullOrWhiteSpace(receipt.HostProvider) ||
            string.IsNullOrWhiteSpace(receipt.HostIdentity) ||
            string.IsNullOrWhiteSpace(receipt.HostRunnerImage) ||
            string.IsNullOrWhiteSpace(receipt.HostPlatform) ||
            string.IsNullOrWhiteSpace(receipt.IsolationAdapterVersion) ||
            string.IsNullOrWhiteSpace(receipt.SigningKeyId) ||
            string.IsNullOrWhiteSpace(receipt.Signature))
            errors.Add("Host provenance, runner image/platform, adapter version, and signature metadata are required.");
        if (!HostReceiptJsonV1.TryDecodeSignature(receipt.Signature, out _))
            errors.Add("Receipt signature is not a valid version-1 ES256 base64url value.");
        if (receipt.NetworkPolicy != HostNetworkPolicy.DenyAll || !receipt.NetworkEgressDeniedObserved)
            errors.Add("The receipt must attest the deny-all network policy and observed egress denial.");
        if (!IsSha256(receipt.InputArtifactDigest) ||
            receipt.InputArtifactDigest != request.Candidate.ArtifactDigest ||
            receipt.InputArtifactBytes <= 0 ||
            receipt.InputArtifactBytes > request.Limits.MaxArtifactBytes ||
            receipt.InputArtifactCount <= 0 ||
            receipt.InputArtifactCount > request.Limits.MaxArtifactCount)
            errors.Add("Input artifact digest or byte count is missing, mismatched, or above the requested artifact bound.");
        if (receipt.OutputArtifactBytes < 0 ||
            receipt.OutputArtifactBytes > request.Limits.MaxArtifactBytes ||
            receipt.OutputArtifactCount < 0 ||
            receipt.OutputArtifactCount > request.Limits.MaxArtifactCount ||
            (receipt.OutputArtifactCount == 0 && (receipt.OutputArtifactBytes != 0 || receipt.OutputArtifactDigest is not null)) ||
            (receipt.OutputArtifactCount > 0 && (receipt.OutputArtifactBytes <= 0 || !IsSha256(receipt.OutputArtifactDigest))))
            errors.Add("Output artifact digest and byte count are inconsistent or exceed the requested artifact bound.");
        if (!TerminalReasonMatchesOutcome(receipt.Outcome, receipt.TerminalReason))
            errors.Add("Terminal reason does not match the host execution outcome.");

        if (receipt.EffectiveControls is null || !AllControlsObserved(receipt.EffectiveControls))
            errors.Add("One or more required controls were not configured and independently observed as enforced.");
        if (receipt.EffectiveControls is not null && !LimitsAreEnforced(request.Limits, receipt.EffectiveControls))
            errors.Add("One or more observed resource limits exceed the requested bounds or lack numeric enforcement evidence.");
        if (!receipt.ProcessTreeTerminated)
            errors.Add("Complete process-tree termination was not verified.");
        if (!receipt.CleanupSucceeded || !receipt.ResidualResourcesClear)
            errors.Add("Host cleanup or residual-resource verification failed.");
        if (receipt.Outcome is not (HostExecutionOutcome.Completed or HostExecutionOutcome.CandidateFailed))
            errors.Add("Host execution did not reach an ordinary terminal outcome.");

        if (errors.Count == 0)
        {
            try
            {
                if (!signatureVerifier.Verify(canonicalSignedPayload, receipt.Signature, receipt.SigningKeyId, receipt.HostProvider, receipt.HostIdentity))
                    errors.Add("Receipt signature is invalid or its signing key is not trusted.");
            }
            catch
            {
                errors.Add("Receipt signature verification failed unexpectedly.");
            }
        }

        if (errors.Count == 0)
        {
            try
            {
                if (!replayGuard.TryRegister(receipt.ReceiptId))
                    errors.Add("Receipt identifier has already been accepted.");
            }
            catch
            {
                errors.Add("Receipt replay registration failed unexpectedly.");
            }
        }

        return errors.Count == 0 ? HostContractValidationResult.Valid() : HostContractValidationResult.Invalid(errors);
    }

    private static bool TerminalReasonMatchesOutcome(HostExecutionOutcome outcome, HostTerminalReason reason) =>
        (outcome, reason) switch
        {
            (HostExecutionOutcome.Completed, HostTerminalReason.Completed) => true,
            (HostExecutionOutcome.CandidateFailed, HostTerminalReason.CandidateExitNonZero) => true,
            (HostExecutionOutcome.TimedOut, HostTerminalReason.WatchdogTimeout) => true,
            (HostExecutionOutcome.Cancelled, HostTerminalReason.CancellationRequested) => true,
            (HostExecutionOutcome.ResourceLimited, HostTerminalReason.ResourceLimitExceeded) => true,
            (HostExecutionOutcome.ProvisioningFailed, HostTerminalReason.ProvisioningError) => true,
            (HostExecutionOutcome.IsolationUnverified, HostTerminalReason.IsolationControlUnverified) => true,
            (HostExecutionOutcome.TerminationFailed, HostTerminalReason.ProcessTreeTerminationFailed) => true,
            (HostExecutionOutcome.CleanupFailed, HostTerminalReason.CleanupFailed) => true,
            _ => false
        };

    private static bool AllControlsObserved(HostEffectiveControls controls) =>
        new[]
        {
            controls.NoCredentials, controls.NoHostMounts, controls.MetadataBlocked,
            controls.NetworkEgressDenied, controls.ReadOnlyCandidateInput, controls.BoundedScratch,
            controls.WallClockLimit, controls.CpuLimit, controls.MemoryLimit, controls.ProcessLimit, controls.DiskLimit,
            controls.OutputLimit, controls.ArtifactLimit, controls.ArtifactCountLimit
        }.All(control => control is not null && control.Configured && control.ObservedEnforced);

    private static bool LimitsAreEnforced(HostExecutionLimits limits, HostEffectiveControls controls) =>
        IsBounded(controls.WallClockLimit, limits.WallClockSeconds) &&
        IsBounded(controls.CpuLimit, limits.CpuMillicores) &&
        IsBounded(controls.MemoryLimit, limits.MemoryMiB) &&
        IsBounded(controls.ProcessLimit, limits.ProcessCount) &&
        IsBounded(controls.DiskLimit, limits.DiskMiB) &&
        IsBounded(controls.OutputLimit, limits.MaxOutputBytes) &&
        IsBounded(controls.ArtifactLimit, limits.MaxArtifactBytes) &&
        IsBounded(controls.ArtifactCountLimit, limits.MaxArtifactCount);

    private static bool IsBounded(HostControlEvidence? evidence, long requestedLimit) =>
        evidence is not null &&
        evidence.ConfiguredLimit is > 0 && evidence.ObservedLimit is > 0 &&
        evidence.ConfiguredLimit <= requestedLimit && evidence.ObservedLimit <= evidence.ConfiguredLimit;

    private static bool IsRepository(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var parts = value.Split('/');
        return parts.Length == 2 && parts.All(part =>
            part.Length > 0 && part.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.'));
    }

    private static bool IsHex(string? value, int length) =>
        value is not null && value.Length == length && value.All(Uri.IsHexDigit);

    private static bool IsSha256(string? value) =>
        value is not null && value.StartsWith("sha256:", StringComparison.Ordinal) &&
        IsHex(value[7..], 64);
}
