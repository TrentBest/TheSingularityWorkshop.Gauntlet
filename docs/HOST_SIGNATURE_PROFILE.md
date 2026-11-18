# Host Receipt Signature Profile — Version 1

**Status:** ES256 wire encoding, fail-closed verifier, and end-to-end canonical-payload/signature tests pass CI (73 core/security tests and 46 JCS integration tests). The external trust-store implementation and provider onboarding are not complete.

## Fixed version-1 signature profile

Version 1 uses **ES256**: ECDSA over NIST P-256 with SHA-256. Signatures use IEEE P1363 fixed-field concatenation: 32-byte big-endian `R` followed by 32-byte big-endian `S`, for exactly 64 bytes. The wire value is canonical base64url without padding, exactly 86 ASCII characters.

The receipt does not contain an `alg` field and cannot select its own algorithm. The trusted key profile fixes the allowed algorithm to `ES256`. Unsupported algorithms fail closed.

This is the ES256 representation specified by [RFC 7518 §3.4](https://www.rfc-editor.org/rfc/rfc7518#section-3.4), encoded as unpadded base64url per [RFC 7515 §2](https://www.rfc-editor.org/rfc/rfc7515#section-2). .NET 10 provides `ECDsa.VerifyData` with explicit `DSASignatureFormat.IeeeP1363FixedFieldConcatenation`; see the [Microsoft API reference](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.ecdsa.verifydata?view=net-10.0).

## Exact verification sequence

1. Parse the bounded version-1 receipt using `HostReceiptJsonV1`; reject malformed or non-canonical base64url signature values.
2. Build the exact domain-separated bytes using `HostReceiptSignaturePayloadBuilder` and the approved RFC 8785 canonicalizer.
3. Look up `signingKeyId` in a host-controlled `IHostReceiptTrustStore`. The key is not obtained from the receipt.
4. Require exact ordinal matches for key ID, provider ID, and an explicitly allowed host identity.
5. Require algorithm `ES256`, a 256-bit P-256 SPKI public key, and a valid key time window.
6. Reject revoked keys. The validity window is inclusive at `notBeforeUtc` and exclusive at `notAfterUtc`.
7. Decode the 64-byte base64url signature and verify the exact payload using SHA-256 and P1363 format.
8. Return false for unknown keys, identity mismatch, unsupported algorithm, malformed key/signature, invalid/expired/not-yet-valid/revoked keys, trust-store exceptions, and cryptographic failures.

The validator passes its explicit `now` value to the verifier. Verification must not read the local clock, which keeps validity checks deterministic and testable.

## Trust entry

`HostReceiptTrustedKey` contains:

- Immutable `KeyId`
- `ProviderId`
- Exact allowed host identities
- Fixed algorithm identifier
- DER SubjectPublicKeyInfo public-key bytes
- UTC `NotBeforeUtc` and `NotAfterUtc` instants
- Revocation state

The current `IHostReceiptTrustStore` is an interface only. A concrete store, durable audit, authenticated distribution, key rotation/revocation operations, and provider identity onboarding remain outstanding. The trust store must be outside the candidate-controlled execution environment and controlled by the host/operator.

## Key lifecycle and operational boundaries

- Private signing keys remain only in the host provider's signing service or hardware-backed key facility. They are never stored in Gauntlet, candidate artifacts, test fixtures, or repository secrets.
- Public keys are provisioned out of band into the trust store.
- Every key has a unique immutable key ID. Never reuse a key ID for replacement key material.
- Rotation introduces a new key ID and overlapping validity windows only when explicitly authorized by provider policy.
- Revocation is an immediate fail-closed flag in this initial implementation. A production store must make revocation updates auditable and promptly visible to all verifiers.
- Provider and host identity matching is exact and case-sensitive. No wildcard host identity is accepted by the verifier.
- The receipt cannot override algorithm, public key, provider, host identity, validity, or revocation policy.
- The signature verifier authenticates a receipt; it does not prove the host's OS isolation controls were actually enforced. Those claims still require independent host qualification and attestation.

## Current implementation boundary

Implemented in source:

- Strict 64-byte unpadded base64url signature decoding in `HostReceiptJsonV1`.
- Explicit signature bytes are passed to `IHostReceiptSignatureVerifier`, together with the exact canonical payload, key ID, provider, host identity, and caller-supplied verification time.
- `EcdsaP256HostReceiptSignatureVerifier` uses the .NET 10 ECDSA API and injected `IHostReceiptTrustStore`.
- The verifier checks key ID, provider, allowed host, ES256 algorithm, UTC validity window, revocation state, SPKI import completeness, and P-256 key size before cryptographic verification.
- Trust-store and cryptographic failures return false.

Not implemented:

- A concrete trust store or key-distribution mechanism.
- Provider signing service or private-key management.
- Provider onboarding, rotation/revocation control plane, or audit persistence.
- A production-approved canonicalizer dependency; the MackySoft candidate remains isolated to test-only qualification.
- A disposable host adapter or candidate execution authorization.

No private key or real provider credential is needed for the unit tests; they generate ephemeral P-256 keys in memory.
