# Host Adapter Architecture Decision

**Status:** proposed implementation direction; not an implemented or qualified sandbox.

## Decision

Use GitHub Actions as the orchestration and evidence-transport layer only. Candidate-controlled workloads must execute in a separate, one-attempt, disposable isolation environment provisioned and controlled by a host-side isolation service.

The GitHub Actions job that checks out Gauntlet, validates requests, or uploads reports must not execute candidate binaries, candidate tests, build targets, package scripts, or other candidate-controlled code as part of the isolation proof.

The first implementation target is a Linux host adapter backed by a disposable microVM or equivalently strong per-attempt VM boundary. The isolation service, not the candidate and not a workflow YAML declaration, is responsible for creating, verifying, terminating, cleaning, and attesting the environment.

## Why not execute hostile code directly in the GitHub Actions job?

A GitHub-hosted runner is useful CI infrastructure, but the workflow job is not the security boundary defined by [HOST_ISOLATION_CONTRACT.md](HOST_ISOLATION_CONTRACT.md). Candidate-controlled build/test steps execute with the job's process identity and can interact with the job environment. Setting workflow permissions to read-only, omitting secrets, or using a container reduces exposure but does not by itself prove all required host controls, process-tree termination, network denial, or cleanup.

Therefore:
- GitHub Actions is the control plane and report transport, not the hostile-code sandbox.
- Candidate execution is not permitted in the orchestration job.
- A plain `docker run` invocation is not accepted as proof of isolation.
- If the disposable isolation service is unavailable or cannot attest every required control, the attempt is rejected/incomplete and no candidate code runs.

## Trust zones

| Zone | Trusted responsibilities | Must not receive |
|---|---|---|
| GitHub Actions orchestration | Resolve exact candidate identity, request a run, transfer bounded artifacts, validate returned receipt/report | Candidate execution, broad repository write token, publish/deploy credentials |
| Isolation control service | Authenticate narrowly scoped run requests, provision disposable VM, enforce policy, observe lifecycle, sign receipt | Candidate-controlled code in its own control process |
| Disposable execution VM | Run exactly one declared workload under enforced limits; emit bounded outputs | CI credentials, cloud metadata, runner control-plane credentials, sibling-job access |
| CiAi | Independently validate provider trust, exact candidate/artifact binding, report and receipt integrity, gate policy | Authority delegated to Gauntlet's score or badge |

## Request and artifact flow

1. An orchestration workflow resolves repository identity, full commit SHA, artifact digest, manifest digest, profile identity, run ID, and attempt number.
2. A trusted packaging step creates a bounded input artifact. Candidate code is not invoked during packaging.
3. The workflow obtains a short-lived, narrowly scoped request identity (prefer workload identity/OIDC where the service supports audience and subject restrictions). No long-lived service credential is stored in the candidate workspace.
4. The isolation service validates the request and provisions a fresh disposable VM.
5. The service verifies effective controls before execution: no credentials, no metadata access, deny-by-default egress, bounded CPU/memory/processes/disk/output, isolated filesystem, and watchdog-controlled lifecycle.
6. Only after successful verification does the VM receive the declared artifact and fixed entry point.
7. The service captures exit status, resource-limit events, bounded logs/artifacts, process-tree termination, cleanup, and residual-resource checks.
8. The service emits an authenticated receipt binding the exact request, effective controls, observed outcome, and cleanup result.
9. The orchestration workflow transports the report and receipt. CiAi independently validates their identity, integrity, provenance, and policy before using them.

The service must not trust candidate-produced claims that controls were active. Candidate output is data, not attestation.

## Required isolation-service capabilities

The service is not considered implementable merely because it can launch a VM. Before candidate execution is enabled, it must demonstrate:

- fresh VM identity and clean state for every attempt;
- no inherited credentials, host mounts, control sockets, or cloud metadata access;
- default-deny egress enforced outside the guest, including DNS and IPv4/IPv6 paths;
- finite CPU, memory, process, wall-clock, disk, artifact, and log limits;
- immutable/read-only input and bounded scratch/output locations;
- watchdog termination of the VM, not just the guest process;
- verified destruction and residual-resource checks;
- authenticated receipts with key identity/rotation and replay protection;
- operator-visible quarantine behavior after failed termination or cleanup;
- audit records that do not depend on candidate-controlled logs.

If the selected provider cannot expose verifiable evidence for one of these requirements, the adapter must report the capability as unsupported and refuse the run.

## Receipt validation boundary

The isolation service signs or otherwise authenticates the receipt. The Gauntlet adapter verifies its cryptographic integrity and schema, but CiAi must independently validate the receipt and bind it to the exact CI run and candidate artifact. The receipt must distinguish requested, configured, and observed controls.

The version-1 signed payload uses RFC 8785 JSON Canonicalization Scheme (JCS): serialize the documented receipt wire schema, reject duplicate property names and invalid/non-I-JSON values, remove only the top-level `signature` property, canonicalize to UTF-8 without a BOM, then sign the bytes prefixed by the exact ASCII domain separator `TheSingularityWorkshop.Gauntlet.HostExecutionReceipt.v1\n`. `signingKeyId` remains inside the signed payload. The provider trust profile separately selects the signature algorithm and trusted-key policy. See [HOST_ISOLATION_CONTRACT.md](HOST_ISOLATION_CONTRACT.md) for the normative rules and test-vector requirement. The C# receipt model now includes host runner image/platform and adapter version, an explicit deny-all network policy plus observed egress-denial state, input/output artifact digests, byte counts, and artifact counts, and a terminal-reason enum. Validation binds the input digest to the requested candidate artifact, enforces artifact byte and count ceilings, and checks that terminal reason matches the outcome. This is still a domain-model validation scaffold, not a frozen JSON wire DTO, JCS implementation, signature verifier, or host attestation implementation.

A valid receipt is necessary, not sufficient, for a passing campaign. Report integrity, required probe coverage, profile identity, finding severity, and CiAi policy remain separate checks.

## Staged implementation plan

1. **Contract types and validation:** define versioned request, effective-control, terminal-outcome, and receipt models. Unit-test malformed, missing, unsupported, inconsistent, expired, replayed, and identity-mismatched data. These tests prove schema behavior only.
2. **Canonical wire schema and signature vectors:** complete all receipt fields, freeze JSON property names/types, implement RFC 8785 using a reviewed implementation, and publish positive/negative canonicalization and signature test vectors. Do not use a homemade cryptographic primitive or claim authenticity from unit-test doubles.
3. **Provider interface:** define a host-neutral provisioning/execution/termination/attestation interface. No implementation may claim isolation from interface conformance alone.
4. **Disposable host prototype:** select a concrete provider and implement one-attempt VM provisioning, policy enforcement, watchdog, cleanup, and authenticated receipt generation.
5. **Host adversarial qualification:** execute controlled tests in disposable infrastructure attempting metadata access, egress, filesystem escape, process/memory/disk exhaustion, descendant survival, output escape, and cleanup interference. Verify host-observed denial and termination.
6. **CiAi advisory adapter:** only after qualification, provide the report/receipt as a read-only provider packet and let CiAi independently validate it.
7. **Real candidate opt-in:** enable candidate-controlled execution only after all preceding gates pass and the operator explicitly enables the qualified provider.

## Explicit non-claims

This decision does not implement a VM provider, create credentials, configure cloud infrastructure, prove GitHub runner isolation, or authorize candidate execution. The current in-process `ProbeRunner` remains limited to trusted Gauntlet-owned probes. No hostile candidate code has been run under this design.

## Acceptance gate

The first host adapter is not complete until its integration suite demonstrates, from outside the guest, that each required boundary is enforced, that the entire VM/process tree is terminated, that cleanup is verified, and that the resulting authenticated receipt is bound to the exact requested candidate and attempt. Any unknown result fails closed.
