namespace TheSingularityWorkshop.Gauntlet;

/// <summary>Pure validation and aggregation for versioned dependency graph projections.</summary>
/// <remarks>
/// This type validates projection structure and local evidence bindings. It does not authenticate
/// providers, verify report signatures, recompute snapshot digests, schedule work, or authorize gates.
/// Those trust and policy decisions remain with CiAi.
/// </remarks>
public static class DependencyGraphEvaluator
{
    public static DependencyGraphValidation Validate(DependencyGraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var errors = new List<string>();

        if (snapshot.SchemaVersion != DependencyGraphSnapshot.CurrentSchemaVersion)
            errors.Add($"Unsupported graph schema version: {snapshot.SchemaVersion}.");
        if (string.IsNullOrWhiteSpace(snapshot.SnapshotId))
            errors.Add("Snapshot ID is required.");
        if (!IsSha256(snapshot.SnapshotDigest))
            errors.Add("Snapshot digest must be a 64-character SHA-256 hex value.");
        if (string.IsNullOrWhiteSpace(snapshot.PipelineAttemptId))
            errors.Add("Pipeline attempt ID is required.");
        if (snapshot.Stages is null || snapshot.Stages.Count == 0)
            errors.Add("At least one dependency stage is required.");

        var stages = snapshot.Stages ?? [];
        var byId = new Dictionary<string, DependencyStage>(StringComparer.Ordinal);
        foreach (var stage in stages)
        {
            if (stage is null)
            {
                errors.Add("Stage collection contains a null entry.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(stage.StageId))
            {
                errors.Add("Every stage requires a stable stage ID.");
                continue;
            }

            if (!byId.TryAdd(stage.StageId, stage))
                errors.Add($"Duplicate stage ID: {stage.StageId}.");

            ValidateStage(stage, errors);
        }

        foreach (var stage in byId.Values)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var upstreamId in stage.UpstreamStageIds ?? [])
            {
                if (string.IsNullOrWhiteSpace(upstreamId))
                    errors.Add($"Stage '{stage.StageId}' has an empty upstream stage ID.");
                else if (!seen.Add(upstreamId))
                    errors.Add($"Stage '{stage.StageId}' repeats upstream stage '{upstreamId}'.");
                else if (string.Equals(stage.StageId, upstreamId, StringComparison.Ordinal))
                    errors.Add($"Stage '{stage.StageId}' cannot depend on itself.");
                else if (!byId.ContainsKey(upstreamId))
                    errors.Add($"Stage '{stage.StageId}' references missing upstream stage '{upstreamId}'.");
            }
        }

        var order = TopologicalOrder(byId, errors);
        return new DependencyGraphValidation(errors.Count == 0, errors.AsReadOnly(), order.AsReadOnly());
    }

    public static DependencyGraphEvaluation Evaluate(DependencyGraphSnapshot snapshot)
    {
        var validation = Validate(snapshot);
        if (!validation.IsValid)
            return new DependencyGraphEvaluation(
                DependencyGraphStatus.Invalid, 0,
                snapshot.Stages?.Count(x => x.Required) ?? 0,
                validation.Errors);

        var required = snapshot.Stages.Where(x => x.Required).ToArray();
        var passed = required.Count(x => x.Status == DependencyStageStatus.Passed);
        var statuses = required.Select(x => x.Status).ToHashSet();

        // Evidence-quality failures outrank operational outcomes; no bad evidence can be hidden by a failure/pass.
        var aggregate = statuses.Contains(DependencyStageStatus.Invalid) ? DependencyGraphStatus.Invalid
            : statuses.Contains(DependencyStageStatus.Untrusted) ? DependencyGraphStatus.Untrusted
            : statuses.Contains(DependencyStageStatus.Incomplete) ? DependencyGraphStatus.Incomplete
            : statuses.Contains(DependencyStageStatus.Failed) ? DependencyGraphStatus.Failed
            : statuses.Contains(DependencyStageStatus.Blocked) ? DependencyGraphStatus.Blocked
            : statuses.Contains(DependencyStageStatus.Running) ? DependencyGraphStatus.Running
            : statuses.Contains(DependencyStageStatus.NotYetRun) ? DependencyGraphStatus.NotYetRun
            : DependencyGraphStatus.Passed;

        var reasons = aggregate switch
        {
            DependencyGraphStatus.Passed => new[] { "Every required dependency stage has accepted evidence for this snapshot." },
            DependencyGraphStatus.Failed => new[] { "At least one required dependency stage has a validated blocking failure." },
            DependencyGraphStatus.Blocked => new[] { "At least one required dependency stage is blocked by an unmet prerequisite." },
            DependencyGraphStatus.Incomplete => new[] { "Required dependency evidence is incomplete." },
            DependencyGraphStatus.Invalid => new[] { "Dependency evidence or graph structure is invalid." },
            DependencyGraphStatus.Untrusted => new[] { "At least one required dependency stage has untrusted evidence." },
            DependencyGraphStatus.Running => new[] { "At least one required dependency stage is running." },
            _ => new[] { "The pipeline has not started all required dependency stages." }
        };

        return new DependencyGraphEvaluation(aggregate, passed, required.Length, Array.AsReadOnly(reasons));
    }

    /// <summary>Returns whether a stage may start after all declared prerequisites have passed.</summary>
    public static bool IsEligible(DependencyGraphSnapshot snapshot, string stageId)
    {
        var validation = Validate(snapshot);
        if (!validation.IsValid)
            return false;

        var stage = snapshot.Stages.SingleOrDefault(x => string.Equals(x.StageId, stageId, StringComparison.Ordinal));
        if (stage is null || stage.Status != DependencyStageStatus.NotYetRun)
            return false;

        var byId = snapshot.Stages.ToDictionary(x => x.StageId, StringComparer.Ordinal);
        return stage.UpstreamStageIds.All(id => byId[id].Status == DependencyStageStatus.Passed);
    }

    private static void ValidateStage(DependencyStage stage, List<string> errors)
    {
        if (stage.Candidate is null)
        {
            errors.Add($"Stage '{stage.StageId}' has no candidate identity.");
            return;
        }

        var candidate = stage.Candidate;
        if (!IsRepository(candidate.Repository))
            errors.Add($"Stage '{stage.StageId}' has an invalid owner/repository identity.");
        if (string.IsNullOrWhiteSpace(candidate.PackageId))
            errors.Add($"Stage '{stage.StageId}' has no package identity.");
        if (!IsCommitSha(candidate.SourceCommitSha))
            errors.Add($"Stage '{stage.StageId}' has an invalid source commit SHA.");
        if (!IsSha256(candidate.ArtifactDigest))
            errors.Add($"Stage '{stage.StageId}' has an invalid artifact SHA-256 digest.");
        if (candidate.ManifestDigest is not null && !IsSha256(candidate.ManifestDigest))
            errors.Add($"Stage '{stage.StageId}' has an invalid manifest SHA-256 digest.");
        if (string.IsNullOrWhiteSpace(stage.AttemptId))
            errors.Add($"Stage '{stage.StageId}' has no attempt identity.");
        if (!Enum.IsDefined(stage.Status))
            errors.Add($"Stage '{stage.StageId}' has an unknown status value.");

        var terminalEvidenceRequired = stage.Status is DependencyStageStatus.Passed or DependencyStageStatus.Failed;
        if (terminalEvidenceRequired && !HasAcceptedEvidence(stage.Evidence))
            errors.Add($"Terminal stage '{stage.StageId}' requires accepted, fully bound campaign evidence.");
        if (stage.Evidence is { } evidence)
        {
            if (!Enum.IsDefined(evidence.ValidationState))
                errors.Add($"Stage '{stage.StageId}' has an unknown evidence validation state.");
            if (evidence.ValidationState == DependencyEvidenceState.Accepted)
            {
                if (string.IsNullOrWhiteSpace(evidence.ProfileId) || evidence.ProfileVersion < 1 ||
                    !IsSha256(evidence.ProfileDigest) || string.IsNullOrWhiteSpace(evidence.RunId) ||
                    !IsSha256(evidence.ReportDigest) || string.IsNullOrWhiteSpace(evidence.ProviderId))
                    errors.Add($"Stage '{stage.StageId}' has incomplete accepted evidence identity.");
            }
        }
    }

    private static bool HasAcceptedEvidence(DependencyStageEvidence? evidence) =>
        evidence is not null &&
        evidence.ValidationState == DependencyEvidenceState.Accepted &&
        !string.IsNullOrWhiteSpace(evidence.ProfileId) &&
        evidence.ProfileVersion > 0 &&
        IsSha256(evidence.ProfileDigest) &&
        !string.IsNullOrWhiteSpace(evidence.RunId) &&
        IsSha256(evidence.ReportDigest) &&
        !string.IsNullOrWhiteSpace(evidence.ProviderId);

    private static List<string> TopologicalOrder(
        IReadOnlyDictionary<string, DependencyStage> stages,
        List<string> errors)
    {
        var indegree = stages.Keys.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        var downstream = stages.Keys.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);

        foreach (var stage in stages.Values)
        foreach (var upstreamId in stage.UpstreamStageIds ?? [])
        {
            if (!stages.ContainsKey(upstreamId))
                continue;
            indegree[stage.StageId]++;
            downstream[upstreamId].Add(stage.StageId);
        }

        var ready = new SortedSet<string>(indegree.Where(x => x.Value == 0).Select(x => x.Key), StringComparer.Ordinal);
        var order = new List<string>(stages.Count);
        while (ready.Count > 0)
        {
            var current = ready.Min!;
            ready.Remove(current);
            order.Add(current);
            foreach (var next in downstream[current].OrderBy(x => x, StringComparer.Ordinal))
                if (--indegree[next] == 0)
                    ready.Add(next);
        }

        if (order.Count != stages.Count)
            errors.Add("Dependency graph contains a cycle.");
        return order;
    }

    private static bool IsRepository(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Split('/') is [var owner, var repo] &&
        !string.IsNullOrWhiteSpace(owner) &&
        !string.IsNullOrWhiteSpace(repo);

    private static bool IsCommitSha(string? value) =>
        value is { Length: 40 or 64 } && value.All(Uri.IsHexDigit);

    private static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);
}
