#!/usr/bin/env python3
"""Verify the static dependency-machine SVG is a complete, accessible preview."""
from pathlib import Path
import sys

if len(sys.argv) != 2:
    raise SystemExit("usage: verify-dependency-machine-fixture.py <svg-path>")

path = Path(sys.argv[1])
svg = path.read_text(encoding="utf-8")
required = [
    "THE GAUNTLET",
    "DEPENDENCY MACHINE",
    "PREVIEW FIXTURE",
    "fsm-api",
    "gui-core",
    "serialization",
    "gui-wpf",
    "renderer",
    "physics",
    "PASSED",
    "FAILED",
    "BLOCKED",
    "GATE HELD",
]
missing = [marker for marker in required if marker not in svg]
if missing:
    raise SystemExit(f"dependency-machine SVG is missing expected labels: {', '.join(missing)}")

for forbidden in ("<script", "foreignObject", "javascript:", "https://"):
    if forbidden in svg:
        raise SystemExit(f"dependency-machine SVG contains forbidden content: {forbidden}")

namespace = 'xmlns="http://www.w3.org/2000/svg"'
if "http://" in svg.replace(namespace, ""):
    raise SystemExit("dependency-machine SVG contains an external HTTP reference")

print(f"Verified static dependency-machine preview: {path}")
