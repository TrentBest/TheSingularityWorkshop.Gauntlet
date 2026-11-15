namespace TheSingularityWorkshop.Gauntlet;

/// <summary>One immutable historical attempt with its evaluated result.</summary>
public sealed record CampaignHistoryEntry
{
    /// <summary>Creates one historical campaign attempt.</summary>
    public CampaignHistoryEntry(
        string runId,
        int attempt,
        string profileId,
        int profileVersion,
        string profileDigest,
        DateTimeOffset? completedAt,
        GateStatus status,
        decimal? score)
    {
        RunId = runId;
        Attempt = attempt;
        ProfileId = profileId;
        ProfileVersion = profileVersion;
        ProfileDigest = profileDigest;
        CompletedAt = completedAt;
        Status = status;
        Score = score;
    }

    /// <summary>Unique campaign identity.</summary>
    public string RunId { get; init; }

    /// <summary>One-based attempt number.</summary>
    public int Attempt { get; init; }

    /// <summary>Campaign profile identity.</summary>
    public string ProfileId { get; init; }

    /// <summary>Campaign profile version.</summary>
    public int ProfileVersion { get; init; }

    /// <summary>Digest of the immutable campaign profile.</summary>
    public string ProfileDigest { get; init; }

    /// <summary>Completion timestamp, or null if no valid completion exists.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Evaluated campaign status.</summary>
    public GateStatus Status { get; init; }

    /// <summary>Final score, or null when the attempt is incomplete.</summary>
    public decimal? Score { get; init; }
}

/// <summary>Comparable campaign metrics for one exact profile version.</summary>
public sealed record HistorySummary
{
    /// <summary>Creates a history summary.</summary>
    public HistorySummary(
        int comparableAttempts,
        int excludedIncompatibleAttempts,
        int completed,
        int survived,
        int failed,
        int incomplete,
        int scoreSampleCount,
        decimal? averageScore,
        decimal? delta,
        string? latestRunId,
        string? previousRunId)
    {
        ComparableAttempts = comparableAttempts;
        ExcludedIncompatibleAttempts = excludedIncompatibleAttempts;
        Completed = completed;
        Survived = survived;
        Failed = failed;
        Incomplete = incomplete;
        ScoreSampleCount = scoreSampleCount;
        AverageScore = averageScore;
        Delta = delta;
        LatestRunId = latestRunId;
        PreviousRunId = previousRunId;
    }

    /// <summary>Number of attempts belonging to the requested profile cohort.</summary>
    public int ComparableAttempts { get; init; }

    /// <summary>Attempts excluded because their profile identity or version differs.</summary>
    public int ExcludedIncompatibleAttempts { get; init; }

    /// <summary>Completed attempts with a valid final score.</summary>
    public int Completed { get; init; }

    /// <summary>Completed attempts that passed the campaign gate.</summary>
    public int Survived { get; init; }

    /// <summary>Completed attempts that failed the campaign gate.</summary>
    public int Failed { get; init; }

    /// <summary>Matching attempts without a valid completed score.</summary>
    public int Incomplete { get; init; }

    /// <summary>Number of scores included in the average.</summary>
    public int ScoreSampleCount { get; init; }

    /// <summary>Average score across completed comparable attempts.</summary>
    public decimal? AverageScore { get; init; }

    /// <summary>Latest score minus the immediately preceding comparable score.</summary>
    public decimal? Delta { get; init; }

    /// <summary>Run identity for the latest comparable completed score.</summary>
    public string? LatestRunId { get; init; }

    /// <summary>Run identity for the preceding comparable completed score.</summary>
    public string? PreviousRunId { get; init; }
}

/// <summary>Builds deterministic history metrics without mixing incompatible campaign profiles.</summary>
public static class HistoryAggregator
{
    /// <summary>Summarizes all supplied attempts for the exact immutable profile identity.</summary>
    /// <param name="profile">The trusted campaign profile defining the comparison cohort.</param>
    /// <param name="history">Historical attempts; retries remain distinct attempts.</param>
    /// <returns>Comparable averages, latest-run delta, and pass/fail counters.</returns>
    public static HistorySummary Summarize(
        CampaignProfile profile,
        IEnumerable<CampaignHistoryEntry> history)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(history);

        var entries = history.ToArray();
        if (entries.Any(entry => entry is null))
            throw new ArgumentException("History cannot contain null entries.", nameof(history));

        if (entries.Any(entry => string.IsNullOrWhiteSpace(entry.RunId) || entry.Attempt < 1))
            throw new ArgumentException("Every history entry requires a run ID and positive attempt number.", nameof(history));

        var duplicate = entries
            .GroupBy(entry => (entry.RunId, entry.Attempt))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"Attempt '{duplicate.Key.RunId}/{duplicate.Key.Attempt}' appears more than once.", nameof(history));

        var comparable = entries
            .Where(entry =>
                StringComparer.Ordinal.Equals(entry.ProfileId, profile.Id) &&
                entry.ProfileVersion == profile.Version &&
                StringComparer.Ordinal.Equals(entry.ProfileDigest, profile.Digest))
            .ToArray();

        var completed = comparable
            .Where(entry =>
                entry.Status is GateStatus.Passed or GateStatus.Failed &&
                entry.CompletedAt is not null &&
                entry.Score is >= 0m and <= 100m)
            .OrderBy(entry => entry.CompletedAt)
            .ThenBy(entry => entry.RunId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Attempt)
            .ToArray();

        var survived = completed.Count(entry => entry.Status == GateStatus.Passed);
        var failed = completed.Count(entry => entry.Status == GateStatus.Failed);
        var latest = completed.LastOrDefault();
        var previous = completed.Length > 1 ? completed[^2] : null;
        decimal? average = completed.Length == 0
            ? null
            : decimal.Round(completed.Average(entry => entry.Score!.Value), 2, MidpointRounding.AwayFromZero);
        decimal? delta = latest is null || previous is null
            ? null
            : decimal.Round(latest.Score!.Value - previous.Score!.Value, 2, MidpointRounding.AwayFromZero);

        return new HistorySummary(
            comparable.Length,
            entries.Length - comparable.Length,
            completed.Length,
            survived,
            failed,
            comparable.Length - completed.Length,
            completed.Length,
            average,
            delta,
            latest?.RunId,
            previous?.RunId);
    }
}
