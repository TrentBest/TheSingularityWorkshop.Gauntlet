using TheSingularityWorkshop.Gauntlet;
using Xunit;

namespace TheSingularityWorkshop.Gauntlet.Tests;

public sealed class ProbeRunnerTests
{
    private static readonly CandidateIdentity Candidate = new(
        "TrentBest/GauntletFixture",
        new string('a', 40),
        $"sha256:{new string('b', 64)}",
        null);

    private static CampaignProfile Profile(params ProbeDefinition[] probes) => new(
        "probe-runner-smoke",
        1,
        $"sha256:{new string('c', 64)}",
        probes,
        new HashSet<FindingSeverity> { FindingSeverity.High, FindingSeverity.Critical });

    [Fact]
    public async Task Runner_executes_in_stable_probe_order_and_stamps_elapsed_time()
    {
        var profile = Profile(
            new ProbeDefinition("zeta", 1, true, 1m),
            new ProbeDefinition("alpha", 1, true, 1m));
        var runner = new ProbeRunner();

        var results = await runner.RunAsync(
            profile,
            Candidate,
            "run-001",
            [new PassingProbe("zeta"), new PassingProbe("alpha")],
            new ProbeExecutionLimits(TimeSpan.FromSeconds(1), 10));

        Assert.Equal(["alpha", "zeta"], results.Select(x => x.ProbeId));
        Assert.All(results, result => Assert.Equal(ProbeOutcome.Passed, result.Outcome));
        Assert.All(results, result => Assert.True(result.Duration >= TimeSpan.Zero));
    }

    [Fact]
    public async Task Runner_rejects_probe_not_declared_by_profile()
    {
        var runner = new ProbeRunner();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.RunAsync(
                Profile(new ProbeDefinition("declared", 1, true, 1m)),
                Candidate,
                "run-002",
                [new PassingProbe("undeclared")],
                new ProbeExecutionLimits(TimeSpan.FromSeconds(1), 10)));

        Assert.Contains("not declared", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Runner_rejects_duplicate_probe_identity()
    {
        var runner = new ProbeRunner();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            runner.RunAsync(
                Profile(new ProbeDefinition("alpha", 1, true, 1m)),
                Candidate,
                "run-003",
                [new PassingProbe("alpha"), new PassingProbe("alpha")],
                new ProbeExecutionLimits(TimeSpan.FromSeconds(1), 10)));
    }

    [Fact]
    public async Task Runner_marks_cooperatively_completed_over_time_probe_as_timed_out()
    {
        var runner = new ProbeRunner();
        var results = await runner.RunAsync(
            Profile(new ProbeDefinition("slow", 1, true, 1m)),
            Candidate,
            "run-004",
            [new DelayedProbe()],
            new ProbeExecutionLimits(TimeSpan.FromMilliseconds(1), 10));

        Assert.Single(results);
        Assert.Equal(ProbeOutcome.TimedOut, results[0].Outcome);
    }

    [Fact]
    public async Task Runner_propagates_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var runner = new ProbeRunner();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            runner.RunAsync(
                Profile(new ProbeDefinition("alpha", 1, true, 1m)),
                Candidate,
                "run-005",
                [new PassingProbe("alpha")],
                new ProbeExecutionLimits(TimeSpan.FromSeconds(1), 10),
                cancellation.Token));
    }

    private sealed class PassingProbe(string id) : IGauntletProbe
    {
        public string Id => id;
        public int Version => 1;

        public ValueTask<ProbeExecutionObservation> ExecuteAsync(
            ProbeExecutionContext context,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new ProbeExecutionObservation(ProbeOutcome.Passed, 2, 2, []));
    }

    private sealed class DelayedProbe : IGauntletProbe
    {
        public string Id => "slow";
        public int Version => 1;

        public async ValueTask<ProbeExecutionObservation> ExecuteAsync(
            ProbeExecutionContext context,
            CancellationToken cancellationToken)
        {
            await Task.Delay(15, cancellationToken);
            return new ProbeExecutionObservation(ProbeOutcome.Passed, 1, 1, []);
        }
    }
}
