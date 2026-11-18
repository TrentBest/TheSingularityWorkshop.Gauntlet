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

The parser uses the Workshop standard, `System.Text.Json`, with strict number handling, case-sensitive property matching, unknown-member rejection, required members, and a bounded depth. The raw-byte preflight rejects duplicate decoded names and the UTF-8 BOM before DTO mapping.

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
| `signature` | string | Required signature encoding/value; algorithm is selected by external trust profile |

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
5. Pass this JSON object to the separately reviewed RFC 8785 JCS implementation.
6. Prefix the resulting canonical UTF-8 bytes with the exact domain separator:
   `TheSingularityWorkshop.Gauntlet.HostExecutionReceipt.v1\\n`.
7. Supply those exact bytes and the key/provider/host identity context to the injected verifier.

`SerializeSignatureExcluded` currently performs only steps 2–4 and returns ordinary JSON bytes for a future JCS canonicalizer. Its output is **not** canonical JSON and is **not** a signed message. No cryptographic verification or provider trust is implemented by this DTO/parser.

## Required tests before the wire gate is considered complete

- Positive version-1 round-trip and exact property names.
- Unknown fields at root and nested levels.
- Missing required fields and explicit nullable fields.
- Duplicate property names, including escaped aliases and nested duplicates.
- Invalid UTF-8, BOM, invalid Unicode scalar values, comments, trailing commas, extra top-level values, and excessive nesting.
- Case-mismatched names, unknown schema versions, unknown enum values, numeric enum values, and numeric coercion attempts.
- Payload size boundary, overflow values, I-JSON safe-integer boundaries, and malformed digests.
- UTC timestamp enforcement and output-artifact null/count/byte consistency.
- Signature-excluded projection removes only `signature` and retains `signingKeyId`.
- Independent RFC 8785 vectors and exact domain-separated byte vectors (tracked in `JCS_IMPLEMENTATION_ASSESSMENT.md`).

The DTO/parser tests do not prove JCS conformance, signature authenticity, durable replay resistance, or host isolation.
