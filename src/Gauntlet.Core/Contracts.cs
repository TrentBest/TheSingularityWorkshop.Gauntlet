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
/// <param name="Repository">Canonical owner/repository identity.</param>
/// <param name="CommitSha">Full immutable Git commit SHA.</param>
/// <param name="ArtifactDigest">SHA-256 digest of the exact candidate artifact.</param>
/// <param name="ManifestDigest">Optional SHA-256 digest of the MicroBundle or Experience manifest.</param>
public sealed record CandidateIdentity(
    string Repository,
    string CommitSha,
    string ArtifactDigest,
    string? ManifestDigest);

/// <summary>Defines one versioned probe included in a campaign profile.</summary>
/// <param name="Id">Stable probe identity.</param>
/// <param name="Version">Probe contract version.</param>
/// <param name="Required">Whether the probe is required for a complete score and pass.</param>
/// <param name="Weight">Positive relative weight used by the score calculation.</param>
public sealed record ProbeDefinition(
    string Id,
    int Version,
    bool Required,
    decimal Weight);

/// <summary>An immutable, versioned inventory and policy for a campaign.</summary>
/// <param name="Id">Stable campaign profile identity.</param>
/// <param name="Version">Profile contract version.</param>
/// <param name="Digest">SHA-256 digest of the canonical profile definition.</param>
/// <param name="Probes">Declared required and optional probes.</param>
/// <param name="BlockingSeverities">Profile-defined severities that block a pass; critical and high always block.</param>
public sealed record CampaignProfile(
    string Id,
    int Version,
    string Digest,
    IReadOnlyList<ProbeDefinition> Probes,
    IReadOnlySet<FindingSeverity> BlockingSeverities);

/// <summary>A reproducible observation emitted by a probe.</summary>
/// <param name="Id">Stable finding identity within the report.</param>
/// <param name="ProbeId">Identity of the probe that produced the finding.</param>
/// <param name="Severity">Impact classification; unknown severity fails closed.</param>
/// <param name="Summary">Concise description of the observed behavior.</param>
/// <param name="ReproductionReference">Optional reference to bounded reproduction evidence.</param>
public sealed record Finding(
    string Id,
    string ProbeId,
    FindingSeverity Severity,
    string Summary,
    string? ReproductionReference = null);

/// <summary>Records the outcome and assertions produced by one probe attempt.</summary>
/// <param name="ProbeId">Stable probe identity.</param>
/// <param name="ProbeVersion">Version of the executed probe contract.</param>
/// <param name="Outcome">Probe execution outcome.</param>
/// <param name="PassedAssertions">Number of assertions that passed.</param>
/// <param name="TotalAssertions">Total number of assertions evaluated.</param>
/// <param name="Findings">Findings observed by this probe.</param>
/// <param name="Duration">Elapsed probe execution time.</param>
public sealed record ProbeResult(
    string ProbeId,
    int ProbeVersion,
    ProbeOutcome Outcome,
    int PassedAssertions,
    int TotalAssertions,
    IReadOnlyList<Finding> Findings,
    TimeSpan Duration);

/// <summary>Immutable evidence record for one campaign attempt.</summary>
/// <param name="SchemaVersion">Version of the serialized report contract.</param>
/// <param name="Candidate">Exact candidate source and artifact identity.</param>
/// <param name="GauntletVersion">Version of the Gauntlet implementation.</param>
/// <param name="ProfileId">Campaign profile identity.</param>
/// <param name="ProfileVersion">Campaign profile version.</param>
/// <param name="ProfileDigest">Digest of the exact profile definition used.</param>
/// <param name="EnvironmentId">Runner environment identity.</param>
/// <param name="RunId">Unique campaign identity.</param>
/// <param name="Attempt">One-based attempt number; retries remain distinct.</param>
/// <param name="StartedAt">Campaign start timestamp.</param>
/// <param name="CompletedAt">Completion timestamp, or null while incomplete.</param>
/// <param name="CampaignState">Execution lifecycle state.</param>
/// <param name="ProbeResults">Observed probe outcomes and findings.</param>
/// <param name="ReportDigest">Digest of canonical report content, excluding this field.</param>
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

/// <summary>Contains the result of evaluating a run against its trusted profile and candidate.</summary>
/// <param name="Status">Deterministic gate status.</param>
/// <param name="Score">Weighted score from 0 to 100, or null when required evidence is incomplete.</param>
/// <param name="Reasons">Stable, human-readable explanations for the status.</param>
/// <param name="Findings">Findings retained from the report.</param>
public sealed record EvaluationResult(
    GateStatus Status,
    decimal? Score,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<Finding> Findings);
