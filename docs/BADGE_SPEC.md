# The Gauntlet Badge Specification

## Creative direction

An original, heraldic software knight faces a mechanical proving course: rotating blades, swinging hammers, pressure plates, gates, and a final exit. The visual language should evoke a medieval tournament contraption and the dramatic, physical spectacle of a knight attempting an impossible course. It must be original artwork, not a reproduction of a film frame, costume, actor likeness, or existing logo.

The knight is a visual metaphor for the candidate software. The apparatus represents adversarial probes. The badge communicates the current candidate's verified result.

## States and choreography

### Untested — yellow

- Gold/yellow border and restrained label.
- Knight waits at the starting line.
- Apparatus is completely still.
- Show no score or survival statistics unless there is a verified historical cohort; no completed run means no average.

### Failed — red

- Red border and explicit FAILED label.
- Knight repeatedly enters the course.
- A hazard catches the knight; armor/shield/helmet scatter or the knight is knocked back.
- Reset and repeat the defeat loop.
- The current score and history metrics remain readable and tied to the exact run.
- Animation is theatrical presentation of a recorded failure, not an active exploit attempt.

### Passed — green

- Green border and explicit PASSED label.
- Knight swiftly traverses the course.
- Knight reaches the far side, raises both hands in celebration, holds briefly, then restarts.
- Do not imply that a pass authorizes release.

### Blocked / unknown — neutral

- Neutral gray/amber treatment, never green.
- Apparatus may be paused with a warning marker.
- Explain that evidence is stale, incomplete, invalid, or untrusted.
- This state must not be confused with verified never-tested yellow.

## Metrics

The compact badge should prioritize:
- current verdict and exact candidate identity;
- current run score;
- average score for the comparable profile cohort;
- delta versus the previous comparable run;
- survived count;
- failed count.

If space is constrained, show verdict and score on the badge and put detailed metrics in the linked report. Do not omit provenance to fit decorative statistics.

## Implementation contract

- Author the scene as a GUI semantic tree and serialize it with TheSingularityWorkshop.Workshop.Gui.Svg.SvgGuiRenderer.
- Keep status-to-scene mapping deterministic and covered by tests.
- Use only local SVG definitions and resources; no external images, scripts, fonts, or network references.
- Use declarative SVG/CSS animation only. The badge does not run FSM_API, execute probes, or poll a test process.
- Provide a reduced-motion/static presentation and accessible title/description.
- Validate all status states, unknown values, stale SHAs, missing fields, and malformed metrics.
- Keep the generated SVG reproducible: the sample regenerates it and CI compares it byte-for-byte with the checked-in snapshot.

## Embed contract

A repository may embed the badge in a README or CI summary. The image/status URL must resolve to a status projection for the exact candidate SHA. The badge links to the full report. A branch-level alias may exist for convenience, but the displayed current verdict must still identify the resolved commit SHA.
