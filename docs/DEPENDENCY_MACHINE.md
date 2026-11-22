# Dependency Machine — staged campaigns and badge projection

**Status:** design baseline; implementation is intentionally staged.  
**Related:** [Gauntlet issue #4](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/issues/4), [Badge Specification](BADGE_SPEC.md), [Autonomous Development Pipeline](AUTONOMOUS_DEVELOPMENT_PIPELINE.md).

## The machine

Evolve the badge from one knight facing one generic course into a deterministic, multi-stage mechanical proving machine. Each dependency is a named stage in an explicit dependency graph. The knight's journey visualizes one exact pipeline attempt through those stages.

Use the broad visual language of an elaborate medieval obstacle course: pendulum axes, rotating drums, swinging hammers, counterweights, trap floors, crushers, gates, and recovery platforms. Artwork and choreography must be original; do not reproduce a film frame, character, costume, actor likeness, or logo.

The complexity and number of stages communicate the size and shape of a package's dependency surface. Large graphs may be grouped into regions with a detailed per-stage report, but required dependencies must never be silently omitted.

## Ownership and authority

| Concern | Owner |
|---|---|
| Dependency graph, topological scheduling, parallel branches, downstream eligibility | CiAi |
| Candidate build and ordinary tests | CI host orchestrated by CiAi |
| Adversarial campaign definitions, probes, findings, run reports | Gauntlet |
| Independent evidence/provenance validation and gate policy | CiAi |
| Stage status projection and deterministic scene mapping | Gauntlet contracts + GUI.SVG renderer |
| Human approval for privileged repository/release actions | Human / existing host adapter |

The badge is a read-only projection. It never runs tests, polls a process, makes a gate decision, or authorizes a downstream stage. Animation is driven only by a validated, immutable pipeline snapshot.

## Pipeline semantics

Every dependency stage carries enough identity to prevent a green result drifting to a different candidate:

- Stable stage ID and repository/package identity.
- Exact source commit SHA.
- Exact artifact digest (and manifest digest when applicable).
- Declared upstream stage IDs.
- Required campaign profile ID and version.
- Gauntlet run ID and report digest.
- Evidence provider identity and trust/validation outcome.
- Attempt identity and snapshot version.

A mutable branch name is a locator, never candidate identity. A stage result is stale when any required source, artifact, profile, attempt, or graph identity no longer matches the pipeline snapshot. Digests use the existing Gauntlet canonical form `sha256:` followed by 64 lowercase hexadecimal characters.

### Stage states

| State | Meaning | Machine behavior |
|---|---|---|
| Not yet run | Eligible, but no campaign attempt has started | Knight waits at stage entrance; mechanism idle |
| Running | A current attempt is in progress | Stage operates; knight approaches or enters the mechanism |
| Passed | Required evidence for this exact candidate is validated and policy passed | Mechanism safely releases the knight |
| Failed | A validated campaign or required gate failed | Stage-specific consequence; descendants become blocked |
| Blocked | One or more required upstream stages did not pass | Gate locks; knight waits before the stage |
| Incomplete | Required evidence is missing or the attempt did not complete | Mechanism pauses; never show success |
| Invalid | Evidence is malformed, inconsistent, or mismatched | Warning state; do not animate a pass/fail outcome |
| Untrusted | Provider, signature, receipt, or trust chain is not accepted | Locked stage with explicit trust warning |

Not-yet-run is distinct from blocked: the former is eligible and waiting for scheduling; the latter is ineligible because an upstream prerequisite is unsatisfied. A stage is not passed merely because its build succeeded. It must satisfy the declared Gauntlet campaign and independent CiAi evidence validation.

### Dependency scheduling

- CiAi consumes an explicit directed acyclic graph (DAG); display order is not dependency order.
- Independent branches may run in parallel.
- A join stage becomes eligible only when every required incoming dependency has passed.
- A failed stage blocks its descendants, but does not retroactively fail unrelated branches.
- A retry is a new attempt with its own immutable evidence. Never overwrite an earlier failed report with a later pass.
- The graph snapshot and digest are pinned to the pipeline attempt. A graph edit creates a new snapshot; it cannot silently rewrite an existing run.
- Dependency artifacts are pinned by digest. Downstream builds cannot consume a moving branch/package version while the dependency gate is unresolved.

## Mechanical language

Stage-to-apparatus assignment is stable by stage identity, not random and not dependent on current status. Status changes choreography, not which machine component represents the dependency.

| Apparatus archetype | Possible stage role | Failure presentation |
|---|---|---|
| Pendulum axe | Foundational/core dependency | Knight knocked back; shield or helmet tumbles |
| Rotating drum / gear | Runtime or serialization dependency | Knight spun off course |
| Swinging hammer | UI/framework adapter dependency | Knight struck sideways |
| Pressure plate / crusher | Integration or resource-boundary dependency | Knight briefly flattened into a harmless stylized silhouette |
| Trap floor / pit | Test/compatibility dependency | Floor opens; knight drops to a lower recovery path |
| Locked portcullis | Blocked downstream stage | Knight waits; no false failure animation |
| Final drawbridge / bell | Aggregate pipeline completion | Opens only when all required stages have accepted evidence |

These are expressive metaphors, not literal descriptions of defects. The linked report supplies the actual failed probe, expected/observed behavior, severity, and provenance.

## Badge composition

The compact badge should show overall pipeline state, knight position/progress, stable stage names or concise labels, stage-level status, exact pipeline attempt/snapshot identity, and a link to the detailed report.

For large graphs, group stages by dependency tier or named subsystem. Keep a textual stage list available so the badge remains understandable without animation or color.

Overall state is an aggregation, not a replacement for stage states:
- Failed if a required stage has a validated blocking failure.
- Blocked if no blocking failure is established but a required prerequisite is not passed.
- Incomplete, invalid, or untrusted when evidence quality prevents a verdict.
- Passed only when every required stage is passed for the same graph snapshot and compatible pipeline attempt.
- Not yet run when the pipeline has not started.

The version-1 projection contract uses this deterministic precedence when required stages have simultaneous states: **Invalid → Untrusted → Incomplete → Failed → Blocked → Running → Not yet run → Passed**. Evidence-quality states outrank operational outcomes, so a red failure cannot disguise invalid or untrusted evidence. Unknown enum values, malformed digests, duplicate/missing graph identities, and cycles fail closed as Invalid.

## Animation and accessibility

- Use GUI semantic scene construction and GUI.SVG; preserve reproducible rendering.
- Status-to-scene mapping must be deterministic and covered by tests.
- Use declarative local SVG/CSS animation only; no scripts, remote resources, polling, or embedded execution.
- Provide reduced-motion and static alternatives.
- Accessible descriptions state actual status and stage names, not just color or motion.
- Failure choreography is a theatrical replay of a recorded result, not an active exploit attempt.
- Never animate an untrusted or invalid result as though it were a confirmed failure or success.

## Implementation sequence

1. **Implemented in PR #5:** version-1 graph/stage contracts, candidate/evidence identity checks, DAG validation, deterministic topological order, prerequisite eligibility, and fail-closed aggregate projection.
2. Expand tests for retries, stale graph snapshots, optional stages, malformed evidence, and every mixed-state precedence combination; CiAi remains responsible for cryptographic/provider trust and digest recomputation.
3. **Implemented in PR #5:** deterministic tier/row layout and a snapshot-driven multi-stage GUI.SVG storyboard with dependency tracks, distinct mechanical archetypes, stage labels, a single advancing knight marker, a knocked-down failure pose with a separated helmet, aggregate gate, and a preview-only fixture watermark.
4. **Implemented in PR #5:** declarative per-stage pendulum, gear, crusher, and trap choreography; knight advance/celebration/failure motion; helmet scatter; and a `prefers-reduced-motion` static fallback.
5. Connect a read-only CiAi projection only after CiAi independently validates each stage's source, artifact, profile, report, provider, and attempt binding.
6. Enable downstream scheduling only through CiAi's separately tested policy gates; never from badge output.

## Explicit non-goals

- The badge does not become a scheduler or evidence authority.
- Gauntlet's trusted in-process probe runner does not execute candidate-controlled code.
- A high aggregate score cannot hide a failed required invariant or required dependency.
- No package publication, release, merge, deployment, protected-branch mutation, or branch deletion is authorized by this design.
