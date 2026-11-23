using TheSingularityWorkshop.Gauntlet;
using Xunit;

namespace Gauntlet.Core.Tests;

public sealed class DependencyGraphReadOnlyProjectionTests
{
    private const string Digest = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Commit = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Creates_a_deterministic_topological_projection_without_evidence_payloads()
    {
        var graph = Graph(Stage("join", ["left", "right"], DependencyStageStatus.Blocked),
            Stage("right", ["core"], DependencyStageStatus.Blocked),
            Stage("left", ["core"], DependencyStageStatus.Blocked),
            Stage("core"));

        var projection = DependencyGraphReadOnlyProjection.Create(graph);

        Assert.True(projection.IsStructurallyValid);
        Assert.Equal(DependencyGraphStatus.Blocked, projection.Status);
        Assert.Equal(new[] { "core", "left", "right", "join" }, projection.Stages.Select(x => x.StageId));
        Assert.All(projection.Stages, stage => Assert.Null(stage.EvidenceValidationState));
    }

    [Fact]
    public void Projects_evidence_diagnostics_without_exposing_evidence_reference_or_bundle()
    {
        var evidence = new DependencyStageEvidence(
            "profile-a", 1, Digest, "run-42", Digest, "github-actions",
            DependencyEvidenceState.Accepted, Digest,
            "https://example.invalid/private-evidence", "pipeline-attempt-1", "attempt-core");
        var graph = Graph(Stage("core", status: DependencyStageStatus.Passed) with { Evidence = evidence });

        var stage = Assert.Single(DependencyGraphReadOnlyProjection.Create(graph).Stages);

        Assert.Equal(DependencyEvidenceState.Accepted, stage.EvidenceValidationState);
        Assert.Equal("profile-a", stage.EvidenceProfileId);
        Assert.Equal("run-42", stage.EvidenceRunId);
        Assert.Equal("github-actions", stage.EvidenceProviderId);
        Assert.Equal(Digest, stage.EvidenceReportDigest);
        Assert.DoesNotContain("private-evidence", string.Join("|", stage.GetType().GetProperties().Select(p => p.GetValue(stage)?.ToString())));
    }

    [Fact]
    public void Invalid_snapshot_projects_as_invalid_and_is_never_upgraded()
    {
        var graph = Graph(Stage("core")) with { SnapshotDigest = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb" };

        var projection = DependencyGraphReadOnlyProjection.Create(graph);

        Assert.False(projection.IsStructurallyValid);
        Assert.Equal(DependencyGraphStatus.Invalid, projection.Status);
        Assert.Single(projection.Stages);
    }

    [Fact]
    public void Invalid_duplicate_stage_ids_remain_visible_without_throwing()
    {
        var graph = Graph(Stage("duplicate"), Stage("duplicate"));

        var projection = DependencyGraphReadOnlyProjection.Create(graph);

        Assert.False(projection.IsStructurallyValid);
        Assert.Equal(DependencyGraphStatus.Invalid, projection.Status);
        Assert.Equal(2, projection.Stages.Count);
    }

    [Fact]
    public void Projection_copies_stage_and_upstream_collections()
    {
        var upstream = new List<string> { "core" };
        var stages = new List<DependencyStage> { Stage("core"), Stage("ui", upstream, DependencyStageStatus.Blocked) };
        var graph = Graph(stages.ToArray());

        var projection = DependencyGraphReadOnlyProjection.Create(graph);
        upstream.Add("mutated-after-projection");
        stages.Clear();

        Assert.Equal(new[] { "core" }, projection.Stages.Single(x => x.StageId == "ui").UpstreamStageIds);
        Assert.Equal(2, projection.Stages.Count);
    }

    private static DependencyGraphSnapshot Graph(params DependencyStage[] stages) =>
        DependencyGraphSnapshotIntegrity.Seal(new DependencyGraphSnapshot(
            DependencyGraphSnapshot.CurrentSchemaVersion, "snapshot-1", Digest, "pipeline-attempt-1", stages));

    private static DependencyStage Stage(
        string id,
        IReadOnlyList<string>? upstream = null,
        DependencyStageStatus status = DependencyStageStatus.NotYetRun) =>
        new(id,
            new DependencyCandidateIdentity("owner/repo", id, Commit, Digest, null),
            upstream ?? Array.Empty<string>(),
            true,
            $"attempt-{id}",
            status);
}
