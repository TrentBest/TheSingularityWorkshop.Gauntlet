# SURPS conformance profile v1

**Status:** fixture-validated profile definition; not a verified SURPS runtime adapter.

This profile is the first concrete Gauntlet/SURPS integration artifact. Its closed [JSON Schema](schemas/surps-conformance-profile-v1.schema.json) defines the v1 interchange shape; the fixture test independently checks the pinned example and its evidence binding. It pins one tabletop sourcebook and makes its selection explicit:

- SURPS is opted in for this campaign.
- Exactly one rulebook is selected: `surps.core.spying` version `0.1.0`, CORE LOD 0.
- Its scope is `campaign:core-spying`.
- No higher-LOD book is silently selected or treated as an override.
- The scenario points to the offline tabletop procedure, which remains the authoritative playable procedure for this fixture.

## Evidence boundary

The profile's four required probes cover only:

1. opt-in selection is explicit;
2. rulebook ID and version are pinned;
3. LOD and scope are explicit;
4. an offline procedure is referenced.

The fixture does **not** execute a digital spying simulator, instantiate domain providers, prove game balance, or certify Physics/Gestures/Dungeon/City behavior. The sourcebook remains a tabletop playtest draft. No candidate-supplied code is executed, and this profile does not claim host isolation.

## Identity and digest

The profile pins the SURPS repository commit and source descriptor/procedure paths. `profileDigest` is SHA-256 over the compact UTF-8 JSON representation of the complete profile object with the `profileDigest` property omitted. Property order is the file order. A consumer must compare the digest and exact source commit; branch names are not identity.

Gauntlet's existing `CampaignProfile` and `RunReport` contracts bind the run to this profile ID, version, and digest. The evaluator remains the existing deterministic evaluator; this fixture does not create a second verdict system.

## Next integration step

A future adapter may translate a validated profile into SURPS/FSM_COS composition requests. That adapter must be added only after SURPS and FSM_COS agree on the stable provider and composition contracts. It must preserve per-experience opt-in, exact rulebook versions, scope, and LOD selection. Gauntlet will continue to own probes and evidence, not rulebook semantics or runtime composition.
