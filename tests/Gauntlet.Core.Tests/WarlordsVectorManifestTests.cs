using System.Text;
using TheSingularityWorkshop.Gauntlet.Integrations.Warlords;
using Xunit;

namespace TheSingularityWorkshop.Gauntlet.Tests;

public sealed class WarlordsVectorManifestTests
{
    private const string ValidVector = """
    {
      "id": "azure-opening-capture",
      "schemaVersion": 1,
      "scenario": { "key": "four-kingdoms", "version": 1 },
      "rulesVersion": "warlords-four-kingdoms/1",
      "seed": 17,
      "commands": [
        {
          "sequence": 1,
          "kind": "march",
          "source": "azure-keep",
          "destination": "frostmarch",
          "armies": 4,
          "expected": {
            "result": "accepted",
            "event": { "sequence": 1, "ruleId": "combat.seeded-d6-strength-comparison" }
          }
        },
        {
          "sequence": 2,
          "kind": "endTurn",
          "expected": { "result": "accepted" }
        }
      ],
      "expectedFinalSnapshot": {
        "phase": "command",
        "activeKingdom": "Crimson",
        "winner": null,
        "territories": [],
        "events": []
      }
    }
    """;

    private static WarlordsVectorManifest Parse(string json) =>
        WarlordsVectorManifest.Parse(Encoding.UTF8.GetBytes(json));

    [Fact]
    public void Parses_supported_v1_vector_as_data()
    {
        var vector = Parse(ValidVector);
        Assert.Equal("azure-opening-capture", vector.Id);
        Assert.Equal(17, vector.Seed);
        Assert.Equal(2, vector.Commands.Count);
        Assert.Equal("march", vector.Commands[0].Kind);
        Assert.Equal("accepted", vector.Commands[0].ExpectedResult);
        Assert.Equal(1, vector.Commands[0].ExpectedEvent!.Value.GetProperty("sequence").GetInt32());
        Assert.Equal("Crimson", vector.ExpectedFinalSnapshot.GetProperty("activeKingdom").GetString());
    }

    [Fact]
    public void Rejects_unsupported_schema_version()
    {
        var json = ValidVector.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2", StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(json));
    }

    [Fact]
    public void Rejects_unsupported_scenario_or_rules_version()
    {
        var scenario = ValidVector.Replace("four-kingdoms", "unknown-map", StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(scenario));
        var rules = ValidVector.Replace("warlords-four-kingdoms/1", "warlords-four-kingdoms/2", StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(rules));
    }

    [Fact]
    public void Rejects_duplicate_or_non_contiguous_command_sequences()
    {
        var duplicate = ValidVector.Replace("\"sequence\": 2", "\"sequence\": 1", StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(duplicate));
        var gap = ValidVector.Replace("\"sequence\": 2", "\"sequence\": 3", StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(gap));
    }

    [Fact]
    public void Rejects_accepted_march_without_complete_expected_event()
    {
        var json = ValidVector.Replace(
            "\"event\": { \"sequence\": 1, \"ruleId\": \"combat.seeded-d6-strength-comparison\" }",
            "\"unused\": true",
            StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(json));
    }

    [Fact]
    public void Rejects_rejected_command_with_an_event()
    {
        var json = ValidVector.Replace("\"result\": \"accepted\"", "\"result\": \"rejected\"", StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(json));
    }

    [Fact]
    public void Rejects_duplicate_json_properties()
    {
        var json = ValidVector.Replace(
            "\"schemaVersion\": 1,",
            "\"schemaVersion\": 1, \"schemaVersion\": 1,",
            StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(json));
    }

    [Fact]
    public void Rejects_duplicate_properties_inside_golden_event_and_snapshot_data()
    {
        var duplicateEvent = ValidVector.Replace(
            "\"ruleId\": \"combat.seeded-d6-strength-comparison\"",
            "\"ruleId\": \"combat.seeded-d6-strength-comparison\", \"ruleId\": \"tampered\"",
            StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(duplicateEvent));

        var duplicateSnapshot = ValidVector.Replace(
            "\"activeKingdom\": \"Crimson\"",
            "\"activeKingdom\": \"Crimson\", \"activeKingdom\": \"Azure\"",
            StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(duplicateSnapshot));
    }

    [Fact]
    public void Rejects_unknown_command_kind_and_invalid_march_count()
    {
        var kind = ValidVector.Replace("\"kind\": \"march\"", "\"kind\": \"executeScript\"", StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(kind));
        var armies = ValidVector.Replace("\"armies\": 4", "\"armies\": 0", StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(armies));
    }


    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Preserves_non_positive_army_counts_for_rejected_marches(int armies)
    {
        var json = ValidVector
            .Replace("\"armies\": 4", $"\"armies\": {armies}", StringComparison.Ordinal)
            .Replace("\"result\": \"accepted\"", "\"result\": \"rejected\"", StringComparison.Ordinal);
        json = json.Replace(
            ",\n            \"event\": { \"sequence\": 1, \"ruleId\": \"combat.seeded-d6-strength-comparison\" }",
            string.Empty,
            StringComparison.Ordinal);

        var vector = Parse(json);

        Assert.Equal("rejected", vector.Commands[0].ExpectedResult);
        Assert.Equal(armies, vector.Commands[0].Armies);
        Assert.Null(vector.Commands[0].ExpectedEvent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Rejects_non_positive_army_counts_for_accepted_marches(int armies)
    {
        var json = ValidVector.Replace(
            "\"armies\": 4",
            $"\"armies\": {armies}",
            StringComparison.Ordinal);

        Assert.Throws<FormatException>(() => Parse(json));
    }

    [Fact]
    public void Rejects_missing_final_snapshot()
    {
        var json = ValidVector.Replace("\"expectedFinalSnapshot\": {", "\"finalSnapshot\": {", StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(json));
    }

    [Fact]
    public void Rejects_unknown_envelope_properties_and_march_fields_on_end_turn()
    {
        var rootProperty = ValidVector.Replace(
            "\"schemaVersion\": 1,",
            "\"schemaVersion\": 1, \"futureBehavior\": \"execute\",",
            StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(rootProperty));

        var commandProperty = ValidVector.Replace(
            "\"kind\": \"march\",",
            "\"kind\": \"march\", \"script\": \"candidate-code\",",
            StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(commandProperty));

        var endTurnWithMarchData = ValidVector.Replace(
            "\"kind\": \"endTurn\",",
            "\"kind\": \"endTurn\", \"source\": \"azure-keep\",",
            StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => Parse(endTurnWithMarchData));
    }
}
