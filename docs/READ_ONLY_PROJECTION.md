# Read-only dependency projection

`DependencyGraphReadOnlyProjection.Create` maps a versioned graph snapshot into a deterministic, presentation-safe view for reports and renderers.

- Valid snapshots are ordered by the evaluator's deterministic topological order.
- Invalid snapshots remain visibly `Invalid`; their stages are listed in ordinal stage-ID order for diagnosis.
- The projection contains candidate identifiers, stage state, attempt identity, and prerequisite IDs. It deliberately omits raw evidence payloads.
- The projection is immutable from the caller's perspective and copies its stage and prerequisite collections.

**Trust boundary:** this is a local structural projection only. A valid snapshot digest is not an issuer signature, provider authentication, a receipt, or permission to execute. CiAi remains responsible for authenticating the issuer/provider, independently validating evidence, scheduling work, and authorizing gates. Consumers must not treat `IsStructurallyValid` or `Status == Passed` as an authorization decision.
