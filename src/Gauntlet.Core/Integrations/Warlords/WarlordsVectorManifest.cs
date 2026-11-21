using System.Text.Json;

namespace TheSingularityWorkshop.Gauntlet.Integrations.Warlords;

/// <summary>
/// Validated, data-only view of one Experiences-owned Warlords campaign vector.
/// This type never loads candidate assemblies or invokes candidate code.
/// </summary>
public sealed record WarlordsVectorManifest(
    string Id,
    int SchemaVersion,
    string ScenarioKey,
    int ScenarioVersion,
    string RulesVersion,
    long Seed,
    IReadOnlyList<WarlordsVectorCommand> Commands,
    JsonElement ExpectedFinalSnapshot)
{
    public const int SupportedSchemaVersion = 1;
    public const string SupportedScenarioKey = "four-kingdoms";
    public const int SupportedScenarioVersion = 1;
    public const string SupportedRulesVersion = "warlords-four-kingdoms/1";

    /// <summary>Parses and validates the stable v1 fixture envelope. Domain event and snapshot bodies remain opaque JSON data.</summary>
    public static WarlordsVectorManifest Parse(ReadOnlyMemory<byte> utf8Json)
    {
        using var document = JsonDocument.Parse(utf8Json, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 64
        });

        var root = document.RootElement;
        RequireKind(root, JsonValueKind.Object, "vector");
        RejectDuplicateProperties(root, "vector");

        var id = RequiredString(root, "id");
        var schemaVersion = RequiredInt32(root, "schemaVersion");
        var scenario = RequiredObject(root, "scenario");
        RejectDuplicateProperties(scenario, "scenario");
        var scenarioKey = RequiredString(scenario, "key");
        var scenarioVersion = RequiredInt32(scenario, "version");
        var rulesVersion = RequiredString(root, "rulesVersion");
        var seed = RequiredInt64(root, "seed");
        var commandsElement = RequiredArray(root, "commands");
        var snapshot = RequiredObject(root, "expectedFinalSnapshot");

        if (string.IsNullOrWhiteSpace(id))
            throw Invalid("Vector id must not be empty.");
        if (schemaVersion != SupportedSchemaVersion)
            throw Invalid($"Unsupported vector schema version '{schemaVersion}'.");
        if (!StringComparer.Ordinal.Equals(scenarioKey, SupportedScenarioKey) ||
            scenarioVersion != SupportedScenarioVersion)
            throw Invalid($"Unsupported Warlords scenario '{scenarioKey}' version '{scenarioVersion}'.");
        if (!StringComparer.Ordinal.Equals(rulesVersion, SupportedRulesVersion))
            throw Invalid($"Unsupported Warlords rules version '{rulesVersion}'.");
        if (commandsElement.GetArrayLength() == 0)
            throw Invalid("Vector must contain at least one command.");

        var commands = new List<WarlordsVectorCommand>();
        var sequences = new HashSet<int>();
        var expectedSequence = 1;

        foreach (var item in commandsElement.EnumerateArray())
        {
            RequireKind(item, JsonValueKind.Object, "command");
            RejectDuplicateProperties(item, "command");
            var sequence = RequiredInt32(item, "sequence");
            var kind = RequiredString(item, "kind");
            var expected = RequiredObject(item, "expected");
            RejectDuplicateProperties(expected, "command expected result");

            if (sequence < 1 || !sequences.Add(sequence))
                throw Invalid($"Command sequence '{sequence}' is invalid or duplicated.");
            if (sequence != expectedSequence++)
                throw Invalid("Command sequences must be contiguous and ordered from 1.");
            if (kind is not ("march" or "endTurn"))
                throw Invalid($"Unsupported command kind '{kind}'.");

            string? source = null;
            string? destination = null;
            int? armies = null;
            if (kind == "march")
            {
                source = RequiredString(item, "source");
                destination = RequiredString(item, "destination");
                armies = RequiredInt32(item, "armies");
                if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination) || armies <= 0)
                    throw Invalid($"March command {sequence} has invalid source, destination, or army count.");
            }

            var result = RequiredString(expected, "result");
            if (result is not ("accepted" or "rejected"))
                throw Invalid($"Command {sequence} has unsupported expected result '{result}'.");

            JsonElement? expectedEvent = null;
            if (expected.TryGetProperty("event", out var eventElement))
            {
                if (eventElement.ValueKind != JsonValueKind.Object)
                    throw Invalid($"Command {sequence} expected event must be an object.");
                expectedEvent = eventElement.Clone();
            }

            if (result == "accepted" && kind == "march" && expectedEvent is null)
                throw Invalid($"Accepted march command {sequence} must include its complete expected event.");
            if (result == "rejected" && expectedEvent is not null)
                throw Invalid($"Rejected command {sequence} must not declare an accepted event.");

            commands.Add(new WarlordsVectorCommand(
                sequence, kind, source, destination, armies, result, expectedEvent));
        }

        return new WarlordsVectorManifest(
            id, schemaVersion, scenarioKey, scenarioVersion, rulesVersion, seed,
            commands.AsReadOnly(), snapshot.Clone());
    }

    private static JsonElement RequiredObject(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var value))
            throw Invalid($"Required object '{name}' is missing.");
        RequireKind(value, JsonValueKind.Object, name);
        return value;
    }

    private static JsonElement RequiredArray(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var value))
            throw Invalid($"Required array '{name}' is missing.");
        RequireKind(value, JsonValueKind.Array, name);
        return value;
    }

    private static string RequiredString(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            throw Invalid($"Required string '{name}' is missing or invalid.");
        return value.GetString()!;
    }

    private static int RequiredInt32(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var value) || !value.TryGetInt32(out var result))
            throw Invalid($"Required integer '{name}' is missing or invalid.");
        return result;
    }

    private static long RequiredInt64(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var value) || !value.TryGetInt64(out var result))
            throw Invalid($"Required integer '{name}' is missing or invalid.");
        return result;
    }

    private static void RequireKind(JsonElement value, JsonValueKind kind, string name)
    {
        if (value.ValueKind != kind)
            throw Invalid($"'{name}' must be a JSON {kind}.");
    }

    private static void RejectDuplicateProperties(JsonElement value, string name)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            if (!seen.Add(property.Name))
                throw Invalid($"'{name}' contains duplicate property '{property.Name}'.");
        }
    }

    private static FormatException Invalid(string message) => new(message);
}

/// <summary>One ordered command and its literal expected outcome from the authoritative Warlords domain.</summary>
public sealed record WarlordsVectorCommand(
    int Sequence,
    string Kind,
    string? Source,
    string? Destination,
    int? Armies,
    string ExpectedResult,
    JsonElement? ExpectedEvent);
