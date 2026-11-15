#!/usr/bin/env python3
"""Verify that each badge fixture is valid, accessible, and state-faithful."""

from pathlib import Path
import sys
import xml.etree.ElementTree as ET

SVG = "{http://www.w3.org/2000/svg}"
ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else "artifacts")

EXPECTED = {
    "gauntlet-untested.svg": ("NOT YET TESTED", "#e7b84b", "knight-still"),
    "gauntlet-failed.svg": ("THE GAUNTLET WINS", "#ef5365", "knight-fail"),
    "gauntlet-passed.svg": ("KNIGHT SURVIVED", "#42d392", "knight-pass"),
    "gauntlet-blocked.svg": ("EVIDENCE BLOCKED", "#94a3b8", "knight-still"),
}

for filename, (label, accent, knight_class) in EXPECTED.items():
    path = ROOT / filename
    if not path.is_file():
        raise SystemExit(f"Missing generated badge: {path}")

    root = ET.parse(path).getroot()
    if root.tag != f"{SVG}svg":
        raise SystemExit(f"{filename}: root is not SVG")

    title = root.find(f"{SVG}title")
    desc = root.find(f"{SVG}desc")
    if title is None or not (title.text or "").strip():
        raise SystemExit(f"{filename}: accessible title missing")
    if desc is None or not (desc.text or "").strip():
        raise SystemExit(f"{filename}: accessible description missing")

    ids = [node.attrib["id"] for node in root.iter() if "id" in node.attrib]
    if len(ids) != len(set(ids)):
        raise SystemExit(f"{filename}: duplicate SVG IDs")

    text = " ".join((node.text or "") for node in root.iter(f"{SVG}text"))
    if label not in text:
        raise SystemExit(f"{filename}: expected state label missing")

    border = root.find(f".//{SVG}rect[@id='badge-border']")
    if border is None or border.attrib.get("stroke") != accent:
        raise SystemExit(f"{filename}: state border color mismatch")

    knight = root.find(f".//{SVG}g[@id='knight']")
    if knight is None or knight.attrib.get("class") != knight_class:
        raise SystemExit(f"{filename}: knight state mapping mismatch")

    styles = root.find(f".//{SVG}style")
    if styles is None or "prefers-reduced-motion" not in (styles.text or ""):
        raise SystemExit(f"{filename}: reduced-motion fallback missing")

    for node in root.iter():
        href = node.attrib.get("href")
        if href and not href.startswith("#"):
            raise SystemExit(f"{filename}: external reference is forbidden: {href}")

print(f"Verified {len(EXPECTED)} GUI.SVG badge state fixtures in {ROOT}.")
