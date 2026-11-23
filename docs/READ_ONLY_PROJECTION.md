# Read-only dependency projection

`DependencyGraphReadOnlyProjection.Create` maps a versioned graph snapshot into a deterministic, presentation-safe view for reports and renderers.

- Valid snapshots are ordered by the evaluator's deterministic topological order.
- Invalid snapshots remain visibly `Invalid`; their stages are listed in ordinal stage-ID order for diagnosis.
- The projection contains candidate identifiers, stage state, attempt identity, prerequisite IDs, and bounded evidence diagnostic metadata: validation state, profile ID, provider run ID, provider ID, and report digest.
- It deliberately excludes evidence payloads, evidence bundle references, and external evidence URLs. A renderer can identify which evidence decision it is displaying without gaining a dereference path or access to raw evidence.
- The projection is immutable from the caller's perspective and copies its stage and prerequisite collections.

**Trust boundary:** this is a local structural projection only. A valid snapshot digest is not an issuer signature, provider authentication, a receipt, or permission to execute. `IsStructurallyValid` and `EvidenceValidationState` are descriptive fields, not trust decisions. CiAi remains responsible for authenticating the issuer/provider, independently validating evidence, scheduling work, and authorizing gates. Consumers must not treat `IsStructurallyValid`, `EvidenceValidationState == Accepted`, or `Status == Passed` as an authorization decision.
