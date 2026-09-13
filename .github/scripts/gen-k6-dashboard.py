#!/usr/bin/env python3
"""
gen-k6-dashboard.py
Agrega métricas dos cenários k6 e envia para a API da VPS via POST /api/k6/runs.
O dashboard HTML é servido pela própria API — sem dependência do GitHub Pages.
"""
import json, os, pathlib, shutil, urllib.request, urllib.error

run   = os.environ.get('RUN_NUMBER', '0')
ttype = os.environ.get('TEST_TYPE', 'all')
url   = os.environ.get('TARGET_URL', 'http://2.25.122.11:5020')
vus   = os.environ.get('VUS_OVERRIDE', 'padrao')
api   = os.environ.get('API_BASE', 'http://2.25.122.11:5020')

scenarios = ['smoke', 'load', 'stress', 'spike', 'soak']
rows = []

# Copiar dashboards individuais para dist/ (mantido para artifacts)
dist = pathlib.Path('dist')
dist.mkdir(exist_ok=True)

for sc in scenarios:
    src = pathlib.Path(f'dashboards/{sc}')
    if not src.exists():
        continue
    dst = dist / sc
    dst.mkdir(exist_ok=True)
    for f in ['dashboard.html', 'summary.json']:
        if (src / f).exists():
            shutil.copy(src / f, dst / f)
    p = src / 'summary.json'
    if not p.exists():
        continue
    try:
        s    = json.loads(p.read_text())
        dur  = s.get('metrics',{}).get('http_req_duration',{}).get('values',{})
        reqs = s.get('metrics',{}).get('http_reqs',{}).get('values',{})
        errs = s.get('metrics',{}).get('http_req_failed',{}).get('values',{})
        chks = s.get('metrics',{}).get('checks',{}).get('values',{})
        ok   = all(
            (v if isinstance(v, bool) else v.get('ok', False))
            for md in s.get('metrics', {}).values()
            for v in md.get('thresholds', {}).values()
        )
        thresholds = []
        for metric, md in s.get('metrics', {}).items():
            for cond, val in md.get('thresholds', {}).items():
                ok_thr = val if isinstance(val, bool) else val.get('ok', False)
                mvals  = md.get('values', {})
                cur = round(mvals.get('p(95)', mvals.get('rate', 0)), 4)
                thresholds.append({'metric': metric, 'condition': cond, 'actual': cur, 'passed': ok_thr})
        rows.append({
            'sc': sc,
            'p50':  round(dur.get('med', 0), 1),
            'p95':  round(dur.get('p(95)', 0), 1),
            'p99':  round(dur.get('p(99)', 0), 1),
            'avg':  round(dur.get('avg', 0), 1),
            'rps':  round(reqs.get('rate', 0), 2),
            'total': int(reqs.get('count', 0)),
            'err':  round(errs.get('rate', 0) * 100, 2),
            'checks_pass': int(chks.get('passes', 0)),
            'checks_fail': int(chks.get('fails', 0)),
            'ok':  ok,
            'thresholds': thresholds,
        })
    except Exception as e:
        print(f'Erro ao processar {sc}: {e}')

# Montar payload do run
if rows:
    # KPIs agregados do smoke (cenário mais limpo)
    smoke = next((r for r in rows if r['sc'] == 'smoke'), rows[0])
    all_thr = [t for r in rows for t in r['thresholds']]
else:
    smoke   = {}
    all_thr = []

payload = {
    'run':  run,
    'meta': {
        'run_number': run,
        'test_type':  ttype,
        'target_url': url,
        'vus':        vus,
        'run_date':   __import__('datetime').datetime.utcnow().strftime('%Y-%m-%d %H:%M UTC'),
    },
    'kpis': {
        'p50':           smoke.get('p50'),
        'p95':           smoke.get('p95'),
        'p99':           smoke.get('p99'),
        'avg':           smoke.get('avg'),
        'rps':           smoke.get('rps'),
        'total_requests':smoke.get('total'),
        'error_rate':    round(smoke.get('err', 0) / 100, 4) if smoke else None,
    },
    'scenarios': rows,
    'thresholds': all_thr,
}

body = json.dumps(payload, ensure_ascii=False).encode('utf-8')

# Postar na API
try:
    req = urllib.request.Request(
        f'{api}/api/k6/runs',
        data=body,
        headers={'Content-Type': 'application/json'},
        method='POST',
    )
    resp = urllib.request.urlopen(req, timeout=15)
    print(f'✓ Dados enviados para API: {resp.status} — run #{run}, {len(rows)} cenários')
except urllib.error.URLError as e:
    print(f'⚠ Não foi possível enviar para API ({e}) — dados salvos apenas no artifact')

# Gerar index.html simples para artifact (sem placeholder)
summary_html = f"""<!doctype html>
<html lang="pt-BR">
<head><meta charset="utf-8"><title>K6 Run #{run}</title>
<style>body{{background:#0f172a;color:#e2e8f0;font-family:system-ui;padding:32px}}
h1{{color:#38bdf8}}table{{border-collapse:collapse;width:100%}}
th,td{{padding:8px 12px;border-bottom:1px solid #334155;text-align:left}}
th{{color:#64748b;font-size:.8rem;text-transform:uppercase}}
.ok{{color:#22c55e}}.fail{{color:#ef4444}}</style></head>
<body>
<h1>📊 K6 Dashboard — Run #{run}</h1>
<p style="color:#94a3b8">{ttype.upper()} | {url} | VUs: {vus}</p>
<p>👉 <a href="https://comprai.2.25.122.11.nip.io/k6" style="color:#38bdf8">Abrir dashboard completo na VPS</a></p>
<table><thead><tr><th>Cenário</th><th>p50</th><th>p95</th><th>p99</th><th>Req/s</th><th>Total</th><th>Erro%</th><th>Status</th></tr></thead>
<tbody>{"".join(f'<tr><td>{r["sc"]}</td><td>{r["p50"]}ms</td><td>{r["p95"]}ms</td><td>{r["p99"]}ms</td><td>{r["rps"]}</td><td>{r["total"]:,}</td><td>{r["err"]}%</td><td class="{"ok" if r["ok"] else "fail"}">{"✅ OK" if r["ok"] else "❌ FALHOU"}</td></tr>' for r in rows)}
</tbody></table>
</body></html>"""

(dist / 'index.html').write_text(summary_html, encoding='utf-8')
print(f'✓ dist/index.html gerado para artifact ({len(rows)} cenários)')
