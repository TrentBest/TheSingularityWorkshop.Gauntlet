namespace TheSingularityWorkshop.Gauntlet;

/// <summary>Describes the terminal or active state of a campaign execution.</summary>
public enum CampaignState
{
    /// <summary>The campaign is still executing.</summary>
    Running,
    /// <summary>All campaign execution has completed.</summary>
    Completed,
    /// <summary>The campaign was explicitly cancelled.</summary>
    Cancelled,
    /// <summary>A declared execution limit was reached.</summary>
    TimedOut
}

/// <summary>Describes the outcome reported by one probe.</summary>
public enum ProbeOutcome
{
    /// <summary>Every assertion evaluated by the probe passed.</summary>
    Passed,
    /// <summary>One or more assertions or required invariants failed.</summary>
    Failed,
    /// <summary>The probe was deliberately not executed.</summary>
    Skipped,
    /// <summary>The probe could not produce a conclusive result.</summary>
    Error,
    /// <summary>The probe exceeded its execution limit.</summary>
    TimedOut
}

/// <summary>Classifies the impact of an observed finding.</summary>
public enum FindingSeverity
{
    /// <summary>A useful observation that does not indicate a defect.</summary>
    Informational,
    /// <summary>A low-impact defect or unexpected behavior.</summary>
    Low,
    /// <summary>A moderate defect whose gate impact is profile-defined.</summary>
    Medium,
    /// <summary>A high-impact finding that always blocks a pass.</summary>
    High,
    /// <summary>A critical finding that always blocks a pass.</summary>
    Critical,
    /// <summary>The finding could not be classified; this always blocks a pass.</summary>
    Unknown
}

/// <summary>Represents the deterministic evaluation result for a campaign.</summary>
public enum GateStatus
{
    /// <summary>All required probes passed and no blocking finding exists.</summary>
    Passed,
    /// <summary>A required invariant failed or a blocking finding exists.</summary>
    Failed,
    /// <summary>Required evidence is missing or did not complete.</summary>
    Incomplete,
    /// <summary>The report, profile, identity, or probe data is malformed.</summary>
    Invalid,
    /// <summary>An external evidence consumer has not established provider trust.</summary>
    Untrusted
}

/// <summary>Identifies the exact repository revision and artifact under examination.</summary>
public sealed record CandidateIdentity
{
    /// <summary>Creates an immutable candidate identity.</summary>
    public CandidateIdentity(string repository, string commitSha, string artifactDigest, string? manifestDigest)
    {
        Repository = repository;
        CommitSha = commitSha;
        ArtifactDigest = artifactDigest;
        ManifestDigest = manifestDigest;
    }

    /// <summary>Canonical owner/repository identity.</summary>
    public string Repository { get; init; }

    /// <summary>Full immutable Git commit SHA.</summary>
    public string CommitSha { get; init; }

    /// <summary>SHA-256 digest of the exact candidate artifact.</summary>
    public string ArtifactDigest { get; init; }

    /// <summary>Optional SHA-256 digest of the MicroBundle or Experience manifest.</summary>
    public string? ManifestDigest { get; init; }
}

/// <summary>Defines one versioned probe included in a campaign profile.</summary>
public sealed record ProbeDefinition
{
    /// <summary>Creates a probe definition.</summary>
    public ProbeDefinition(string id, int version, bool required, decimal weight)
    {
        Id = id;
        Version = version;
        Required = required;
        Weight = weight;
    }

    /// <summary>Stable probe identity.</summary>
    public string Id { get; init; }

    /// <summary>Probe contract version.</summary>
    public int Version { get; init; }

    /// <summary>Whether the probe is required for a complete score and pass.</summary>
    public bool Required { get; init; }

    /// <summary>Positive relative weight used by the score calculation.</summary>
    public decimal Weight { get; init; }
}

/// <summary>An immutable, versioned inventory and policy for a campaign.</summary>
public sealed record CampaignProfile
{
    /// <summary>Creates a campaign profile.</summary>
    public CampaignProfile(
        string id,
        int version,
        string digest,
        IReadOnlyList<ProbeDefinition> probes,
        IReadOnlySet<FindingSeverity> blockingSeverities)
    {
        Id = id;
        Version = version;
        Digest = digest;
        Probes = probes;
        BlockingSeverities = blockingSeverities;
    }

    /// <summary>Stable campaign profile identity.</summary>
    public string Id { get; init; }

    /// <summary>Profile contract version.</summary>
    public int Version { get; init; }

    /// <summary>SHA-256 digest of the canonical profile definition.</summary>
    public string Digest { get; init; }

    /// <summary>Declared required and optional probes.</summary>
    public IReadOnlyList<ProbeDefinition> Probes { get; init; }

    /// <summary>Profile-defined severities that block a pass; critical and high always block.</summary>
    public IReadOnlySet<FindingSeverity> BlockingSeverities { get; init; }
}

/// <summary>A reproducible observation emitted by a probe.</summary>
public sealed record Finding
{
    /// <summary>Creates a finding.</summary>
    public Finding(string id, string probeId, FindingSeverity severity, string summary, string? reproductionReference = null)
    {
        Id = id;
        ProbeId = probeId;
        Severity = severity;
        Summary = summary;
        ReproductionReference = reproductionReference;
    }

    /// <summary>Stable finding identity within the report.</summary>
    public string Id { get; init; }

    /// <summary>Identity of the probe that produced the finding.</summary>
    public string ProbeId { get; init; }

    /// <summary>Impact classification; unknown severity fails closed.</summary>
    public FindingSeverity Severity { get; init; }

    /// <summary>Concise description of the observed behavior.</summary>
    public string Summary { get; init; }

    /// <summary>Optional reference to bounded reproduction evidence.</summary>
    public string? ReproductionReference { get; init; }
}

/// <summary>Records the outcome and assertions produced by one probe attempt.</summary>
public sealed record ProbeResult
{
    /// <summary>Creates a probe result.</summary>
    public ProbeResult(
        string probeId,
        int probeVersion,
        ProbeOutcome outcome,
        int passedAssertions,
        int totalAssertions,
        IReadOnlyList<Finding> findings,
        TimeSpan duration)
    {
        ProbeId = probeId;
        ProbeVersion = probeVersion;
        Outcome = outcome;
        PassedAssertions = passedAssertions;
        TotalAssertions = totalAssertions;
        Findings = findings;
        Duration = duration;
    }

    /// <summary>Stable probe identity.</summary>
    public string ProbeId { get; init; }

    /// <summary>Version of the executed probe contract.</summary>
    public int ProbeVersion { get; init; }

    /// <summary>Probe execution outcome.</summary>
    public ProbeOutcome Outcome { get; init; }

    /// <summary>Number of assertions that passed.</summary>
    public int PassedAssertions { get; init; }

    /// <summary>Total number of assertions evaluated.</summary>
    public int TotalAssertions { get; init; }

    /// <summary>Findings observed by this probe.</summary>
    public IReadOnlyList<Finding> Findings { get; init; }

    /// <summary>Elapsed probe execution time.</summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>Immutable evidence record for one campaign attempt.</summary>
public sealed record RunReport
{
    /// <summary>Creates a run report.</summary>
    public RunReport(
        int schemaVersion,
        CandidateIdentity candidate,
        string gauntletVersion,
        string profileId,
        int profileVersion,
        string profileDigest,
        string environmentId,
        string runId,
        int attempt,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt,
        CampaignState campaignState,
        IReadOnlyList<ProbeResult> probeResults,
        string? reportDigest)
    {
        SchemaVersion = schemaVersion;
        Candidate = candidate;
        GauntletVersion = gauntletVersion;
        ProfileId = profileId;
        ProfileVersion = profileVersion;
        ProfileDigest = profileDigest;
        EnvironmentId = environmentId;
        RunId = runId;
        Attempt = attempt;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        CampaignState = campaignState;
        ProbeResults = probeResults;
        ReportDigest = reportDigest;
    }

    /// <summary>Version of the serialized report contract.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Exact candidate source and artifact identity.</summary>
    public CandidateIdentity Candidate { get; init; }

    /// <summary>Version of the Gauntlet implementation.</summary>
    public string GauntletVersion { get; init; }

    /// <summary>Campaign profile identity.</summary>
    public string ProfileId { get; init; }

    /// <summary>Campaign profile version.</summary>
    public int ProfileVersion { get; init; }

    /// <summary>Digest of the exact profile definition used.</summary>
    public string ProfileDigest { get; init; }

    /// <summary>Runner environment identity.</summary>
    public string EnvironmentId { get; init; }

    /// <summary>Unique campaign identity.</summary>
    public string RunId { get; init; }

    /// <summary>One-based attempt number; retries remain distinct.</summary>
    public int Attempt { get; init; }

    /// <summary>Campaign start timestamp.</summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>Completion timestamp, or null while incomplete.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Execution lifecycle state.</summary>
    public CampaignState CampaignState { get; init; }

    /// <summary>Observed probe outcomes and findings.</summary>
    public IReadOnlyList<ProbeResult> ProbeResults { get; init; }

    /// <summary>Digest of canonical report content, excluding this field.</summary>
    public string? ReportDigest { get; init; }
}

/// <summary>Contains the result of evaluating a run against its trusted profile and candidate.</summary>
public sealed record EvaluationResult
{
    /// <summary>Creates a deterministic evaluation result.</summary>
    public EvaluationResult(GateStatus status, decimal? score, IReadOnlyList<string> reasons, IReadOnlyList<Finding> findings)
    {
        Status = status;
        Score = score;
        Reasons = reasons;
        Findings = findings;
    }

    /// <summary>Deterministic gate status.</summary>
    public GateStatus Status { get; init; }

    /// <summary>Weighted score from 0 to 100, or null when required evidence is incomplete.</summary>
    public decimal? Score { get; init; }

    /// <summary>Stable, human-readable explanations for the status.</summary>
    public IReadOnlyList<string> Reasons { get; init; }

    /// <summary>Findings retained from the report.</summary>
    public IReadOnlyList<Finding> Findings { get; init; }
}
