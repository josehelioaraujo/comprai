#!/usr/bin/env python3
"""
Parseia StrykerOutput/reports/mutation-report.json
e gera mutation-output/latest.json para o QA Hub.
"""

import json
import os
import sys
from datetime import datetime, timezone
from pathlib import Path

REPORT_PATH = Path("StrykerOutput/reports/mutation-report.json")
OUT_DIR = Path("mutation-output")
OUT_FILE = OUT_DIR / "latest.json"


def badge_status(score: float) -> str:
    if score >= 70:
        return "green"
    elif score >= 50:
        return "yellow"
    return "red"


def parse_file_metrics(files: dict) -> list:
    results = []
    for filepath, file_data in files.items():
        mutants = file_data.get("mutants", [])
        total = len(mutants)
        killed = sum(1 for m in mutants if m.get("status") == "Killed")
        survived = sum(1 for m in mutants if m.get("status") == "Survived")
        timeout = sum(1 for m in mutants if m.get("status") == "Timeout")
        no_coverage = sum(1 for m in mutants if m.get("status") == "NoCoverage")
        score = round((killed / total * 100), 1) if total > 0 else 0.0

        results.append({
            "file": filepath,
            "total": total,
            "killed": killed,
            "survived": survived,
            "timeout": timeout,
            "no_coverage": no_coverage,
            "score": score,
            "status": badge_status(score)
        })

    results.sort(key=lambda x: x["score"])
    return results


def main():
    if not REPORT_PATH.exists():
        print(f"[ERROR] Arquivo nao encontrado: {REPORT_PATH}", file=sys.stderr)
        sys.exit(1)

    with open(REPORT_PATH, "r", encoding="utf-8") as f:
        report = json.load(f)

    schema_version = report.get("schemaVersion", "unknown")
    system = report.get("system", {})
    thresholds = system.get("thresholds", {"high": 80, "low": 70})

    files = report.get("files", {})
    file_metrics = parse_file_metrics(files)

    total = sum(f["total"] for f in file_metrics)
    killed = sum(f["killed"] for f in file_metrics)
    survived = sum(f["survived"] for f in file_metrics)
    timeout = sum(f["timeout"] for f in file_metrics)
    no_coverage = sum(f["no_coverage"] for f in file_metrics)
    score = round((killed / total * 100), 1) if total > 0 else 0.0

    run_number = os.environ.get("GITHUB_RUN_NUMBER", "0")
    sha = os.environ.get("GITHUB_SHA", "unknown")[:7]
    timestamp = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    output = {
        "run": run_number,
        "sha": sha,
        "timestamp": timestamp,
        "schema_version": schema_version,
        "score": score,
        "status": badge_status(score),
        "thresholds": thresholds,
        "summary": {
            "total": total,
            "killed": killed,
            "survived": survived,
            "timeout": timeout,
            "no_coverage": no_coverage
        },
        "files": file_metrics
    }

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    with open(OUT_FILE, "w", encoding="utf-8") as f:
        json.dump(output, f, ensure_ascii=False, indent=2)

    print(f"[OK] latest.json gerado -- Score: {score}% | Killed: {killed}/{total}")
    print(f"     Survived: {survived} | Timeout: {timeout} | NoCoverage: {no_coverage}")
    print(f"     Run #{run_number} | SHA: {sha}")


if __name__ == "__main__":
    main()