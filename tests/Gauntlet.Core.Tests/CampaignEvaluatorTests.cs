using Xunit;
using TheSingularityWorkshop.Gauntlet;

namespace TheSingularityWorkshop.Gauntlet.Tests;

public sealed class CampaignEvaluatorTests
{
    private static readonly CandidateIdentity Candidate = new(
        "TrentBest/Fixture",
        new string('a', 40),
        $"sha256:{new string('b', 64)}",
        null);

    private static CampaignProfile Profile(params ProbeDefinition[] probes) =>
        new(
            "fixture-profile",
            1,
            $"sha256:{new string('c', 64)}",
            probes,
            new HashSet<FindingSeverity>
            {
                FindingSeverity.Critical,
                FindingSeverity.High
            });

    private static RunReport Report(
        CampaignProfile profile,
        IReadOnlyList<ProbeResult> results,
        CampaignState state = CampaignState.Completed) =>
        new(
            1,
            Candidate,
            "0.1.0-dev",
            profile.Id,
            profile.Version,
            profile.Digest,
            "ubuntu-24.04-dotnet-8",
            "fixture-run-001",
            1,
            DateTimeOffset.Parse("2026-11-14T12:00:00Z"),
            state == CampaignState.Completed
                ? DateTimeOffset.Parse("2026-11-14T12:00:02Z")
                : null,
            state,
            results,
            null);

    [Fact]
    public void All_required_probes_passing_produces_a_100_score()
    {
        var profile = Profile(
            new ProbeDefinition("input-boundaries", 1, true, 1m),
            new ProbeDefinition("state-invariants", 1, true, 3m));
        var report = Report(profile,
        [
            new ProbeResult("input-boundaries", 1, ProbeOutcome.Passed, 4, 4, [], TimeSpan.FromMilliseconds(10)),
            new ProbeResult("state-invariants", 1, ProbeOutcome.Passed, 8, 8, [], TimeSpan.FromMilliseconds(20))
        ]);

        var result = CampaignEvaluator.Evaluate(profile, Candidate, report);

        Assert.Equal(GateStatus.Passed, result.Status);
        Assert.Equal(100m, result.Score);
    }

    [Fact]
    public void Weighted_assertion_ratio_is_calculated_and_required_failure_fails_gate()
    {
        var profile = Profile(
            new ProbeDefinition("cheap-check", 1, true, 1m),
            new ProbeDefinition("important-check", 1, true, 3m));
        var report = Report(profile,
        [
            new ProbeResult("cheap-check", 1, ProbeOutcome.Passed, 2, 2, [], TimeSpan.Zero),
            new ProbeResult("important-check", 1, ProbeOutcome.Failed, 1, 2, [], TimeSpan.Zero)
        ]);

        var result = CampaignEvaluator.Evaluate(profile, Candidate, report);

        Assert.Equal(GateStatus.Failed, result.Status);
        Assert.Equal(62.5m, result.Score);
    }

    [Fact]
    public void Missing_required_probe_is_incomplete_and_has_no_final_score()
    {
        var profile = Profile(
            new ProbeDefinition("input-boundaries", 1, true, 1m),
            new ProbeDefinition("state-invariants", 1, true, 1m));
        var report = Report(profile,
        [
            new ProbeResult("input-boundaries", 1, ProbeOutcome.Passed, 4, 4, [], TimeSpan.Zero)
        ]);

        var result = CampaignEvaluator.Evaluate(profile, Candidate, report);

        Assert.Equal(GateStatus.Incomplete, result.Status);
        Assert.Null(result.Score);
    }

    [Fact]
    public void Candidate_sha_or_artifact_mismatch_is_invalid()
    {
        var profile = Profile(new ProbeDefinition("input-boundaries", 1, true, 1m));
        var report = Report(profile,
        [
            new ProbeResult("input-boundaries", 1, ProbeOutcome.Passed, 4, 4, [], TimeSpan.Zero)
        ]);
        var wrongCandidate = Candidate with { CommitSha = new string('d', 40) };

        var result = CampaignEvaluator.Evaluate(profile, wrongCandidate, report);

        Assert.Equal(GateStatus.Invalid, result.Status);
        Assert.Null(result.Score);
    }

    [Fact]
    public void Critical_finding_on_optional_probe_blocks_a_perfect_score()
    {
        var profile = Profile(
            new ProbeDefinition("required-contract", 1, true, 1m),
            new ProbeDefinition("optional-abuse-case", 1, false, 1m));
        var critical = new Finding("finding-001", "optional-abuse-case", FindingSeverity.Critical, "Unexpected privileged operation.");
        var report = Report(profile,
        [
            new ProbeResult("required-contract", 1, ProbeOutcome.Passed, 3, 3, [], TimeSpan.Zero),
            new ProbeResult("optional-abuse-case", 1, ProbeOutcome.Passed, 2, 2, [critical], TimeSpan.Zero)
        ]);

        var result = CampaignEvaluator.Evaluate(profile, Candidate, report);

        Assert.Equal(GateStatus.Failed, result.Status);
        Assert.Equal(100m, result.Score);
        Assert.Contains(result.Reasons, reason => reason.Contains("Critical", StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_severity_fails_closed()
    {
        var profile = Profile(new ProbeDefinition("required-contract", 1, true, 1m));
        var finding = new Finding("finding-unknown", "required-contract", FindingSeverity.Unknown, "Severity was not classified.");
        var report = Report(profile,
        [
            new ProbeResult("required-contract", 1, ProbeOutcome.Passed, 1, 1, [finding], TimeSpan.Zero)
        ]);

        var result = CampaignEvaluator.Evaluate(profile, Candidate, report);

        Assert.Equal(GateStatus.Failed, result.Status);
    }

    [Fact]
    public void Duplicate_probe_results_are_invalid()
    {
        var profile = Profile(new ProbeDefinition("required-contract", 1, true, 1m));
        var report = Report(profile,
        [
            new ProbeResult("required-contract", 1, ProbeOutcome.Passed, 1, 1, [], TimeSpan.Zero),
            new ProbeResult("required-contract", 1, ProbeOutcome.Passed, 1, 1, [], TimeSpan.Zero)
        ]);

        var result = CampaignEvaluator.Evaluate(profile, Candidate, report);

        Assert.Equal(GateStatus.Invalid, result.Status);
        Assert.Null(result.Score);
    }

    [Fact]
    public void Running_campaign_is_incomplete_not_a_pass()
    {
        var profile = Profile(new ProbeDefinition("required-contract", 1, true, 1m));
        var report = Report(profile, [], CampaignState.Running);

        var result = CampaignEvaluator.Evaluate(profile, Candidate, report);

        Assert.Equal(GateStatus.Incomplete, result.Status);
        Assert.Null(result.Score);
    }

    [Fact]
    public void Probe_version_mismatch_is_invalid()
    {
        var profile = Profile(new ProbeDefinition("required-contract", 2, true, 1m));
        var report = Report(profile,
        [
            new ProbeResult("required-contract", 1, ProbeOutcome.Passed, 1, 1, [], TimeSpan.Zero)
        ]);

        var result = CampaignEvaluator.Evaluate(profile, Candidate, report);

        Assert.Equal(GateStatus.Invalid, result.Status);
    }
}
