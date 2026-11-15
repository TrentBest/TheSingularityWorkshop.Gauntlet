# Gauntlet in the Autonomous Development Pipeline

## Principle

Autonomous development should not mean autonomous trust. A model may discover a defect, explain evidence, and propose a repair; deterministic systems decide whether the candidate satisfies its declared contracts, and existing human authorization remains authoritative for privileged repository and release actions.

Gauntlet is the adversarial verification participant between candidate construction and evidence acceptance. It is not the pipeline controller.

## Proposed pipeline

~~~text
Candidate change
      |
      v
CiAi identifies exact repository + commit
      |
      v
Build / ordinary tests / compatibility / performance
      |
      v
FSM_COS candidate composition (where the target is a MicroBundle or Experience)
      |
      v
Gauntlet campaign
  | probes | invariants | bounded abuse cases | regression replay |
      |
      v
Immutable report + report digest + exact artifact provenance
      |
      v
CiAi independently validates provider and candidate binding
      |
      +--------------------------+
      |                          |
   Evidence rejected         Evidence accepted
      |                          |
  Fail closed                   v
                        Gate policy evaluation
                                 |
                      +----------+----------+
                      |                     |
                   Failed                 Passed
                      |                     |
                      v                     v
             HeadlessAi assessment    Candidate is eligible
             of pinned findings       for next gate only
                      |
                      v
             Foundry bounded repair
             on isolated work branch
                      |
                      v
             Rebuild + ordinary tests
                      |
                      v
             Re-run same Gauntlet profile
                      |
                      v
             Compare score, findings,
             and regression history
                      |
                      +------> CiAi
                                 |
                                 v
                         Human review / approval
                                 |
                                 v
                         Merge / publish / release
~~~

## Responsibility map

| Participant | Responsibility | Trust boundary |
|---|---|---|
| FSM_API | State/context primitives for explicit lifecycle orchestration | Does not make evidence or release decisions |
| FSM_COS | Compose the target MicroBundle/Experience in its declared runtime context | Does not grant the candidate elevated host privileges |
| Gauntlet | Run bounded adversarial probes and produce deterministic findings | Candidate and probe code are untrusted |
| CI host | Build, provision sandbox, enforce CPU/memory/time/filesystem/network limits | Secrets and host privileges never flow to candidate code |
| CiAi | Observe CI, validate provenance, correlate exact evidence, evaluate policy | Unknown, stale, mismatched, or incomplete evidence fails closed |
| HeadlessAi | Explain pinned findings and propose candidate patches | Model output is advisory and untrusted |
| Foundry | Execute bounded repair tasks, track attempts, preserve human handoff | Cannot bypass CiAi gates or authorize publication |
| Human | Review privileged changes and authorize merge/release/publication | Final authority remains outside the model loop |

## Gate sequence

### Gate 0 — Candidate identity

Resolve the immutable commit SHA and exact artifact/manifest digest. A mutable branch name is not a candidate identity.

### Gate 1 — Build and ordinary verification

Compile the candidate, run its normal tests, and apply the repository's existing compatibility and performance requirements. Gauntlet does not replace these checks.

### Gate 2 — Adversarial campaign

Select an immutable profile. A quick profile may run on every change; a deeper profile may run on a merge queue, scheduled campaign, or explicit release candidate. Profile identity and version must be displayed with every score.

### Gate 3 — Evidence integrity

Seal the report and bind it to repository, commit, artifact, manifest, profile, runner environment, attempt, and report digest. CiAi independently checks the binding before it trusts the report.

### Gate 4 — Policy evaluation

A high score cannot conceal a failed required invariant or critical/high finding. Missing required probes, invalid evidence, and stale results cannot pass. The first CiAi integration should be advisory; a required status check is a separate adoption step.

### Gate 5 — Bounded autonomous repair

When a campaign fails:
1. Pin the finding and its reproduction fixture.
2. Ask HeadlessAi to explain the evidence and propose a minimal repair.
3. Have Foundry apply the proposal only in an isolated, explicitly scoped work area.
4. Rebuild and rerun ordinary tests.
5. Rerun the same immutable Gauntlet profile against the new exact candidate.
6. Compare the new run with its predecessor and retain both reports.
7. Stop after a bounded number of attempts or when confidence/diagnostics are insufficient; request human review.

A model-generated patch is never evidence that the defect is fixed. Only a new completed campaign and the rest of the required checks can demonstrate that.

## Campaign tiers

The profile is the unit of comparison. Do not mix scores from different profiles into one average.

- **Smoke:** fast deterministic boundary checks for frequent CI feedback.
- **Standard:** broader contract and misuse campaign for pull requests.
- **Deep:** property-based, mutation-oriented, and expanded regression campaigns on a scheduled or explicit run.
- **Release candidate:** immutable, fully declared profile with required evidence completeness and stricter severity policy.

Each profile has its own average, delta, sample count, and survived/failed totals.

## What the developer receives

A failed run should return more than a red X:

- exact candidate SHA and artifact identity;
- campaign profile and version;
- failed probe and invariant;
- expected behavior versus observed behavior;
- severity and why it blocks;
- bounded reproduction input or fixture;
- logs/artifact references with secret redaction;
- a suggested next diagnostic step;
- regression fixture identity when one has been captured.

The developer can embed a reusable Gauntlet workflow in their CI. CiAi can then consume the report as a provider and publish the authoritative gate status. The badge is a compact status projection that links to the full report.

## Metrics without vanity scoring

- **Run score:** weighted assertion pass ratio for one completed campaign.
- **Average:** mean score for the same profile/version cohort only.
- **Delta:** latest comparable score minus the previous comparable run.
- **Survived:** completed attempts that passed.
- **Failed:** completed attempts that failed.
- **Incomplete:** attempts without a valid completed score.

Retries remain separate attempts. Never overwrite a failed report with a later pass. Display sample count and profile identity next to averages. The score is diagnostic; required invariants and severity policy decide the verdict.

## Safety and authorization

The first implementation runs against harmless, deliberately vulnerable fixtures. Real candidate execution requires a verified isolated runner with strict quotas, no credentials by default, no uncontrolled network egress, bounded artifacts, and reliable cleanup.

No autonomous repair loop may merge, publish, release, deploy, delete protected branches, or weaken required gates. A Gauntlet pass is one evidence input, not release authorization.

## Current implementation boundary

The repository currently contains:
- initial versioned campaign/run/finding contracts;
- a deterministic campaign evaluator with exact candidate/profile matching, required-probe completeness, weighted scoring, and fail-closed severity handling;
- canonical report digest sealing and verification;
- comparable history aggregation for average, delta, survived, failed, and incomplete counts;
- a GUI.SVG-backed animated badge generator with four validated state fixtures;
- a deterministic, sequential in-process runner for Gauntlet-owned trusted probes;
- runner checks for profile membership, version identity, duplicate IDs, assertion bounds, cancellation, and elapsed duration;
- a harmless in-memory vulnerable fixture whose negative-quantity defect is detected as a high-severity finding and rejected by the campaign evaluator.

The runner is not a sandbox. It cannot preempt non-cooperative code or enforce process, memory, filesystem, or network isolation. It does not execute candidate-supplied code.

The next milestone is to implement and validate a host-owned isolation adapter against the requirements in [HOST_ISOLATION_CONTRACT.md](HOST_ISOLATION_CONTRACT.md) and the proposed [HOST_ADAPTER_DECISION.md](HOST_ADAPTER_DECISION.md). GitHub Actions remains orchestration and report transport; candidate-controlled execution belongs only in a separately provisioned, disposable, host-attested environment. That contract is a design specification, not evidence that any host currently enforces those controls. The adapter must pass host-level adversarial integration tests and produce a verifiable execution receipt before candidate-controlled execution is enabled. The CiAi provider adapter follows that milestone; neither adapter is complete.
