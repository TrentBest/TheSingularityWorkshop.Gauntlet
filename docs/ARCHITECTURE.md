# Gauntlet Architecture

**Status:** initial architecture contract. This document distinguishes current repository state from intended behavior. A design statement is not implementation evidence.

## 1. Purpose

Gauntlet is an adversarial verification system for a precisely identified software candidate, especially a MicroBundle or Experience intended to enter a release process.

It attempts to discover contract violations by supplying hostile-but-contained inputs, exercising boundary conditions, challenging invariants, and replaying known defects. It returns durable evidence and actionable findings.

Gauntlet does not own composition, CI execution authority, release authorization, or unrestricted security testing.

## 2. System boundaries

| Component | Owns | Does not own |
|---|---|---|
| FSM_API | State/context primitives and FSM behavior | Campaign policy or release verdicts |
| FSM_COS | MicroBundle dependency resolution, composition, and RuntimeAssembly | Adversarial probe execution or release authorization |
| Gauntlet.Core | Campaign profiles, probes, invariants, findings, score inputs, run reports, receipt wire contract, signature-payload construction | CI provider trust, host credentials, merge/release permissions |
| Gauntlet.Security | ES256 signature verification against an injected host trust store | Private-key custody, trust-store provisioning/distribution, host isolation, merge/release permissions |
| CI host | Build/test execution, runner isolation, process and resource lifecycle | Gauntlet's domain verdict |
| CiAi | Exact-run observation, evidence correlation, trust validation, gate policy, reporting | Probe implementation or implicit permission escalation |
| HeadlessAi | Provider-neutral model invocation and response normalization | Trust decisions, gate overrides, repository-write authorization |
| Foundry | Bounded task/repair lifecycle, coordination, human handoff | Independent authority to publish or merge |
| GUI.SVG | GUI semantic-tree-to-SVG serialization | Campaign state, scoring, or live test execution |

Gauntlet should remain independently understandable and runnable. Add dependencies only when a concrete contract requires them. Do not reference FSM_COS merely to gain access to MicroBundle composition: a host adapter should supply the candidate and its declared context.

## 3. Candidate identity

A campaign is meaningful only when its target is unambiguous. At minimum, bind a run to:

- canonical repository identity;
- full candidate commit SHA;
- candidate artifact digest and, where applicable, MicroBundle/Experience manifest digest;
- campaign profile ID and immutable profile version/digest;
- Gauntlet implementation version;
- runner/environment identity and relevant platform/runtime facts;
- run ID and attempt number;
- start/completion timestamps;
- complete probe inventory and exact outcomes.

Do not infer tested source from a mutable branch name, a caller-provided display label, or a badge URL. A report for one SHA cannot satisfy another SHA.

## 4. Campaign lifecycle

The intended lifecycle is:

1. **Resolve** the exact candidate and immutable campaign profile.
2. **Validate** scope, supported capabilities, expected invariants, and resource limits.
3. **Provision** an isolated execution environment owned by the host.
4. **Execute** deterministic probes within the declared limits.
5. **Observe** process/test outcomes and collect bounded diagnostics.
6. **Evaluate** required invariants, probe completeness, and policy.
7. **Persist** an immutable run report and digest.
8. **Publish** a read-only report/status projection for CiAi and presentation clients.
9. **Replay** any defect as a regression fixture in a later campaign.

A run may be cancelled, timed out, or resource-limited. Those are explicit terminal outcomes, not success.

## 5. Probe model

Each probe declares:

- stable probe ID and version;
- purpose and threat/contract category;
- input generator or fixture identity;
- required capability and target scope;
- preconditions and expected invariants;
- execution time, memory, output, and iteration limits;
- cleanup behavior;
- severity and gate relevance;
- deterministic seed or replay identity where randomness is used.

Initial probe families:

- empty, null, malformed, oversized, duplicate, reordered, and unexpected inputs;
- minimum/maximum and off-by-one boundaries;
- property-based and mutation-oriented contract checks;
- invalid state transitions and lifecycle misuse;
- serialization/deserialization and round-trip failures;
- dependency, version, and manifest mismatches;
- resource exhaustion attempts constrained to strict quotas;
- replay of previously discovered defects.

Do not perform uncontrolled production-target scanning, credential harvesting, destructive operations, arbitrary network egress, or unbounded resource consumption. The first proof target is a deliberately vulnerable, harmless fixture.

### Current execution boundary

`ProbeRunner` currently executes only Gauntlet-owned `IGauntletProbe` implementations sequentially in-process. It checks profile identity, duplicate IDs, assertion bounds, cancellation, and elapsed duration after cooperative completion. It does not provide process isolation, hard preemption, memory quotas, filesystem restrictions, or network controls.

The first vulnerable target is a pure in-memory fixture with an intentional negative-quantity validation defect. This proves probe-to-finding-to-evaluator behavior only. It is not evidence that arbitrary candidate code can safely be executed.

Candidate-supplied code remains out of scope until a host-owned isolation adapter can enforce and attest process, filesystem, network, CPU, memory, time, artifact, and cleanup boundaries. If those controls cannot be established, execution must fail closed.

## 6. Findings and severity

A finding records the exact probe, candidate identity, observed behavior, expected invariant, bounded reproduction data, severity, and regression-fixture identity when available.

Severity is policy input, not decoration. A critical or high finding can fail a campaign regardless of its aggregate score. Medium/low handling is profile-defined and versioned. Unknown severity or missing required diagnostics must not silently become informational.

Model-generated explanations may help a developer understand a finding, but the authoritative facts remain the probe result, captured evidence, and deterministic evaluator.

## 7. Score and verdict are different

The score is a measurement of completed, comparable campaign assertions. The verdict is a policy decision.

For a complete campaign, the initial score definition is a weighted pass ratio:

score = 100 × passed assertion weight / total required assertion weight

Weights are fixed by the immutable campaign profile. An assertion that was not executed is not a pass. An incomplete campaign has no final score and cannot pass.

The verdict is failed if any required invariant fails or a finding meets a profile's blocking severity threshold. A campaign passes only when all required probes complete, all required invariants pass, and no blocking finding remains. Invalid, stale, untrusted, or incomplete evidence is non-passing.

History metrics are computed only across comparable profile/version cohorts:

- average score across completed comparable runs;
- score delta against the immediately preceding comparable run;
- total completed campaigns;
- survived campaigns (passed);
- campaigns that failed the gate;
- findings by severity.

A later green run never overwrites an earlier failure. Averages must disclose their cohort and sample count. Do not combine different profiles or incompatible policy versions into one apparently meaningful average.

## 8. CI and CiAi integration

Gauntlet produces a report; CiAi independently validates it. CiAi must verify repository, candidate SHA, artifact/manifest digest, run/attempt identity, profile identity, report digest, completeness, and provider trust before treating the report as actionable evidence.

Start with read-only/advisory reporting. Only a separately reviewed policy decision may make Gauntlet a required check. No green result grants permission to merge, publish, release, or deploy.

The integration reuses CiAi's existing exact-run evidence and feedback lane. Do not add a competing callback, authorization ledger, or evidence schema when the existing contracts can be extended.

## 9. Badge as a projection

The badge is generated from the same validated status model used by reports. It is not a second state machine and never serves as proof by itself.

- Untested: no valid completed campaign for the exact candidate.
- Failed: the exact candidate has a valid completed failing report.
- Passed: the exact candidate has a valid completed passing report.
- Blocked/unknown: stale, invalid, untrusted, incomplete, or ambiguous evidence.

A badge must expose or link to the report identity and candidate SHA. Unknown states never become green.

GUI.SVG serializes the authored semantic GUI tree. It does not own the campaign clock or execute FSM_API. SVG animation is declarative presentation interpreted by the viewer.

## 10. Security and operations

The host owns sandbox creation and destruction, process isolation, filesystem permissions, network policy, secret injection, and resource quotas. The runner must default to no credentials and no network egress. Probe code and candidate code are untrusted.

Persist bounded logs and artifacts with size limits. Redact secrets before evidence leaves the host. Fail closed if isolation, cleanup, evidence capture, or provenance validation cannot be established.

The version-1 receipt uses ES256 (ECDSA P-256/SHA-256, P1363) with a strict 64-byte base64url wire value. `Gauntlet.Core` builds the exact canonical, domain-separated signature payload; `Gauntlet.Security` verifies it against an injected trust store with exact key/provider/host binding, key validity, and revocation checks. The trust store and provider key lifecycle are host/operator-owned and are not implemented by Gauntlet yet. See [HOST_SIGNATURE_PROFILE.md](HOST_SIGNATURE_PROFILE.md).

## 11. Non-goals

- Replacing ordinary unit, integration, compatibility, or performance tests.
- Claiming that finite probes prove software is defect-free or secure.
- Scanning arbitrary third-party production systems.
- Running arbitrary candidate code with host privileges.
- Letting a model decide whether evidence is trusted.
- Automatically merging, publishing, releasing, or deploying a candidate.
