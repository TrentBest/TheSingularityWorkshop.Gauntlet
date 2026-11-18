# RFC 8785 Canonicalizer Assessment

**Status:** candidate identified for an isolated integration spike; no dependency has been added and no signature-verification claim is made.

## Decision summary

The strongest .NET package candidate found in this review is `MackySoft.Json.Canonicalization` 0.1.0:

- NuGet package: https://www.nuget.org/packages/MackySoft.Json.Canonicalization/0.1.0
- Source repository: https://github.com/mackysoft/dotnet-foundations
- Reviewed source snapshot: `2a0a9f8fd12f37a17cd54dc09904e3d8a484d6fa`
- Reviewed canonicalizer source blob: `68eb9c043a57a8f207d40d109f69239123e85e22`
- Target: `netstandard2.1`, compatible with Gauntlet's `net8.0` core.
- Declared direct dependency: `System.Text.Json >= 8.0.5`.

This is a candidate, not an unconditional trust decision. It is a relatively new package with one published version. Before making it part of the trusted receipt-verification path, pin and review the exact package contents, verify package/source correspondence and license notices, inspect dependency advisories, and run Gauntlet-owned independent vectors in CI.

## Why it is the leading candidate

The package exposes a narrow API that returns caller-owned canonical UTF-8 bytes. Its raw UTF-8 entry point performs strict input checks before canonicalization, including:

- invalid UTF-8 and byte-order mark rejection;
- comments, trailing commas, extra top-level values, and malformed JSON rejection;
- duplicate decoded property-name rejection (including escaped aliases);
- invalid Unicode / unpaired surrogate rejection;
- non-finite or binary64-unrepresentable number rejection;
- negative-zero rejection;
- a maximum raw-input nesting depth of 64.

Its source uses the C# ECMAScript number serializer from the RFC 8785 authors' `cyberphone/json-canonicalization` repository at pinned commit `19d51d7fe467d4706a3ff08adf8a748f29fc21e0`. The package retains upstream headers and includes Apache-2.0 and MPL-2.0 third-party notices.

The package's test suite includes RFC 8785 Appendix G vectors, Appendix B finite-number vectors, strict-input and semantic-rejection cases, duplicate decoded property names, negative zero, depth boundaries, deep parsed-element traversal, and returned-buffer ownership. These are useful upstream tests, but they do not replace Gauntlet's own compatibility vectors or CI verification.

## Updated dependency and security review (2026-11-18)

The NuGet listing still shows only version `0.1.0`, last updated 2026-07-24, with approximately 11.3K downloads at the time of this review. It targets `netstandard2.1`, which is compatible with the Gauntlet `net10.0` core. The package declares `System.Text.Json >= 8.0.5` as a dependency. See the [NuGet package page](https://www.nuget.org/packages/MackySoft.Json.Canonicalization/0.1.0).

That dependency floor is not sufficient as a Workshop security policy. The package audit must inspect the *resolved* dependency graph and pin the System.Text.Json implementation to the current supported .NET 10 servicing line when using this package. As of this review, NuGet lists `System.Text.Json 10.0.15` as the latest .NET 10 servicing package, published 2026-11-10. The November 2026 CVE-2026-120796 advisory applies to .NET 11 preview/runtime deliveries and standalone System.Text.Json 11 prereleases, fixed in 11.0.0; it does not list the .NET 10.0.15 line as affected. See the [System.Text.Json 10.0.15 package page](https://www.nuget.org/packages/System.Text.Json/10.0.15) and [Microsoft's November 2026 advisory](https://github.com/dotnet/runtime/security/advisories/GHSA-cpw3-h88j-9f6x).

The package's declared `8.0.5` floor is the patched version for the older CVE-2024-43485 issue, but it remains an old dependency baseline and should not be allowed to win resolution by accident. The production gate therefore requires an explicit current dependency policy, lock/restore evidence, and a clean vulnerability audit. The .NET 10 runtime itself supplies System.Text.Json, but the package dependency graph still needs to be inspected rather than assumed.

The package page confirms the source repository, the pinned RFC-author source lineage, and included Apache-2.0/MPL-2.0 notices. RFC 8785 Appendix G independently lists the `cyberphone/json-canonicalization` .NET/C# implementation as verified compatible with JCS. The MackySoft package is therefore still the most convenient integration candidate, while the RFC authors' implementation remains the stronger provenance fallback if the package's dependency or maintenance profile is unacceptable.

### Source-level inspection at the reviewed commit

The package source at `2a0a9f8fd12f37a17cd54dc09904e3d8a484d6fa` was inspected directly:

- Canonicalizer source blob: `68eb9c043a57a8f207d40d109f69239123e85e22`.
- The raw-byte API validates strict UTF-8, rejects BOM, comments, trailing commas, extra top-level values, and enforces a 64-level raw parse depth.
- It detects duplicate decoded property names, rejects unpaired UTF-16 surrogates, rejects non-finite/binary64-unrepresentable numbers and negative zero, sorts object property names ordinally, and emits UTF-8 canonical bytes.
- The canonicalizer uses an iterative traversal for parsed elements; upstream tests exercise 10,000-level parsed object/array values without recursive call-stack traversal.
- Upstream tests include RFC 8785 official vectors, Appendix B finite-number vectors, malformed syntax, invalid UTF-8, duplicate escaped aliases, invalid Unicode, number rounding, depth boundaries, and caller-owned output buffers.
- The canonicalizer assembly exports only three public contract types: the canonicalizer, a classified exception, and its failure-kind enum.
- Third-party notices identify embedded upstream material from the RFC authors' repository, V8, David M. Gay, and WebPKI.org. The package carries MIT, Apache-2.0, MPL-2.0, and BSD-style notice obligations. The actual nupkg and all shipped notice files still need to be checked before production acceptance.

Source references: [canonicalizer implementation](https://github.com/mackysoft/dotnet-foundations/blob/2a0a9f8fd12f37a17cd54dc09904e3d8a484d6fa/src/MackySoft.Json.Canonicalization/Rfc8785JsonCanonicalizer.cs), [contract tests](https://github.com/mackysoft/dotnet-foundations/blob/2a0a9f8fd12f37a17cd54dc09904e3d8a484d6fa/tests/MackySoft.Json.Canonicalization.Tests/Rfc8785JsonCanonicalizerContractTests.cs), [official vector tests](https://github.com/mackysoft/dotnet-foundations/blob/2a0a9f8fd12f37a17cd54dc09904e3d8a484d6fa/tests/MackySoft.Json.Canonicalization.Tests/Rfc8785OfficialVectorTests.cs), and [third-party notices](https://github.com/mackysoft/dotnet-foundations/blob/2a0a9f8fd12f37a17cd54dc09904e3d8a484d6fa/src/MackySoft.Json.Canonicalization/THIRD-PARTY-NOTICES.md).

### Gauntlet test-only spike

A dedicated `tests/Gauntlet.Jcs.Tests` project now references `MackySoft.Json.Canonicalization 0.1.0` and directly pins `System.Text.Json 10.0.15`. It does not add the canonicalizer to the shipping core package. CI passed 65 core tests and 44 JCS integration tests, including all six official RFC 8785 JSON vectors and all 23 Appendix B finite-number vectors. It confirmed the resolved graph uses System.Text.Json 10.0.15 (not 8.0.5), reported no vulnerable transitive packages, and completed core, badge, and Sith workflows on the same commit. The committed lock file passed a fresh `--locked-mode` restore on [the verified CI run](https://github.com/TrentBest/TheSingularityWorkshop.Gauntlet/actions/runs/44208233538). The spike exercises Gauntlet's actual signature-excluded DTO projection plus the exact domain separator. The direct System.Text.Json reference is intentional to override the package's older minimum; NU1510 is narrowly suppressed with an explanatory project comment. This is an isolated qualification spike only; it does not constitute package acceptance.

These are research and test-only integration findings, not production package acceptance. The test-only spike now commits `tests/Gauntlet.Jcs.Tests/packages.lock.json`, including NuGet SHA-512 content hashes for the canonicalizer, System.Text.Json 10.0.15, and all test dependencies. CI restores it with `--locked-mode`, which validates the downloaded package against its content hash, and uploads the lock as an auditable artifact. The canonicalizer package content hash is `HJ+v0kv+SilTYDXrGEsLy5pof3W4SHhBmBv01T2OBRaGwjX/a4FaBlYeurLLs2eS8vdqcuo2V5eMfTir01kVPw==`; the System.Text.Json 10.0.15 hash is `KUTJbYaWXRanRCxW46HMH/UnBpLuerq+xrnfWOep0JpUzPRfYdZ4X0DoMOFEkoZQUeFusPdVFLf92L8Agq+3Zg==`. The locked CI run passed 65 core tests, 15 JCS tests, dependency resolution, and the vulnerability audit with no vulnerable transitive packages. Binary/source parity and full legal/notice review remain open. Independent RFC vectors still need expansion.

## Integration constraints

If the package passes the independent integration spike:

1. Use only `Rfc8785JsonCanonicalizer.Canonicalize(ReadOnlySpan<byte>)` on the original bounded UTF-8 receipt payload. Do not parse with permissive settings and then pass a `JsonElement`; that overload explicitly delegates source syntax and parser-depth policy to the caller's parser.
2. Enforce a strict maximum receipt byte length before parsing/canonicalization. The package deliberately does not impose a byte-size limit.
3. Parse and validate the versioned wire schema independently. JCS canonicalization does not validate Gauntlet receipt semantics, required fields, enum vocabulary, identity binding, or unknown-field policy.
4. Remove only the top-level `signature` member from the validated wire object. Keep `signingKeyId` and every other defined field in the signed payload. Reject duplicate property names before any map/object projection can collapse them.
5. Canonicalize the signature-excluded object, prepend the exact UTF-8 domain separator
   `TheSingularityWorkshop.Gauntlet.HostExecutionReceipt.v1\n`, and pass those exact bytes to the provider-selected signature verifier.
6. Keep algorithm selection, key identity, validity, rotation, revocation, and provider trust policy outside the canonicalizer.
7. Treat every parse, schema, canonicalization, signature, and replay failure as a hard rejection.

## Alternatives and trade-offs

### RFC authors' reference implementation

RFC 8785 Appendix G lists the `cyberphone/json-canonicalization` .NET implementation among implementations verified as compatible with JCS. This is the strongest provenance reference. It is source code in a multi-language repository rather than the narrowly packaged NuGet dependency evaluated above. It remains a viable fallback if the package integration audit fails.

### Other NuGet candidate

`Jcs.Net` 0.1.1 was also found, but the available evidence in this review was insufficient to assess its source provenance, test vectors, strict raw-input behavior, and dependency boundary. It is not selected.

## Required spike exit criteria

- [ ] Pin package version and package content hash; verify the package against the reviewed source snapshot.
- [ ] Review the full transitive dependency graph and current advisories.
- [ ] Verify license and third-party notices in the actual nupkg.
- [ ] Add Gauntlet-owned positive and negative vectors, including RFC Appendix B number vectors and escaped duplicate property names.
- [ ] Verify the exact domain-separated bytes and signature-excluded projection independently.
- [ ] Verify unknown schema versions, unknown fields, duplicate names, invalid Unicode, oversized payloads, and malformed signatures all fail closed.
- [ ] Build and run the complete Gauntlet test suite in CI.
- [x] Keep the verifier interface byte-oriented so it receives the exact canonical signed payload, not a domain object that it must independently serialize. The core validation seam accepts non-empty canonical signed bytes and passes them with signing key ID/provider/host identity to the verifier; canonical byte production remains unimplemented.

Until these criteria pass, the current receipt signature verifier remains an injected test scaffold. The version-1 JSON DTO/parser is now defined in `HostReceiptJsonV1.cs` and documented in `RECEIPT_WIRE_V1.md`; it is a frozen wire shape, not a canonicalization or signature implementation.
