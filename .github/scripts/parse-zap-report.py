#!/usr/bin/env python3
"""Parse ZAP JSON report -> OpsWatch pentest/latest.json"""

import argparse
import json
import os
import re
import sys
from datetime import datetime, timezone

CWE_TO_OWASP = {
    "22": "A01:2021", "284": "A01:2021", "285": "A01:2021", "639": "A01:2021",
    "261": "A02:2021", "311": "A02:2021", "312": "A02:2021", "319": "A02:2021",
    "326": "A02:2021", "327": "A02:2021", "330": "A02:2021",
    "77": "A03:2021", "78": "A03:2021", "79": "A03:2021", "89": "A03:2021", "94": "A03:2021",
    "73": "A04:2021", "209": "A04:2021",
    "16": "A05:2021", "611": "A05:2021", "614": "A05:2021",
    "1035": "A06:2021",
    "287": "A07:2021", "384": "A07:2021", "521": "A07:2021",
    "345": "A08:2021", "829": "A08:2021",
    "778": "A09:2021",
    "918": "A10:2021",
}

OWASP_NAMES = {
    "A01:2021": "Broken Access Control",
    "A02:2021": "Cryptographic Failures",
    "A03:2021": "Injection",
    "A04:2021": "Insecure Design",
    "A05:2021": "Security Misconfiguration",
    "A06:2021": "Vulnerable and Outdated Components",
    "A07:2021": "Identification and Authentication Failures",
    "A08:2021": "Software and Data Integrity Failures",
    "A09:2021": "Security Logging and Monitoring Failures",
    "A10:2021": "Server-Side Request Forgery",
}

RISK_MAP = {"3": "High", "2": "Medium", "1": "Low", "0": "Informational"}


def strip_html(text):
    if not text:
        return ""
    text = re.sub(r"<[^>]+>", " ", text)
    return re.sub(r"\s+", " ", text).strip()


def get_status(summary):
    if summary["high"] > 0:
        return "vulnerable"
    if summary["medium"] > 0:
        return "attention"
    return "approved"


def parse(input_path, output_path, target, run_id):
    now = datetime.now(timezone.utc).isoformat()

    if not os.path.exists(input_path):
        print(f"[WARN] {input_path} nao encontrado — gerando relatorio vazio")
        result = {
            "generated": now, "target": target, "scan_type": "api",
            "run_id": run_id,
            "error": "Relatorio ZAP nao gerado — verifique se a API esta acessivel e o Swagger esta exposto",
            "summary": {"high": 0, "medium": 0, "low": 0, "informational": 0, "total": 0},
            "status": "error", "findings": [],
        }
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        with open(output_path, "w") as f:
            json.dump(result, f, indent=2, ensure_ascii=False)
        return

    with open(input_path, "r", encoding="utf-8") as f:
        raw = json.load(f)

    alerts = []
    for site in raw.get("site", []):
        alerts.extend(site.get("alerts", []))

    summary = {"high": 0, "medium": 0, "low": 0, "informational": 0, "total": 0}
    findings = []

    for alert in alerts:
        risk_code = str(alert.get("riskcode", "0"))
        risk = RISK_MAP.get(risk_code, "Informational")
        risk_key = risk.lower() if risk.lower() in summary else "informational"
        summary[risk_key] += 1
        summary["total"] += 1

        cwe = str(alert.get("cweid", ""))
        owasp_id = CWE_TO_OWASP.get(cwe, "N/A")
        owasp_name = OWASP_NAMES.get(owasp_id, "Outros")

        instances = [
            {
                "url": i.get("uri", ""),
                "method": i.get("method", ""),
                "param": i.get("param", ""),
                "evidence": i.get("evidence", ""),
            }
            for i in alert.get("instances", [])
        ]

        findings.append({
            "id": alert.get("pluginid", ""),
            "name": alert.get("name", alert.get("alert", "")),
            "risk": risk,
            "risk_code": int(risk_code),
            "confidence": alert.get("confidence", ""),
            "owasp": owasp_id,
            "owasp_name": owasp_name,
            "description": strip_html(alert.get("desc", "")),
            "solution": strip_html(alert.get("solution", "")),
            "reference": strip_html(alert.get("reference", "")),
            "cwe": cwe,
            "count": int(alert.get("count", len(instances))),
            "instances": instances[:5],
        })

    findings.sort(key=lambda x: x["risk_code"], reverse=True)

    result = {
        "generated": now,
        "target": target,
        "scan_type": "api",
        "run_id": run_id,
        "summary": summary,
        "status": get_status(summary),
        "findings": findings,
    }

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2, ensure_ascii=False)

    print(f"[OK] {len(findings)} findings | High:{summary['high']} Med:{summary['medium']} Low:{summary['low']} Info:{summary['informational']}")
    print(f"[OK] Status: {result['status']} | Output: {output_path}")


if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("--input",   default="zap_report.json")
    p.add_argument("--output",  default="pentest-output/latest.json")
    p.add_argument("--target",  default="https://comprai.2.25.122.11.nip.io")
    p.add_argument("--run-id",  default="")
    args = p.parse_args()
    parse(args.input, args.output, args.target, args.run_id)
