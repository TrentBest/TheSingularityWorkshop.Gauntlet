namespace TheSingularityWorkshop.Gauntlet;

/// <summary>Versioned state vocabulary for a dependency-machine stage.</summary>
public enum DependencyStageStatus
{
    NotYetRun = 0,
    Running = 1,
    Passed = 2,
    Failed = 3,
    Blocked = 4,
    Incomplete = 5,
    Invalid = 6,
    Untrusted = 7
}

/// <summary>Independent evidence validation state attached to a stage projection.</summary>
public enum DependencyEvidenceState
{
    None = 0,
    Pending = 1,
    Accepted = 2,
    Rejected = 3
}

/// <summary>Versioned aggregate state of a dependency-machine snapshot.</summary>
public enum DependencyGraphStatus
{
    NotYetRun = 0,
    Running = 1,
    Passed = 2,
    Failed = 3,
    Blocked = 4,
    Incomplete = 5,
    Invalid = 6,
    Untrusted = 7
}

/// <summary>Exact immutable candidate identity for one dependency stage.</summary>
public sealed record DependencyCandidateIdentity(
    string Repository,
    string PackageId,
    string SourceCommitSha,
    string ArtifactDigest,
    string? ManifestDigest);

/// <summary>Evidence identity required to bind a terminal stage result to one exact campaign attempt.</summary>
public sealed record DependencyStageEvidence(
    string ProfileId,
    int ProfileVersion,
    string ProfileDigest,
    string RunId,
    string ReportDigest,
    string ProviderId,
    DependencyEvidenceState ValidationState,
    string? EvidenceBundleDigest = null,
    string? EvidenceReference = null,
    string? PipelineAttemptId = null,
    string? StageAttemptId = null);

/// <summary>One dependency node and its read-only status projection.</summary>
public sealed record DependencyStage
{
    private IReadOnlyList<string> _upstreamStageIds = Array.Empty<string>();

    public DependencyStage(
        string stageId,
        DependencyCandidateIdentity candidate,
        IReadOnlyList<string> upstreamStageIds,
        bool required,
        string attemptId,
        DependencyStageStatus status,
        DependencyStageEvidence? evidence = null)
    {
        StageId = stageId;
        Candidate = candidate;
        UpstreamStageIds = upstreamStageIds;
        Required = required;
        AttemptId = attemptId;
        Status = status;
        Evidence = evidence;
    }

    public string StageId { get; init; }
    public DependencyCandidateIdentity Candidate { get; init; }
    public IReadOnlyList<string> UpstreamStageIds
    {
        get => _upstreamStageIds;
        init => _upstreamStageIds = Array.AsReadOnly(value?.ToArray() ?? []);
    }
    public bool Required { get; init; }
    public string AttemptId { get; init; }
    public DependencyStageStatus Status { get; init; }
    public DependencyStageEvidence? Evidence { get; init; }
}

/// <summary>One immutable, versioned dependency graph and pipeline-attempt snapshot.</summary>
public sealed record DependencyGraphSnapshot
{
    private IReadOnlyList<DependencyStage> _stages = Array.Empty<DependencyStage>();

    public const int CurrentSchemaVersion = 2;

    public DependencyGraphSnapshot(
        int schemaVersion,
        string snapshotId,
        string snapshotDigest,
        string pipelineAttemptId,
        IReadOnlyList<DependencyStage> stages)
    {
        SchemaVersion = schemaVersion;
        SnapshotId = snapshotId;
        SnapshotDigest = snapshotDigest;
        PipelineAttemptId = pipelineAttemptId;
        Stages = stages;
    }

    public int SchemaVersion { get; init; }
    public string SnapshotId { get; init; }
    public string SnapshotDigest { get; init; }
    public string PipelineAttemptId { get; init; }
    public IReadOnlyList<DependencyStage> Stages
    {
        get => _stages;
        init => _stages = Array.AsReadOnly(value?.ToArray() ?? []);
    }
}

/// <summary>Validation outcome for a dependency graph snapshot.</summary>
public sealed record DependencyGraphValidation(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> TopologicalStageIds);

/// <summary>Deterministic aggregate projection for a validated dependency graph.</summary>
public sealed record DependencyGraphEvaluation(
    DependencyGraphStatus Status,
    int PassedRequiredStages,
    int TotalRequiredStages,
    IReadOnlyList<string> Reasons);
