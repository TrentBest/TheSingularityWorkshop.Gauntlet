using Xunit;
using TheSingularityWorkshop.Gauntlet;

namespace TheSingularityWorkshop.Gauntlet.Tests;

public sealed class RunReportIntegrityTests
{
    private static CandidateIdentity Candidate => new(
        "TrentBest/Fixture",
        new string('a', 40),
        $"sha256:{new string('b', 64)}",
        null);

    private static CampaignProfile Profile => new(
        "integrity-profile",
        1,
        $"sha256:{new string('c', 64)}",
        [new ProbeDefinition("probe-a", 1, true, 1m), new ProbeDefinition("probe-b", 1, true, 1m)],
        new HashSet<FindingSeverity> { FindingSeverity.Critical, FindingSeverity.High });

    private static RunReport Report => new(
        1,
        Candidate,
        "0.1.0-dev",
        Profile.Id,
        Profile.Version,
        Profile.Digest,
        "runner-fixture",
        "integrity-run",
        1,
        DateTimeOffset.Parse("2026-11-14T12:00:00Z"),
        DateTimeOffset.Parse("2026-11-14T12:00:01Z"),
        CampaignState.Completed,
        [
            new ProbeResult("probe-b", 1, ProbeOutcome.Passed, 2, 2,
                [new Finding("finding-b", "probe-b", FindingSeverity.Low, "A low-severity observation.")], TimeSpan.FromMilliseconds(20)),
            new ProbeResult("probe-a", 1, ProbeOutcome.Passed, 1, 1,
                [new Finding("finding-a", "probe-a", FindingSeverity.Informational, "A useful observation.")], TimeSpan.FromMilliseconds(10))
        ],
        null);

    [Fact]
    public void Sealed_report_verifies()
    {
        var sealedReport = RunReportIntegrity.Seal(Report);

        Assert.StartsWith("sha256:", sealedReport.ReportDigest);
        Assert.True(RunReportIntegrity.Verify(sealedReport));
    }

    [Fact]
    public void Any_content_mutation_invalidates_the_digest()
    {
        var sealedReport = RunReportIntegrity.Seal(Report);
        var changedProbe = sealedReport.ProbeResults[0] with { PassedAssertions = 0 };
        var tampered = sealedReport with
        {
            ProbeResults = [changedProbe, sealedReport.ProbeResults[1]]
        };

        Assert.False(RunReportIntegrity.Verify(tampered));
    }

    [Fact]
    public void Probe_and_finding_order_do_not_change_canonical_digest()
    {
        var original = RunReportIntegrity.Seal(Report);
        var reordered = Report with
        {
            ProbeResults = Report.ProbeResults
                .Reverse()
                .Select(result => result with { Findings = result.Findings.Reverse().ToArray() })
                .ToArray()
        };
        var sealedReordered = RunReportIntegrity.Seal(reordered);

        Assert.Equal(original.ReportDigest, sealedReordered.ReportDigest);
    }

    [Fact]
    public void Missing_or_malformed_digest_never_verifies()
    {
        Assert.False(RunReportIntegrity.Verify(Report));
        Assert.False(RunReportIntegrity.Verify(Report with { ReportDigest = "sha256:bad" }));
    }

    [Fact]
    public void Duplicate_probe_identity_cannot_be_canonicalized()
    {
        var duplicate = Report with
        {
            ProbeResults = [Report.ProbeResults[0], Report.ProbeResults[0]]
        };

        Assert.Throws<InvalidOperationException>(() => RunReportIntegrity.ComputeDigest(duplicate));
    }
}
