namespace TheSingularityWorkshop.Gauntlet;

/// <summary>
/// Read-only, presentation-safe projection of a dependency graph snapshot.
/// This projection is not an issuer-authentication result and cannot authorize execution.
/// </summary>
public sealed record DependencyGraphReadOnlyProjection
{
    private DependencyGraphReadOnlyProjection(
        int schemaVersion,
        string snapshotId,
        string snapshotDigest,
        string pipelineAttemptId,
        DependencyGraphStatus status,
        int passedRequiredStages,
        int totalRequiredStages,
        bool isStructurallyValid,
        IReadOnlyList<DependencyStageReadOnlyProjection> stages)
    {
        SchemaVersion = schemaVersion;
        SnapshotId = snapshotId;
        SnapshotDigest = snapshotDigest;
        PipelineAttemptId = pipelineAttemptId;
        Status = status;
        PassedRequiredStages = passedRequiredStages;
        TotalRequiredStages = totalRequiredStages;
        IsStructurallyValid = isStructurallyValid;
        Stages = Array.AsReadOnly(stages.ToArray());
    }

    public int SchemaVersion { get; }
    public string SnapshotId { get; }
    public string SnapshotDigest { get; }
    public string PipelineAttemptId { get; }
    public DependencyGraphStatus Status { get; }
    public int PassedRequiredStages { get; }
    public int TotalRequiredStages { get; }

    /// <summary>Indicates local graph/digest validation only; it does not establish issuer or provider trust.</summary>
    public bool IsStructurallyValid { get; }

    public IReadOnlyList<DependencyStageReadOnlyProjection> Stages { get; }

    /// <summary>
    /// Creates a deterministic read-only view. Invalid snapshots remain visibly Invalid;
    /// this method never upgrades evidence or authenticates the snapshot issuer.
    /// </summary>
    public static DependencyGraphReadOnlyProjection Create(DependencyGraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var validation = DependencyGraphEvaluator.Validate(snapshot);
        var evaluation = DependencyGraphEvaluator.Evaluate(snapshot);
        var byId = (snapshot.Stages ?? Array.Empty<DependencyStage>())
            .Where(stage => stage is not null)
            .ToDictionary(stage => stage.StageId, StringComparer.Ordinal);

        var orderedIds = validation.IsValid
            ? validation.TopologicalStageIds
            : byId.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray();

        var stages = orderedIds
            .Where(byId.ContainsKey)
            .Select(id => DependencyStageReadOnlyProjection.From(byId[id]))
            .ToArray();

        return new DependencyGraphReadOnlyProjection(
            snapshot.SchemaVersion,
            snapshot.SnapshotId,
            snapshot.SnapshotDigest,
            snapshot.PipelineAttemptId,
            evaluation.Status,
            evaluation.PassedRequiredStages,
            evaluation.TotalRequiredStages,
            validation.IsValid,
            stages);
    }
}

/// <summary>Presentation-only fields for one dependency stage; evidence payloads are deliberately excluded.</summary>
public sealed record DependencyStageReadOnlyProjection(
    string StageId,
    string? Repository,
    string? PackageId,
    string? SourceCommitSha,
    string? ArtifactDigest,
    bool Required,
    string AttemptId,
    DependencyStageStatus Status,
    IReadOnlyList<string> UpstreamStageIds)
{
    internal static DependencyStageReadOnlyProjection From(DependencyStage stage) =>
        new(
            stage.StageId,
            stage.Candidate?.Repository,
            stage.Candidate?.PackageId,
            stage.Candidate?.SourceCommitSha,
            stage.Candidate?.ArtifactDigest,
            stage.Required,
            stage.AttemptId,
            stage.Status,
            Array.AsReadOnly((stage.UpstreamStageIds ?? Array.Empty<string>()).ToArray()));
}
