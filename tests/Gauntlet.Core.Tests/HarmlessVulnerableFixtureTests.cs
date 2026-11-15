using TheSingularityWorkshop.Gauntlet;
using Xunit;

namespace TheSingularityWorkshop.Gauntlet.Tests;

public sealed class HarmlessVulnerableFixtureTests
{
    [Fact]
    public async Task Gauntlet_probe_defeats_the_intentionally_vulnerable_fixture()
    {
        var profile = new CampaignProfile(
            "harmless-fixture",
            1,
            $"sha256:{new string('c', 64)}",
            [new ProbeDefinition("reservation-quantity-positive", 1, true, 1m)],
            new HashSet<FindingSeverity> { FindingSeverity.High, FindingSeverity.Critical });
        var candidate = new CandidateIdentity(
            "TrentBest/GauntletFixture",
            new string('a', 40),
            $"sha256:{new string('b', 64)}",
            null);

        var runner = new ProbeRunner();
        var probeResults = await runner.RunAsync(
            profile,
            candidate,
            "fixture-run-001",
            [new HarmlessVulnerableFixtureProbe()],
            new ProbeExecutionLimits(TimeSpan.FromSeconds(1), 10));

        var report = RunReportIntegrity.Seal(new RunReport(
            1,
            candidate,
            "0.1.0-dev",
            profile.Id,
            profile.Version,
            profile.Digest,
            "dotnet-8-test-host",
            "fixture-run-001",
            1,
            DateTimeOffset.Parse("2026-11-14T12:00:00Z"),
            DateTimeOffset.Parse("2026-11-14T12:00:01Z"),
            CampaignState.Completed,
            probeResults,
            null));

        var evaluation = CampaignEvaluator.Evaluate(profile, candidate, report);

        Assert.Single(probeResults);
        Assert.Equal(ProbeOutcome.Failed, probeResults[0].Outcome);
        Assert.Equal(2, probeResults[0].PassedAssertions);
        Assert.Equal(3, probeResults[0].TotalAssertions);
        Assert.Contains(probeResults[0].Findings, finding =>
            finding.Severity == FindingSeverity.High &&
            finding.ReproductionReference == "fixture:reservation-quantity-negative");
        Assert.Equal(GateStatus.Failed, evaluation.Status);
        Assert.Equal(66.67m, evaluation.Score);
    }
}
