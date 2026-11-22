using System.Text.Json;
using TheSingularityWorkshop.Gauntlet;
using TheSingularityWorkshop.Workshop.Gui;
using TheSingularityWorkshop.Workshop.Gui.Svg;

var arguments = ParseArguments(args);
var input = Get(arguments, "input", "samples/DependencyMachineBadge/fixtures/parallel-join.json");
var output = Path.GetFullPath(Get(arguments, "output", "artifacts/dependency-machine.svg"));
var json = File.ReadAllText(input);
var snapshot = JsonSerializer.Deserialize<DependencyGraphSnapshot>(
    json,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidDataException("Dependency graph fixture did not contain a snapshot.");

var validation = DependencyGraphEvaluator.Validate(snapshot);
if (!validation.IsValid)
    throw new InvalidDataException("Invalid dependency graph: " + string.Join(" | ", validation.Errors));

var evaluation = DependencyGraphEvaluator.Evaluate(snapshot);
var stages = snapshot.Stages.ToDictionary(stage => stage.StageId, StringComparer.Ordinal);
var depth = new Dictionary<string, int>(StringComparer.Ordinal);
foreach (var stageId in validation.TopologicalStageIds)
{
    var stage = stages[stageId];
    depth[stageId] = stage.UpstreamStageIds.Count == 0
        ? 0
        : stage.UpstreamStageIds.Max(id => depth[id]) + 1;
}

var rowsByDepth = validation.TopologicalStageIds
    .GroupBy(id => depth[id])
    .ToDictionary(group => group.Key, group => group.ToArray());
var rowById = rowsByDepth.SelectMany(pair => pair.Value.Select((id, row) => (id, row)))
    .ToDictionary(item => item.id, item => item.row, StringComparer.Ordinal);
var positions = new Dictionary<string, (int X, int Y)>(StringComparer.Ordinal);
foreach (var stageId in validation.TopologicalStageIds)
    positions[stageId] = (34 + depth[stageId] * 190, 76 + rowById[stageId] * 112);

var maxDepth = depth.Values.Max();
var maxRows = rowsByDepth.Values.Max(rows => rows.Length);
var width = 70 + (maxDepth + 1) * 190;
var height = 86 + maxRows * 112;
var aggregateColor = Color(evaluation.Status);

var scene = GuiBuilder.Create("svg-scene", "dependency-machine")
    .Text("The Gauntlet — dependency machine")
    .Property("viewBox", $"0 0 {width} {height}")
    .Property("role", "img")
    .Property("aria-label", $"Dependency machine preview: {evaluation.Status}")
    .Property("description",
        $"Preview fixture only. Pipeline {snapshot.PipelineAttemptId}; snapshot {snapshot.SnapshotId}; " +
        $"{evaluation.PassedRequiredStages} of {evaluation.TotalRequiredStages} required stages passed. " +
        string.Join(", ", snapshot.Stages.Select(stage => $"{stage.StageId}: {stage.Status}")) + ".");

scene.Child("svg-rect", "background", node => node
    .Property("x", "1").Property("y", "1")
    .Property("width", (width - 2).ToString()).Property("height", (height - 2).ToString())
    .Property("rx", "14").Property("fill", "#0b1420")
    .Property("stroke", aggregateColor).Property("stroke-width", "2"));

AddText(scene, "title", "THE GAUNTLET · DEPENDENCY MACHINE", 22, 26, 11, "#f8fafc", "800");
AddText(scene, "aggregate", evaluation.Status.ToString().ToUpperInvariant(), 22, 45, 7, aggregateColor, "800");
AddText(scene, "fixture-warning", "PREVIEW FIXTURE · NOT PIPELINE EVIDENCE", 22, 59, 5, "#fbbf24", "700");
AddText(scene, "attempt", $"ATTEMPT {snapshot.PipelineAttemptId}  ·  SNAPSHOT {Shorten(snapshot.SnapshotId)}",
    Math.Max(22, width - 310), 26, 4.5, "#94a3b8", "600");

// Draw dependency links first so stage machinery sits above the tracks.
foreach (var stage in snapshot.Stages)
{
    var target = positions[stage.StageId];
    foreach (var upstreamId in stage.UpstreamStageIds)
    {
        var source = positions[upstreamId];
        scene.Child("svg-path", $"edge-{SafeId(upstreamId)}-{SafeId(stage.StageId)}", edge => edge
            .Property("d", $"M {source.X + 156} {source.Y + 42} C {source.X + 174} {source.Y + 42}, {target.X - 18} {target.Y + 42}, {target.X} {target.Y + 42}")
            .Property("fill", "none").Property("stroke", "#64748b")
            .Property("stroke-width", "2").Property("stroke-dasharray", "4 3"));
    }
}

foreach (var stage in snapshot.Stages)
{
    var (x, y) = positions[stage.StageId];
    var accent = Color(stage.Status);
    var archetype = Archetype(stage.StageId);
    scene.Child("svg-g", $"stage-{SafeId(stage.StageId)}", group =>
    {
        group.Property("transform", $"translate({x} {y})");
        group.Child("svg-rect", "stage-panel", panel => panel
            .Property("x", "0").Property("y", "0").Property("width", "156").Property("height", "84")
            .Property("rx", "8").Property("fill", "#172638")
            .Property("stroke", accent).Property("stroke-width", "1.5"));
        AddText(group, "stage-name", Shorten(stage.StageId), 8, 15, 6.4, "#f8fafc", "700");
        AddText(group, "stage-status", stage.Status.ToString().ToUpperInvariant(), 8, 27, 4.8, accent, "800");
        AddText(group, "stage-repo", stage.Candidate.Repository, 8, 77, 3.7, "#94a3b8", "500");
        DrawMechanism(group, archetype, accent);
        AddText(group, "stage-required", stage.Required ? "REQUIRED" : "OPTIONAL", 112, 15, 3.4, "#94a3b8", "600");
    });
}

// The final bell/drawbridge is a completion symbol, never a release authorization.
var finishX = 34 + (maxDepth + 1) * 190;
scene.Child("svg-g", "final-drawbridge", gate =>
{
    gate.Property("transform", $"translate({finishX} 112)");
    gate.Child("svg-rect", "gate-post", post => post.Property("x", "0").Property("y", "-22")
        .Property("width", "4").Property("height", "50").Property("fill", "#94a3b8"));
    gate.Child("svg-rect", "gate-door", door => door.Property("x", "4").Property("y", "-22")
        .Property("width", "22").Property("height", "5").Property("fill", aggregateColor));
    AddText(gate, "finish-label", evaluation.Status == DependencyGraphStatus.Passed ? "ALL CLEAR" : "GATE HELD",
        0, 42, 4.2, aggregateColor, "800");
});
AddText(scene, "progress", $"{evaluation.PassedRequiredStages}/{evaluation.TotalRequiredStages} REQUIRED STAGES PASSED",
    22, height - 12, 5, "#cbd5e1", "700");

var svg = new SvgGuiRenderer().Render(scene.Build(), width: width * 2, height: height * 2);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, svg);
Console.WriteLine($"Rendered {evaluation.Status} dependency-machine preview to {output}");

static void DrawMechanism(GuiBuilder group, string archetype, string accent)
{
    group.Child("svg-g", "mechanism", machine =>
    {
        machine.Property("transform", "translate(78 48)");
        switch (archetype)
        {
            case "axe":
                machine.Child("svg-line", "pendulum", line => line.Property("x1", "0").Property("y1", "-18")
                    .Property("x2", "0").Property("y2", "3").Property("stroke", "#cbd5e1").Property("stroke-width", "2"));
                machine.Child("svg-path", "axe-head", head => head.Property("d", "M-11 2 L0 0 L10 8 L-2 11 Z")
                    .Property("fill", "#94a3b8").Property("stroke", accent));
                break;
            case "gear":
                machine.Child("svg-circle", "gear", circle => circle.Property("cx", "0").Property("cy", "0")
                    .Property("r", "13").Property("fill", "none").Property("stroke", accent).Property("stroke-width", "4")
                    .Property("stroke-dasharray", "5 2"));
                machine.Child("svg-circle", "gear-hub", hub => hub.Property("cx", "0").Property("cy", "0")
                    .Property("r", "3").Property("fill", "#cbd5e1"));
                break;
            case "crusher":
                machine.Child("svg-rect", "crusher-top", rect => rect.Property("x", "-15").Property("y", "-13")
                    .Property("width", "30").Property("height", "7").Property("fill", "#94a3b8").Property("stroke", accent));
                machine.Child("svg-path", "crusher-teeth", teeth => teeth.Property("d", "M-12 -6 L-8 1 L-4 -6 L0 1 L4 -6 L8 1 L12 -6")
                    .Property("fill", "none").Property("stroke", accent).Property("stroke-width", "2"));
                break;
            case "trap":
                machine.Child("svg-path", "trap-floor", floor => floor.Property("d", "M-18 8 L-10 -5 L-2 8 L6 -5 L14 8")
                    .Property("fill", "none").Property("stroke", accent).Property("stroke-width", "3"));
                break;
            default:
                machine.Child("svg-rect", "portcullis", gate => gate.Property("x", "-12").Property("y", "-14")
                    .Property("width", "24").Property("height", "28").Property("fill", "none")
                    .Property("stroke", accent).Property("stroke-width", "2"));
                for (var i = -8; i <= 8; i += 8)
                    machine.Child("svg-line", $"bar-{i}", bar => bar.Property("x1", i.ToString()).Property("y1", "-13")
                        .Property("x2", i.ToString()).Property("y2", "13").Property("stroke", accent).Property("stroke-width", "1.5"));
                break;
        }
    });
}

static void AddText(GuiBuilder parent, string id, string value, int x, int y, double size, string fill, string weight) =>
    parent.Child("svg-text", id, node => node.Text(value)
        .Property("x", x.ToString()).Property("y", y.ToString())
        .Property("fill", fill).Property("font-family", "Segoe UI,Arial,sans-serif")
        .Property("font-size", size.ToString(System.Globalization.CultureInfo.InvariantCulture))
        .Property("font-weight", weight));

static string Color(DependencyStageStatus status) => status switch
{
    DependencyStageStatus.Passed => "#42d392",
    DependencyStageStatus.Failed => "#ef5365",
    DependencyStageStatus.Running => "#60a5fa",
    DependencyStageStatus.NotYetRun => "#e7b84b",
    DependencyStageStatus.Blocked => "#94a3b8",
    DependencyStageStatus.Incomplete => "#f59e0b",
    DependencyStageStatus.Invalid => "#f97316",
    _ => "#a78bfa"
};

static string Color(DependencyGraphStatus status) => status switch
{
    DependencyGraphStatus.Passed => "#42d392",
    DependencyGraphStatus.Failed => "#ef5365",
    DependencyGraphStatus.Running => "#60a5fa",
    DependencyGraphStatus.NotYetRun => "#e7b84b",
    DependencyGraphStatus.Blocked => "#94a3b8",
    DependencyGraphStatus.Incomplete => "#f59e0b",
    DependencyGraphStatus.Invalid => "#f97316",
    _ => "#a78bfa"
};

static string Archetype(string stageId)
{
    // Stable FNV-1a hashing avoids process-randomized string hash codes.
    uint hash = 2166136261;
    foreach (var character in stageId)
        hash = (hash ^ character) * 16777619;
    return (hash % 5) switch { 0 => "axe", 1 => "gear", 2 => "crusher", 3 => "trap", _ => "gate" };
}

static string SafeId(string value) => new(value.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
static string Shorten(string value) => value.Length <= 18 ? value : value[..18];
static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback) =>
    values.TryGetValue(key, out var value) ? value : fallback;

static Dictionary<string, string> ParseArguments(string[] values)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < values.Length; i++)
    {
        if (!values[i].StartsWith("--", StringComparison.Ordinal))
            continue;
        var key = values[i][2..];
        result[key] = i + 1 < values.Length && !values[i + 1].StartsWith("--", StringComparison.Ordinal)
            ? values[++i]
            : "true";
    }
    return result;
}
