#!/usr/bin/env python3
"""Verify the dependency-machine SVG's state-specific visual and accessibility contracts."""
from pathlib import Path
import re
import sys

if len(sys.argv) != 3 or sys.argv[2] not in {"failed", "passed"}:
    raise SystemExit("usage: verify-dependency-machine-fixture.py <svg-path> <failed|passed>")

path = Path(sys.argv[1])
expected_state = sys.argv[2]
svg = path.read_text(encoding="utf-8")
common = [
    "THE GAUNTLET",
    "DEPENDENCY MACHINE",
    "PREVIEW FIXTURE",
    "fsm-api",
    "gui-core",
    "serialization",
    "gui-wpf",
    "renderer",
    "physics",
    "pendulum-swing",
    "knight-impact",
    "prefers-reduced-motion",
]
state_specific = {
    "failed": [
        "STRUCK DOWN",
        "traveler-flying-helmet",
        "GATE HELD",
        "final-gate-closed",
        "FAILED",
        "BLOCKED",
    ],
    "passed": [
        "COURSE CLEARED",
        "ALL CLEAR",
        "final-gate-open",
        "PASSED",
    ],
}
missing = [marker for marker in common + state_specific[expected_state] if marker not in svg]
if missing:
    raise SystemExit(f"{expected_state} dependency-machine SVG is missing expected labels: {', '.join(missing)}")

ids = re.findall(r'\bid="([^"]+)"', svg)
if len(ids) != len(set(ids)):
    raise SystemExit("dependency-machine SVG contains duplicate element IDs")

for forbidden in ("<script", "foreignObject", "javascript:", "https://"):
    if forbidden in svg:
        raise SystemExit(f"dependency-machine SVG contains forbidden content: {forbidden}")

namespace = 'xmlns="http://www.w3.org/2000/svg"'
if "http://" in svg.replace(namespace, ""):
    raise SystemExit("dependency-machine SVG contains an external HTTP reference")

if expected_state == "failed" and "final-gate-open" in svg:
    raise SystemExit("failed dependency-machine fixture must never open the final gate")
if expected_state == "passed" and "final-gate-closed" in svg:
    raise SystemExit("passed dependency-machine fixture must open the final gate")

print(f"Verified {expected_state} dependency-machine preview: {path}")
