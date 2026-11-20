using System.Security.Cryptography;
using TheSingularityWorkshop.Gauntlet;

namespace TheSingularityWorkshop.Gauntlet.Security;

/// <summary>
/// Immutable public-key trust entry for one host receipt signing key.
/// Private key material is never accepted by this type.
/// </summary>
public sealed class HostReceiptTrustedKey
{
    private readonly byte[] _subjectPublicKeyInfo;
    private readonly HashSet<string> _allowedHostIdentities;

    /// <summary>Gets the immutable key identifier used by the provider.</summary>
    public string KeyId { get; }

    /// <summary>Gets the provider identity to which this key is bound.</summary>
    public string ProviderId { get; }

    /// <summary>Gets the fixed signature algorithm identifier.</summary>
    public string Algorithm { get; }

    /// <summary>Gets the UTC instant at which this key becomes valid.</summary>
    public DateTimeOffset NotBeforeUtc { get; }

    /// <summary>Gets the exclusive UTC instant at which this key expires.</summary>
    public DateTimeOffset NotAfterUtc { get; }

    /// <summary>Gets whether the key is currently revoked.</summary>
    public bool IsRevoked { get; }

    /// <summary>Gets a read-only view of the DER SubjectPublicKeyInfo bytes.</summary>
    public ReadOnlyMemory<byte> SubjectPublicKeyInfo => _subjectPublicKeyInfo.ToArray();

    /// <summary>Creates a trust entry by copying the supplied public key and allowed host identities.</summary>
    public HostReceiptTrustedKey(
        string keyId,
        string providerId,
        string algorithm,
        IEnumerable<string> allowedHostIdentities,
        ReadOnlySpan<byte> subjectPublicKeyInfo,
        DateTimeOffset notBeforeUtc,
        DateTimeOffset notAfterUtc,
        bool isRevoked)
    {
        ArgumentNullException.ThrowIfNull(allowedHostIdentities);

        KeyId = keyId;
        ProviderId = providerId;
        Algorithm = algorithm;
        _allowedHostIdentities = new HashSet<string>(
            allowedHostIdentities.Where(value => !string.IsNullOrWhiteSpace(value)),
            StringComparer.Ordinal);
        _subjectPublicKeyInfo = subjectPublicKeyInfo.ToArray();
        NotBeforeUtc = notBeforeUtc;
        NotAfterUtc = notAfterUtc;
        IsRevoked = isRevoked;
    }

    /// <summary>Returns true when the host identity is explicitly allowed by this trust entry.</summary>
    public bool AllowsHostIdentity(string hostIdentity) =>
        !string.IsNullOrWhiteSpace(hostIdentity) && _allowedHostIdentities.Contains(hostIdentity);
}

/// <summary>Provides trusted host signing keys from an independently managed trust source.</summary>
public interface IHostReceiptTrustStore
{
    /// <summary>Looks up one key by its immutable key identifier.</summary>
    bool TryGetKey(string keyId, out HostReceiptTrustedKey? trustedKey);
}

/// <summary>
/// Verifies ES256 host receipt signatures using .NET's ECDSA implementation and an injected trust store.
/// </summary>
public sealed class EcdsaP256HostReceiptSignatureVerifier : IHostReceiptSignatureVerifier
{
    /// <summary>Version-1 signature algorithm identifier: ECDSA P-256 with SHA-256.</summary>
    public const string Algorithm = "ES256";

    private readonly IHostReceiptTrustStore _trustStore;

    /// <summary>Creates a verifier bound to an independently managed public-key trust store.</summary>
    public EcdsaP256HostReceiptSignatureVerifier(IHostReceiptTrustStore trustStore) =>
        _trustStore = trustStore ?? throw new ArgumentNullException(nameof(trustStore));

    /// <inheritdoc />
    public bool Verify(
        ReadOnlyMemory<byte> canonicalSignedPayload,
        string signature,
        string signingKeyId,
        string hostProvider,
        string hostIdentity,
        DateTimeOffset verificationTime)
    {
        try
        {
            if (canonicalSignedPayload.IsEmpty ||
                verificationTime.Offset != TimeSpan.Zero ||
                !HostReceiptJsonV1.TryDecodeSignature(signature, out byte[] signatureBytes) ||
                string.IsNullOrWhiteSpace(signingKeyId) ||
                string.IsNullOrWhiteSpace(hostProvider) ||
                string.IsNullOrWhiteSpace(hostIdentity))
                return false;

            if (!_trustStore.TryGetKey(signingKeyId, out HostReceiptTrustedKey? trustedKey) ||
                trustedKey is null)
                return false;

            if (!string.Equals(trustedKey.KeyId, signingKeyId, StringComparison.Ordinal) ||
                !string.Equals(trustedKey.ProviderId, hostProvider, StringComparison.Ordinal) ||
                !trustedKey.AllowsHostIdentity(hostIdentity) ||
                !string.Equals(trustedKey.Algorithm, Algorithm, StringComparison.Ordinal) ||
                trustedKey.IsRevoked ||
                trustedKey.NotBeforeUtc.Offset != TimeSpan.Zero ||
                trustedKey.NotAfterUtc.Offset != TimeSpan.Zero ||
                trustedKey.NotAfterUtc <= trustedKey.NotBeforeUtc ||
                verificationTime < trustedKey.NotBeforeUtc ||
                verificationTime >= trustedKey.NotAfterUtc)
                return false;

            using ECDsa publicKey = ECDsa.Create();
            ReadOnlySpan<byte> subjectPublicKeyInfo = trustedKey.SubjectPublicKeyInfo.Span;
            publicKey.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out int bytesRead);

            if (bytesRead != subjectPublicKeyInfo.Length || publicKey.KeySize != 256)
                return false;

            return publicKey.VerifyData(
                canonicalSignedPayload.Span,
                signatureBytes,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (Exception)
        {
            // Trust-store, key-import, and cryptographic failures are all fail-closed.
            return false;
        }
    }
}
