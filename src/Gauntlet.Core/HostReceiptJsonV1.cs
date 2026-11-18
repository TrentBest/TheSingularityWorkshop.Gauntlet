using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TheSingularityWorkshop.Gauntlet;

/// <summary>
/// Frozen version-1 JSON wire representation of a host execution receipt.
/// This type is deliberately separate from the domain receipt record.
/// </summary>
/// <summary>Version-1 receipt wire DTO with fixed JSON field names.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record HostExecutionReceiptV1
{
    /// <summary>Gets the Schema Version wire value.</summary>

    [JsonPropertyName("schemaVersion")]
    public required int SchemaVersion { get; init; }

    /// <summary>Gets the Receipt Id wire value.</summary>


    [JsonPropertyName("receiptId")]
    public required string ReceiptId { get; init; }

    /// <summary>Gets the Request Id wire value.</summary>


    [JsonPropertyName("requestId")]
    public required string RequestId { get; init; }

    /// <summary>Gets the Candidate wire value.</summary>


    [JsonPropertyName("candidate")]
    public required HostReceiptCandidateV1 Candidate { get; init; }

    /// <summary>Gets the Profile Id wire value.</summary>


    [JsonPropertyName("profileId")]
    public required string ProfileId { get; init; }

    /// <summary>Gets the Profile Version wire value.</summary>


    [JsonPropertyName("profileVersion")]
    public required int ProfileVersion { get; init; }

    /// <summary>Gets the Profile Digest wire value.</summary>


    [JsonPropertyName("profileDigest")]
    public required string ProfileDigest { get; init; }

    /// <summary>Gets the Run Id wire value.</summary>


    [JsonPropertyName("runId")]
    public required string RunId { get; init; }

    /// <summary>Gets the Attempt wire value.</summary>


    [JsonPropertyName("attempt")]
    public required int Attempt { get; init; }

    /// <summary>Gets the Started At wire value.</summary>


    [JsonPropertyName("startedAt")]
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>Gets the Completed At wire value.</summary>


    [JsonPropertyName("completedAt")]
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>Gets the Outcome wire value.</summary>


    [JsonPropertyName("outcome")]
    public required string Outcome { get; init; }

    /// <summary>Gets the Terminal Reason wire value.</summary>


    [JsonPropertyName("terminalReason")]
    public required string TerminalReason { get; init; }

    /// <summary>Gets the Host Runner Image wire value.</summary>


    [JsonPropertyName("hostRunnerImage")]
    public required string HostRunnerImage { get; init; }

    /// <summary>Gets the Host Platform wire value.</summary>


    [JsonPropertyName("hostPlatform")]
    public required string HostPlatform { get; init; }

    /// <summary>Gets the Isolation Adapter Version wire value.</summary>


    [JsonPropertyName("isolationAdapterVersion")]
    public required string IsolationAdapterVersion { get; init; }

    /// <summary>Gets the Network Policy wire value.</summary>


    [JsonPropertyName("networkPolicy")]
    public required string NetworkPolicy { get; init; }

    /// <summary>Gets the Network Egress Denied Observed wire value.</summary>


    [JsonPropertyName("networkEgressDeniedObserved")]
    public required bool NetworkEgressDeniedObserved { get; init; }

    /// <summary>Gets the Input Artifact Digest wire value.</summary>


    [JsonPropertyName("inputArtifactDigest")]
    public required string InputArtifactDigest { get; init; }

    /// <summary>Gets the Input Artifact Bytes wire value.</summary>


    [JsonPropertyName("inputArtifactBytes")]
    public required long InputArtifactBytes { get; init; }

    /// <summary>Gets the Input Artifact Count wire value.</summary>


    [JsonPropertyName("inputArtifactCount")]
    public required int InputArtifactCount { get; init; }

    /// <summary>Gets the Output Artifact Digest wire value.</summary>


    [JsonPropertyName("outputArtifactDigest")]
    public required string? OutputArtifactDigest { get; init; }

    /// <summary>Gets the Output Artifact Bytes wire value.</summary>


    [JsonPropertyName("outputArtifactBytes")]
    public required long OutputArtifactBytes { get; init; }

    /// <summary>Gets the Output Artifact Count wire value.</summary>


    [JsonPropertyName("outputArtifactCount")]
    public required int OutputArtifactCount { get; init; }

    /// <summary>Gets the Effective Controls wire value.</summary>


    [JsonPropertyName("effectiveControls")]
    public required HostReceiptEffectiveControlsV1 EffectiveControls { get; init; }

    /// <summary>Gets the Process Tree Terminated wire value.</summary>


    [JsonPropertyName("processTreeTerminated")]
    public required bool ProcessTreeTerminated { get; init; }

    /// <summary>Gets the Cleanup Succeeded wire value.</summary>


    [JsonPropertyName("cleanupSucceeded")]
    public required bool CleanupSucceeded { get; init; }

    /// <summary>Gets the Residual Resources Clear wire value.</summary>


    [JsonPropertyName("residualResourcesClear")]
    public required bool ResidualResourcesClear { get; init; }

    /// <summary>Gets the Host Provider wire value.</summary>


    [JsonPropertyName("hostProvider")]
    public required string HostProvider { get; init; }

    /// <summary>Gets the Host Identity wire value.</summary>


    [JsonPropertyName("hostIdentity")]
    public required string HostIdentity { get; init; }

    /// <summary>Gets the Signing Key Id wire value.</summary>


    [JsonPropertyName("signingKeyId")]
    public required string SigningKeyId { get; init; }

    /// <summary>Gets the Signature wire value.</summary>


    [JsonPropertyName("signature")]
    public required string Signature { get; init; }
}

/// <summary>Candidate source and artifact identity in the receipt wire schema.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HostReceiptCandidateV1
{
    /// <summary>Gets the Repository wire value.</summary>

    [JsonPropertyName("repository")]
    public required string Repository { get; init; }

    /// <summary>Gets the Commit Sha wire value.</summary>


    [JsonPropertyName("commitSha")]
    public required string CommitSha { get; init; }

    /// <summary>Gets the Artifact Digest wire value.</summary>


    [JsonPropertyName("artifactDigest")]
    public required string ArtifactDigest { get; init; }

    /// <summary>Gets the Manifest Digest wire value.</summary>


    [JsonPropertyName("manifestDigest")]
    public required string? ManifestDigest { get; init; }
}

/// <summary>All required host isolation control observations in the receipt wire schema.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HostReceiptEffectiveControlsV1
{
    /// <summary>Gets the No Credentials wire value.</summary>

    [JsonPropertyName("noCredentials")]
    public required HostReceiptControlEvidenceV1 NoCredentials { get; init; }
    /// <summary>Gets the No Host Mounts wire value.</summary>

    [JsonPropertyName("noHostMounts")]
    public required HostReceiptControlEvidenceV1 NoHostMounts { get; init; }
    /// <summary>Gets the Metadata Blocked wire value.</summary>

    [JsonPropertyName("metadataBlocked")]
    public required HostReceiptControlEvidenceV1 MetadataBlocked { get; init; }
    /// <summary>Gets the Network Egress Denied wire value.</summary>

    [JsonPropertyName("networkEgressDenied")]
    public required HostReceiptControlEvidenceV1 NetworkEgressDenied { get; init; }
    /// <summary>Gets the Read Only Candidate Input wire value.</summary>

    [JsonPropertyName("readOnlyCandidateInput")]
    public required HostReceiptControlEvidenceV1 ReadOnlyCandidateInput { get; init; }
    /// <summary>Gets the Bounded Scratch wire value.</summary>

    [JsonPropertyName("boundedScratch")]
    public required HostReceiptControlEvidenceV1 BoundedScratch { get; init; }
    /// <summary>Gets the Wall Clock Limit wire value.</summary>

    [JsonPropertyName("wallClockLimit")]
    public required HostReceiptControlEvidenceV1 WallClockLimit { get; init; }
    /// <summary>Gets the Cpu Limit wire value.</summary>

    [JsonPropertyName("cpuLimit")]
    public required HostReceiptControlEvidenceV1 CpuLimit { get; init; }
    /// <summary>Gets the Memory Limit wire value.</summary>

    [JsonPropertyName("memoryLimit")]
    public required HostReceiptControlEvidenceV1 MemoryLimit { get; init; }
    /// <summary>Gets the Process Limit wire value.</summary>

    [JsonPropertyName("processLimit")]
    public required HostReceiptControlEvidenceV1 ProcessLimit { get; init; }
    /// <summary>Gets the Disk Limit wire value.</summary>

    [JsonPropertyName("diskLimit")]
    public required HostReceiptControlEvidenceV1 DiskLimit { get; init; }
    /// <summary>Gets the Output Limit wire value.</summary>

    [JsonPropertyName("outputLimit")]
    public required HostReceiptControlEvidenceV1 OutputLimit { get; init; }
    /// <summary>Gets the Artifact Limit wire value.</summary>

    [JsonPropertyName("artifactLimit")]
    public required HostReceiptControlEvidenceV1 ArtifactLimit { get; init; }
    /// <summary>Gets the Artifact Count Limit wire value.</summary>

    [JsonPropertyName("artifactCountLimit")]
    public required HostReceiptControlEvidenceV1 ArtifactCountLimit { get; init; }
}

/// <summary>Configured and independently observed evidence for one host control.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HostReceiptControlEvidenceV1
{
    /// <summary>Gets the Configured wire value.</summary>

    [JsonPropertyName("configured")]
    public required bool Configured { get; init; }

    /// <summary>Gets the Observed Enforced wire value.</summary>


    [JsonPropertyName("observedEnforced")]
    public required bool ObservedEnforced { get; init; }

    /// <summary>Gets the Configured Limit wire value.</summary>


    [JsonPropertyName("configuredLimit")]
    public required long? ConfiguredLimit { get; init; }

    /// <summary>Gets the Observed Limit wire value.</summary>


    [JsonPropertyName("observedLimit")]
    public required long? ObservedLimit { get; init; }
}

/// <summary>
/// Strict System.Text.Json boundary for the version-1 host receipt wire contract.
/// RFC 8785 canonicalization and signature verification are intentionally separate.
/// </summary>
public static class HostReceiptJsonV1
{
    /// <summary>Maximum accepted UTF-8 receipt payload size before parsing.</summary>
    public const int MaximumReceiptBytes = 1_048_576;
    private const long MaximumIJsonSafeInteger = 9_007_199_254_740_991L;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        NumberHandling = JsonNumberHandling.Strict,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        MaxDepth = 32,
        WriteIndented = false
    };

    /// <summary>Parses a bounded receipt, rejecting duplicate decoded property names and unknown fields.</summary>
    public static HostExecutionReceiptV1 Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.IsEmpty || utf8Json.Length > MaximumReceiptBytes)
            throw new JsonException("Receipt payload is empty or exceeds the maximum byte length.");

        if (utf8Json.Length >= 3 && utf8Json[0] == 0xEF && utf8Json[1] == 0xBB && utf8Json[2] == 0xBF)
            throw new JsonException("Receipt JSON must be UTF-8 without a byte-order mark.");

        try
        {
            RejectDuplicatePropertyNames(utf8Json);
        }
        catch (InvalidOperationException exception)
        {
            throw new JsonException("Receipt JSON contains invalid Unicode.", exception);
        }

        var receipt = JsonSerializer.Deserialize<HostExecutionReceiptV1>(utf8Json, SerializerOptions)
            ?? throw new JsonException("Receipt JSON must contain an object.");

        ValidateWireValues(receipt);
        return receipt;
    }

    /// <summary>
    /// Serializes the validated wire DTO without the top-level signature member.
    /// The returned bytes are JSON input for the separate RFC 8785 canonicalizer;
    /// they are not themselves canonical JSON or the final signed message.
    /// </summary>
    public static byte[] SerializeSignatureExcluded(HostExecutionReceiptV1 receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ValidateWireValues(receipt);

        var node = JsonSerializer.SerializeToNode(receipt, SerializerOptions)
            ?? throw new JsonException("Receipt serialization produced no JSON object.");
        node.AsObject().Remove("signature");
        return JsonSerializer.SerializeToUtf8Bytes(node, SerializerOptions);
    }

    private static void ValidateWireValues(HostExecutionReceiptV1 receipt)
    {
        if (receipt.SchemaVersion != 1)
            throw new JsonException("Unsupported host receipt schema version.");
        if (receipt.Candidate is null || receipt.EffectiveControls is null)
            throw new JsonException("Receipt candidate and effectiveControls are required.");
        if (receipt.StartedAt.Offset != TimeSpan.Zero || receipt.CompletedAt.Offset != TimeSpan.Zero ||
            receipt.CompletedAt < receipt.StartedAt)
            throw new JsonException("Receipt timestamps must be ordered and use the UTC offset.");
        if (!IsDefined<HostExecutionOutcome>(receipt.Outcome) ||
            !IsDefined<HostTerminalReason>(receipt.TerminalReason) ||
            !IsDefined<HostNetworkPolicy>(receipt.NetworkPolicy))
            throw new JsonException("Receipt contains an unknown outcome, terminalReason, or networkPolicy value.");

        if (string.IsNullOrWhiteSpace(receipt.ReceiptId) ||
            string.IsNullOrWhiteSpace(receipt.RequestId) ||
            string.IsNullOrWhiteSpace(receipt.ProfileId) ||
            string.IsNullOrWhiteSpace(receipt.ProfileDigest) ||
            string.IsNullOrWhiteSpace(receipt.RunId) ||
            string.IsNullOrWhiteSpace(receipt.HostRunnerImage) ||
            string.IsNullOrWhiteSpace(receipt.HostPlatform) ||
            string.IsNullOrWhiteSpace(receipt.IsolationAdapterVersion) ||
            string.IsNullOrWhiteSpace(receipt.InputArtifactDigest) ||
            string.IsNullOrWhiteSpace(receipt.HostProvider) ||
            string.IsNullOrWhiteSpace(receipt.HostIdentity) ||
            string.IsNullOrWhiteSpace(receipt.SigningKeyId) ||
            string.IsNullOrWhiteSpace(receipt.Signature) ||
            string.IsNullOrWhiteSpace(receipt.Candidate.Repository) ||
            string.IsNullOrWhiteSpace(receipt.Candidate.CommitSha) ||
            string.IsNullOrWhiteSpace(receipt.Candidate.ArtifactDigest))
            throw new JsonException("Receipt contains a missing required string value.");

        if (!IsHex(receipt.Candidate.CommitSha, 40) ||
            !IsSha256(receipt.Candidate.ArtifactDigest) ||
            !IsSha256(receipt.ProfileDigest) ||
            !IsSha256(receipt.InputArtifactDigest) ||
            (receipt.Candidate.ManifestDigest is not null && !IsSha256(receipt.Candidate.ManifestDigest)) ||
            (receipt.OutputArtifactDigest is not null && !IsSha256(receipt.OutputArtifactDigest)))
            throw new JsonException("Receipt contains a malformed commit or SHA-256 digest.");

        if (receipt.ProfileVersion <= 0 || receipt.Attempt <= 0 ||
            receipt.InputArtifactBytes <= 0 || receipt.InputArtifactCount <= 0 ||
            receipt.OutputArtifactBytes < 0 || receipt.OutputArtifactCount < 0)
            throw new JsonException("Receipt versions, attempts, and artifact counts/bytes are outside valid bounds.");

        if (!IsSafeInteger(receipt.InputArtifactBytes) ||
            !IsSafeInteger(receipt.OutputArtifactBytes))
            throw new JsonException("Receipt numeric values must be within the I-JSON safe integer range.");

        if (receipt.OutputArtifactCount == 0 &&
            (receipt.OutputArtifactBytes != 0 || receipt.OutputArtifactDigest is not null))
            throw new JsonException("An empty output artifact set must have zero bytes and a null digest.");
        if (receipt.OutputArtifactCount > 0 &&
            (receipt.OutputArtifactBytes <= 0 || string.IsNullOrWhiteSpace(receipt.OutputArtifactDigest)))
            throw new JsonException("A non-empty output artifact set requires positive bytes and a digest.");

        ValidateControls(receipt.EffectiveControls);
    }

    private static void ValidateControls(HostReceiptEffectiveControlsV1 controls)
    {
        var evidence = new[]
        {
            controls.NoCredentials, controls.NoHostMounts, controls.MetadataBlocked,
            controls.NetworkEgressDenied, controls.ReadOnlyCandidateInput, controls.BoundedScratch,
            controls.WallClockLimit, controls.CpuLimit, controls.MemoryLimit, controls.ProcessLimit,
            controls.DiskLimit, controls.OutputLimit, controls.ArtifactLimit, controls.ArtifactCountLimit
        };

        if (evidence.Any(item => item is null))
            throw new JsonException("Every effective control evidence object is required.");

        if (evidence.Any(item =>
                (item.ConfiguredLimit is not null && !IsSafeInteger(item.ConfiguredLimit.Value)) ||
                (item.ObservedLimit is not null && !IsSafeInteger(item.ObservedLimit.Value))))
            throw new JsonException("Host control limits must be within the I-JSON safe integer range.");
    }

    private static bool IsSafeInteger(long value) =>
        value >= -MaximumIJsonSafeInteger && value <= MaximumIJsonSafeInteger;

    private static bool IsHex(string value, int length) =>
        value.Length == length && value.All(Uri.IsHexDigit);

    private static bool IsSha256(string value) =>
        value.Length == 71 &&
        value.StartsWith("sha256:", StringComparison.Ordinal) &&
        IsHex(value[7..], 64);

    private static bool IsDefined<TEnum>(string value) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed) &&
        Enum.IsDefined(parsed) &&
        string.Equals(parsed.ToString(), value, StringComparison.Ordinal);

    private static void RejectDuplicatePropertyNames(ReadOnlySpan<byte> utf8Json)
    {
        var reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 32
        });
        var objectProperties = new Stack<HashSet<string>>();

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    objectProperties.Push(new HashSet<string>(StringComparer.Ordinal));
                    break;
                case JsonTokenType.EndObject:
                    if (objectProperties.Count == 0)
                        throw new JsonException("Unexpected JSON object terminator.");
                    objectProperties.Pop();
                    break;
                case JsonTokenType.PropertyName:
                    if (objectProperties.Count == 0)
                        throw new JsonException("JSON property appears outside an object.");
                    var name = reader.GetString() ?? throw new JsonException("JSON property name is invalid.");
                    ValidateUnicodeScalars(name);
                    if (!objectProperties.Peek().Add(name))
                        throw new JsonException($"Duplicate JSON property name '{name}'.");
                    break;
                case JsonTokenType.String:
                    ValidateUnicodeScalars(reader.GetString() ?? throw new JsonException("JSON string value is invalid."));
                    break;
            }
        }

        if (objectProperties.Count != 0)
            throw new JsonException("JSON object was not completely closed.");
    }

    private static void ValidateUnicodeScalars(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                    throw new JsonException("JSON strings must contain valid Unicode scalar values.");
                index++;
            }
            else if (char.IsLowSurrogate(current))
            {
                throw new JsonException("JSON strings must contain valid Unicode scalar values.");
            }
        }
    }
}
