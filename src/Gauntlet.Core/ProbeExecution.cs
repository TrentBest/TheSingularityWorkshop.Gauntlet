using System.Diagnostics;

namespace TheSingularityWorkshop.Gauntlet;

/// <summary>
/// A Gauntlet-owned probe implementation. Probe code is trusted infrastructure;
/// this interface is not an isolation boundary for candidate-supplied code.
/// </summary>
public interface IGauntletProbe
{
    /// <summary>Stable probe identity matching one definition in the trusted profile.</summary>
    string Id { get; }

    /// <summary>Version matching the corresponding profile definition.</summary>
    int Version { get; }

    /// <summary>Executes one bounded probe and returns its assertion observations.</summary>
    ValueTask<ProbeExecutionObservation> ExecuteAsync(
        ProbeExecutionContext context,
        CancellationToken cancellationToken);
}

/// <summary>Execution facts and cooperative limits supplied to a probe.</summary>
public sealed record ProbeExecutionContext(
    string RunId,
    string CandidateSha,
    int MaximumAssertions);

/// <summary>Probe-owned observations before the runner stamps identity and elapsed time.</summary>
public sealed record ProbeExecutionObservation(
    ProbeOutcome Outcome,
    int PassedAssertions,
    int TotalAssertions,
    IReadOnlyList<Finding> Findings);

/// <summary>Cooperative limits for the trusted in-process probe runner.</summary>
public sealed record ProbeExecutionLimits
{
    /// <summary>Creates validated cooperative execution limits.</summary>
    public ProbeExecutionLimits(TimeSpan maximumDuration, int maximumAssertions)
    {
        if (maximumDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maximumDuration));
        if (maximumAssertions < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumAssertions));

        MaximumDuration = maximumDuration;
        MaximumAssertions = maximumAssertions;
    }

    /// <summary>Maximum allowed elapsed time, checked after cooperative probe completion.</summary>
    public TimeSpan MaximumDuration { get; }

    /// <summary>Maximum assertion count accepted from a probe.</summary>
    public int MaximumAssertions { get; }
}

/// <summary>
/// Executes only Gauntlet-owned trusted probes in-process. This runner is not a
/// sandbox: it cannot preempt non-cooperative code or enforce memory/filesystem/network limits.
/// </summary>
public sealed class ProbeRunner
{
    /// <summary>Executes the supplied probes sequentially against a trusted profile.</summary>
    public async Task<IReadOnlyList<ProbeResult>> RunAsync(
        CampaignProfile profile,
        CandidateIdentity candidate,
        string runId,
        IEnumerable<IGauntletProbe> probes,
        ProbeExecutionLimits limits,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(probes);
        ArgumentNullException.ThrowIfNull(limits);

        var definitions = profile.Probes.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var supplied = probes.ToArray();
        if (supplied.Any(x => x is null))
            throw new ArgumentException("Probe collection cannot contain null entries.", nameof(probes));

        var duplicate = supplied.GroupBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"Probe '{duplicate.Key}' was supplied more than once.", nameof(probes));

        var results = new List<ProbeResult>(supplied.Length);
        foreach (var probe in supplied.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!definitions.TryGetValue(probe.Id, out var definition))
                throw new InvalidOperationException($"Probe '{probe.Id}' is not declared by the trusted profile.");
            if (probe.Version != definition.Version)
                throw new InvalidOperationException($"Probe '{probe.Id}' version does not match the trusted profile.");

            var context = new ProbeExecutionContext(runId, candidate.CommitSha, limits.MaximumAssertions);
            var stopwatch = Stopwatch.StartNew();
            ProbeExecutionObservation observation;
            try
            {
                observation = await probe.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                stopwatch.Stop();
                results.Add(new ProbeResult(
                    probe.Id,
                    probe.Version,
                    ProbeOutcome.Error,
                    0,
                    0,
                    [new Finding(
                        $"{probe.Id}:execution-error",
                        probe.Id,
                        FindingSeverity.Unknown,
                        "Probe execution raised an exception; exception details are intentionally omitted from the public run report.")],
                    stopwatch.Elapsed));
                continue;
            }

            stopwatch.Stop();
            if (observation is null || observation.Findings is null)
                throw new InvalidOperationException($"Probe '{probe.Id}' returned an invalid observation.");

            if (observation.TotalAssertions < 0 ||
                observation.TotalAssertions > limits.MaximumAssertions ||
                observation.PassedAssertions < 0 ||
                observation.PassedAssertions > observation.TotalAssertions ||
                !Enum.IsDefined(observation.Outcome))
                throw new InvalidOperationException($"Probe '{probe.Id}' returned invalid assertion counts or outcome.");

            var outcome = stopwatch.Elapsed > limits.MaximumDuration
                ? ProbeOutcome.TimedOut
                : observation.Outcome;

            results.Add(new ProbeResult(
                probe.Id,
                probe.Version,
                outcome,
                observation.PassedAssertions,
                observation.TotalAssertions,
                observation.Findings,
                stopwatch.Elapsed));
        }

        return results;
    }
}
