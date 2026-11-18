using System.Text;
using System.Text.Json;
using TheSingularityWorkshop.Gauntlet;
using Xunit;

namespace TheSingularityWorkshop.Gauntlet.Tests;

public sealed class HostReceiptJsonV1Tests
{
    private static HostReceiptControlEvidenceV1 Evidence => new()
    {
        Configured = true,
        ObservedEnforced = true,
        ConfiguredLimit = null,
        ObservedLimit = null
    };

    private static HostExecutionReceiptV1 Receipt => new()
    {
        SchemaVersion = 1,
        ReceiptId = "receipt-1",
        RequestId = "request-1",
        Candidate = new HostReceiptCandidateV1
        {
            Repository = "TrentBest/Fixture",
            CommitSha = new string('a', 40),
            ArtifactDigest = $"sha256:{new string('b', 64)}",
            ManifestDigest = null
        },
        ProfileId = "strict-profile",
        ProfileVersion = 1,
        ProfileDigest = $"sha256:{new string('c', 64)}",
        RunId = "run-1",
        Attempt = 1,
        StartedAt = DateTimeOffset.Parse("2026-11-15T12:00:00Z"),
        CompletedAt = DateTimeOffset.Parse("2026-11-15T12:00:02Z"),
        Outcome = "Completed",
        TerminalReason = "Completed",
        HostRunnerImage = "fixture-image@sha256:abc",
        HostPlatform = "linux-x64",
        IsolationAdapterVersion = "fixture/1.0",
        NetworkPolicy = "DenyAll",
        NetworkEgressDeniedObserved = true,
        InputArtifactDigest = $"sha256:{new string('b', 64)}",
        InputArtifactBytes = 4096,
        InputArtifactCount = 1,
        OutputArtifactDigest = null,
        OutputArtifactBytes = 0,
        OutputArtifactCount = 0,
        EffectiveControls = new HostReceiptEffectiveControlsV1
        {
            NoCredentials = Evidence,
            NoHostMounts = Evidence,
            MetadataBlocked = Evidence,
            NetworkEgressDenied = Evidence,
            ReadOnlyCandidateInput = Evidence,
            BoundedScratch = Evidence,
            WallClockLimit = Evidence,
            CpuLimit = Evidence,
            MemoryLimit = Evidence,
            ProcessLimit = Evidence,
            DiskLimit = Evidence,
            OutputLimit = Evidence,
            ArtifactLimit = Evidence,
            ArtifactCountLimit = Evidence
        },
        ProcessTreeTerminated = true,
        CleanupSucceeded = true,
        ResidualResourcesClear = true,
        HostProvider = "fixture-provider",
        HostIdentity = "host-fixture-1",
        SigningKeyId = "test-key-1",
        Signature = "test-signature"
    };

    private static byte[] Json(HostExecutionReceiptV1? receipt = null) =>
        JsonSerializer.SerializeToUtf8Bytes(receipt ?? Receipt, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

    [Fact]
    public void Valid_v1_receipt_deserializes_with_exact_wire_values()
    {
        var parsed = HostReceiptJsonV1.Deserialize(Json());

        Assert.Equal(1, parsed.SchemaVersion);
        Assert.Equal("receipt-1", parsed.ReceiptId);
        Assert.Equal("Completed", parsed.Outcome);
        Assert.Equal("DenyAll", parsed.NetworkPolicy);
        Assert.Equal("test-key-1", parsed.SigningKeyId);
    }

    [Fact]
    public void Signature_excluded_projection_retains_key_id_and_removes_only_signature()
    {
        var parsed = HostReceiptJsonV1.Deserialize(Json());
        using var document = JsonDocument.Parse(HostReceiptJsonV1.SerializeSignatureExcluded(parsed));

        Assert.False(document.RootElement.TryGetProperty("signature", out _));
        Assert.Equal("test-key-1", document.RootElement.GetProperty("signingKeyId").GetString());
        Assert.Equal("receipt-1", document.RootElement.GetProperty("receiptId").GetString());
    }

    [Fact]
    public void Duplicate_decoded_property_names_are_rejected()
    {
        var json = Encoding.UTF8.GetString(Json()).Replace(
            "\"receiptId\":\"receipt-1\"",
            "\"receiptId\":\"receipt-1\",\"receipt\\u0049d\":\"shadow\"",
            StringComparison.Ordinal);

        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Unknown_fields_are_rejected()
    {
        var json = Encoding.UTF8.GetString(Json()).Replace(
            "\"receiptId\":\"receipt-1\"",
            "\"receiptId\":\"receipt-1\",\"unexpected\":true",
            StringComparison.Ordinal);

        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Property_names_are_case_sensitive()
    {
        var json = Encoding.UTF8.GetString(Json()).Replace(
            "\"receiptId\":\"receipt-1\"",
            "\"ReceiptId\":\"receipt-1\"",
            StringComparison.Ordinal);

        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Unsupported_schema_and_unknown_enum_values_are_rejected()
    {
        var unsupported = Receipt with { SchemaVersion = 2 };
        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(Json(unsupported)));

        var unknownOutcome = Receipt with { Outcome = "FutureOutcome" };
        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(Json(unknownOutcome)));
    }

    [Fact]
    public void Oversized_empty_and_malformed_payloads_are_rejected()
    {
        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(ReadOnlySpan<byte>.Empty));
        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(new byte[HostReceiptJsonV1.MaximumReceiptBytes + 1]));
        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(Encoding.UTF8.GetBytes("{\"schemaVersion\":1,}")));
    }

    [Fact]
    public void Missing_required_fields_and_inconsistent_output_artifacts_are_rejected()
    {
        var missing = Encoding.UTF8.GetString(Json()).Replace(
            "\"receiptId\":\"receipt-1\",",
            "",
            StringComparison.Ordinal);
        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(Encoding.UTF8.GetBytes(missing)));

        var inconsistent = Receipt with { OutputArtifactCount = 0, OutputArtifactBytes = 1 };
        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(Json(inconsistent)));
    }

    [Fact]
    public void Invalid_utf8_is_rejected()
    {
        var valid = Json();
        var invalid = new byte[valid.Length + 1];
        valid.CopyTo(invalid, 0);
        invalid[^1] = 0xFF;

        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(invalid));
    }
    [Fact]
    public void Bom_and_unpaired_surrogate_are_rejected()
    {
        var valid = Json();
        var withBom = new byte[valid.Length + 3];
        withBom[0] = 0xEF;
        withBom[1] = 0xBB;
        withBom[2] = 0xBF;
        valid.CopyTo(withBom, 3);
        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(withBom));

        var invalidUnicode = Encoding.UTF8.GetString(valid).Replace(
            "\"hostIdentity\":\"host-fixture-1\"",
            "\"hostIdentity\":\"\\uD800\"",
            StringComparison.Ordinal);
        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(Encoding.UTF8.GetBytes(invalidUnicode)));
    }

    [Fact]
    public void Unsafe_integer_values_malformed_digests_and_non_utc_times_are_rejected()
    {
        var unsafeInteger = Receipt with { InputArtifactBytes = 9_007_199_254_740_992L };
        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(Json(unsafeInteger)));

        var malformedDigest = Receipt with { ProfileDigest = "sha256:xyz" };
        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(Json(malformedDigest)));

        var nonUtc = Receipt with
        {
            StartedAt = DateTimeOffset.Parse("2026-11-15T05:00:00-07:00")
        };
        Assert.ThrowsAny<JsonException>(() => HostReceiptJsonV1.Deserialize(Json(nonUtc)));
    }

}
