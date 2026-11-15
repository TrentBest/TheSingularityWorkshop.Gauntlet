using Xunit;
using TheSingularityWorkshop.Gauntlet;

namespace TheSingularityWorkshop.Gauntlet.Tests;

public sealed class HistoryAggregatorTests
{
    private static CampaignProfile Profile() =>
        new(
            "stable-profile",
            2,
            $"sha256:{new string('c', 64)}",
            [new ProbeDefinition("required-contract", 1, true, 1m)],
            new HashSet<FindingSeverity> { FindingSeverity.Critical, FindingSeverity.High });

    private static CampaignHistoryEntry Entry(
        string runId,
        GateStatus status,
        decimal? score,
        int attempt = 1,
        DateTimeOffset? completedAt = null,
        string? profileDigest = null) =>
        new(
            runId,
            attempt,
            "stable-profile",
            2,
            profileDigest ?? $"sha256:{new string('c', 64)}",
            completedAt ?? DateTimeOffset.Parse("2026-11-14T12:00:00Z"),
            status,
            score);

    [Fact]
    public void Average_and_delta_use_only_completed_comparable_attempts()
    {
        var profile = Profile();
        var history = new[]
        {
            Entry("run-1", GateStatus.Passed, 80m, completedAt: DateTimeOffset.Parse("2026-11-14T12:00:00Z")),
            Entry("run-2", GateStatus.Failed, 60m, completedAt: DateTimeOffset.Parse("2026-11-14T12:01:00Z")),
            Entry("run-3", GateStatus.Passed, 90m, completedAt: DateTimeOffset.Parse("2026-11-14T12:02:00Z"))
        };

        var result = HistoryAggregator.Summarize(profile, history);

        Assert.Equal(3, result.Completed);
        Assert.Equal(2, result.Survived);
        Assert.Equal(1, result.Failed);
        Assert.Equal(76.67m, result.AverageScore);
        Assert.Equal(30m, result.Delta);
        Assert.Equal("run-3", result.LatestRunId);
        Assert.Equal("run-2", result.PreviousRunId);
    }

    [Fact]
    public void Incompatible_profiles_are_excluded_from_average_and_counters()
    {
        var profile = Profile();
        var incompatible = Entry("other-profile", GateStatus.Passed, 100m, profileDigest: $"sha256:{new string('d', 64)}");

        var result = HistoryAggregator.Summarize(profile,
        [
            Entry("current-profile", GateStatus.Passed, 80m),
            incompatible
        ]);

        Assert.Equal(1, result.Completed);
        Assert.Equal(1, result.ExcludedIncompatibleAttempts);
        Assert.Equal(80m, result.AverageScore);
        Assert.Null(result.Delta);
    }

    [Fact]
    public void Incomplete_attempts_are_counted_but_never_enter_score()
    {
        var profile = Profile();
        var result = HistoryAggregator.Summarize(profile,
        [
            Entry("completed", GateStatus.Passed, 95m),
            Entry("incomplete", GateStatus.Incomplete, null, completedAt: null)
        ]);

        Assert.Equal(2, result.ComparableAttempts);
        Assert.Equal(1, result.Completed);
        Assert.Equal(1, result.Incomplete);
        Assert.Equal(95m, result.AverageScore);
    }

    [Fact]
    public void Single_completed_attempt_has_no_delta()
    {
        var result = HistoryAggregator.Summarize(Profile(),
        [
            Entry("only-run", GateStatus.Failed, 42m)
        ]);

        Assert.Equal(42m, result.AverageScore);
        Assert.Null(result.Delta);
        Assert.Null(result.PreviousRunId);
    }

    [Fact]
    public void Duplicate_attempt_identity_is_rejected()
    {
        var entry = Entry("same-run", GateStatus.Passed, 100m);

        Assert.Throws<ArgumentException>(() =>
            HistoryAggregator.Summarize(Profile(), [entry, entry]));
    }
}
