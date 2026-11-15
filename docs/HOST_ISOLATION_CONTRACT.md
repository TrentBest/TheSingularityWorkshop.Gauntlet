# Host Isolation Contract

**Status:** design contract; no sandbox implementation is claimed by this document.

## Purpose

This contract defines the minimum evidence a CI host must provide before Gauntlet may execute candidate-controlled code. Gauntlet's in-process `ProbeRunner` is not an isolation boundary and must never be promoted into one by configuration or naming.

The host provisions and owns the execution boundary. Gauntlet consumes a host-issued execution grant, submits a bounded workload, and records the host's observed outcome. A caller-supplied boolean such as `isSandboxed: true` is not evidence.

## Trust boundary and defaults

The isolation adapter must enforce these defaults:

- No repository, CI, cloud, package-publishing, deployment, or personal credentials are mounted or forwarded.
- No network egress; loopback and internal metadata endpoints are denied unless a profile explicitly requires a narrowly scoped capability and the host can enforce it.
- Candidate source and artifacts are treated as hostile data. Candidate code cannot access the host workspace, Docker/container socket, runner control plane, sibling jobs, or host credentials.
- The candidate receives only an explicit read-only input set and a separate bounded writable scratch area.
- No host privilege escalation, privileged containers, host PID namespace, host network namespace, or unrestricted device access.
- Resource and output limits are enforced by the host, not merely checked by the managed process after it returns.

A capability that cannot be enforced on the selected host/platform is unavailable. The adapter must reject the request rather than silently downgrade it.

## Required execution limits

Every execution request declares finite limits for:

| Limit | Required enforcement |
|---|---|
| Wall-clock duration | Host watchdog terminates the entire workload after the deadline |
| CPU | Host-enforced quota or equivalent scheduler control |
| Memory | Host-enforced hard limit; OOM/limit termination is reported distinctly |
| Processes/threads | Host-enforced process/task ceiling where supported |
| Filesystem | Read-only candidate input, bounded scratch, no host workspace escape |
| Network | Deny by default; explicit allowlist only where enforceable |
| Output/logs | Maximum bytes, bounded file count, safe path handling |
| Artifacts | Maximum total bytes and count; no symlink/path traversal escape |
| Retries | Finite attempt count; each attempt has a distinct identity |

Limits must be validated before provisioning. Missing, zero-ambiguous, negative, unsupported, or internally inconsistent limits reject the request.

## Host execution lifecycle

1. **Validate request:** bind repository, full commit SHA, artifact digest, manifest digest where applicable, profile digest, run ID, attempt, and requested limits.
2. **Provision:** create a fresh isolated environment with no inherited secrets and the declared filesystem/network policy.
3. **Verify controls:** obtain host-side evidence that each required control is active. Configuration intent alone is insufficient.
4. **Execute:** launch only the declared candidate workload with a fixed entry point and bounded inputs.
5. **Observe:** capture exit status, termination reason, resource-limit events, bounded stdout/stderr, and artifact metadata.
6. **Terminate:** stop the complete process tree, including descendants, on completion, cancellation, timeout, or policy violation.
7. **Clean:** destroy the execution environment and remove its writable state.
8. **Attest:** return a host-signed or otherwise independently verifiable execution receipt binding the request identity, effective controls, observed outcome, and cleanup result.
9. **Persist:** store the bounded report and receipt as immutable attempt evidence.

A new attempt must not reuse a writable environment from a previous attempt.

## Execution receipt

The receipt must contain, at minimum:

- receipt schema version and unique receipt ID;
- host provider, runner image/platform identity, and isolation adapter version;
- repository identity, full candidate commit SHA, artifact digest, and manifest digest;
- Gauntlet version, profile ID/version/digest, run ID, and attempt number;
- requested limits and effective enforced limits;
- network policy and effective egress state;
- input and output artifact digests, byte counts, and artifact counts;
- start/end timestamps and terminal reason;
- process-tree termination result;
- cleanup result and residual-resource check;
- host identity and verifiable receipt integrity/authenticity metadata.

The receipt must distinguish **requested**, **configured**, and **observed/enforced** controls. It must not represent a configured control as verified enforcement.

## Canonical receipt and signature contract

Version 1 receipt signatures use the JSON Canonicalization Scheme (JCS), RFC 8785. This is a wire-format requirement, not a claim that the current core library implements JCS or cryptography.

The signed message is constructed as follows:

1. Serialize the complete receipt using the versioned wire schema with exact, documented JSON property names and types.
2. Reject duplicate JSON property names, invalid Unicode, non-I-JSON values, unknown schema versions, and malformed fields before canonicalization. Do not silently coerce values.
3. Remove the top-level `signature` property entirely. Retain `signingKeyId` and every other receipt field in the signed payload.
4. Canonicalize the resulting JSON object according to RFC 8785. The canonical bytes are UTF-8 without a byte-order mark.
5. Prefix those bytes with the exact ASCII domain separator `TheSingularityWorkshop.Gauntlet.HostExecutionReceipt.v1\n`. The signature algorithm signs/verifies this complete byte sequence.

The algorithm and signature encoding are selected by a separately versioned provider trust profile; this contract does not invent a cryptographic primitive or prescribe a key store. The verifier must bind `signingKeyId` to an explicitly trusted provider identity and enforce key validity, rotation, revocation, and algorithm policy. Unknown keys, unsupported algorithms, malformed signatures, canonicalization failures, and verifier exceptions fail closed.

Implementations must share published canonicalization and signature test vectors before interoperability is claimed. A non-empty signature string, a successful JSON parse, or a self-reported key ID is never proof of authenticity.

The current C# receipt record and injected verifier remain a schema-validation scaffold, not an implementation of this wire contract. The domain model now carries host runner image/platform and adapter version, an explicit deny-all network policy and observed egress-denial state, input/output artifact digests, byte counts, and artifact counts, and an explicit terminal reason. Validation binds the input digest to the candidate artifact and enforces both byte and count ceilings for input and output artifacts. Validation binds the input digest to the candidate artifact, enforces artifact byte and count ceilings, and checks terminal-reason/outcome consistency. A frozen JSON wire DTO, RFC 8785 implementation, positive/negative signature vectors, and real trusted-key verifier remain outstanding. Do not enable a host adapter or candidate execution until those pieces are completed and independently tested.

## Fail-closed conditions

Candidate execution is denied, or the attempt is recorded as non-passing, if any of the following occurs:

- the host cannot prove the requested isolation controls are enforced;
- credentials or unexpected host mounts are detected;
- network denial or an approved egress allowlist cannot be verified;
- a hard resource limit is unsupported or cannot be observed;
- the process tree cannot be terminated;
- cleanup or residual-resource checks fail;
- the receipt is missing, invalid, unauthenticated, or mismatched to the exact request;
- output exceeds a declared bound or escapes the artifact collection boundary;
- the host reports an unknown termination state.

A failed cleanup is a security incident requiring quarantine of the runner/environment; it is not a successful test with a warning.

## Outcome semantics

The adapter must preserve distinct terminal outcomes: completed, candidate-failed, timed-out, cancelled, resource-limited, provisioning-failed, isolation-unverified, termination-failed, cleanup-failed, and receipt-invalid.

Only a completed execution with a valid receipt and successful cleanup may contribute ordinary probe outcomes. All other states remain incomplete, invalid, or untrusted evidence and cannot produce a passing campaign.

## Platform qualification

This contract is platform-neutral. A future implementation must name and test its actual host mechanism (for example, a dedicated ephemeral CI job/runner plus OS-enforced controls). A container boundary by itself must not be described as a complete sandbox without validating its namespaces, capabilities, mounts, cgroups/quotas, network policy, daemon exposure, and cleanup behavior.

The implementation must include adversarial integration tests that attempt to exceed each declared boundary and verify host-observed denial/termination. Unit tests of request validation are necessary but do not prove operating-system enforcement.

## Current schema-validation implementation

The core library now defines version-1 request, finite-limit, effective-control, terminal-outcome, and receipt models in `HostIsolationContracts.cs`. Its validator rejects malformed or expired requests, unsupported schema versions, non-positive limits, unbounded attempts, candidate/profile/run mismatches, expired receipt timestamps, controls that were configured but not independently observed, observed CPU/memory/process/disk/wall-clock/output/artifact quotas above the requested ceilings, overlong attempts, unsuccessful process-tree termination, failed cleanup, and non-ordinary host outcomes.

Receipt signature verification and replay registration are explicit injected interfaces. A verifier or replay-guard exception is converted into a failed validation result; it must not escape as an accepted or ambiguous receipt. Missing nested control evidence is likewise rejected without throwing. The verifier contract must validate the exact canonical receipt representation described above; the current interface is a seam for an adapter, not a canonicalization or cryptographic implementation. The core library does not contain a signing algorithm, trusted-key store, persistent replay database, provider adapter, or host implementation. Those responsibilities must be supplied by a separately reviewed adapter and CiAi must independently validate the evidence. Unit tests exercise schema and validation behavior only; they do not prove cryptographic authenticity, durable replay resistance, or OS-level enforcement.

## Explicit non-goals

- Treating .NET cancellation tokens or elapsed-time checks as hard preemption.
- Treating a subprocess as a sandbox.
- Trusting candidate-produced claims about its own isolation.
- Passing CI secrets into candidate-controlled execution.
- Running arbitrary hostile code on a developer workstation or a shared privileged runner.
- Allowing a Gauntlet pass to authorize merge, publication, release, or deployment.
