#!/usr/bin/env python3
"""Fail when Domain or Application line coverage is below NFR-058 / NFR-059."""
from __future__ import annotations

import argparse
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

DOMAIN_MIN = 80.0
APPLICATION_MIN = 70.0
PACKAGES = {
    "TwinParticles.CheckEngine.Domain": DOMAIN_MIN,
    "TwinParticles.CheckEngine.Application": APPLICATION_MIN,
}


def find_cobertura(results_dir: Path) -> Path:
    matches = sorted(results_dir.rglob("coverage.cobertura.xml"))
    if not matches:
        raise FileNotFoundError(f"coverage.cobertura.xml not found under {results_dir}")
    return matches[0]


def package_line_percent(root: ET.Element) -> dict[str, float]:
    rates: dict[str, float] = {}
    for package in root.findall(".//package"):
        name = package.attrib.get("name") or ""
        if name in PACKAGES:
            rates[name] = float(package.attrib.get("line-rate") or 0.0) * 100.0
    return rates


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--results-dir",
        default="TestResults",
        help="Directory that contains coverlet cobertura output",
    )
    args = parser.parse_args()
    results_dir = Path(args.results_dir)
    cobertura = find_cobertura(results_dir)
    root = ET.parse(cobertura).getroot()
    rates = package_line_percent(root)

    failures: list[str] = []
    for package, minimum in PACKAGES.items():
        actual = rates.get(package)
        if actual is None:
            failures.append(f"{package}: missing from {cobertura}")
            continue
        status = "ok" if actual + 1e-9 >= minimum else "FAIL"
        print(f"[checkengine-coverage] {package}: {actual:.2f}% (min {minimum:.0f}%) {status}")
        if actual + 1e-9 < minimum:
            failures.append(f"{package}: {actual:.2f}% < {minimum:.0f}%")

    if failures:
        print("[checkengine-coverage] NFR-058/NFR-059 gate failed:", "; ".join(failures), file=sys.stderr)
        return 1

    print("[checkengine-coverage] Domain ≥ 80% and Application ≥ 70% (NFR-058 / NFR-059)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
