# CiAi evidence handoff for the dependency machine

**Status:** integration contract only; no production provider adapter or candidate execution is enabled.

This document connects the Gauntlet dependency snapshot to the existing CiAi exact-run evidence lane. It does not create a second authorization ledger or treat a Gauntlet status as trusted evidence.

## Authority split

- **Gauntlet** owns campaign profiles, trusted probes, findings, scoring inputs, and immutable campaign reports.
- **The host adapter** owns isolation, resource enforcement, credentials, process-tree termination, and cleanup. The current in-process runner is only for Gauntlet-owned trusted probes.
- **CiAi** owns authenticated GitHub/provider retrieval, exact-run provenance validation, independent Gauntlet report/receipt validation, replay/freshness checks, and gate policy.
- **The dependency-machine renderer** consumes a read-only projection. It does not schedule work or authorize a gate.

The optional `CiAi.WithGauntlet()` registration is declarative discovery/configuration only. It does not execute a campaign, fetch a report, or make candidate-controlled execution safe.

## Required handoff sequence

1. A trusted host starts an explicitly selected immutable Gauntlet profile for one exact candidate and records the profile ID, version, and digest.
2. The host runs the campaign only inside a separately qualified isolation boundary. GitHub Actions may orchestrate and transport evidence but must not execute untrusted candidate code in its orchestration job.
3. Gauntlet emits an immutable report and, when candidate execution is eventually qualified, a host receipt. The report and receipt must be independently retrievable as exact artifacts with provider-observed digests.
4. CiAi collects the exact GitHub workflow run **and attempt**, all jobs, and the provenance artifact through its trusted provider boundary. It validates repository, candidate checkout SHA, workflow path/ref/SHA, event identity, completeness, and protected workflow-definition trust. Pull-request head/base/merge identities remain distinct.
5. CiAi independently validates the Gauntlet report schema and digest, profile identity, Gauntlet version, exact candidate identity, run/attempt binding, evidence completeness, provider/host receipt authenticity, freshness, and replay state.
6. Only after that validation may CiAi publish a typed accepted/rejected evidence decision to the dependency graph. Missing, stale, incomplete, mismatched, conflicting, invalid, or untrusted evidence is non-passing.
7. The renderer displays that projection as a status view. A green stage or badge is not a permission to execute, merge, publish, release, or deploy.

## Identity mapping

| Dependency-machine field | Required source / rule |
|---|---|
| `DependencyCandidateIdentity.Repository` | Canonical owner/repository from the independently observed CiAi run |
| `PackageId` | Exact dependency/package identity from the pinned dependency graph; never inferred from a display label |
| `SourceCommitSha` | Exact candidate checkout SHA from validated CiAi provenance, not merely the workflow-definition SHA |
| `ArtifactDigest` | Provider-observed digest of the exact candidate artifact or manifest being tested |
| `ManifestDigest` | Digest of the immutable campaign/profile manifest when the stage contract requires it |
| `DependencyStageEvidence.ProfileId/Version/Digest` | Exact profile selected for the run and independently matched to the report |
| `RunId` | Exact provider workflow run identifier, bound together with its attempt |
| `ReportDigest` | Independently recomputed digest of the immutable Gauntlet report |
| `ProviderId` | Registered and authenticated provider identity; a string supplied by the report is not authentication |
| `EvidenceBundleDigest` | Provider-observed digest of the complete evidence bundle, including the receipt where required |
| `EvidenceReference` | Exact durable HTTPS reference to the evidence artifact/report |
| `PipelineAttemptId` | Exact pipeline attempt identity; do not substitute provider RunId/RunAttempt |
| `StageAttemptId` / `DependencyStage.AttemptId` | Unique attempt for this graph stage; must match the validated report binding |

CiAi's `CiWorkflowEvidencePacket` is the exact-run envelope for workflow observations and validated workflow provenance. It is **not** itself a Gauntlet report, a host receipt, or a Gauntlet issuer signature. Its `WorkflowDefinitionTrusted`, `CheckoutVerified`, `EvidenceComplete`, and `IsSuccessful` facts must not be repurposed as proof that the Gauntlet report or host execution is trusted.

In particular, the host must not set workflow-definition trust from a workflow-supplied boolean. CiAi's protected workflow-definition policy and independent provider retrieval remain authoritative. Durable packet reload must re-evaluate trust rather than trusting a serialized decision.

## Stage-state mapping

- `Accepted` evidence validation may be assigned only after CiAi independently validates the exact report and all required receipt/provenance bindings.
- `Rejected` is used for an explicitly invalid, mismatched, stale, replayed, or untrusted report.
- `Pending` is used while independent validation is incomplete.
- `None` means no evidence was supplied; it is not a pass.
- `Passed` is permitted only when the stage's required upstreams have passed and CiAi has accepted the complete exact-attempt evidence.
- `Failed`, `Blocked`, `Incomplete`, `Invalid`, and `Untrusted` must remain distinct. Do not collapse any of them into a green state.

A locally valid snapshot digest proves only that the snapshot matches its own deterministic serialization. It is not an issuer signature. The read-only projection's `IsStructurallyValid` and aggregate `Passed` state are presentation facts, not authorization decisions.

## Current blockers

Candidate-controlled execution remains disabled until a production disposable-VM/strong-isolation host, OS-level resource and network enforcement, process-tree termination, cleanup verification, authenticated receipts, durable replay protection, provider onboarding, and independent adversarial qualification are complete.

The Warlords vector adapter is also awaiting the authoritative golden-vector corpus from Experiences. Do not invent expected events or pin a corpus digest until those exact fixtures exist.

## Contract acceptance tests before implementation

The adapter must prove rejection of at least:

- a different repository, candidate SHA, workflow definition, profile, report digest, run, pipeline attempt, or stage attempt;
- an absent, duplicate, truncated, stale, incomplete, or conflicting provider observation;
- a report whose digest differs from the provider-observed artifact digest;
- a forged, expired, revoked, or replayed receipt;
- a pull-request packet whose head/base/merge-parent identities do not match independent provider observations;
- a workflow-definition trust flag supplied without protected host-side verification;
- a passing report with any required upstream stage missing or not accepted.

Until these contracts and tests are implemented, the integration remains advisory/design-only and no production Gauntlet stage may be marked trusted or required.
