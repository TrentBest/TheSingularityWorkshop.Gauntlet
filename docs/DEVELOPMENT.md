# Local Development

## Repository layout

The badge generator uses the real GUI.Core and GUI.SVG source projects. Clone both repositories as siblings:

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

## Current verification boundary

The source and workflow are authored, but the first successful CI run is still required to claim that the generator compiles and all badge fixtures pass. The badge sample is a presentation proof; it does not execute Gauntlet probes or provide evidence that a candidate survived.
