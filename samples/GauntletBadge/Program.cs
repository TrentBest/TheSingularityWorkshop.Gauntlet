using System.Globalization;
using TheSingularityWorkshop.Workshop.Gui;
using TheSingularityWorkshop.Workshop.Gui.Svg;

var options = ParseArguments(args);
var requestedState = Get(options, "state", "untested").ToLowerInvariant();
var state = requestedState is "untested" or "failed" or "passed" or "blocked"
    ? requestedState
    : "blocked";

var candidate = Get(options, "candidate-sha", "preview-fixture");
var runId = Get(options, "run-id", "preview");
var score = OptionalNumber(options, "score");
var average = OptionalNumber(options, "average");
var delta = OptionalNumber(options, "delta");
var survived = OptionalInteger(options, "survived", 0);
var failed = OptionalInteger(options, "failed", 0);
var output = Path.GetFullPath(Get(options, "output", $"docs/assets/gauntlet-badge-{state}.svg"));

var accent = state switch
{
    "untested" => "#e7b84b",
    "failed" => "#ef5365",
    "passed" => "#42d392",
    _ => "#94a3b8"
};
var statusText = state switch
{
    "untested" => "NOT YET TESTED",
    "failed" => "THE GAUNTLET WINS",
    "passed" => "KNIGHT SURVIVED",
    _ => "EVIDENCE BLOCKED"
};
var description = state switch
{
    "untested" => "No completed Gauntlet campaign is recorded for this candidate. The knight and apparatus are waiting at the starting line.",
    "failed" => "The exact candidate failed its Gauntlet campaign. The knight is repeatedly defeated by the adversarial course.",
    "passed" => "The exact candidate passed its Gauntlet campaign. The knight crosses the course and celebrates with both hands raised.",
    _ => "The Gauntlet evidence is incomplete, invalid, stale, or untrusted. This state is not a pass."
};

var scene = GuiBuilder.Create("svg-scene", "gauntlet-badge")
    .Text("The Gauntlet — adversarial software verification")
    .Property("viewBox", "0 0 440 132")
    .Property("role", "img")
    .Property("aria-label", $"The Gauntlet: {statusText}")
    .Property("description", $"{description} Candidate {candidate}. Run {runId}.");

scene.Child("svg-defs", "definitions", defs => defs
    .Child("svg-linear-gradient", "badge-metal", gradient => gradient
        .Property("x1", "0%").Property("y1", "0%").Property("x2", "100%").Property("y2", "100%")
        .Child("svg-stop", "metal-start", stop => stop.Property("offset", "0%").Property("stop-color", "#1b293b"))
        .Child("svg-stop", "metal-end", stop => stop.Property("offset", "100%").Property("stop-color", "#080e18")))
    .Child("svg-linear-gradient", "steel", gradient => gradient
        .Property("x1", "0%").Property("y1", "0%").Property("x2", "0%").Property("y2", "100%")
        .Child("svg-stop", "steel-top", stop => stop.Property("offset", "0%").Property("stop-color", "#dbe7f0"))
        .Child("svg-stop", "steel-bottom", stop => stop.Property("offset", "100%").Property("stop-color", "#60758b"))));

var styles = """
  .course-hammer { transform-box: fill-box; transform-origin: 50% 0%; }
  .course-gear { transform-box: fill-box; transform-origin: center; }
  .knight-fail { transform-box: fill-box; transform-origin: center; animation: defeat 2.8s ease-in-out infinite; }
  .knight-pass { transform-box: fill-box; transform-origin: center; animation: traverse 4.2s cubic-bezier(.55,.05,.25,1) infinite; }
  .course-active .course-hammer { animation: hammer 1.15s ease-in-out infinite alternate; }
  .course-active .course-gear { animation: gear 3.2s linear infinite; }
  .arms-cheer { transform-box: fill-box; transform-origin: center; animation: cheer 4.2s ease-in-out infinite; }
  @keyframes defeat {
    0%, 12% { transform: translate(0,0) rotate(0deg); }
    28% { transform: translate(42px,0) rotate(-9deg); }
    42%, 58% { transform: translate(54px,13px) rotate(78deg); }
    74% { transform: translate(22px,8px) rotate(24deg); }
    88%, 100% { transform: translate(0,0) rotate(0deg); }
  }
  @keyframes traverse {
    0%, 8% { transform: translateX(0); }
    54% { transform: translateX(190px); }
    65%, 78% { transform: translateX(190px); }
    100% { transform: translateX(0); }
  }
  @keyframes hammer {
    from { transform: rotate(-48deg); }
    to { transform: rotate(48deg); }
  }
  @keyframes gear { to { transform: rotate(360deg); } }
  @keyframes cheer {
    0%, 52%, 100% { transform: translateY(0); }
    64% { transform: translateY(-3px); }
    76% { transform: translateY(0); }
  }
  @media (prefers-reduced-motion: reduce) {
    .knight-fail, .knight-pass, .course-active .course-hammer,
    .course-active .course-gear, .arms-cheer { animation: none !important; }
  }
  """;
scene.Child("svg-style", "badge-motion", style => style.Text(styles));

scene.Child("svg-rect", "badge-background", node => node
    .Property("x", "1").Property("y", "1").Property("width", "438").Property("height", "130")
    .Property("rx", "12").Property("fill", "url(#badge-metal)"));
scene.Child("svg-rect", "badge-border", node => node
    .Property("x", "1.5").Property("y", "1.5").Property("width", "437").Property("height", "129")
    .Property("rx", "12").Property("fill", "none").Property("stroke", accent).Property("stroke-width", "1.8"));

AddText(scene, "brand", "THE SINGULARITY WORKSHOP", "16", "18", "5.1", "#c6d5e4", "700");
AddText(scene, "title", "THE GAUNTLET", "16", "36", "10", "#f8fafc", "800");
AddText(scene, "status", statusText, "16", "49", "5.5", accent, "800");

var scoreText = score is null ? "—" : score.Value.ToString("0.0", CultureInfo.InvariantCulture);
var averageText = average is null ? "—" : average.Value.ToString("0.0", CultureInfo.InvariantCulture);
var deltaText = delta is null ? "—" : $"{(delta.Value > 0 ? "+" : string.Empty)}{delta.Value.ToString("0.0", CultureInfo.InvariantCulture)}";
AddText(scene, "score-label", "RUN SCORE", "252", "16", "3.1", "#8fa6bb", "600");
AddText(scene, "score-value", scoreText, "252", "27", "7", "#f8fafc", "800");
AddText(scene, "average-label", "AVG", "314", "16", "3.1", "#8fa6bb", "600");
AddText(scene, "average-value", averageText, "314", "27", "7", "#f8fafc", "800");
AddText(scene, "delta-label", "DELTA", "366", "16", "3.1", "#8fa6bb", "600");
AddText(scene, "delta-value", deltaText, "366", "27", "7", accent, "800");

scene.Child("svg-rect", "course-floor", node => node
    .Property("x", "18").Property("y", "111").Property("width", "404").Property("height", "2")
    .Property("fill", "#60758b").Property("opacity", "0.8"));
scene.Child("svg-rect", "start-post", node => node
    .Property("x", "58").Property("y", "62").Property("width", "2").Property("height", "49")
    .Property("fill", "#8fa6bb"));
scene.Child("svg-rect", "finish-post", node => node
    .Property("x", "330").Property("y", "62").Property("width", "2").Property("height", "49")
    .Property("fill", "#8fa6bb"));
scene.Child("svg-path", "finish-flag", node => node
    .Property("d", "M332 64 L350 69 L332 75 Z").Property("fill", accent));

var courseClass = state is "failed" or "passed" ? "course-active" : "course-still";
scene.Child("svg-g", "gauntlet-course", course => course
    .Property("class", courseClass)
    .Child("svg-g", "swinging-hammer", hammer => hammer
        .Property("class", "course-hammer").Property("transform", "translate(174 63)")
        .Child("svg-line", "hammer-shaft", line => line.Property("x1", "0").Property("y1", "0")
            .Property("x2", "0").Property("y2", "26").Property("stroke", "#9fb0c1").Property("stroke-width", "2"))
        .Child("svg-rect", "hammer-head", head => head.Property("x", "-9").Property("y", "23")
            .Property("width", "18").Property("height", "8").Property("rx", "2")
            .Property("fill", "url(#steel)").Property("stroke", "#e2e8f0").Property("stroke-width", "0.7")))
    .Child("svg-g", "spinning-gear", gear => gear
        .Property("class", "course-gear").Property("transform", "translate(235 91)")
        .Child("svg-circle", "gear-wheel", wheel => wheel.Property("cx", "0").Property("cy", "0")
            .Property("r", "8").Property("fill", "#26384b").Property("stroke", "#aab9c8").Property("stroke-width", "1.4"))
        .Child("svg-circle", "gear-hub", hub => hub.Property("cx", "0").Property("cy", "0")
            .Property("r", "2.4").Property("fill", "#e7b84b")))
    .Child("svg-path", "course-spikes", spikes => spikes
        .Property("d", "M266 111 L272 99 L278 111 L284 99 L290 111 L296 99 L302 111")
        .Property("fill", "none").Property("stroke", "#aab9c8").Property("stroke-width", "2")));

var knightClass = state switch
{
    "failed" => "knight-fail",
    "passed" => "knight-pass",
    _ => "knight-still"
};
scene.Child("svg-g", "knight", knight => knight
    .Property("class", knightClass).Property("transform", "translate(88 91)")
    .Child("svg-path", "knight-cape", cape => cape
        .Property("d", "M-7 -13 L-14 -8 L-11 9 L-4 5 Z").Property("fill", "#7c3444"))
    .Child("svg-rect", "knight-torso", torso => torso
        .Property("x", "-7").Property("y", "-14").Property("width", "14").Property("height", "19")
        .Property("rx", "3").Property("fill", "url(#steel)").Property("stroke", "#eef4fa").Property("stroke-width", "0.8"))
    .Child("svg-path", "knight-helmet", helmet => helmet
        .Property("d", "M-8 -16 Q-10 -30 0 -31 Q10 -30 8 -16 Z")
        .Property("fill", "url(#steel)").Property("stroke", "#f8fafc").Property("stroke-width", "0.9"))
    .Child("svg-line", "helmet-visor", visor => visor.Property("x1", "-6").Property("y1", "-21")
        .Property("x2", "6").Property("y2", "-21").Property("stroke", "#18283a").Property("stroke-width", "2"))
    .Child("svg-path", "helmet-plume", plume => plume.Property("d", "M0 -30 Q-2 -37 5 -39")
        .Property("fill", "none").Property("stroke", "#bd5365").Property("stroke-width", "2").Property("stroke-linecap", "round"))
    .Child("svg-g", "knight-arms", arms => arms
        .Property("class", state == "passed" ? "arms-cheer" : "arms")
        .Child("svg-line", "left-arm", line => line.Property("x1", "-6").Property("y1", "-10")
            .Property("x2", "-15").Property("y2", state == "passed" ? "-28" : "-2")
            .Property("stroke", "#dbe7f0").Property("stroke-width", "3").Property("stroke-linecap", "round"))
        .Child("svg-line", "right-arm", line => line.Property("x1", "6").Property("y1", "-10")
            .Property("x2", "15").Property("y2", state == "passed" ? "-28" : "-2")
            .Property("stroke", "#dbe7f0").Property("stroke-width", "3").Property("stroke-linecap", "round"))
        .Child("svg-circle", "left-hand", hand => hand.Property("cx", "-15").Property("cy", state == "passed" ? "-28" : "-2")
            .Property("r", "2.1").Property("fill", "#e7b84b"))
        .Child("svg-circle", "right-hand", hand => hand.Property("cx", "15").Property("cy", state == "passed" ? "-28" : "-2")
            .Property("r", "2.1").Property("fill", "#e7b84b")))
    .Child("svg-line", "left-leg", leg => leg.Property("x1", "-4").Property("y1", "4")
        .Property("x2", "-7").Property("y2", "15").Property("stroke", "#dbe7f0").Property("stroke-width", "3").Property("stroke-linecap", "round"))
    .Child("svg-line", "right-leg", leg => leg.Property("x1", "4").Property("y1", "4")
        .Property("x2", "7").Property("y2", "15").Property("stroke", "#dbe7f0").Property("stroke-width", "3").Property("stroke-linecap", "round"))
    .Child("svg-path", "knight-shield", shield => shield.Property("d", "M-13 -12 L-20 -14 L-20 -3 Q-18 3 -13 6 Q-8 3 -7 -3 L-7 -14 Z")
        .Property("fill", "#31516c").Property("stroke", "#e7b84b").Property("stroke-width", "0.8"))
    .Child("svg-path", "shield-emblem", emblem => emblem.Property("d", "M-14 -11 L-10 -11 L-12 -4 Z")
        .Property("fill", "#e7b84b")));

AddText(scene, "history", $"SURVIVED {survived}     FAILED {failed}", "16", "126", "4.1", "#cbd5e1", "700");
AddText(scene, "candidate", $"SHA {Shorten(candidate)}  ·  RUN {Shorten(runId)}", "236", "126", "3.4", "#8fa6bb", "600");

var svg = new SvgGuiRenderer().Render(scene.Build(), width: 1320, height: 396);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, svg);
Console.WriteLine($"Generated {state} badge for {candidate}: {output}");

static void AddText(GuiBuilder parent, string id, string text, string x, string y, string size, string fill, string weight) =>
    parent.Child("svg-text", id, node => node.Text(text)
        .Property("x", x).Property("y", y).Property("fill", fill)
        .Property("font-family", "Segoe UI,Arial,sans-serif")
        .Property("font-size", size).Property("font-weight", weight));

static Dictionary<string, string> ParseArguments(string[] values)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < values.Length; i++)
    {
        if (!values[i].StartsWith("--", StringComparison.Ordinal))
            continue;
        var key = values[i][2..];
        var value = i + 1 < values.Length && !values[i + 1].StartsWith("--", StringComparison.Ordinal)
            ? values[++i]
            : "true";
        result[key] = value;
    }
    return result;
}

static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback) =>
    values.TryGetValue(key, out var value) ? value : fallback;

static double? OptionalNumber(IReadOnlyDictionary<string, string> values, string key)
{
    if (!values.TryGetValue(key, out var value) || value == "—" ||
        !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ||
        !double.IsFinite(parsed) || parsed is < 0 or > 100)
        return null;
    return parsed;
}

static int OptionalInteger(IReadOnlyDictionary<string, string> values, string key, int fallback) =>
    values.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
        ? Math.Max(0, parsed)
        : fallback;

static string Shorten(string value) => value.Length <= 12 ? value : value[..12];
