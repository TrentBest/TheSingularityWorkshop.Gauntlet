# Warlords campaign integration

Gauntlet consumes Warlords vectors as **trusted, versioned data**. Experiences remains authoritative for game rules, command legality, event semantics, and expected outcomes.

## Current integration boundary

- Experiences owns the fixture corpus and the domain-level tests that prove its golden events and snapshots.
- Gauntlet owns campaign profiles, trusted probe definitions, findings, evaluation inputs, and sealed run reports.
- CiAi independently validates provider trust and exact source/run correlation.
- The Warlords parser in `src/Gauntlet.Core/Integrations/Warlords/WarlordsVectorManifest.cs` validates the v1 envelope and exposes expected event/snapshot payloads as opaque `JsonElement` data.
- The parser does not load arbitrary CLR types, resolve assembly names, execute scripts, or invoke candidate code.

## Supported v1 envelope

Each Experiences-owned vector declares:

- `id`, `schemaVersion`;
- `scenario.key` / `scenario.version` (`four-kingdoms` / `1`);
- `rulesVersion` (`warlords-four-kingdoms/1`);
- integer `seed`;
- ordered `commands` with contiguous 1-based `sequence`;
- each command's `kind` (`march` or `endTurn`) and `expected.result` (`accepted` or `rejected`);
- accepted march commands include their complete literal `expected.event`;
- `expectedFinalSnapshot` contains the complete authoritative final domain snapshot.

The parser deliberately does not reinterpret combat, turns, territory ownership, or snapshot contents. Those semantics remain in the Warlords domain and its golden-vector tests. A rejected command must not carry an accepted event.

## Campaign integration status

**Status:** v1 data-contract parser and malformed-input tests are implemented; campaign execution is not enabled.

This is not candidate execution and does not establish a campaign pass. The complete Experiences fixture corpus and pinned source/artifact acquisition are still required before a trusted-fixture campaign can be assembled. The eventual profile must pin the corpus digest and the exact candidate identity.

Candidate replay remains **blocked** until Gauntlet's host-owned isolation adapter is independently qualified. The current in-process `ProbeRunner` is for Gauntlet-owned trusted probes only; it is not a sandbox. Never execute candidate-controlled code in the orchestration job or this process.

## Next implementation steps

1. Bind the parser to the first complete Experiences-owned golden vector and test the full literal event/snapshot payloads.
2. Consume the actual Experiences-owned corpus at a pinned commit/artifact and verify its digest.
3. Map each vector to trusted campaign probe definitions without duplicating Warlords rules.
4. Build an evidence/report projection that remains incomplete until qualified host replay exists.
5. Keep CiAi as the independent evidence authority.
