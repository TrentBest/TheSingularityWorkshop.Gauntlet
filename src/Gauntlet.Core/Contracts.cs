namespace TheSingularityWorkshop.Gauntlet;

public enum CampaignState
{
    Running,
    Completed,
    Cancelled,
    TimedOut
}

public enum ProbeOutcome
{
    Passed,
    Failed,
    Skipped,
    Error,
    TimedOut
}

public enum FindingSeverity
{
    Informational,
    Low,
    Medium,
    High,
    Critical,
    Unknown
}

public enum GateStatus
{
    Passed,
    Failed,
    Incomplete,
    Invalid,
    Untrusted
}

public sealed record CandidateIdentity(
    string Repository,
    string CommitSha,
    string ArtifactDigest,
    string? ManifestDigest);

public sealed record ProbeDefinition(
    string Id,
    int Version,
    bool Required,
    decimal Weight);

public sealed record CampaignProfile(
    string Id,
    int Version,
    string Digest,
    IReadOnlyList<ProbeDefinition> Probes,
    IReadOnlySet<FindingSeverity> BlockingSeverities);

public sealed record Finding(
    string Id,
    string ProbeId,
    FindingSeverity Severity,
    string Summary,
    string? ReproductionReference = null);

public sealed record ProbeResult(
    string ProbeId,
    int ProbeVersion,
    ProbeOutcome Outcome,
    int PassedAssertions,
    int TotalAssertions,
    IReadOnlyList<Finding> Findings,
    TimeSpan Duration);

public sealed record RunReport(
    int SchemaVersion,
    CandidateIdentity Candidate,
    string GauntletVersion,
    string ProfileId,
    int ProfileVersion,
    string ProfileDigest,
    string EnvironmentId,
    string RunId,
    int Attempt,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    CampaignState CampaignState,
    IReadOnlyList<ProbeResult> ProbeResults,
    string? ReportDigest);

public sealed record EvaluationResult(
    GateStatus Status,
    decimal? Score,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<Finding> Findings);
