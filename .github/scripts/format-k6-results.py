#!/usr/bin/env python3
import json, sys, os
from datetime import datetime

def format_k6_results():
    try:
        with open('summary.json', 'r') as f:
            k6_data = json.load(f)
    except FileNotFoundError:
        print("summary.json not found!")
        sys.exit(1)
    meta = {
        "test_type": os.environ.get("K6_TEST_TYPE", "stress"),
        "target_url": os.environ.get("K6_TARGET_URL", "https://api.seu-dominio.com"),
        "run_date": datetime.now().strftime("%Y-%m-%d"),
        "run_number": os.environ.get("GITHUB_RUN_NUMBER", "0"),
        "vus": int(os.environ.get("K6_VUS", "0")),
        "duration": os.environ.get("K6_DURATION", "0s")
    }
    scenarios = []
    if "metrics" in k6_data:
        metrics = k6_data["metrics"]
        scenario_names = set()
        for metric_name in metrics.keys():
            if "{" in metric_name:
                tag_part = metric_name.split("{")[1].replace("}", "")
                if "scenario:" in tag_part:
                    scenario_names.add(tag_part.split("scenario:")[1])
        for scenario_name in ["smoke", "load", "stress", "spike", "soak"]:
            if scenario_name in scenario_names:
                scenarios.append({
                    "sc": scenario_name,
                    "p50": get_metric(metrics, "http_req_duration", scenario_name, "p50", "value"),
                    "p95": get_metric(metrics, "http_req_duration", scenario_name, "p95", "value"),
                    "p99": get_metric(metrics, "http_req_duration", scenario_name, "p99", "value"),
                    "avg": get_metric(metrics, "http_req_duration", scenario_name, "avg", "value"),
                    "rps": get_metric(metrics, "http_reqs", scenario_name, "rate", "value"),
                    "total": int(get_metric(metrics, "http_reqs", scenario_name, "value", "value") or 0),
                    "err": float(get_metric(metrics, "http_req_failed", scenario_name, "value", "value") or 0) / max(1, int(get_metric(metrics, "http_reqs", scenario_name, "value", "value") or 1)),
                    "checks_pass": int(get_metric(metrics, "checks", scenario_name, "pass", "value") or 0),
                    "checks_fail": int(get_metric(metrics, "checks", scenario_name, "fail", "value") or 0),
                    "ok": True,
                    "thresholds": []
                })
    if not scenarios:
        scenarios = [{"sc": "smoke", "p50": 0, "p95": 0, "p99": 0, "avg": 0, "rps": 0, "total": 0, "err": 0, "checks_pass": 0, "checks_fail": 0, "ok": True, "thresholds": []}]
    dashboard_data = {"meta": meta, "scenarios": scenarios}
    timestamp = datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")
    output_file = f"k6-result-{timestamp}.json"
    with open(output_file, 'w') as f:
        json.dump(dashboard_data, f, indent=2)
    print(f"Result: {output_file}")
    return output_file

def get_metric(metrics, metric_name, scenario, stat, default_key="value"):
    try:
        key = f"{metric_name}{{scenario:{scenario}}}"
        if key in metrics:
            if stat in metrics[key]:
                return metrics[key][stat]
            elif default_key in metrics[key]:
                return metrics[key][default_key]
    except (KeyError, TypeError):
        pass
    return None

if __name__ == "__main__":
    format_k6_results()