namespace TheSingularityWorkshop.Gauntlet.Tests;

/// <summary>
/// A pure in-memory fixture with an intentional validation defect. It performs
/// no filesystem, process, network, or privileged operation.
/// </summary>
internal static class HarmlessVulnerableFixture
{
    /// <summary>
    /// Accepts positive quantities as intended, but intentionally also accepts
    /// negative quantities. This models a missing lower-bound invariant only.
    /// </summary>
    public static bool IsValidReservationQuantity(int quantity) => quantity != 0;
}
