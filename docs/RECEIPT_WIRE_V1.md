# Host Execution Receipt JSON — Version 1

**Status:** Version-1 DTO and strict parser implemented; canonicalization and signature verification remain separate, unfinished gates.

This document and `src/Gauntlet.Core/HostReceiptJsonV1.cs` define the frozen version-1 JSON wire shape. The wire DTO is intentionally separate from `HostExecutionReceipt`, the in-process domain record. Do not serialize the domain record directly as a public protocol.

## Encoding and parser contract

- Encoding is UTF-8 without a byte-order mark.
- The raw document must contain 1–1,048,576 bytes.
- JSON nesting depth is limited to 32.
- Comments and trailing commas are rejected.
- Property names are case-sensitive and must match the names below exactly.
- Duplicate decoded property names are rejected, including escaped aliases such as `receiptId` and `receipt\\u0049d`.
- Unknown fields are rejected at every object level.
- Required properties must be present, even when their declared value is nullable.
- Numbers are strict JSON integers for the numeric fields below. Floating-point values, strings-as-numbers, and overflow are rejected. Every integer must remain within the I-JSON safe integer range (−(2^53−1) through +(2^53−1)) so JCS implementations cannot silently round it.
- Enum-like values are exact, case-sensitive strings. Numeric enum representations and unknown strings are rejected.
- Strings must contain valid Unicode scalar values. Empty required identity/metadata strings are rejected.
- Timestamps must parse as `DateTimeOffset`, have a zero UTC offset, and be ordered (`completedAt` must not precede `startedAt`).
- Unsupported schema versions are rejected. A future incompatible wire change receives a new DTO and schema version; version 1 is not silently reinterpreted.

The parser uses the Workshop standard, `System.Text.Json`, with strict number handling, case-sensitive property matching, unknown-member rejection, required members, and a bounded depth. The raw-byte preflight rejects duplicate decoded names and the UTF-8 BOM before DTO mapping. The signature-excluded projection is also capped at 1 MiB after serialization so escaping cannot expand a bounded input into an unbounded canonicalizer payload.

## Top-level fields

All fields are required unless the nullable behavior is explicitly stated.

| JSON property | JSON type | Meaning / rule |
|---|---|---|
| `schemaVersion` | integer | Exactly `1` |
| `receiptId` | string | Unique receipt identity; replay guard key |
| `requestId` | string | Exact host request identity |
| `candidate` | object | Exact source/artifact identity; see below |
| `profileId` | string | Profile identity |
| `profileVersion` | integer | Positive profile version |
| `profileDigest` | string | Profile SHA-256 identity |
| `runId` | string | Campaign run identity |
| `attempt` | integer | One-based attempt |
| `startedAt` | string | UTC timestamp |
| `completedAt` | string | UTC timestamp |
| `outcome` | string | One of the values below |
| `terminalReason` | string | One of the values below |
| `hostRunnerImage` | string | Immutable host runner image identity |
| `hostPlatform` | string | Host OS/architecture identity |
| `isolationAdapterVersion` | string | Host adapter version |
| `networkPolicy` | string | Version 1 accepts only `DenyAll` |
| `networkEgressDeniedObserved` | boolean | Host-observed egress denial |
| `inputArtifactDigest` | string | SHA-256 digest of the input artifact set |
| `inputArtifactBytes` | integer | Input artifact byte count |
| `inputArtifactCount` | integer | Input artifact count; positive and bounded by request |
| `outputArtifactDigest` | string or null | Null only when output count and bytes are zero |
| `outputArtifactBytes` | integer | Non-negative output byte count |
| `outputArtifactCount` | integer | Non-negative output artifact count |
| `effectiveControls` | object | Required control observations; see below |
| `processTreeTerminated` | boolean | Host attestation of full process-tree termination |
| `cleanupSucceeded` | boolean | Host attestation of cleanup |
| `residualResourcesClear` | boolean | Host attestation of no residual execution resources |
| `hostProvider` | string | Provider identity |
| `hostIdentity` | string | Specific host/runner identity |
| `signingKeyId` | string | Key identity interpreted only under external provider trust policy |
| `signature` | string | ES256 signature: exactly 64 bytes encoded as canonical unpadded base64url (86 ASCII characters); algorithm is fixed by v1, not selected by receipt data |

### Candidate object

| JSON property | JSON type | Meaning |
|---|---|---|
| `repository` | string | Canonical owner/repository identity |
| `commitSha` | string | Full immutable 40-character commit SHA |
| `artifactDigest` | string | SHA-256 digest of the exact candidate artifact |
| `manifestDigest` | string or null | Optional SHA-256 manifest digest |

### Effective control evidence

All 14 control properties are required objects:

- `noCredentials`
- `noHostMounts`
- `metadataBlocked`
- `networkEgressDenied`
- `readOnlyCandidateInput`
- `boundedScratch`
- `wallClockLimit`
- `cpuLimit`
- `memoryLimit`
- `processLimit`
- `diskLimit`
- `outputLimit`
- `artifactLimit`
- `artifactCountLimit`

Each control object has exactly these required properties:

| JSON property | JSON type | Meaning |
|---|---|---|
| `configured` | boolean | Host configuration says the control is enabled |
| `observedEnforced` | boolean | Independent host observation confirms enforcement |
| `configuredLimit` | integer or null | Configured numeric ceiling, where applicable |
| `observedLimit` | integer or null | Independently observed numeric ceiling, where applicable |

A configured control is not proof of enforcement. The structural/domain validator must compare requested, configured, and observed limits and reject missing or unobserved evidence.

### Signature algorithm and encoding

Version 1 fixes the signature algorithm to **ES256**: ECDSA over NIST P-256 with SHA-256, using IEEE P1363 fixed-field concatenation. The wire value is canonical base64url without padding; it must decode to exactly 64 bytes and round-trip to the same 86-character string. The receipt does not carry an algorithm selector. The trusted provider key profile independently pins ES256 and binds the key ID to its provider and permitted host identities.

This follows the ES256 signature representation in [RFC 7518 §3.4](https://www.rfc-editor.org/rfc/rfc7518#section-3.4). .NET 10's `ECDsa.VerifyData` supports explicit `DSASignatureFormat.IeeeP1363FixedFieldConcatenation`; see [Microsoft's .NET 10 API reference](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.ecdsa.verifydata?view=net-10.0).

The private signing key remains exclusively in the host provider's signing service. Gauntlet receives only the public key through a separately managed trust store. Key validity, revocation, provider/host identity binding, and rotation are trust-store policy, not candidate-controlled receipt fields.

### Exact enum strings

`outcome` accepts: `Completed`, `CandidateFailed`, `TimedOut`, `Cancelled`, `ResourceLimited`, `ProvisioningFailed`, `IsolationUnverified`, `TerminationFailed`, `CleanupFailed`.

`terminalReason` accepts: `Completed`, `CandidateExitNonZero`, `WatchdogTimeout`, `CancellationRequested`, `ResourceLimitExceeded`, `ProvisioningError`, `IsolationControlUnverified`, `ProcessTreeTerminationFailed`, `CleanupFailed`.

`networkPolicy` accepts only: `DenyAll`.

## Signature-excluded projection

The signature input is not produced by serializing the domain record. The process is:

1. Parse and validate the original bounded UTF-8 document using this versioned wire DTO.
2. Create a JSON object from the validated DTO.
3. Remove only the top-level `signature` property.
4. Retain `signingKeyId` and every other version-1 field.
5. `HostReceiptSignaturePayloadBuilder.Build` passes the projection to an injected `IHostReceiptCanonicalizer` implementation of RFC 8785 JCS.
6. The builder prefixes the canonical UTF-8 bytes with the exact domain separator:
   `TheSingularityWorkshop.Gauntlet.HostExecutionReceipt.v1\\n`.
7. The builder returns the domain-separated payload, bounded to 1 MiB total.
8. `IHostReceiptSignatureVerifier.Verify` receives those exact bytes, the original `signature` string, `signingKeyId`, `hostProvider`, and `hostIdentity`.

`SerializeSignatureExcluded` performs steps 2–4 and returns ordinary JSON bytes for the canonicalizer. `HostReceiptSignaturePayloadBuilder` implements the complete byte-construction pipeline, but the production canonicalizer dependency is not yet accepted. The MackySoft implementation is currently used only by the isolated test project. Neither class implements cryptographic verification or provider trust.

## Required tests before the wire gate is considered complete

- Positive version-1 round-trip and exact property names.
- Unknown fields at root and nested levels.
- Missing required fields and explicit nullable fields.
- Duplicate property names, including escaped aliases and nested duplicates.
- Invalid UTF-8, BOM, invalid Unicode scalar values, comments, trailing commas, extra top-level values, and excessive nesting.
- Case-mismatched names, unknown schema versions, unknown enum values, numeric enum values, and numeric coercion attempts.
- Input and signature-excluded output payload size boundaries, overflow values, I-JSON safe-integer boundaries, and malformed digests.
- UTC timestamp enforcement and output-artifact null/count/byte consistency.
- Signature-excluded projection removes only `signature` and retains `signingKeyId`.
- Independent RFC 8785 vectors and exact domain-separated byte vectors (tracked in `JCS_IMPLEMENTATION_ASSESSMENT.md`).

The DTO/parser tests do not prove signature authenticity, trusted-key lifecycle, durable replay resistance, or host isolation. The ES256 verifier is a separate implementation gate.
