using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TheSingularityWorkshop.Gauntlet;

/// <summary>
/// Frozen version-1 JSON wire representation of a host execution receipt.
/// This type is deliberately separate from the domain receipt record.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HostExecutionReceiptV1
{
    [JsonPropertyName("schemaVersion")]
    public required int SchemaVersion { get; init; }

    [JsonPropertyName("receiptId")]
    public required string ReceiptId { get; init; }

    [JsonPropertyName("requestId")]
    public required string RequestId { get; init; }

    [JsonPropertyName("candidate")]
    public required HostReceiptCandidateV1 Candidate { get; init; }

    [JsonPropertyName("profileId")]
    public required string ProfileId { get; init; }

    [JsonPropertyName("profileVersion")]
    public required int ProfileVersion { get; init; }

    [JsonPropertyName("profileDigest")]
    public required string ProfileDigest { get; init; }

    [JsonPropertyName("runId")]
    public required string RunId { get; init; }

    [JsonPropertyName("attempt")]
    public required int Attempt { get; init; }

    [JsonPropertyName("startedAt")]
    public required DateTimeOffset StartedAt { get; init; }

    [JsonPropertyName("completedAt")]
    public required DateTimeOffset CompletedAt { get; init; }

    [JsonPropertyName("outcome")]
    public required string Outcome { get; init; }

    [JsonPropertyName("terminalReason")]
    public required string TerminalReason { get; init; }

    [JsonPropertyName("hostRunnerImage")]
    public required string HostRunnerImage { get; init; }

    [JsonPropertyName("hostPlatform")]
    public required string HostPlatform { get; init; }

    [JsonPropertyName("isolationAdapterVersion")]
    public required string IsolationAdapterVersion { get; init; }

    [JsonPropertyName("networkPolicy")]
    public required string NetworkPolicy { get; init; }

    [JsonPropertyName("networkEgressDeniedObserved")]
    public required bool NetworkEgressDeniedObserved { get; init; }

    [JsonPropertyName("inputArtifactDigest")]
    public required string InputArtifactDigest { get; init; }

    [JsonPropertyName("inputArtifactBytes")]
    public required long InputArtifactBytes { get; init; }

    [JsonPropertyName("inputArtifactCount")]
    public required int InputArtifactCount { get; init; }

    [JsonPropertyName("outputArtifactDigest")]
    public required string? OutputArtifactDigest { get; init; }

    [JsonPropertyName("outputArtifactBytes")]
    public required long OutputArtifactBytes { get; init; }

    [JsonPropertyName("outputArtifactCount")]
    public required int OutputArtifactCount { get; init; }

    [JsonPropertyName("effectiveControls")]
    public required HostReceiptEffectiveControlsV1 EffectiveControls { get; init; }

    [JsonPropertyName("processTreeTerminated")]
    public required bool ProcessTreeTerminated { get; init; }

    [JsonPropertyName("cleanupSucceeded")]
    public required bool CleanupSucceeded { get; init; }

    [JsonPropertyName("residualResourcesClear")]
    public required bool ResidualResourcesClear { get; init; }

    [JsonPropertyName("hostProvider")]
    public required string HostProvider { get; init; }

    [JsonPropertyName("hostIdentity")]
    public required string HostIdentity { get; init; }

    [JsonPropertyName("signingKeyId")]
    public required string SigningKeyId { get; init; }

    [JsonPropertyName("signature")]
    public required string Signature { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HostReceiptCandidateV1
{
    [JsonPropertyName("repository")]
    public required string Repository { get; init; }

    [JsonPropertyName("commitSha")]
    public required string CommitSha { get; init; }

    [JsonPropertyName("artifactDigest")]
    public required string ArtifactDigest { get; init; }

    [JsonPropertyName("manifestDigest")]
    public required string? ManifestDigest { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HostReceiptEffectiveControlsV1
{
    [JsonPropertyName("noCredentials")]
    public required HostReceiptControlEvidenceV1 NoCredentials { get; init; }
    [JsonPropertyName("noHostMounts")]
    public required HostReceiptControlEvidenceV1 NoHostMounts { get; init; }
    [JsonPropertyName("metadataBlocked")]
    public required HostReceiptControlEvidenceV1 MetadataBlocked { get; init; }
    [JsonPropertyName("networkEgressDenied")]
    public required HostReceiptControlEvidenceV1 NetworkEgressDenied { get; init; }
    [JsonPropertyName("readOnlyCandidateInput")]
    public required HostReceiptControlEvidenceV1 ReadOnlyCandidateInput { get; init; }
    [JsonPropertyName("boundedScratch")]
    public required HostReceiptControlEvidenceV1 BoundedScratch { get; init; }
    [JsonPropertyName("wallClockLimit")]
    public required HostReceiptControlEvidenceV1 WallClockLimit { get; init; }
    [JsonPropertyName("cpuLimit")]
    public required HostReceiptControlEvidenceV1 CpuLimit { get; init; }
    [JsonPropertyName("memoryLimit")]
    public required HostReceiptControlEvidenceV1 MemoryLimit { get; init; }
    [JsonPropertyName("processLimit")]
    public required HostReceiptControlEvidenceV1 ProcessLimit { get; init; }
    [JsonPropertyName("diskLimit")]
    public required HostReceiptControlEvidenceV1 DiskLimit { get; init; }
    [JsonPropertyName("outputLimit")]
    public required HostReceiptControlEvidenceV1 OutputLimit { get; init; }
    [JsonPropertyName("artifactLimit")]
    public required HostReceiptControlEvidenceV1 ArtifactLimit { get; init; }
    [JsonPropertyName("artifactCountLimit")]
    public required HostReceiptControlEvidenceV1 ArtifactCountLimit { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HostReceiptControlEvidenceV1
{
    [JsonPropertyName("configured")]
    public required bool Configured { get; init; }

    [JsonPropertyName("observedEnforced")]
    public required bool ObservedEnforced { get; init; }

    [JsonPropertyName("configuredLimit")]
    public required long? ConfiguredLimit { get; init; }

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

        RejectDuplicatePropertyNames(utf8Json);

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
    }

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
                    if (!objectProperties.Peek().Add(name))
                        throw new JsonException($"Duplicate JSON property name '{name}'.");
                    break;
            }
        }

        if (objectProperties.Count != 0)
            throw new JsonException("JSON object was not completely closed.");
    }
}
