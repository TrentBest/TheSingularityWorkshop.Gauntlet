namespace TheSingularityWorkshop.Gauntlet;

/// <summary>
/// Deterministically evaluates a completed campaign against a trusted, immutable profile.
/// This class performs no I/O and does not execute probes.
/// </summary>
public static class CampaignEvaluator
{
    /// <summary>Evaluates a report against an independently supplied candidate identity and trusted campaign profile.</summary>
    /// <param name="profile">The immutable profile whose probe inventory and policy are authoritative.</param>
    /// <param name="expectedCandidate">The exact candidate identity expected by the caller.</param>
    /// <param name="report">The observed campaign report to evaluate.</param>
    /// <returns>A deterministic pass, fail, incomplete, or invalid result. Provider trust is evaluated separately.</returns>
    public static EvaluationResult Evaluate(
        CampaignProfile profile,
        CandidateIdentity expectedCandidate,
        RunReport report)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(expectedCandidate);
        ArgumentNullException.ThrowIfNull(report);

        var reasons = new List<string>();

        if (!ValidateProfile(profile, reasons))
            return Invalid(reasons);

        if (!RunReportIntegrity.Verify(report))
            reasons.Add("Run report digest is missing, malformed, or does not match its canonical content.");

        if (report.SchemaVersion != 1)
            reasons.Add("Unsupported run report schema version.");

        if (!Equals(report.Candidate, expectedCandidate))
            reasons.Add("Run report candidate identity does not match the expected repository, commit, and artifact.");

        if (!StringComparer.Ordinal.Equals(report.ProfileId, profile.Id) ||
            report.ProfileVersion != profile.Version ||
            !StringComparer.Ordinal.Equals(report.ProfileDigest, profile.Digest))
            reasons.Add("Run report campaign profile identity does not match the trusted profile.");

        if (string.IsNullOrWhiteSpace(report.GauntletVersion) ||
            string.IsNullOrWhiteSpace(report.EnvironmentId) ||
            string.IsNullOrWhiteSpace(report.RunId) ||
            report.Attempt < 1)
            reasons.Add("Run report is missing a required execution identity.");

        if (report.CompletedAt is { } completedAt && completedAt < report.StartedAt)
            reasons.Add("Run completion timestamp precedes its start timestamp.");

        if (!IsValidCandidate(expectedCandidate))
            reasons.Add("Expected candidate identity is malformed.");

        if (!Enum.IsDefined(report.CampaignState))
            reasons.Add("Run report contains an unknown campaign state.");

        if (report.StartedAt == default)
            reasons.Add("Run report is missing its start timestamp.");

        if (reasons.Count > 0)
            return Invalid(reasons);

        if (report.CampaignState != CampaignState.Completed || report.CompletedAt is null)
        {
            return new EvaluationResult(
                GateStatus.Incomplete,
                null,
                ["Campaign did not reach a valid completed state."],
                CollectFindings(report));
        }

        var definitions = profile.Probes.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var results = report.ProbeResults ?? Array.Empty<ProbeResult>();
        if (results.Any(result => result is null))
            reasons.Add("Run report contains a null probe result.");

        if (reasons.Count > 0)
            return Invalid(reasons);

        var duplicateProbe = results
            .GroupBy(x => x.ProbeId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateProbe is not null)
            reasons.Add($"Probe result '{duplicateProbe.Key}' appears more than once.");

        foreach (var result in results)
        {
            if (!definitions.TryGetValue(result.ProbeId, out var definition))
            {
                reasons.Add($"Run report contains undeclared probe '{result.ProbeId}'.");
                continue;
            }

            if (result.ProbeVersion != definition.Version)
                reasons.Add($"Probe '{result.ProbeId}' version does not match the trusted profile.");

            if (!Enum.IsDefined(result.Outcome))
                reasons.Add($"Probe '{result.ProbeId}' contains an unknown outcome.");

            if (result.Duration < TimeSpan.Zero ||
                result.PassedAssertions < 0 ||
                result.TotalAssertions < 0 ||
                result.PassedAssertions > result.TotalAssertions)
                reasons.Add($"Probe '{result.ProbeId}' contains invalid duration or assertion counts.");

            if (result.Outcome is ProbeOutcome.Passed or ProbeOutcome.Failed)
            {
                if (result.TotalAssertions == 0)
                    reasons.Add($"Probe '{result.ProbeId}' claims a conclusive outcome without assertions.");
                else if (result.Outcome == ProbeOutcome.Passed && result.PassedAssertions != result.TotalAssertions)
                    reasons.Add($"Probe '{result.ProbeId}' claims to pass but has failed assertions.");
                else if (result.Outcome == ProbeOutcome.Failed && result.PassedAssertions == result.TotalAssertions)
                    reasons.Add($"Probe '{result.ProbeId}' claims to fail but all assertions passed.");
            }

            if (result.Findings is null)
            {
                reasons.Add($"Probe '{result.ProbeId}' has a missing findings collection.");
                continue;
            }

            if (result.Findings.Any(finding => finding is null))
            {
                reasons.Add($"Probe '{result.ProbeId}' contains a null finding.");
                continue;
            }

            foreach (var finding in result.Findings)
            {
                if (string.IsNullOrWhiteSpace(finding.Id) ||
                    !StringComparer.Ordinal.Equals(finding.ProbeId, result.ProbeId) ||
                    string.IsNullOrWhiteSpace(finding.Summary))
                    reasons.Add($"Probe '{result.ProbeId}' contains an invalid finding identity or summary.");
            }
        }

        var duplicateFinding = results
            .Where(x => x.Findings is not null)
            .SelectMany(x => x.Findings)
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateFinding is not null)
            reasons.Add($"Finding ID '{duplicateFinding.Key}' appears more than once.");

        if (reasons.Count > 0)
            return Invalid(reasons);

        var resultById = results.ToDictionary(x => x.ProbeId, StringComparer.Ordinal);
        var missingOrIncomplete = new List<string>();

        foreach (var definition in profile.Probes.Where(x => x.Required))
        {
            if (!resultById.TryGetValue(definition.Id, out var result))
            {
                missingOrIncomplete.Add($"Required probe '{definition.Id}' is missing.");
                continue;
            }

            if (result.TotalAssertions == 0)
            {
                missingOrIncomplete.Add($"Required probe '{definition.Id}' produced no assertions.");
                continue;
            }

            if (result.Outcome is not (ProbeOutcome.Passed or ProbeOutcome.Failed))
                missingOrIncomplete.Add($"Required probe '{definition.Id}' did not reach a conclusive outcome.");

        }

        if (reasons.Count > 0)
            return Invalid(reasons);

        if (missingOrIncomplete.Count > 0)
        {
            return new EvaluationResult(
                GateStatus.Incomplete,
                null,
                missingOrIncomplete,
                CollectFindings(report));
        }

        var required = profile.Probes.Where(x => x.Required).ToArray();
        var totalWeight = required.Sum(x => x.Weight);
        var passedWeight = required.Sum(definition =>
        {
            var result = resultById[definition.Id];
            return definition.Weight * result.PassedAssertions / result.TotalAssertions;
        });
        var score = decimal.Round(100m * passedWeight / totalWeight, 2, MidpointRounding.AwayFromZero);

        var findings = CollectFindings(report);
        var blockingFindings = findings
            .Where(finding =>
                !Enum.IsDefined(finding.Severity) ||
                finding.Severity is FindingSeverity.Unknown or FindingSeverity.High or FindingSeverity.Critical ||
                profile.BlockingSeverities.Contains(finding.Severity))
            .ToArray();

        var failedRequired = required
            .Where(definition => resultById[definition.Id].Outcome == ProbeOutcome.Failed)
            .Select(definition => $"Required invariant probe '{definition.Id}' failed.")
            .ToArray();

        if (failedRequired.Length > 0 || blockingFindings.Length > 0)
        {
            reasons.AddRange(failedRequired);
            reasons.AddRange(blockingFindings.Select(finding =>
                $"Blocking {finding.Severity} finding '{finding.Id}' was reported."));
            return new EvaluationResult(GateStatus.Failed, score, reasons, findings);
        }

        return new EvaluationResult(
            GateStatus.Passed,
            score,
            ["All required probes completed and no blocking finding was reported."],
            findings);
    }

    private static bool ValidateProfile(CampaignProfile profile, List<string> reasons)
    {
        if (string.IsNullOrWhiteSpace(profile.Id) ||
            profile.Version < 1 ||
            !IsSha256Digest(profile.Digest) ||
            profile.Probes is null ||
            profile.BlockingSeverities is null)
        {
            reasons.Add("Campaign profile identity or collections are invalid.");
            return false;
        }

        if (profile.Probes.Any(probe => probe is null))
        {
            reasons.Add("Campaign profile contains a null probe definition.");
            return false;
        }

        if (profile.Probes.Count == 0 || !profile.Probes.Any(x => x.Required))
            reasons.Add("Campaign profile must declare at least one required probe.");

        foreach (var probe in profile.Probes)
        {
            if (string.IsNullOrWhiteSpace(probe.Id) || probe.Version < 1 || probe.Weight <= 0)
                reasons.Add("Campaign profile contains an invalid probe definition.");
        }

        var duplicate = profile.Probes
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            reasons.Add($"Campaign profile declares probe '{duplicate.Key}' more than once.");

        return reasons.Count == 0;
    }

    private static bool IsValidCandidate(CandidateIdentity candidate) =>
        !string.IsNullOrWhiteSpace(candidate.Repository) &&
        candidate.Repository.Contains('/') &&
        IsHexDigest(candidate.CommitSha, 40, 64) &&
        IsSha256Digest(candidate.ArtifactDigest) &&
        (candidate.ManifestDigest is null || IsSha256Digest(candidate.ManifestDigest));

    private static bool IsSha256Digest(string? value) =>
        value is not null &&
        value.StartsWith("sha256:", StringComparison.Ordinal) &&
        IsHexDigest(value[7..], 64, 64);

    private static bool IsHexDigest(string? value, int minimumLength, int maximumLength) =>
        value is not null &&
        value.Length >= minimumLength &&
        value.Length <= maximumLength &&
        value.All(Uri.IsHexDigit);

    private static IReadOnlyList<Finding> CollectFindings(RunReport report) =>
        (report.ProbeResults ?? Array.Empty<ProbeResult>())
        .Where(result => result.Findings is not null)
        .SelectMany(result => result.Findings)
        .OrderBy(finding => finding.Id, StringComparer.Ordinal)
        .ToArray();

    private static EvaluationResult Invalid(IEnumerable<string> reasons) =>
        new(GateStatus.Invalid, null, reasons.ToArray(), Array.Empty<Finding>());
}
