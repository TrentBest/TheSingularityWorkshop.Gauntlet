#!/usr/bin/env python3
"""Generate deterministic preview-only evidence-quality state fixtures."""
from copy import deepcopy
from hashlib import sha256
import json
from pathlib import Path
import sys

if len(sys.argv) != 3:
    raise SystemExit("usage: generate-dependency-machine-state-fixtures.py <source-fixture> <output-directory>")

source = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
output = Path(sys.argv[2])
output.mkdir(parents=True, exist_ok=True)

states = {
    "incomplete": (5, 1),
    "invalid": (6, 3),
    "untrusted": (7, 3),
}

for name, (status, evidence_state) in states.items():
    fixture = deepcopy(source)
    fixture["snapshotId"] = f"preview-fixture-{name}-v2"
    fixture["pipelineAttemptId"] = f"preview-only-{name}"

    for stage in fixture["stages"]:
        if stage["stageId"] == "fsm-api":
            stage["status"] = status
            stage["evidence"]["validationState"] = evidence_state
            stage["evidence"]["pipelineAttemptId"] = fixture["pipelineAttemptId"]
        else:
            stage["status"] = 4
            stage["evidence"] = None

    canonical = deepcopy(fixture)
    canonical["snapshotDigest"] = None
    canonical["stages"] = sorted(canonical["stages"], key=lambda item: item["stageId"])
    for stage in canonical["stages"]:
        stage["upstreamStageIds"] = sorted(stage["upstreamStageIds"])
    payload = json.dumps(canonical, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    fixture["snapshotDigest"] = "sha256:" + sha256(payload).hexdigest()
    (output / f"dependency-machine-{name}.json").write_text(
        json.dumps(fixture, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"Generated {name}: {fixture['snapshotDigest']}")
