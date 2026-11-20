using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using TheSingularityWorkshop.Gauntlet.Security;
using Xunit;

namespace TheSingularityWorkshop.Gauntlet.Tests;

public sealed class EcdsaP256HostReceiptSignatureVerifierTests
{
    private const string KeyId = "provider-key-2026-01";
    private const string Provider = "fixture-provider";
    private const string Host = "host-fixture-1";
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-18T12:00:00Z");
    private static readonly byte[] Payload = Encoding.UTF8.GetBytes(
        "TheSingularityWorkshop.Gauntlet.HostExecutionReceipt.v1\n{\"receiptId\":\"r1\"}");

    [Fact]
    public void Valid_es256_signature_verifies_for_exact_key_provider_host_and_payload()
    {
        using var privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] publicKey = privateKey.ExportSubjectPublicKeyInfo();
        string signature = Sign(privateKey, Payload);
        var trustEntry = Entry(publicKey);
        var verifier = Verifier(trustEntry);

        Assert.True(verifier.Verify(Payload, signature, KeyId, Provider, Host, Now));
    }

    [Fact]
    public void Modified_payload_or_signature_is_rejected()
    {
        using var privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] publicKey = privateKey.ExportSubjectPublicKeyInfo();
        string signature = Sign(privateKey, Payload);
        var verifier = Verifier(Entry(publicKey));

        byte[] modifiedPayload = Payload.ToArray();
        modifiedPayload[^1] ^= 0x01;
        string modifiedSignature = (signature[0] == 'A' ? "B" : "A") + signature[1..];

        Assert.False(verifier.Verify(modifiedPayload, signature, KeyId, Provider, Host, Now));
        Assert.False(verifier.Verify(Payload, modifiedSignature, KeyId, Provider, Host, Now));
    }

    [Fact]
    public void Unknown_key_provider_mismatch_and_unlisted_host_are_rejected()
    {
        using var privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var verifier = Verifier(Entry(privateKey.ExportSubjectPublicKeyInfo()));
        string signature = Sign(privateKey, Payload);

        Assert.False(verifier.Verify(Payload, signature, "unknown-key", Provider, Host, Now));
        Assert.False(verifier.Verify(Payload, signature, KeyId, "other-provider", Host, Now));
        Assert.False(verifier.Verify(Payload, signature, KeyId, Provider, "other-host", Now));
    }

    [Fact]
    public void Expired_not_yet_valid_and_revoked_keys_are_rejected()
    {
        using var privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] publicKey = privateKey.ExportSubjectPublicKeyInfo();
        string signature = Sign(privateKey, Payload);

        var notYetValid = Verifier(Entry(publicKey, notBefore: Now.AddSeconds(1)));
        var expired = Verifier(Entry(publicKey, notAfter: Now));
        var revoked = Verifier(Entry(publicKey, isRevoked: true));

        Assert.False(notYetValid.Verify(Payload, signature, KeyId, Provider, Host, Now));
        Assert.False(expired.Verify(Payload, signature, KeyId, Provider, Host, Now));
        Assert.False(revoked.Verify(Payload, signature, KeyId, Provider, Host, Now));
    }

    [Fact]
    public void Algorithm_key_curve_malformed_spki_and_non_utc_time_are_rejected()
    {
        using var p256 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        string signature = Sign(p256, Payload);

        var wrongAlgorithm = Verifier(Entry(p256.ExportSubjectPublicKeyInfo(), algorithm: "ES384"));
        var wrongCurve = Verifier(Entry(p384.ExportSubjectPublicKeyInfo()));
        var malformedKey = Verifier(Entry([0x01, 0x02, 0x03]));

        Assert.False(wrongAlgorithm.Verify(Payload, signature, KeyId, Provider, Host, Now));
        Assert.False(wrongCurve.Verify(Payload, signature, KeyId, Provider, Host, Now));
        Assert.False(malformedKey.Verify(Payload, signature, KeyId, Provider, Host, Now));
        Assert.False(Verifier(Entry(p256.ExportSubjectPublicKeyInfo())).Verify(
            Payload, signature, KeyId, Provider, Host, Now.ToOffset(TimeSpan.FromHours(-8))));
    }

    [Fact]
    public void Validity_window_is_inclusive_at_start_and_exclusive_at_end()
    {
        using var privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] publicKey = privateKey.ExportSubjectPublicKeyInfo();
        string signature = Sign(privateKey, Payload);
        var verifier = Verifier(Entry(publicKey, notBefore: Now, notAfter: Now.AddMinutes(1)));

        Assert.True(verifier.Verify(Payload, signature, KeyId, Provider, Host, Now));
        Assert.False(verifier.Verify(Payload, signature, KeyId, Provider, Host, Now.AddMinutes(1)));
    }

    [Fact]
    public void Trust_store_exceptions_fail_closed()
    {
        var verifier = new EcdsaP256HostReceiptSignatureVerifier(new ThrowingTrustStore());

        Assert.False(verifier.Verify(Payload, new string('A', 86), KeyId, Provider, Host, Now));
    }



    [Fact]
    public void Trusted_key_copies_public_key_bytes_and_allowed_host_identities()
    {
        using var privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] publicKey = privateKey.ExportSubjectPublicKeyInfo();
        var hosts = new List<string> { Host };
        var entry = new HostReceiptTrustedKey(
            KeyId,
            Provider,
            EcdsaP256HostReceiptSignatureVerifier.Algorithm,
            hosts,
            publicKey,
            Now.AddMinutes(-1),
            Now.AddMinutes(1),
            false);

        Array.Clear(publicKey);
        hosts.Clear();

        Assert.True(entry.AllowsHostIdentity(Host));
        Assert.NotEmpty(entry.SubjectPublicKeyInfo.ToArray());
    }

    [Fact]
    public void Trusted_key_public_key_memory_cannot_mutate_the_trust_snapshot()
    {
        using var privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] original = privateKey.ExportSubjectPublicKeyInfo();
        var entry = Entry(original);

        ReadOnlyMemory<byte> exposed = entry.SubjectPublicKeyInfo;
        Assert.True(MemoryMarshal.TryGetArray(exposed, out ArraySegment<byte> segment));
        Assert.NotNull(segment.Array);
        segment.Array![segment.Offset] ^= 0xFF;

        Assert.Equal(original, entry.SubjectPublicKeyInfo.ToArray());
    }

    [Fact]
    public void Immutable_trust_store_snapshots_entries_and_rejects_duplicate_key_ids()
    {
        using var privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var entry = Entry(privateKey.ExportSubjectPublicKeyInfo());
        var mutableSource = new List<HostReceiptTrustedKey> { entry };
        var store = new ImmutableHostReceiptTrustStore(mutableSource);

        mutableSource.Clear();

        Assert.True(store.TryGetKey(KeyId, out var found));
        Assert.Same(entry, found);
        Assert.False(store.TryGetKey("unknown-key", out _));
        Assert.Throws<ArgumentException>(() =>
            new ImmutableHostReceiptTrustStore([entry, entry]));
    }

    private static HostReceiptTrustedKey Entry(
        byte[] publicKey,
        string algorithm = EcdsaP256HostReceiptSignatureVerifier.Algorithm,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null,
        bool isRevoked = false) =>
        new(
            KeyId,
            Provider,
            algorithm,
            [Host],
            publicKey,
            notBefore ?? Now.AddMinutes(-1),
            notAfter ?? Now.AddMinutes(1),
            isRevoked);

    private static EcdsaP256HostReceiptSignatureVerifier Verifier(HostReceiptTrustedKey key) =>
        new(new SingleKeyTrustStore(key));

    private static string Sign(ECDsa privateKey, byte[] payload)
    {
        byte[] signature = privateKey.SignData(
            payload,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return Convert.ToBase64String(signature)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class SingleKeyTrustStore(HostReceiptTrustedKey key) : IHostReceiptTrustStore
    {
        public bool TryGetKey(string keyId, out HostReceiptTrustedKey? trustedKey)
        {
            trustedKey = string.Equals(keyId, key.KeyId, StringComparison.Ordinal) ? key : null;
            return trustedKey is not null;
        }
    }

    private sealed class ThrowingTrustStore : IHostReceiptTrustStore
    {
        public bool TryGetKey(string keyId, out HostReceiptTrustedKey? trustedKey) =>
            throw new InvalidOperationException("Trust store unavailable.");
    }
}
