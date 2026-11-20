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

The receipt protocol is versioned JSON using the Workshop standard, `System.Text.Json`. The exact version-1 field names, types, required/nullable behavior, and strict parsing rules are defined in [RECEIPT_WIRE_V1.md](RECEIPT_WIRE_V1.md).

The parser rejects payloads over 1 MiB, excessive nesting, UTF-8 BOMs, malformed UTF-8/Unicode, duplicate decoded property names (including escaped aliases), comments, trailing commas, additional top-level values, unknown fields, unknown schema versions, numeric coercion, invalid digests, non-UTC timestamps, and values outside the I-JSON safe-integer range. The signature must be canonical unpadded base64url encoding of exactly 64 bytes.

### Exact signed payload

`HostReceiptSignaturePayloadBuilder.Build` implements the version-1 payload construction:

1. Serialize the validated version-1 DTO using the frozen wire property names.
2. Remove only the top-level `signature` property; retain `signingKeyId` and every other field.
3. Pass the signature-excluded JSON bytes to an injected `IHostReceiptCanonicalizer` implementing RFC 8785 JCS.
4. Prefix the canonical UTF-8 bytes with the exact domain separator `TheSingularityWorkshop.Gauntlet.HostExecutionReceipt.v1\\n`.
5. Return the exact domain-separated bytes, enforcing a 1 MiB total payload ceiling.

The signature verifier receives those exact bytes, the original signature string, `signingKeyId`, provider identity, host identity, and caller-supplied UTC verification time. It must not independently serialize a domain object or reconstruct the signature value.

### Fixed version-1 signature profile

Version 1 fixes **ES256**: ECDSA P-256 with SHA-256 and IEEE P1363 fixed-field concatenation. The wire signature is 64 bytes, encoded as canonical unpadded base64url (86 ASCII characters). The receipt has no algorithm selector; algorithm policy is fixed by the trusted key profile. See [HOST_SIGNATURE_PROFILE.md](HOST_SIGNATURE_PROFILE.md), [RFC 7518 §3.4](https://www.rfc-editor.org/rfc/rfc7518#section-3.4), and [RFC 7515 §2](https://www.rfc-editor.org/rfc/rfc7515#section-2).

`Gauntlet.Security` implements fail-closed ES256 verification using .NET's ECDSA APIs. It binds key ID, provider, and exact allowed host identity; checks algorithm, UTC validity window, revocation, SPKI import completeness, and P-256 key size; then verifies the exact domain-separated bytes. Verification uses the caller-supplied time, not the verifier's local clock.

### Canonicalizer qualification and trust boundaries

The MackySoft RFC 8785 implementation is currently referenced only by `tests/Gauntlet.Jcs.Tests`; it is not a shipping `Gauntlet.Core` dependency. The test-only project pins its packages in `packages.lock.json`, uses System.Text.Json 10.0.15, and passes the six official RFC 8785 JSON vectors, all 23 Appendix B finite-number vectors, negative/malformed cases, the DTO projection/domain-separator test, and end-to-end ES256 verification. The nupkg source/binary parity and full license/notice review remain release gates. See [JCS_IMPLEMENTATION_ASSESSMENT.md](JCS_IMPLEMENTATION_ASSESSMENT.md).

`ImmutableHostReceiptTrustStore` provides a concrete process-local immutable snapshot with duplicate-key rejection. It is not a persistent or authenticated key-distribution service. Production trust-store provisioning, durable audit, atomic refresh, provider onboarding, key rotation/revocation operations, and replay-guard persistence remain outstanding. The private signing key must remain exclusively in the host provider's signing service or hardware-backed facility; Gauntlet receives public keys only.

### Durable replay protection contract

`IHostReceiptReplayGuard.TryRegister(receiptId)` is an injected persistence boundary, not an in-memory cache recommendation. A production implementation must satisfy all of the following:

- **Atomic uniqueness:** concurrent calls from different threads, processes, hosts, and service instances for the same identifier in the same trust domain have one winner at most. The uniqueness operation must be linearizable (or provide an equivalent database uniqueness transaction).
- **Durable success:** returning `true` means the identifier's registration has been durably committed. A process crash or machine restart after that return must not make the identifier available again.
- **Duplicate result:** return `false` only when the identifier is already durably registered. Never treat a timeout, unavailable store, uncertain commit, or storage error as a fresh identifier; surface failures so validation remains fail-closed.
- **Stable scope and identity:** identifiers are compared exactly and case-sensitively. The guard's configured trust domain defines the uniqueness namespace; all providers whose receipts are accepted in that domain share the same namespace. Receipt producers must generate collision-resistant IDs within that namespace.
- **Retention:** registrations must remain effective for as long as a receipt could still be accepted. No time-based eviction is safe until the protocol defines and enforces a maximum receipt-acceptance horizon (including clock skew and recovery behavior). A storage cleanup policy must not silently shorten that horizon.
- **Recovery and operations:** restart/recovery must preserve committed registrations; backups, replication, failover, and restore procedures must not roll the replay ledger back behind accepted receipts. Storage corruption or inability to establish the ledger's current state denies acceptance.

The validator calls the guard only after structural, request-binding, outcome, cleanup, and signature checks pass. Its existing exception handling rejects a failed registration. The current test `ReplayGuard` is deliberately process-local and proves only the validator's call ordering and duplicate handling; it is not evidence of durability, multi-process atomicity, or production readiness. A future adapter must add integration tests against its actual durable store, including concurrent writers, process restart, uncertain commit, failover/restore, and retention-boundary cases.

A valid signature authenticates receipt bytes; it does not prove the host's OS isolation controls were enforced. Host isolation qualification and CiAi's independent evidence validation remain mandatory. Do not enable candidate-controlled execution until the disposable host adapter and its OS-enforced controls are implemented and independently tested.

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

## Current schema-validation and security implementation

The core library defines versioned host request, finite-limit, effective-control, terminal-outcome, and receipt models in `HostIsolationContracts.cs`. Its validator binds the repository, full commit SHA, candidate artifact digest, profile identity/version/digest, run/attempt, request validity window, configured/observed limits, deny-all network state, artifact digests/bytes/counts, terminal reason, process-tree termination, cleanup, and residual-resource evidence.

The version-1 `System.Text.Json` wire DTO/parser is implemented in `HostReceiptJsonV1.cs`. It performs strict bounded parsing and exposes the canonical signature decoder. `HostReceiptSignaturePayloadBuilder` creates the exact RFC 8785/domain-separated byte payload through an injected canonicalizer. The canonicalizer package remains test-only.

`Gauntlet.Security` contains `EcdsaP256HostReceiptSignatureVerifier`, `HostReceiptTrustedKey`, the `IHostReceiptTrustStore` contract, and `ImmutableHostReceiptTrustStore`. The immutable store is a process-local snapshot only. No production key distribution, persistent trust store, provider signing service, durable replay database, provider adapter, or host isolation implementation exists yet.

The current validated test evidence is tracked in [RELEASE_READINESS.md](RELEASE_READINESS.md). Unit and integration tests prove the managed contract and cryptographic path under ephemeral test keys; they do not prove OS-level enforcement, a production trust-store lifecycle, or a disposable host boundary.

## Explicit non-goals

- Treating .NET cancellation tokens or elapsed-time checks as hard preemption.
- Treating a subprocess as a sandbox.
- Trusting candidate-produced claims about its own isolation.
- Passing CI secrets into candidate-controlled execution.
- Running arbitrary hostile code on a developer workstation or a shared privileged runner.
- Allowing a Gauntlet pass to authorize merge, publication, release, or deployment.
