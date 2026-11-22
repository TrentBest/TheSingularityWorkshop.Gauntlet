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
    positions[stageId] = (34 + depth[stageId] * 190, 76 + rowById[stageId] * 132);

var maxDepth = depth.Values.Max();
var maxRows = rowsByDepth.Values.Max(rows => rows.Length);
var width = 70 + (maxDepth + 1) * 190;
var height = 86 + maxRows * 132;
var aggregateColor = GraphColor(evaluation.Status);

var scene = GuiBuilder.Create("svg-scene", "dependency-machine")
    .Text("The Gauntlet — dependency machine")
    .Property("viewBox", $"0 0 {width} {height}")
    .Property("role", "img")
    .Property("aria-label", $"Dependency machine preview: {evaluation.Status}")
    .Property("description",
        $"Preview fixture only. Pipeline {snapshot.PipelineAttemptId}; snapshot {snapshot.SnapshotId}; " +
        $"{evaluation.PassedRequiredStages} of {evaluation.TotalRequiredStages} required stages passed. " +
        string.Join(", ", snapshot.Stages.Select(stage => $"{stage.StageId}: {stage.Status}")) + ".");

var motionStyles = """
  .pendulum-shaft { transform-box: fill-box; transform-origin: 50% 0%; }
  .gear-wheel { transform-box: fill-box; transform-origin: center; }
  .crusher-plate { transform-box: fill-box; transform-origin: 50% 0%; }
  .trap-floor { transform-box: fill-box; transform-origin: center; }
  .stage-running .pendulum-shaft { animation: pendulum-swing 1.1s ease-in-out infinite alternate; }
  .stage-failed .pendulum-shaft { animation: pendulum-swing 0.55s ease-in-out infinite alternate; }
  .stage-running .gear-wheel { animation: gear-turn 2.8s linear infinite; }
  .stage-failed .gear-wheel { animation: gear-turn 0.8s linear infinite; }
  .stage-running .crusher-plate { animation: crusher-drop 1.4s ease-in-out infinite; }
  .stage-failed .crusher-plate { animation: crusher-drop 0.7s ease-in-out infinite; }
  .stage-running .trap-floor { animation: trap-open 2.2s ease-in-out infinite; }
  .stage-failed .trap-floor { animation: trap-open 0.8s ease-in-out infinite; }
  .knight-advancing { animation: knight-step 0.8s ease-in-out infinite; transform-box: fill-box; transform-origin: center; }
  .knight-celebrating { animation: knight-cheer 2.6s ease-in-out infinite; transform-box: fill-box; transform-origin: center; }
  .knight-failed { animation: knight-impact 2.1s ease-in-out infinite; transform-box: fill-box; transform-origin: center; }
  .helmet-scatter { animation: helmet-scatter 2.1s ease-out infinite; transform-box: fill-box; transform-origin: center; }
  @keyframes pendulum-swing { from { transform: rotate(-42deg); } to { transform: rotate(42deg); } }
  @keyframes gear-turn { to { transform: rotate(360deg); } }
  @keyframes crusher-drop { 0%, 45%, 100% { transform: translateY(0); } 60%, 75% { transform: translateY(8px); } }
  @keyframes trap-open { 0%, 35%, 100% { opacity: 1; } 55%, 75% { opacity: 0.25; } }
  @keyframes knight-step { 0%, 100% { transform: translateY(0); } 50% { transform: translateY(-2px); } }
  @keyframes knight-cheer { 0%, 100% { transform: translateY(0) rotate(0); } 45% { transform: translateY(-4px) rotate(-4deg); } 65% { transform: translateY(0) rotate(4deg); } }
  @keyframes knight-impact { 0%, 18%, 100% { transform: translateX(0) rotate(0); } 35%, 70% { transform: translateX(3px) rotate(5deg); } }
  @keyframes helmet-scatter { 0%, 15% { transform: translate(0, 0) rotate(0); } 55%, 85% { transform: translate(10px, -9px) rotate(35deg); } 100% { transform: translate(0, 0) rotate(0); } }
  @media (prefers-reduced-motion: reduce) {
    .pendulum-shaft, .gear-wheel, .crusher-plate, .trap-floor,
    .knight-advancing, .knight-celebrating, .knight-failed, .helmet-scatter {
      animation: none !important;
    }
  }
  """;
scene.Child("svg-style", "dependency-machine-motion", style => style.Text(motionStyles));

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
    var accent = StageColor(stage.Status);
    var archetype = Archetype(stage.StageId);
    var stageKey = SafeId(stage.StageId);
    var stageClass = stage.Status switch
    {
        DependencyStageStatus.Running => "stage-running",
        DependencyStageStatus.Failed => "stage-failed",
        _ => "stage-still"
    };
    scene.Child("svg-g", $"stage-{stageKey}", group =>
    {
        group.Property("class", stageClass).Property("transform", $"translate({x} {y})");
        group.Child("svg-rect", $"{stageKey}-panel", panel => panel
            .Property("x", "0").Property("y", "0").Property("width", "156").Property("height", "84")
            .Property("rx", "8").Property("fill", "#172638")
            .Property("stroke", accent).Property("stroke-width", "1.5"));
        AddText(group, $"{stageKey}-name", Shorten(stage.StageId), 8, 15, 6.4, "#f8fafc", "700");
        AddText(group, $"{stageKey}-status", stage.Status.ToString().ToUpperInvariant(), 8, 27, 4.8, accent, "800");
        AddText(group, $"{stageKey}-repo", stage.Candidate.Repository, 8, 77, 3.7, "#94a3b8", "500");
        DrawMechanism(group, stageKey, archetype, accent);
        AddText(group, $"{stageKey}-required", stage.Required ? "REQUIRED" : "OPTIONAL", 112, 15, 3.4, "#94a3b8", "600");
    });
}

// One original vector knight advances through the static course snapshot.
var travelerStage = snapshot.Stages.FirstOrDefault(stage => stage.Status == DependencyStageStatus.Failed)
    ?? snapshot.Stages.FirstOrDefault(stage => stage.Status == DependencyStageStatus.Running)
    ?? snapshot.Stages.FirstOrDefault(stage => stage.Status == DependencyStageStatus.Blocked)
    ?? snapshot.Stages.FirstOrDefault(stage =>
        stage.Status == DependencyStageStatus.NotYetRun &&
        DependencyGraphEvaluator.IsEligible(snapshot, stage.StageId));

int travelerX;
int travelerY;
DependencyStageStatus travelerStatus;
if (travelerStage is null)
{
    travelerX = 34 + (maxDepth + 1) * 190 + 18;
    travelerY = 112;
    travelerStatus = evaluation.Status == DependencyGraphStatus.Passed
        ? DependencyStageStatus.Passed
        : DependencyStageStatus.NotYetRun;
}
else
{
    var (stageX, stageY) = positions[travelerStage.StageId];
    travelerStatus = travelerStage.Status;
    travelerX = stageX + (travelerStatus is DependencyStageStatus.Blocked or DependencyStageStatus.NotYetRun ? -10 : 78);
    travelerY = stageY + 108;
}
DrawKnight(scene, "traveler", travelerX, travelerY, travelerStatus);

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


static void DrawKnight(GuiBuilder scene, string id, int x, int y, DependencyStageStatus status)
{
    var defeated = status == DependencyStageStatus.Failed;
    var celebrating = status == DependencyStageStatus.Passed;
    var poseClass = defeated ? "knight-failed"
        : celebrating ? "knight-celebrating"
        : status == DependencyStageStatus.Running ? "knight-advancing"
        : "knight-waiting";
    scene.Child("svg-g", id, outer =>
    {
        outer.Property("transform", $"translate({x} {y})");
        outer.Child("svg-g", $"{id}-pose", knight =>
        {
            knight.Property("class", poseClass);
        knight.Child("svg-line", $"{id}-ground", line => line.Property("x1", "-18").Property("y1", "18")
            .Property("x2", "18").Property("y2", "18").Property("stroke", "#64748b").Property("stroke-width", "1.5"));
        if (defeated)
        {
            knight.Child("svg-g", $"{id}-fallen-armor", armor =>
            {
                armor.Property("transform", "translate(-2 5) rotate(68)");
                armor.Child("svg-rect", $"{id}-fallen-body", body => body.Property("x", "-5").Property("y", "-7")
                    .Property("width", "10").Property("height", "15").Property("rx", "2")
                    .Property("fill", "#94a3b8").Property("stroke", "#e2e8f0"));
                armor.Child("svg-line", $"{id}-fallen-leg-left", leg => leg.Property("x1", "-3").Property("y1", "7")
                    .Property("x2", "-12").Property("y2", "13").Property("stroke", "#cbd5e1").Property("stroke-width", "2.5"));
                armor.Child("svg-line", $"{id}-fallen-leg-right", leg => leg.Property("x1", "3").Property("y1", "7")
                    .Property("x2", "11").Property("y2", "12").Property("stroke", "#cbd5e1").Property("stroke-width", "2.5"));
                armor.Child("svg-path", $"{id}-fallen-shield", shield => shield.Property("d", "M-9 -5 L-16 -8 L-16 3 L-10 8 Z")
                    .Property("fill", "#31516c").Property("stroke", "#e7b84b"));
            });
            knight.Child("svg-circle", $"{id}-flying-helmet", helmet => helmet.Property("class", "helmet-scatter").Property("cx", "17").Property("cy", "-9")
                .Property("r", "5").Property("fill", "#cbd5e1").Property("stroke", "#f8fafc"));
            knight.Child("svg-path", $"{id}-flying-plume", plume => plume.Property("d", "M17 -14 Q20 -20 24 -18")
                .Property("fill", "none").Property("stroke", "#ef5365").Property("stroke-width", "2"));
        }
        else
        {
            knight.Child("svg-rect", $"{id}-torso", body => body.Property("x", "-5").Property("y", "-7")
                .Property("width", "10").Property("height", "15").Property("rx", "2")
                .Property("fill", "#94a3b8").Property("stroke", "#f8fafc"));
            knight.Child("svg-path", $"{id}-helmet", helmet => helmet.Property("d", "M-6 -8 Q-8 -19 0 -20 Q8 -19 6 -8 Z")
                .Property("fill", "#cbd5e1").Property("stroke", "#f8fafc"));
            knight.Child("svg-line", $"{id}-visor", visor => visor.Property("x1", "-4").Property("y1", "-12")
                .Property("x2", "4").Property("y2", "-12").Property("stroke", "#1e293b").Property("stroke-width", "1.5"));
            knight.Child("svg-path", $"{id}-plume", plume => plume.Property("d", "M0 -19 Q-2 -25 5 -26")
                .Property("fill", "none").Property("stroke", "#bd5365").Property("stroke-width", "2"));
            knight.Child("svg-line", $"{id}-left-leg", leg => leg.Property("x1", "-3").Property("y1", "7")
                .Property("x2", "-7").Property("y2", "17").Property("stroke", "#cbd5e1").Property("stroke-width", "2.5"));
            knight.Child("svg-line", $"{id}-right-leg", leg => leg.Property("x1", "3").Property("y1", "7")
                .Property("x2", "7").Property("y2", "17").Property("stroke", "#cbd5e1").Property("stroke-width", "2.5"));
            knight.Child("svg-line", $"{id}-left-arm", arm => arm.Property("x1", "-5").Property("y1", "-4")
                .Property("x2", "-13").Property("y2", celebrating ? "-17" : "2")
                .Property("stroke", "#cbd5e1").Property("stroke-width", "2.5"));
            knight.Child("svg-line", $"{id}-right-arm", arm => arm.Property("x1", "5").Property("y1", "-4")
                .Property("x2", "13").Property("y2", celebrating ? "-17" : "2")
                .Property("stroke", "#cbd5e1").Property("stroke-width", "2.5"));
            knight.Child("svg-path", $"{id}-shield", shield => shield.Property("d", "M-9 -5 L-15 -8 L-15 3 L-10 8 Z")
                .Property("fill", "#31516c").Property("stroke", "#e7b84b"));
        }
            AddText(knight, $"{id}-caption", defeated ? "STRUCK DOWN" : status == DependencyStageStatus.Blocked
                ? "HELD AT GATE" : celebrating ? "COURSE CLEARED" : "ADVANCING", -23, 31, 4, defeated ? "#ef5365" : "#cbd5e1", "700");
        });
    });
}

static void DrawMechanism(GuiBuilder group, string stageKey, string archetype, string accent)
{
    group.Child("svg-g", $"{stageKey}-mechanism", machine =>
    {
        machine.Property("transform", "translate(78 48)");
        switch (archetype)
        {
            case "axe":
                machine.Child("svg-line", $"{stageKey}-pendulum", line => line.Property("class", "pendulum-shaft").Property("x1", "0").Property("y1", "-18")
                    .Property("x2", "0").Property("y2", "3").Property("stroke", "#cbd5e1").Property("stroke-width", "2"));
                machine.Child("svg-path", $"{stageKey}-axe-head", head => head.Property("d", "M-11 2 L0 0 L10 8 L-2 11 Z")
                    .Property("fill", "#94a3b8").Property("stroke", accent));
                break;
            case "gear":
                machine.Child("svg-circle", $"{stageKey}-gear", circle => circle.Property("class", "gear-wheel").Property("cx", "0").Property("cy", "0")
                    .Property("r", "13").Property("fill", "none").Property("stroke", accent).Property("stroke-width", "4")
                    .Property("stroke-dasharray", "5 2"));
                machine.Child("svg-circle", $"{stageKey}-gear-hub", hub => hub.Property("cx", "0").Property("cy", "0")
                    .Property("r", "3").Property("fill", "#cbd5e1"));
                break;
            case "crusher":
                machine.Child("svg-rect", $"{stageKey}-crusher-top", rect => rect.Property("class", "crusher-plate").Property("x", "-15").Property("y", "-13")
                    .Property("width", "30").Property("height", "7").Property("fill", "#94a3b8").Property("stroke", accent));
                machine.Child("svg-path", $"{stageKey}-crusher-teeth", teeth => teeth.Property("d", "M-12 -6 L-8 1 L-4 -6 L0 1 L4 -6 L8 1 L12 -6")
                    .Property("fill", "none").Property("stroke", accent).Property("stroke-width", "2"));
                break;
            case "trap":
                machine.Child("svg-path", $"{stageKey}-trap-floor", floor => floor.Property("class", "trap-floor").Property("d", "M-18 8 L-10 -5 L-2 8 L6 -5 L14 8")
                    .Property("fill", "none").Property("stroke", accent).Property("stroke-width", "3"));
                break;
            default:
                machine.Child("svg-rect", $"{stageKey}-portcullis", gate => gate.Property("x", "-12").Property("y", "-14")
                    .Property("width", "24").Property("height", "28").Property("fill", "none")
                    .Property("stroke", accent).Property("stroke-width", "2"));
                for (var i = -8; i <= 8; i += 8)
                    machine.Child("svg-line", $"{stageKey}-bar-{i}", bar => bar.Property("x1", i.ToString()).Property("y1", "-13")
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

static string StageColor(DependencyStageStatus status) => status switch
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

static string GraphColor(DependencyGraphStatus status) => status switch
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
