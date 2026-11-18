using System.Text;
using System.Text.Json;
using MackySoft.Json.Canonicalization;
using TheSingularityWorkshop.Gauntlet;
using Xunit;

namespace TheSingularityWorkshop.Gauntlet.Jcs.Tests;

public sealed class Rfc8785IntegrationTests
{
    private const string DomainSeparator = "TheSingularityWorkshop.Gauntlet.HostExecutionReceipt.v1\n";

    public static TheoryData<string, string> OfficialRfc8785Vectors => new()
    {
        {
            """[56,{"d":true,"10":null,"1":[]}]""",
            "5b35362c7b2231223a5b5d2c223130223a6e756c6c2c2264223a747275657d5d"
        },
        {
            """{"peach":"This sorting order","péché":"is wrong according to French","pêche":"but canonicalization MUST","sin":"ignore locale"}""",
            "7b227065616368223a225468697320736f7274696e67206f72646572222c2270c3a96368c3a9223a2269732077726f6e67206163636f7264696e6720746f204672656e6368222c2270c3aa636865223a226275742063616e6f6e6963616c697a6174696f6e204d555354222c2273696e223a2269676e6f7265206c6f63616c65227d"
        },
        {
            """{"1":{"f":{"f":"hi","F":5},"\n":56.0},"10":{},"":"empty","a":{},"111":[{"e":"yes","E":"no"}],"A":{}}""",
            "7b22223a22656d707479222c2231223a7b225c6e223a35362c2266223a7b2246223a352c2266223a226869227d7d2c223130223a7b7d2c22313131223a5b7b2245223a226e6f222c2265223a22796573227d5d2c2241223a7b7d2c2261223a7b7d7d"
        },
        {
            """{"Unnormalized Unicode":"A\u030a"}""",
            "7b22556e6e6f726d616c697a656420556e69636f6465223a2241cc8a227d"
        },
        {
            """{"numbers":[333333333.33333329,1E30,4.50,2e-3,0.000000000000000000000000001],"string":"\u20ac$\u000F\u000aA'\u0042\u0022\u005c\\\"\/","literals":[null,true,false]}""",
            "7b226c69746572616c73223a5b6e756c6c2c747275652c66616c73655d2c226e756d62657273223a5b3333333333333333332e333333333333332c31652b33302c342e352c302e3030322c31652d32375d2c22737472696e67223a22e282ac245c75303030665c6e4127425c225c5c5c5c5c222f227d"
        },
        {
            @"{""\u20ac"":""Euro Sign"",""\r"":""Carriage Return"",""\u000a"":""Newline"",""1"":""One"",""\u0080"":""Control\u007f"",""\ud83d\ude02"":""Smiley"",""\u00f6"":""Latin Small Letter O With Diaeresis"",""\ufb33"":""Hebrew Letter Dalet With Dagesh"",""</script>"":""Browser Challenge""}",
            "7b225c6e223a224e65776c696e65222c225c72223a2243617272696167652052657475726e222c2231223a224f6e65222c223c2f7363726970743e223a2242726f77736572204368616c6c656e6765222c22c280223a22436f6e74726f6c7f222c22c3b6223a224c6174696e20536d616c6c204c6574746572204f205769746820446961657265736973222c22e282ac223a224575726f205369676e222c22f09f9882223a22536d696c6579222c22efacb3223a22486562726577204c65747465722044616c6574205769746820446167657368227d"
        }
    };

    [Theory]
    [MemberData(nameof(OfficialRfc8785Vectors))]
    public void Canonicalizer_matches_official_rfc8785_vectors(string inputJson, string expectedHex)
    {
        byte[] input = Encoding.UTF8.GetBytes(inputJson);
        byte[] expected = Convert.FromHexString(expectedHex);

        Assert.Equal(expected, Rfc8785JsonCanonicalizer.Canonicalize(input));

        using JsonDocument document = JsonDocument.Parse(input);
        Assert.Equal(expected, Rfc8785JsonCanonicalizer.Canonicalize(document.RootElement));
    }


    public static TheoryData<ulong, string> AppendixBFiniteNumberVectors => new()
    {
        { 0x0000000000000000UL, "0" },
        { 0x0000000000000001UL, "5e-324" },
        { 0x8000000000000001UL, "-5e-324" },
        { 0x7fefffffffffffffUL, "1.7976931348623157e+308" },
        { 0xffefffffffffffffUL, "-1.7976931348623157e+308" },
        { 0x4340000000000000UL, "9007199254740992" },
        { 0xc340000000000000UL, "-9007199254740992" },
        { 0x4430000000000000UL, "295147905179352830000" },
        { 0x44b52d02c7e14af5UL, "9.999999999999997e+22" },
        { 0x44b52d02c7e14af6UL, "1e+23" },
        { 0x44b52d02c7e14af7UL, "1.0000000000000001e+23" },
        { 0x444b1ae4d6e2ef4eUL, "999999999999999700000" },
        { 0x444b1ae4d6e2ef4fUL, "999999999999999900000" },
        { 0x444b1ae4d6e2ef50UL, "1e+21" },
        { 0x3eb0c6f7a0b5ed8cUL, "9.999999999999997e-7" },
        { 0x3eb0c6f7a0b5ed8dUL, "0.000001" },
        { 0x41b3de4355555553UL, "333333333.3333332" },
        { 0x41b3de4355555554UL, "333333333.33333325" },
        { 0x41b3de4355555555UL, "333333333.3333333" },
        { 0x41b3de4355555556UL, "333333333.3333334" },
        { 0x41b3de4355555557UL, "333333333.33333343" },
        { 0xbecbf647612f3696UL, "-0.0000033333333333333333" },
        { 0x43143ff3c1cb0959UL, "1424953923781206.2" }
    };

    [Theory]
    [MemberData(nameof(AppendixBFiniteNumberVectors))]
    public void Canonicalizer_matches_rfc8785_appendix_b_binary64_vectors(ulong ieee754Bits, string expected)
    {
        double value = BitConverter.Int64BitsToDouble(unchecked((long)ieee754Bits));
        byte[] input = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);

        Assert.Equal(expectedBytes, Rfc8785JsonCanonicalizer.Canonicalize(input));

        using JsonDocument document = JsonDocument.Parse(input);
        Assert.Equal(expectedBytes, Rfc8785JsonCanonicalizer.Canonicalize(document.RootElement));
    }


    [Fact]
    public void Canonicalizer_sorts_properties_and_normalizes_primitive_representations()
    {
        var input = Encoding.UTF8.GetBytes("""{"z":true,"b":4.50,"a":1E0}""");

        var canonical = Rfc8785JsonCanonicalizer.Canonicalize(input);

        Assert.Equal("""{"a":1,"b":4.5,"z":true}""", Encoding.UTF8.GetString(canonical));
    }

    [Fact]
    public void Canonicalizer_matches_rfc8785_number_serialization_examples()
    {
        var input = Encoding.UTF8.GetBytes(
            """{"numbers":[333333333.33333329,1E30,4.50,2e-3,0.000000000000000000000000001]}""");

        var canonical = Rfc8785JsonCanonicalizer.Canonicalize(input);

        Assert.Equal(
            """{"numbers":[333333333.3333333,1e+30,4.5,0.002,1e-27]}""",
            Encoding.UTF8.GetString(canonical));
    }

    [Fact]
    public void Canonicalizer_rejects_duplicate_decoded_property_names()
    {
        var input = Encoding.UTF8.GetBytes("""{"outer":{"value":1,"\u0076alue":2}}""");

        var exception = Assert.Throws<JsonCanonicalizationException>(
            () => Rfc8785JsonCanonicalizer.Canonicalize(input));

        Assert.Equal(JsonCanonicalizationFailureKind.DuplicateProperty, exception.FailureKind);
    }

    [Theory]
    [InlineData("-0", JsonCanonicalizationFailureKind.NegativeZero)]
    [InlineData("1e400", JsonCanonicalizationFailureKind.NumberNotRepresentable)]
    [InlineData("""{"value":"\ud800"}""", JsonCanonicalizationFailureKind.InvalidUnicode)]
    [InlineData("""{"\udfff":true}""", JsonCanonicalizationFailureKind.InvalidUnicode)]
    public void Canonicalizer_rejects_non_i_json_values(string json, JsonCanonicalizationFailureKind expected)
    {
        var exception = Assert.Throws<JsonCanonicalizationException>(
            () => Rfc8785JsonCanonicalizer.Canonicalize(Encoding.UTF8.GetBytes(json)));

        Assert.Equal(expected, exception.FailureKind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/* comment */{}")]
    [InlineData("[1,]")]
    [InlineData("{} {}")]
    [InlineData("\uFEFF{}")]
    public void Canonicalizer_rejects_non_strict_raw_json(string json)
    {
        Assert.Throws<JsonCanonicalizationException>(
            () => Rfc8785JsonCanonicalizer.Canonicalize(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Canonicalizer_rejects_invalid_utf8_and_excessive_depth()
    {
        byte[] invalidUtf8 = [0x22, 0xC3, 0x28, 0x22];
        var invalidUtf8Exception = Assert.Throws<JsonCanonicalizationException>(
            () => Rfc8785JsonCanonicalizer.Canonicalize(invalidUtf8));
        Assert.Equal(JsonCanonicalizationFailureKind.InvalidUnicode, invalidUtf8Exception.FailureKind);

        var tooDeep = Encoding.UTF8.GetBytes(new string('[', 65) + "0" + new string(']', 65));
        var depthException = Assert.Throws<JsonCanonicalizationException>(
            () => Rfc8785JsonCanonicalizer.Canonicalize(tooDeep));
        Assert.Equal(JsonCanonicalizationFailureKind.MaximumDepthExceeded, depthException.FailureKind);
    }

    [Fact]
    public void Canonicalizer_returns_independently_owned_bytes()
    {
        byte[] input = """{"b":2,"a":1}"""u8.ToArray();

        byte[] first = Rfc8785JsonCanonicalizer.Canonicalize(input);
        byte[] second = Rfc8785JsonCanonicalizer.Canonicalize(input);

        Assert.NotSame(first, second);
        first[0] = 0;
        Assert.Equal("""{"a":1,"b":2}""", Encoding.UTF8.GetString(second));
    }

    [Fact]
    public void Receipt_projection_canonicalizes_and_domain_separates_exact_signed_bytes()
    {
        byte[] wireJson = JsonSerializer.SerializeToUtf8Bytes(Receipt);
        HostExecutionReceiptV1 receipt = HostReceiptJsonV1.Deserialize(wireJson);

        byte[] signatureExcludedJson = HostReceiptJsonV1.SerializeSignatureExcluded(receipt);
        byte[] canonicalJson = Rfc8785JsonCanonicalizer.Canonicalize(signatureExcludedJson);
        byte[] domainBytes = Encoding.UTF8.GetBytes(DomainSeparator);
        byte[] signedPayload = [.. domainBytes, .. canonicalJson];

        using JsonDocument wireDocument = JsonDocument.Parse(wireJson);
        using JsonDocument canonicalDocument = JsonDocument.Parse(canonicalJson);
        Assert.False(canonicalDocument.RootElement.TryGetProperty("signature", out _));
        Assert.Equal("fixture-key-1", canonicalDocument.RootElement.GetProperty("signingKeyId").GetString());
        Assert.Equal(
            wireDocument.RootElement.EnumerateObject().Count() - 1,
            canonicalDocument.RootElement.EnumerateObject().Count());
        Assert.Equal(domainBytes, signedPayload[..domainBytes.Length]);
        Assert.Equal(
            canonicalJson,
            signedPayload[domainBytes.Length..]);
    }

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
        HostIdentity = "fixture-host",
        SigningKeyId = "fixture-key-1",
        Signature = "fixture-signature"
    };
}
