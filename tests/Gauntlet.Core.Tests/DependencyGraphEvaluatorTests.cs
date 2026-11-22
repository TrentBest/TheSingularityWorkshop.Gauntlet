using Xunit;
using TheSingularityWorkshop.Gauntlet;

namespace Gauntlet.Core.Tests;

public sealed class DependencyGraphEvaluatorTests
{
    private const string Digest = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Commit = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Validates_dag_and_returns_deterministic_topological_order()
    {
        var graph = Graph(
            Stage("join", ["left", "right"], status: DependencyStageStatus.Blocked),
            Stage("right", ["core"], status: DependencyStageStatus.Blocked),
            Stage("left", ["core"], status: DependencyStageStatus.Blocked),
            Stage("core"));

        var result = DependencyGraphEvaluator.Validate(graph);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(new[] { "core", "left", "right", "join" }, result.TopologicalStageIds);
    }

    [Fact]
    public void Rejects_cycles_and_missing_dependencies()
    {
        var cyclic = Graph(Stage("a", ["b"]), Stage("b", ["a"]));
        var missing = Graph(Stage("a", ["absent"]));

        Assert.Contains(DependencyGraphEvaluator.Validate(cyclic).Errors, x => x.Contains("cycle", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(DependencyGraphEvaluator.Validate(missing).Errors, x => x.Contains("missing upstream", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Rejects_duplicate_ids_and_unknown_status_values()
    {
        var duplicate = Graph(Stage("same"), Stage("same"));
        var unknown = Graph(Stage("unknown", status: (DependencyStageStatus)999));

        Assert.Contains(DependencyGraphEvaluator.Validate(duplicate).Errors, x => x.Contains("Duplicate stage ID", StringComparison.Ordinal));
        Assert.False(DependencyGraphEvaluator.Validate(unknown).IsValid);
    }

    [Fact]
    public void Passed_requires_fully_bound_accepted_evidence()
    {
        var graph = Graph(Stage("core", status: DependencyStageStatus.Passed) with { Evidence = null });

        var result = DependencyGraphEvaluator.Validate(graph);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("accepted", StringComparison.Ordinal));
    }

    [Fact]
    public void Accepted_evidence_requires_an_evidence_bundle_digest_and_reference()
    {
        var evidence = Stage("core", status: DependencyStageStatus.Passed).Evidence!;
        var graph = Graph(Stage("core", status: DependencyStageStatus.Passed) with
        {
            Evidence = evidence with { EvidenceBundleDigest = null, EvidenceReference = null }
        });

        Assert.False(DependencyGraphEvaluator.Validate(graph).IsValid);
    }

    [Fact]
    public void Rejects_a_downstream_attempt_started_before_its_prerequisite_passes()
    {
        var graph = Graph(
            Stage("core", status: DependencyStageStatus.Failed),
            Stage("downstream", ["core"], status: DependencyStageStatus.Running));

        Assert.False(DependencyGraphEvaluator.Validate(graph).IsValid);
    }

    [Fact]
    public void Join_is_not_eligible_until_every_upstream_stage_passes()
    {
        var graph = Graph(
            Stage("core", status: DependencyStageStatus.Passed),
            Stage("ui", ["core"], status: DependencyStageStatus.NotYetRun),
            Stage("join", ["core", "ui"], status: DependencyStageStatus.Blocked));

        Assert.True(DependencyGraphEvaluator.IsEligible(graph, "ui"));
        Assert.False(DependencyGraphEvaluator.IsEligible(graph, "join"));
    }

    [Fact]
    public void Failed_stage_blocks_descendant_but_not_independent_branch()
    {
        var graph = Graph(
            Stage("core", status: DependencyStageStatus.Failed),
            Stage("downstream", ["core"], status: DependencyStageStatus.Blocked),
            Stage("independent", status: DependencyStageStatus.NotYetRun));

        Assert.False(DependencyGraphEvaluator.IsEligible(graph, "downstream"));
        Assert.True(DependencyGraphEvaluator.IsEligible(graph, "independent"));
        Assert.Equal(DependencyGraphStatus.Failed, DependencyGraphEvaluator.Evaluate(graph).Status);
    }

    [Theory]
    [InlineData(DependencyStageStatus.Invalid, DependencyGraphStatus.Invalid)]
    [InlineData(DependencyStageStatus.Untrusted, DependencyGraphStatus.Untrusted)]
    [InlineData(DependencyStageStatus.Incomplete, DependencyGraphStatus.Incomplete)]
    [InlineData(DependencyStageStatus.Failed, DependencyGraphStatus.Failed)]
    [InlineData(DependencyStageStatus.Blocked, DependencyGraphStatus.Blocked)]
    [InlineData(DependencyStageStatus.Running, DependencyGraphStatus.Running)]
    [InlineData(DependencyStageStatus.NotYetRun, DependencyGraphStatus.NotYetRun)]
    public void Aggregate_preserves_fail_closed_precedence(
        DependencyStageStatus stageStatus,
        DependencyGraphStatus expected)
    {
        var graph = stageStatus == DependencyStageStatus.Blocked
            ? Graph(Stage("upstream"), Stage("core", ["upstream"], status: stageStatus))
            : Graph(Stage("core", status: stageStatus));

        Assert.Equal(expected, DependencyGraphEvaluator.Evaluate(graph).Status);
    }

    [Fact]
    public void Evidence_quality_failure_cannot_be_hidden_by_a_campaign_failure()
    {
        var graph = Graph(
            Stage("failed", status: DependencyStageStatus.Failed),
            Stage("untrusted", status: DependencyStageStatus.Untrusted));

        Assert.Equal(DependencyGraphStatus.Untrusted, DependencyGraphEvaluator.Evaluate(graph).Status);
    }

    [Fact]
    public void Rejects_graphs_without_required_stages()
    {
        var graph = Graph(Stage("optional") with { Required = false });

        Assert.False(DependencyGraphEvaluator.Validate(graph).IsValid);
    }

    [Fact]
    public void Snapshot_digest_is_order_independent_but_detects_content_mutation()
    {
        var first = Graph(Stage("b"), Stage("a"));
        var reordered = Graph(Stage("a"), Stage("b"));
        var tampered = first with
        {
            Stages = first.Stages.Select(stage => stage.StageId == "a"
                ? stage with { Status = DependencyStageStatus.Failed, Evidence = new DependencyStageEvidence("standard", 1, Digest, "run-a", Digest, "trusted-provider", DependencyEvidenceState.Accepted, Digest, "https://example.invalid/evidence/run-a") }
                : stage).ToArray()
        };

        Assert.Equal(first.SnapshotDigest, reordered.SnapshotDigest);
        Assert.Equal(DependencyGraphSnapshotIntegrity.SerializeCanonical(first), DependencyGraphSnapshotIntegrity.SerializeCanonical(reordered));
        Assert.True(DependencyGraphSnapshotIntegrity.Verify(first));
        Assert.False(DependencyGraphSnapshotIntegrity.Verify(tampered));
        Assert.False(DependencyGraphEvaluator.Validate(tampered).IsValid);
    }

    [Fact]
    public void Snapshot_copies_stage_collection()
    {
        var stages = new List<DependencyStage> { Stage("core") };
        var graph = DependencyGraphSnapshotIntegrity.Seal(new DependencyGraphSnapshot(1, "snapshot-1", Digest, "pipeline-attempt-1", stages));
        stages.Clear();

        Assert.Single(graph.Stages);
    }

    [Fact]
    public void Record_updates_defensively_copy_replacement_collections()
    {
        var upstream = new List<string> { "core" };
        var stage = Stage("ui") with { UpstreamStageIds = upstream };
        upstream.Add("unexpected");

        var stages = new List<DependencyStage> { Stage("core") };
        var graph = Graph(Stage("initial")) with { Stages = stages };
        stages.Clear();

        Assert.Equal(new[] { "core" }, stage.UpstreamStageIds);
        Assert.Single(graph.Stages);
    }

    private static DependencyGraphSnapshot Graph(params DependencyStage[] stages) =>
        DependencyGraphSnapshotIntegrity.Seal(
            new DependencyGraphSnapshot(1, "snapshot-1", Digest, "pipeline-attempt-1", stages));

    private static DependencyStage Stage(
        string id,
        string[]? upstream = null,
        DependencyStageStatus status = DependencyStageStatus.NotYetRun)
    {
        var evidence = status is DependencyStageStatus.Passed or DependencyStageStatus.Failed
            ? new DependencyStageEvidence("standard", 1, Digest, $"run-{id}", Digest, "trusted-provider", DependencyEvidenceState.Accepted, Digest, $"https://example.invalid/evidence/run-{id}")
            : null;

        return new DependencyStage(
            id,
            new DependencyCandidateIdentity("owner/repo", id, Commit, Digest, null),
            upstream ?? [],
            true,
            $"attempt-{id}",
            status,
            evidence);
    }
}
