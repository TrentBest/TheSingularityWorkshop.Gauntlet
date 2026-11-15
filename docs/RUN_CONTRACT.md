# Gauntlet Run Contract

**Status:** proposed v1 domain contract. Field names and serialization format remain subject to implementation and contract tests.

## Identity

Every report must identify the exact thing that was tested:

| Field | Meaning |
|---|---|
| schemaVersion | Version of the report contract |
| repository | Canonical owner/repository identity |
| candidateSha | Full immutable source commit SHA |
| artifactDigest | Digest of the exact candidate artifact |
| manifestDigest | Digest of the MicroBundle/Experience manifest, when applicable |
| gauntletVersion | Runner implementation version |
| profileId / profileVersion | Immutable campaign definition |
| environmentId | Runner image/runtime/platform identity |
| runId / attempt | Unique campaign and retry identity |
| startedAt / completedAt | UTC timestamps |
| reportDigest | Digest of canonical report content, excluding the digest field itself |

A branch name is descriptive metadata only. It never replaces candidateSha.

## Run states

- running: campaign has started; no final verdict.
- passed: all required probes completed and all gate invariants passed.
- failed: a required invariant failed or a blocking finding was produced.
- incomplete: one or more required probes did not complete.
- cancelled: campaign was explicitly cancelled.
- timed-out: a declared execution limit was reached.
- invalid: report, identity, schema, digest, or probe outcome validation failed.
- untrusted: provider or workflow provenance has not been independently trusted.

Only passed can be a positive gate input. Incomplete, cancelled, timed-out, invalid, and untrusted are not equivalent to a completed failure, but none can satisfy a required gate.

## Probe result

Each probe result includes stable probe ID/version, required/optional status, outcome, assertion counts, expected invariant, observed result, severity, bounded diagnostic references, duration, and deterministic replay identity where applicable.

The probe inventory is part of the report. A consumer must compare it with the immutable profile's required inventory; a candidate cannot declare its own probes complete.

## Score

For a complete campaign:

score = 100 × passedRequiredAssertionWeight / totalRequiredAssertionWeight

The profile defines assertion weights. Score is null until all required assertions have terminal outcomes. A score of 100 does not override a blocking finding or a failed required invariant.

Comparable averages and deltas require the same profile identity/version and scoring policy. The report must state the number of runs in the average cohort and identify the preceding comparable run used for delta.

## History counters

- campaignsCompleted: all completed pass/fail campaigns in the selected cohort.
- survived: completed campaigns with a passing verdict.
- failed: completed campaigns with a failing verdict.
- incomplete: campaigns that did not reach a valid completed verdict.

Thus survived + failed = campaignsCompleted for the same cohort. Retries remain distinct attempts and are never silently collapsed.

## Gate evaluation

A consumer evaluates:
1. exact repository and candidate SHA match;
2. exact artifact and manifest digest match;
3. report digest and schema validity;
4. provider/profile/version trust;
5. required probe inventory completeness;
6. all required invariant outcomes;
7. blocking finding severity policy;
8. freshness and attempt ordering.

Any unknown or missing required predicate is non-passing. CiAi owns the final evidence trust and gate decision.

## Badge projection

The status projection must identify candidate SHA, run ID/attempt, profile version, verdict, score (when valid), comparable average/delta, and history counters.

The display mapping is:
- yellow: verified never-tested state for this candidate;
- red: valid completed failing run for this candidate;
- green: valid completed passing run for this candidate;
- neutral: stale, missing-but-ambiguous, incomplete, invalid, or untrusted state.

A status endpoint must not serve a prior candidate's green result as the current candidate's result.
