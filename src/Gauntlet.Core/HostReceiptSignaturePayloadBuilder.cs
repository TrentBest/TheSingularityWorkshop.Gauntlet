using System.Text;
using System.Text.Json;

namespace TheSingularityWorkshop.Gauntlet;

/// <summary>
/// Produces RFC 8785 canonical UTF-8 JSON bytes for a receipt payload.
/// Implementations must reject inputs outside the RFC 8785/JCS contract.
/// </summary>
public interface IHostReceiptCanonicalizer
{
    /// <summary>Canonicalizes one bounded UTF-8 JSON value.</summary>
    /// <param name="utf8Json">The JSON value to canonicalize.</param>
    /// <returns>Newly owned RFC 8785 canonical UTF-8 bytes.</returns>
    byte[] Canonicalize(ReadOnlySpan<byte> utf8Json);
}

/// <summary>
/// Builds the exact domain-separated byte payload that a host receipt signature verifier must verify.
/// This type does not implement cryptography or key trust.
/// </summary>
public static class HostReceiptSignaturePayloadBuilder
{
    /// <summary>The version-1 domain separator, including its terminating LF.</summary>
    public const string DomainSeparator = "TheSingularityWorkshop.Gauntlet.HostExecutionReceipt.v1\n";

    private static readonly byte[] DomainSeparatorBytes = Encoding.UTF8.GetBytes(DomainSeparator);

    /// <summary>
    /// Creates the signature payload from the validated version-1 wire DTO.
    /// Only the top-level signature field is excluded; signingKeyId and all other fields remain signed.
    /// </summary>
    /// <param name="receipt">The version-1 receipt wire DTO.</param>
    /// <param name="canonicalizer">The reviewed RFC 8785 implementation supplied by the host adapter.</param>
    /// <returns>Domain separator followed by canonical JSON UTF-8 bytes.</returns>
    /// <exception cref="JsonException">The canonicalizer returns an empty or oversized payload.</exception>
    public static byte[] Build(
        HostExecutionReceiptV1 receipt,
        IHostReceiptCanonicalizer canonicalizer)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(canonicalizer);

        byte[] signatureExcludedJson = HostReceiptJsonV1.SerializeSignatureExcluded(receipt);
        byte[] canonicalJson = canonicalizer.Canonicalize(signatureExcludedJson);

        if (canonicalJson is null || canonicalJson.Length == 0)
            throw new JsonException("The receipt canonicalizer returned an empty payload.");

        if (canonicalJson.Length > HostReceiptJsonV1.MaximumReceiptBytes - DomainSeparatorBytes.Length)
            throw new JsonException("The canonical signed receipt payload exceeds the maximum byte length.");

        byte[] signedPayload = new byte[DomainSeparatorBytes.Length + canonicalJson.Length];
        DomainSeparatorBytes.CopyTo(signedPayload, 0);
        canonicalJson.CopyTo(signedPayload, DomainSeparatorBytes.Length);
        return signedPayload;
    }
}
