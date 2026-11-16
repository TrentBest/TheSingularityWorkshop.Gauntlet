# Local Development

## Repository layout

The badge generator uses the real GUI.Core and GUI.SVG source projects. The current GUI source targets .NET 10, so install both the .NET 8 SDK (for Gauntlet.Core tests) and the .NET 10 SDK (for the GUI.SVG badge sample). Clone both repositories as siblings:

~~~text
workspace/
  TheSingularityWorkshop.Gauntlet/
  TheSingularityWorkshop.GUI/
~~~

~~~bash
git clone --branch development https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet.git
git clone --branch development https://github.com/TrentBest/TheSingularityWorkshop.GUI.git
cd TheSingularityWorkshop.Gauntlet
dotnet build samples/GauntletBadge/GauntletBadge.csproj --configuration Release
dotnet test tests/Gauntlet.Core.Tests/Gauntlet.Core.Tests.csproj --configuration Release
~~~

Render a preview state:

~~~bash
dotnet run --project samples/GauntletBadge/GauntletBadge.csproj --configuration Release -- --state untested --candidate-sha preview --run-id none --output artifacts/gauntlet-untested.svg
dotnet run --project samples/GauntletBadge/GauntletBadge.csproj --configuration Release -- --state failed --candidate-sha preview --run-id failed-fixture --score 62 --average 81 --delta -12 --survived 4 --failed 2 --output artifacts/gauntlet-failed.svg
dotnet run --project samples/GauntletBadge/GauntletBadge.csproj --configuration Release -- --state passed --candidate-sha preview --run-id passed-fixture --score 98 --average 84 --delta 14 --survived 5 --failed 2 --output artifacts/gauntlet-passed.svg
dotnet run --project samples/GauntletBadge/GauntletBadge.csproj --configuration Release -- --state blocked --candidate-sha preview --run-id blocked-fixture --output artifacts/gauntlet-blocked.svg
~~~

The GitHub Actions badge workflow checks out both repositories, records their exact commit SHAs, builds the same generator, renders all four fixtures, and validates the resulting SVG documents.

The checked-in preview fixtures are not live project verdicts. A production badge must be generated from a validated status projection tied to the exact candidate SHA and report identity.

## Probe runner boundary

The first probe runner executes Gauntlet-owned, trusted probe implementations sequentially in-process. It validates probe/profile identity, assertion bounds, cancellation, and elapsed duration after a probe returns. The elapsed-duration limit is cooperative and cannot preempt non-cooperative code.

This is not a sandbox. It does not enforce memory, filesystem, process, or network isolation and must not load or execute candidate-supplied code. Candidate execution remains blocked until a host-owned isolation adapter can enforce and attest those controls.

The harmless vulnerable fixture is pure in-memory logic: a reservation quantity validator incorrectly accepts a negative quantity. Its probe demonstrates a reproducible high-severity finding without filesystem, process, network, or privileged effects.
