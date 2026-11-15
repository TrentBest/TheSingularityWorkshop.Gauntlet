using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheSingularityWorkshop.Gauntlet;

/// <summary>Canonicalizes and verifies the integrity digest of a campaign run report.</summary>
public static class RunReportIntegrity
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    /// <summary>Returns a copy of the report sealed with its canonical SHA-256 digest.</summary>
    /// <param name="report">The report to seal.</param>
    /// <returns>A report whose digest is computed over all report content except the digest value itself.</returns>
    public static RunReport Seal(RunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return report with { ReportDigest = ComputeDigest(report) };
    }

    /// <summary>Computes the canonical SHA-256 digest for a report.</summary>
    /// <param name="report">The report to canonicalize and hash.</param>
    /// <returns>A lowercase sha256-prefixed digest.</returns>
    public static string ComputeDigest(RunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var results = report.ProbeResults
            ?? throw new InvalidOperationException("A report must contain a probe result collection.");

        if (results.Any(result => result is null))
            throw new InvalidOperationException("A report cannot contain null probe results.");

        foreach (var result in results)
        {
            if (result.Findings is null)
                throw new InvalidOperationException($"Probe '{result.ProbeId}' has no findings collection.");

            if (result.Findings.Any(finding => finding is null))
                throw new InvalidOperationException($"Probe '{result.ProbeId}' contains a null finding.");
        }

        var canonical = report with
        {
            ProbeResults = results
                .OrderBy(result => result.ProbeId, StringComparer.Ordinal)
                .ThenBy(result => JsonSerializer.Serialize(result, SerializerOptions), StringComparer.Ordinal)
                .Select(result => result with
                {
                    Findings = result.Findings
                        .OrderBy(finding => finding.Id, StringComparer.Ordinal)
                        .ThenBy(finding => JsonSerializer.Serialize(finding, SerializerOptions), StringComparer.Ordinal)
                        .ToArray()
                })
                .ToArray(),
            ReportDigest = null
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(canonical, SerializerOptions);
        var digest = SHA256.HashData(bytes);
        return $"sha256:{Convert.ToHexString(digest).ToLowerInvariant()}";
    }

    /// <summary>Checks that a report contains a valid digest matching its canonical content.</summary>
    /// <param name="report">The report to verify.</param>
    /// <returns>True only when a correctly formatted digest matches the report content.</returns>
    public static bool Verify(RunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.ReportDigest is null ||
            report.ReportDigest.Length != 71 ||
            !report.ReportDigest.StartsWith("sha256:", StringComparison.Ordinal))
            return false;

        try
        {
            var expected = ComputeDigest(report);
            var expectedBytes = Encoding.ASCII.GetBytes(expected);
            var actualBytes = Encoding.ASCII.GetBytes(report.ReportDigest);
            return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
