using TheSingularityWorkshop.Gauntlet;

namespace TheSingularityWorkshop.Gauntlet.Tests;

/// <summary>Demonstrates Gauntlet finding a harmless, deterministic input-validation defect.</summary>
internal sealed class HarmlessVulnerableFixtureProbe : IGauntletProbe
{
    public string Id => "reservation-quantity-positive";
    public int Version => 1;

    public ValueTask<ProbeExecutionObservation> ExecuteAsync(
        ProbeExecutionContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cases = new[]
        {
            (Quantity: 1, Expected: true),
            (Quantity: 0, Expected: false),
            (Quantity: -1, Expected: false)
        };

        var passed = 0;
        var findings = new List<Finding>();
        foreach (var testCase in cases)
        {
            var observed = HarmlessVulnerableFixture.IsValidReservationQuantity(testCase.Quantity);
            if (observed == testCase.Expected)
            {
                passed++;
                continue;
            }

            findings.Add(new Finding(
                $"negative-quantity-{Math.Abs(testCase.Quantity)}",
                Id,
                FindingSeverity.High,
                $"Expected quantity {testCase.Quantity} to be rejected, but it was accepted.",
                "fixture:reservation-quantity-negative"));
        }

        var outcome = passed == cases.Length ? ProbeOutcome.Passed : ProbeOutcome.Failed;
        return ValueTask.FromResult(new ProbeExecutionObservation(outcome, passed, cases.Length, findings));
    }
}
