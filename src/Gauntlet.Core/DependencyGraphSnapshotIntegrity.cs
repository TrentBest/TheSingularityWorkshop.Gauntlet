using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheSingularityWorkshop.Gauntlet;

/// <summary>Computes and verifies the deterministic integrity digest of a dependency graph snapshot.</summary>
/// <remarks>
/// Stage and upstream arrays are sorted ordinally because graph meaning does not depend on their input order.
/// The digest detects mutation when compared with a separately trusted snapshot reference; it is not a signature
/// and does not authenticate the issuer.
/// </remarks>
public static class DependencyGraphSnapshotIntegrity
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    /// <summary>Returns a copy of the snapshot sealed with its canonical SHA-256 digest.</summary>
    public static DependencyGraphSnapshot Seal(DependencyGraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot with { SnapshotDigest = ComputeDigest(snapshot) };
    }

    /// <summary>Computes the deterministic SHA-256 digest over snapshot content, excluding the digest field.</summary>
    public static string ComputeDigest(DependencyGraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Stages is null || snapshot.Stages.Any(stage => stage is null))
            throw new InvalidOperationException("A dependency graph must contain a non-null stage collection.");

        var canonical = snapshot with
        {
            SnapshotDigest = null!,
            Stages = snapshot.Stages
                .OrderBy(stage => stage.StageId, StringComparer.Ordinal)
                .Select(stage => stage with
                {
                    UpstreamStageIds = Array.AsReadOnly(
                        (stage.UpstreamStageIds ?? Array.Empty<string>())
                        .OrderBy(id => id, StringComparer.Ordinal)
                        .ToArray())
                })
                .ToArray()
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(canonical, SerializerOptions);
        return "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    /// <summary>True only when the digest is correctly formatted and matches canonical snapshot content.</summary>
    public static bool Verify(DependencyGraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.SnapshotDigest is not { Length: 71 } digest ||
            !digest.StartsWith("sha256:", StringComparison.Ordinal) ||
            digest[7..].Any(character =>
                !(character is >= '0' and <= '9' || character is >= 'a' and <= 'f')))
            return false;

        try
        {
            var expected = Encoding.ASCII.GetBytes(ComputeDigest(snapshot));
            var actual = Encoding.ASCII.GetBytes(digest);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
