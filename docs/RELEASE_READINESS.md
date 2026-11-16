# Gauntlet Release Readiness Checklist

**Purpose:** This is the living, repository-owned checklist for deciding whether Gauntlet is ready to be presented for the owner's final review and explicit authorization to publish. Keep this file current as implementation and evidence change.

**Current release state:** `NOT READY`  
**Final owner review:** `NOT REQUESTED`  
**Publication authorization:** `NOT GRANTED`  
**Last reviewed:** 2026-11-16

> [!IMPORTANT]
> Completing this checklist does not authorize a merge, NuGet publication, GitHub release, deployment, or any other privileged action. The owner must review the exact release candidate and explicitly authorize publication. No automation, passing test, badge, score, or agent may infer that authorization.

## How to use this checklist

- Keep this file on `development` as the working source of truth.
- Mark an item `[x]` only when the implementation exists **and** the stated evidence has been inspected. A design document or intended workflow is not implementation evidence.
- For every completed engineering gate, add links to the exact commit, workflow run, test report, artifact, or other durable evidence in the evidence register below.
- If evidence becomes stale because the relevant code, dependencies, workflow, profile, or policy changes, reopen the affected item.
- Use `[~]` only for work actively in progress; use `[!]` for blocked or explicitly deferred items. Explain why.
- Do not collapse incomplete, blocked, invalid, or untrusted evidence into a pass.
- The release candidate is an immutable commit and package artifact. Re-run applicable gates against that exact candidate; do not rely on a branch name or a prior run.
- Keep the repository branch policy at exactly `master` and `development`. Do not delete branches as part of this checklist without explicit authorization.

## Gate 0 — Scope, ownership, and release boundary

- [x] Repository purpose and non-goals are documented.
- [x] Gauntlet, CI host, CiAi, HeadlessAi, Foundry, FSM_COS, and GUI.SVG ownership boundaries are documented.
- [x] Gauntlet is an evidence provider; CiAi independently validates evidence and owns final gate policy.
- [x] A Gauntlet pass, score, report, or badge cannot authorize merge, publication, release, or deployment.
- [ ] Confirm the intended first public release scope: core contracts/evaluator only, or also a qualified execution/provider capability. Record explicit exclusions.
- [ ] Confirm supported consumer/runtime target frameworks and compatibility policy for the first stable package.
- [ ] Confirm public API namespace, package identity, semantic-versioning policy, and whether the first public version is stable or prerelease.
- [ ] Confirm ownership and maintenance expectations for campaign profiles, probe packs, and host adapters.

**Exit condition:** The public surface and what the release does *not* claim are unambiguous.

## Gate 1 — Core contracts and deterministic evaluation

- [x] Versioned campaign, profile, finding, assertion, run, and report contracts exist.
- [x] Candidate and profile identity are checked exactly; mutable branch labels are not accepted as identity.
- [x] Required-probe completeness and assertion consistency are evaluated.
- [x] Weighted score is separate from verdict; incomplete campaigns do not receive a passing score.
- [x] Blocking severity and required-invariant failures override aggregate score.
- [x] Comparable history cohorts, averages, deltas, survived/failed/incomplete counts are implemented.
- [x] Canonical report digest sealing and integrity verification exist.
- [ ] Review every public contract for nullability, invalid enum values, collection bounds, duplicate identities, overflow, and version evolution.
- [ ] Add or confirm adversarial/property-based tests for evaluator invariants, malformed reports, profile drift, and score edge cases.
- [ ] Confirm deterministic results across repeated runs and supported runtime/platform combinations.
- [ ] Document report schema compatibility and forward/backward compatibility behavior.
- [ ] Decide and document whether report digest canonicalization is cryptographic JCS or a separate explicitly versioned deterministic encoding; do not conflate the two.

**Exit condition:** Core evaluation is deterministic, fully specified, adversarially tested, and cannot turn missing or contradictory evidence into a pass.

## Gate 2 — Probe model and execution safety

- [x] Gauntlet-owned probe interface and sequential in-process runner exist.
- [x] Runner validates profile membership/version, duplicate probe IDs, assertion bounds, cancellation, and cooperative elapsed duration.
- [x] Harmless vulnerable fixture demonstrates a reproducible high-severity finding and failed campaign.
- [x] Documentation explicitly states that the in-process runner is not a sandbox and does not execute candidate-supplied code.
- [ ] Complete probe contract review: stable identity/version, threat category, scope, preconditions, expected invariants, deterministic seed/replay identity, bounded diagnostics, cleanup, and severity semantics.
- [ ] Add runner tests for duplicate/unknown probes, malformed observations, exceptions, cancellation races, time-boundary behavior, overflow, and deterministic replay.
- [ ] Ensure non-cooperative probe behavior cannot be misrepresented as hard-preempted or safely isolated.
- [ ] Define trusted-probe distribution and provenance; never load arbitrary probe implementations from a candidate.
- [ ] Define regression-fixture lifecycle, ownership, minimization, retention, and replay policy.
- [ ] Define probe pack/profile signing or another immutable trust mechanism before accepting externally supplied profiles.
- [ ] Ensure no uncontrolled scanning, destructive actions, credential access, or unrestricted network activity is introduced.

**Exit condition:** Trusted probes are deterministic and bounded within their declared cooperative scope; the product makes no false sandbox claim.

## Gate 3 — Host isolation and candidate-controlled execution

- [x] Host isolation contract documents no credentials, default-deny egress, resource ceilings, fresh environments, lifecycle, attestation, cleanup, and fail-closed behavior.
- [x] Host adapter decision explicitly selects GitHub Actions as orchestration/transport only and a separate disposable VM or equivalent strong boundary for candidate execution.
- [x] Core request/receipt models and structural validator exist, including candidate/profile/run binding, requested/configured/observed controls, artifact counts/bytes/digests, terminal reason, cleanup, and injected signature/replay seams.
- [x] Documentation states that these models are not a host implementation, cryptographic verifier, or proof of OS enforcement.
- [ ] Freeze a versioned JSON wire schema and DTOs using the Workshop JSON standard (`System.Text.Json`); pin exact property names, types, required fields, null behavior, and unknown-field policy.
- [ ] Enforce bounded raw receipt size and strict UTF-8/JSON parsing before schema mapping.
- [ ] Reject duplicate decoded property names, invalid Unicode, non-I-JSON values, unknown schema versions, and malformed values without coercion.
- [ ] Implement RFC 8785 JCS canonicalization using an audited implementation; complete dependency, source provenance, package hash, license, and advisory review before accepting any package.
- [ ] Add independent positive/negative RFC 8785 vectors, including number edge cases, property ordering, duplicate names, Unicode, depth, and signature-excluded projection.
- [ ] Implement and review the exact signature payload: remove only top-level `signature`, retain `signingKeyId`, canonicalize, UTF-8 encode, prepend the documented domain separator.
- [x] Keep the signature-verifier boundary byte-oriented: validation requires non-empty canonical signed bytes and passes them with signing key ID/provider/host identity context. This is an interface seam only; JCS byte production and cryptographic verification remain open.
- [ ] Implement provider trust policy: key identity, algorithm allowlist, key validity, rotation, revocation, and provider binding. No self-asserted key trust.
- [ ] Implement durable replay protection with concurrency and retention semantics; test replay across process restarts.
- [ ] Implement a host adapter with fresh one-attempt environments, independently observed controls, watchdog VM termination, and verified cleanup.
- [ ] Prove no credentials, host mounts, control sockets, cloud metadata, sibling jobs, or control-plane access are available to candidate workloads.
- [ ] Prove deny-by-default network egress across DNS, IPv4, IPv6, loopback, and metadata paths; test any explicitly allowed capability separately.
- [ ] Prove CPU, memory, process, wall-clock, disk, output, artifact count/size, and filesystem boundaries at the host/OS layer.
- [ ] Prove process-tree termination, VM destruction, residual-resource checks, quarantine, and cleanup-failure handling.
- [ ] Produce authenticated receipts bound to exact request, artifact, profile, run, attempt, effective controls, observed outcome, and cleanup.
- [ ] Add adversarial host integration tests that attempt to escape or exceed every boundary; tests must observe host-side denial/termination.
- [ ] Ensure candidate-controlled build, test, package, or script execution never occurs in the GitHub Actions orchestration job.
- [ ] Keep candidate execution disabled until every host qualification and receipt-verification item above is complete and independently reviewed.

**Exit condition:** Either (a) candidate execution remains explicitly disabled and the first release scope excludes it, or (b) the host adapter has passed all OS-level adversarial qualification and authenticated receipt tests. A container or unit test alone is not sufficient.

## Gate 4 — CiAi evidence-provider integration

- [x] Architecture documents Gauntlet as a provider and CiAi as independent evidence validator and final gate owner.
- [x] Coordinating CiAi integration issue exists: [CiAi issue #71](https://github.com/TrentBest/TheSingularityWorkshop.CiAi/issues/71).
- [ ] Define the provider contract against CiAi's existing exact-run evidence/feedback lane; avoid a competing authorization ledger or parallel evidence schema.
- [ ] Implement a read-only/advisory provider adapter first.
- [ ] Bind every response to repository, exact candidate SHA, artifact/manifest digest, profile ID/version/digest, Gauntlet version, run ID, attempt, and immutable report identity.
- [ ] CiAi independently verifies report schema, digest, completeness, provider trust, receipt authenticity, freshness, and replay status.
- [ ] Missing, stale, duplicated, mismatched, invalid, or untrusted evidence is non-passing.
- [ ] Model-generated explanations cannot modify findings, score, verdict, trust, or authorization.
- [ ] Add contract/integration tests for forged, replayed, cross-repository, cross-SHA, cross-profile, stale, incomplete, and conflicting provider responses.
- [ ] Demonstrate provider failure does not weaken or bypass existing CiAi gates.
- [ ] Keep Gauntlet advisory until a separately reviewed policy change explicitly makes it required.

**Exit condition:** CiAi can independently validate a Gauntlet report and receipt without trusting Gauntlet's own status assertion.

## Gate 5 — Badge, history, and reporting

- [x] GUI.SVG badge generator uses the real GUI source and renders untested, failed, passed, and blocked fixtures.
- [x] Fixture verifier and badge CI workflow exist.
- [x] Badge documentation states that it is a projection, not authority or proof.
- [ ] Generate production status only from CiAi-validated evidence for the exact candidate SHA.
- [ ] Ensure stale prior-candidate green status can never be shown as the current candidate's status.
- [ ] Link badge/report to exact run, attempt, profile version, candidate SHA, and durable report.
- [ ] Ensure neutral/blocked is used for missing, stale, invalid, incomplete, ambiguous, or untrusted evidence.
- [ ] Verify accessibility, SVG safety, reduced-motion behavior, deterministic output, and safe text escaping.
- [ ] Validate average/delta cohort labels and sample counts; never combine incompatible profile versions.
- [ ] Document report retention, artifact size limits, redaction, access control, and deletion/retention policy.

**Exit condition:** The UI is a truthful, safe, accessible projection of validated evidence and cannot become an alternate authority.

## Gate 6 — Build, tests, security, and repository hygiene

- [x] Core contract test project and GitHub Actions workflow exist.
- [x] Badge and Sith policy workflows exist.
- [x] Workflows use read-only permissions where feasible; badge workflow pins its checkout/setup/upload actions to immutable SHAs.
- [x] Sith policy currently reports exactly `master` and `development`.
- [ ] Run the complete Release build and test suite from a clean checkout of the exact release-candidate SHA.
- [ ] Run all badge fixture generation and verification from a clean checkout; archive the exact generated artifacts. The 2026-11-16 run exposed a target-framework drift: GUI.Core/GUI.SVG now target .NET 10 while the badge sample/workflow still targeted .NET 8. The sample and workflow have been aligned to .NET 10; rerun and verify this gate.
- [ ] Add explicit workflow for formatting/analyzers and fail on warnings selected by policy.
- [ ] Add dependency vulnerability/advisory review and an automated dependency update policy.
- [ ] Review all GitHub Actions permissions, event triggers, third-party actions, token scopes, artifact handling, and untrusted-input interpolation.
- [ ] Review the Sith policy workflow's `contents: write` requirement and ensure its branch/badge mutation behavior is constrained and intentional.
- [ ] Add secret scanning and repository security configuration review; verify no secrets are present in source, logs, artifacts, or test fixtures.
- [ ] Add license inventory/notices and third-party attribution for all shipped dependencies and copied source.
- [ ] Add SBOM generation and archive it with the release evidence.
- [ ] Run package/API compatibility checks and inspect the produced NuGet package contents.
- [ ] Verify package metadata: ID, version, authors/owners, description, project/repository URLs, license expression/file, README, tags, icon, symbols, and release notes.
- [ ] Verify deterministic/reproducible package build where feasible and record package SHA-256.
- [ ] Verify clean install and consumer smoke test from a temporary project using the produced package; do not publish to public NuGet for this test.
- [ ] Verify no accidental dependency on GUI, FSM_COS, FSM_REST, FSM_Serialization, or other optional Workshop components is introduced into the core package.
- [ ] Confirm exactly two repository branches: `development` and `master`; inspect tags and working tree/release artifacts for unintended changes.
- [ ] Confirm all documentation links, examples, commands, and advertised capabilities match the code.

**Exit condition:** A clean, exact-SHA build/test/package pipeline passes; dependency, workflow, security, license, and package contents have been reviewed.

## Gate 7 — Documentation and developer experience

- [x] README describes purpose, boundaries, architecture, initial implementation, and safety caveats.
- [x] Architecture, run contract, development, badge, autonomous pipeline, host isolation, host adapter, and JCS assessment documents exist.
- [ ] Ensure README links to this checklist and clearly displays current release readiness state.
- [ ] Document installation and a minimal consumer example for the actual first release surface.
- [ ] Document how to define/version a profile and probe, execute an allowed campaign, inspect findings, and replay a regression.
- [ ] Document all statuses and terminal outcomes, including what is explicitly not a pass.
- [ ] Document report schema, digest, canonicalization, trust, and receipt semantics without claiming unfinished capabilities.
- [ ] Document how to run every test and verification locally from a clean clone.
- [ ] Ensure all samples are safe, deterministic, and do not execute candidate-controlled code.
- [ ] Add a changelog/release notes entry for the exact release candidate.
- [ ] Perform a documentation-only read-through for contradictory or overstated claims.

**Exit condition:** A new developer can understand the actual supported surface, run safe examples, and interpret evidence without relying on undocumented assumptions.

## Gate 8 — Release candidate and final owner review

- [ ] All applicable gates above are complete, with no unresolved critical/high security issue or unexplained blocked item.
- [ ] Release scope explicitly excludes every unfinished capability; no unfinished host execution or CiAi integration is implied as production-ready.
- [ ] Freeze the candidate commit SHA on `development`; record exact source state and confirm clean branch policy.
- [ ] Run all required workflows against that exact SHA and capture successful run URLs and artifact IDs.
- [ ] Build the package locally/privately from that exact SHA; inspect contents and verify package SHA-256.
- [ ] Complete dependency, license, security, SBOM, API compatibility, and consumer smoke-test evidence for that exact artifact.
- [ ] Prepare a release dossier: scope, known limitations, API/package changes, test matrix, workflow evidence, SBOM, dependency/license review, artifact digest, and rollback/revocation plan.
- [ ] Verify `master` remains the last released state and is not advanced as part of preparation.
- [ ] Present the exact candidate SHA, package ID/version, artifact digest, release notes, all gate results, unresolved risks, and proposed publication action to the owner.
- [ ] Obtain the owner's explicit final review and approval for the exact package/version/artifact.
- [ ] After approval only: perform the separately authorized merge/release/publication steps, then verify the published package and record its immutable URL and digest.
- [ ] Align `master` with the approved released state only after the authorized release procedure is complete.

**Exit condition:** The owner has reviewed and explicitly approved the exact release artifact. Until then, status remains `NOT AUTHORIZED TO PUBLISH`.

## Evidence register

Add one row per material gate result. Prefer immutable commit SHAs and workflow run URLs over branch names or screenshots.

| Date (UTC) | Gate / item | Result | Commit SHA | Evidence |
|---|---|---|---|---|
| 2026-11-15 | Core contract baseline | Existing implementation; prior CI evidence recorded | `035e836731d0549ba49ee94f379237bcc0597ce8` (docs-only follow-up) | [Core workflow baseline](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/runs/43680994913) — 24 passed, 0 failed, 0 skipped at that recorded run |
| 2026-11-15 | Badge generation and fixtures | Passed at recorded run | `035e836731d0549ba49ee94f379237bcc0597ce8` | [Badge workflow baseline](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/runs/43680994906) |
| 2026-11-15 | Sith branch policy | Passed at recorded run | `035e836731d0549ba49ee94f379237bcc0597ce8` | [Sith policy workflow baseline](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/runs/43680994917) |
| 2026-11-15 | RFC 8785 implementation assessment | Research/documentation only; no dependency accepted | `035e836731d0549ba49ee94f379237bcc0597ce8` | [JCS implementation assessment](JCS_IMPLEMENTATION_ASSESSMENT.md) |
| 2026-11-16 | Checklist creation | Added as living release gate; verification still pending | `c70a9bab661eeb019e872eab46149fdfd6a1c2a6` | This file |
| 2026-11-16 | Receipt verifier boundary | Byte-oriented canonical-payload seam and empty-payload rejection added; 49 core tests passed, 0 failed, 0 skipped | `5e2ac13d9e575f3ecd750b2709fd4d755f78d5e7` | [Core workflow](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/runs/43812055247); [Host receipt contract](HOST_ISOLATION_CONTRACT.md) |
| 2026-11-16 | Full current development CI | Core, badge, and Sith policy workflows all succeeded on the same commit | `5e2ac13d9e575f3ecd750b2709fd4d755f78d5e7` | [Core](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/runs/43812055247) · [Badge](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/runs/43812055274) · [Sith](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/runs/43812055261) |
| 2026-11-16 | Badge toolchain drift | Fixed: badge sample now targets net10.0; workflow installs both .NET 8 and 10; all four SVG fixtures built, rendered, verified, and uploaded | `5e2ac13d9e575f3ecd750b2709fd4d755f78d5e7` | [Successful badge workflow](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/runs/43812055274); [prior NU1201 failure](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/runs/43811905312) |

## Final approval record

Do not fill this section on the owner's behalf.

- **Approved by:** _pending_
- **Approval date/time (UTC):** _pending_
- **Approved candidate commit SHA:** _pending_
- **Package ID and version:** _pending_
- **Package SHA-256:** _pending_
- **Explicit authorization reference:** _pending_

**Until every field is completed from an explicit owner approval, publication is not authorized.**
