using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TheSingularityWorkshop.Gauntlet;
using Xunit;

namespace TheSingularityWorkshop.Gauntlet.Tests;

public sealed class SurpsConformanceProfileTests
{
    private const string ProfilePath = "Fixtures/surps-core-spying.profile.json";

    [Fact]
    public void Core_spying_profile_pins_opt_in_rulebook_lod_scope_and_offline_procedure()
    {
        var root = LoadProfile();

        Assert.Equal(1, root["schemaVersion"]!.GetValue<int>());
        Assert.Equal("surps.campaign.core-spying.conformance", root["id"]!.GetValue<string>());
        Assert.Equal(1, root["version"]!.GetValue<int>());
        Assert.Equal(ComputeProfileDigest(root), root["profileDigest"]!.GetValue<string>());

        var source = root["source"]!;
        Assert.Equal("TrentBest/TheSingularityWorkshop.SingularityUniversalRoleplayingSystem",
            source["repository"]!.GetValue<string>());
        Assert.Equal("e559337870d596fd11b62478b0fd356d6f503a9e",
            source["commit"]!.GetValue<string>());
        Assert.Equal("docs/books/core-spying.book.json", source["descriptor"]!.GetValue<string>());

        var selection = root["selection"]!;
        Assert.True(selection["optIn"]!.GetValue<bool>());
        Assert.Equal("explicit-selection-no-implicit-override",
            selection["lodPolicy"]!.GetValue<string>());

        var rulebooks = selection["rulebooks"]!.AsArray();
        var selected = Assert.Single(rulebooks);
        Assert.True(selected!["selected"]!.GetValue<bool>());
        Assert.Equal("surps.core.spying", selected["id"]!.GetValue<string>());
        Assert.Equal("0.1.0", selected["version"]!.GetValue<string>());
        Assert.Equal(0, selected["lod"]!.GetValue<int>());
        Assert.Equal("campaign:core-spying", selected["scope"]!.GetValue<string>());
        Assert.Equal("core", selected["role"]!.GetValue<string>());

        var scenario = root["scenario"]!;
        Assert.Equal("not-implemented", scenario["digitalExecution"]!.GetValue<string>());
        Assert.Equal("docs/books/core-spying.md#9-worked-mission-the-missing-ledger",
            scenario["offlineProcedure"]!.GetValue<string>());

        var limits = root["limits"]!;
        Assert.False(limits["candidateCodeExecution"]!.GetValue<bool>());
        Assert.False(limits["networkEgress"]!.GetValue<bool>());
        Assert.False(limits["hostIsolationClaim"]!.GetValue<bool>());
    }

    [Fact]
    public void Profile_digest_changes_when_rulebook_selection_or_lod_changes()
    {
        var root = LoadProfile();
        var originalDigest = root["profileDigest"]!.GetValue<string>();
        root["selection"]!["rulebooks"]![0]!["lod"] = 1;

        Assert.NotEqual(originalDigest, ComputeProfileDigest(root));
    }

    [Fact]
    public void Profile_probe_inventory_passes_existing_gauntlet_evaluator_and_binds_exact_profile_digest()
    {
        var root = LoadProfile();
        var profileDigest = root["profileDigest"]!.GetValue<string>();
        var probeDefinitions = root["probes"]!.AsArray()
            .Select(probe => new ProbeDefinition(
                probe!["id"]!.GetValue<string>(),
                probe["version"]!.GetValue<int>(),
                probe["required"]!.GetValue<bool>(),
                (decimal)probe["weight"]!.GetValue<double>()))
            .ToArray();

        var profile = new CampaignProfile(
            root["id"]!.GetValue<string>(),
            root["version"]!.GetValue<int>(),
            profileDigest,
            probeDefinitions,
            new HashSet<FindingSeverity> { FindingSeverity.High, FindingSeverity.Critical });

        var candidate = new CandidateIdentity(
            "TrentBest/TheSingularityWorkshop.SingularityUniversalRoleplayingSystem",
            root["source"]!["commit"]!.GetValue<string>(),
            profileDigest,
            null);
        var results = probeDefinitions.Select(definition =>
            new ProbeResult(definition.Id, definition.Version, ProbeOutcome.Passed, 1, 1, [], TimeSpan.Zero))
            .ToArray();
        var report = RunReportIntegrity.Seal(new RunReport(
            1, candidate, "0.1.0-dev", profile.Id, profile.Version, profile.Digest,
            "dotnet-10-test-host", "surps-core-spying-fixture-001", 1,
            DateTimeOffset.Parse("2026-11-23T00:00:00Z"),
            DateTimeOffset.Parse("2026-11-23T00:00:01Z"),
            CampaignState.Completed, results, null));

        var evaluation = CampaignEvaluator.Evaluate(profile, candidate, report);

        Assert.Equal(GateStatus.Passed, evaluation.Status);
        Assert.Equal(100m, evaluation.Score);
        Assert.Equal(profileDigest, report.ProfileDigest);
        Assert.True(RunReportIntegrity.Verify(report));
    }

    private static JsonObject LoadProfile()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ProfilePath);
        return JsonNode.Parse(File.ReadAllText(path))!.AsObject();
    }

    private static string ComputeProfileDigest(JsonObject profile)
    {
        var unsigned = (JsonObject)profile.DeepClone();
        unsigned.Remove("profileDigest");
        var canonical = JsonSerializer.Serialize(unsigned);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return $"sha256:{Convert.ToHexString(digest).ToLowerInvariant()}";
    }
}
