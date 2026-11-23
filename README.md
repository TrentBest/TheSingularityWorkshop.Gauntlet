<!-- The Singularity Workshop moniker -->
<p align="center">
  <a href="https://github.com/TrentBest">
    <img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Renderer/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" />
  </a>
</p>

# The Singularity Workshop — Gauntlet

[![Sith Policy](assets/sith-policy.svg)](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/workflows/sith-policy.yml)
[![GUI.SVG badge contracts](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/workflows/badge.yml/badge.svg?branch=development)](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/workflows/badge.yml)
[![Core contracts](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/workflows/core.yml/badge.svg?branch=development)](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/workflows/core.yml)

**Release readiness: NOT READY — owner review and explicit publication approval required.**

The authoritative working release checklist is [docs/RELEASE_READINESS.md](docs/RELEASE_READINESS.md). It records implementation gates, required evidence, known blockers, exact-candidate verification, and the final owner approval boundary. Keep it current as work progresses.

**Software should earn its release.** Gauntlet is the Workshop's adversarial proving ground for MicroBundles and Experiences: it challenges a precisely identified candidate with bounded, reproducible probes, records what breaks, and returns evidence that the release pipeline can independently verify.

Gauntlet is not a vulnerability scanner that blindly attacks arbitrary targets, a replacement for ordinary tests, or an authorization service. It is a controlled verification campaign with explicit scope, limits, expected invariants, and replayable results.

## 🟦 01 — What

A candidate enters with an exact source revision and artifact identity. A versioned campaign exercises its declared contracts and adversarial boundaries: malformed and unexpected inputs, invalid lifecycle transitions, serialization edges, dependency mismatches, property-based cases, and regression fixtures.

Every completed run produces a durable report containing its probe inventory, outcomes, findings, severities, score inputs, and provenance. A missing or incomplete required probe is not a pass.

Start with [Gauntlet issue #1](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/issues/1) and the [architecture](docs/ARCHITECTURE.md).

## 🟣 02 — Why

A successful build proves that a particular build completed. A passing unit-test suite proves that its selected assertions passed. Neither alone demonstrates that a candidate has survived a deliberate campaign against malformed inputs, boundary conditions, misuse, and hostile-but-contained data.

Gauntlet makes that challenge explicit and repeatable. It preserves failures as regression evidence and gives developers actionable reports instead of hiding the details behind one green or red check.

The score communicates measured campaign performance; it never overrides a critical finding, a failed required invariant, or missing evidence.

## 🩵 03 — How

```text
Exact candidate + artifact/manifest digest
                 |
                 v
        Versioned campaign profile
                 |
                 v
     Bounded, isolated adversarial probes
                 |
                 v
      Findings + invariant outcomes
                 |
                 v
   Provenance-bound, immutable run report
                 |
                 v
       CiAi validates and evaluates
                 |
          +------+------+
          |             |
        Failed         Passed
          |             |
     Repair evidence   Eligible evidence
          |             |
          +------> bounded development loop
```

The ownership boundaries are deliberate:

- **FSM_API** supplies state/context primitives where stateful orchestration needs them.
- **FSM_COS** composes MicroBundles; it does not own the Gauntlet campaign.
- **Gauntlet** owns probes, findings, campaign scoring, and replayable run records.
- **CiAi** independently validates exact-source evidence and evaluates gate policy.
- **HeadlessAi** may assess pinned findings; model output cannot change a verdict or authorize an action.
- **Foundry** coordinates bounded repair work and human handoff.
- **GUI.SVG** generates the animated badge from the same status model; the badge is presentation, not evidence.

See [Architecture](docs/ARCHITECTURE.md), [Run Contract](docs/RUN_CONTRACT.md), [SURPS Conformance Profile v1](docs/SURPS_CONFORMANCE_PROFILE_V1.md) ([schema](docs/schemas/surps-conformance-profile-v1.schema.json)), [Autonomous Development Pipeline](docs/AUTONOMOUS_DEVELOPMENT_PIPELINE.md), and the [local development guide](docs/DEVELOPMENT.md).

## 🟢 04 — See it in a minute

The first executable milestone is a deliberately vulnerable but harmless fixture. Gauntlet will challenge it, report deterministic findings, replay a repaired fixture, and demonstrate both a failed and a survived run.

No real MicroBundle or Experience is treated as a proving target until the runner's isolation, resource limits, provenance, and cleanup behavior have been verified.

The animated badge will have four truthful presentations:

- **Yellow — untested:** no valid completed campaign exists for this candidate; the apparatus is still.
- **Red — failed:** the exact candidate failed; the knight repeatedly meets the apparatus and is defeated.
- **Green — passed:** the exact candidate passed; the knight crosses the course, celebrates with both hands raised, and loops.
- **Neutral — blocked/unknown:** evidence is stale, invalid, incomplete, or untrusted. Never render this state as green.

See the [Badge Specification](docs/BADGE_SPEC.md).

## 🟪 05 — Build the proving ground

This repository is being established. The implementation sequence is:

1. Initial versioned campaign/run/finding contracts and deterministic evaluator are implemented in src/Gauntlet.Core.
2. Weighted scoring and profile-comparable history metrics are implemented and under contract testing.
3. GUI.SVG badge generation and four state fixtures are CI-verified.
4. Canonical report digest and integrity validation are implemented in RunReportIntegrity.
5. Deterministic probe interface and isolated runner.
6. Deliberately vulnerable fixture and replay tests.
7. Read-only/advisory CiAi provider integration.
8. Evidence-backed adoption by selected MicroBundles and Experiences.

Track the work in [Gauntlet #1](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/issues/1) and the coordinated [CiAi integration issue #71](https://github.com/TrentBest/TheSingularityWorkshop.CiAi/issues/71). Release completion is governed by the [living release checklist](docs/RELEASE_READINESS.md).

No merge, publication, release, deployment, protected-branch mutation, or authorization-gate weakening is implied by a passing campaign. Existing human approval remains authoritative.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
