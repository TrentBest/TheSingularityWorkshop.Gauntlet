using System.Collections.ObjectModel;

namespace TheSingularityWorkshop.Gauntlet.Security;

/// <summary>
/// Immutable process-local snapshot of host receipt public-key trust entries.
/// The caller remains responsible for securely provisioning and refreshing the snapshot.
/// </summary>
public sealed class ImmutableHostReceiptTrustStore : IHostReceiptTrustStore
{
    private readonly IReadOnlyDictionary<string, HostReceiptTrustedKey> _keys;

    /// <summary>
    /// Creates a fixed trust snapshot and rejects duplicate or missing key identifiers.
    /// A new instance should be created and atomically swapped by the host when keys rotate or are revoked.
    /// </summary>
    public ImmutableHostReceiptTrustStore(IEnumerable<HostReceiptTrustedKey> trustedKeys)
    {
        ArgumentNullException.ThrowIfNull(trustedKeys);

        var entries = new Dictionary<string, HostReceiptTrustedKey>(StringComparer.Ordinal);
        foreach (HostReceiptTrustedKey? trustedKey in trustedKeys)
        {
            if (trustedKey is null || string.IsNullOrWhiteSpace(trustedKey.KeyId))
                throw new ArgumentException("Trust entries and key identifiers are required.", nameof(trustedKeys));

            if (!entries.TryAdd(trustedKey.KeyId, trustedKey))
                throw new ArgumentException($"Duplicate host receipt key identifier '{trustedKey.KeyId}'.", nameof(trustedKeys));
        }

        _keys = new ReadOnlyDictionary<string, HostReceiptTrustedKey>(entries);
    }

    /// <inheritdoc />
    public bool TryGetKey(string keyId, out HostReceiptTrustedKey? trustedKey)
    {
        if (string.IsNullOrWhiteSpace(keyId))
        {
            trustedKey = null;
            return false;
        }

        return _keys.TryGetValue(keyId, out trustedKey);
    }
}
