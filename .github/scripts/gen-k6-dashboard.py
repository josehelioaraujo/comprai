#!/usr/bin/env python3
"""
gen-k6-dashboard.py (ATUALIZADO)
Agrega métricas dos cenários k6, gera dashboards HTML, e envia para a API da VPS.
"""
import json, os, pathlib, shutil, urllib.request, urllib.error, datetime

run   = os.environ.get('RUN_NUMBER', '0')
ttype = os.environ.get('TEST_TYPE', 'all')
url   = os.environ.get('TARGET_URL', 'http://2.25.122.11:5020')
vus   = os.environ.get('VUS_OVERRIDE', 'padrao')
sha   = os.environ.get('GIT_SHA', '')[:7]
api   = os.environ.get('API_BASE', 'http://2.25.122.11:5020')

scenarios = ['smoke', 'load', 'stress', 'spike', 'soak']
rows = []

# Copiar dashboards individuais para dist/ e k6/results/ (para upload)
dist = pathlib.Path('dist')
dist.mkdir(exist_ok=True)

results = pathlib.Path('k6/results')
results.mkdir(exist_ok=True)

def generate_dashboard_html(scenario_data, scenario_name, run_num):
    """Gera HTML dashboard para um cenário específico."""
    metrics = scenario_data.get('metrics', {})
    dur = metrics.get('http_req_duration', {}).get('values', {})
    reqs = metrics.get('http_reqs', {}).get('values', {})
    errs = metrics.get('http_req_failed', {}).get('values', {})
    chks = metrics.get('checks', {}).get('values', {})

    p50 = round(dur.get('med', 0), 1)
    p95 = round(dur.get('p(95)', 0), 1)
    p99 = round(dur.get('p(99)', 0), 1)
    avg = round(dur.get('avg', 0), 1)
    rps = round(reqs.get('rate', 0), 2)
    total = int(reqs.get('count', 0))
    err_rate = round(errs.get('rate', 0) * 100, 2)
    checks_pass = int(chks.get('passes', 0))
    checks_fail = int(chks.get('fails', 0))

    # Validar thresholds
    thresholds_ok = True
    threshold_details = []
    for metric, md in metrics.items():
        for cond, val in md.get('thresholds', {}).items():
            ok_thr = val if isinstance(val, bool) else val.get('ok', False)
            if not ok_thr:
                thresholds_ok = False
            mvals = md.get('values', {})
            cur = round(mvals.get('p(95)', mvals.get('rate', 0)), 4)
            threshold_details.append({
                'metric': metric,
                'condition': cond,
                'actual': cur,
                'passed': ok_thr
            })

    status_class = "ok" if thresholds_ok else "fail"
    status_text = "✅ PASSOU" if thresholds_ok else "❌ FALHOU"

    # Gerar tabela de thresholds
    thresholds_table = ""
    if threshold_details:
        thresholds_table = "<h2>Validação de Thresholds</h2><table><thead><tr><th>Métrica</th><th>Condição</th><th>Valor Real</th><th>Status</th></tr></thead><tbody>"
        for t in threshold_details:
            t_status = "✅" if t['passed'] else "❌"
            thresholds_table += f"<tr><td>{t['metric']}</td><td>{t['condition']}</td><td>{t['actual']}</td><td>{t_status}</td></tr>"
        thresholds_table += "</tbody></table>"

    html = f"""<!doctype html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>K6 Dashboard - {scenario_name.upper()} (Run #{run_num})</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
            color: #e2e8f0;
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "Roboto", "Oxygen", "Ubuntu", "Cantarell", sans-serif;
            padding: 24px;
            line-height: 1.6;
        }}
        .container {{ max-width: 1200px; margin: 0 auto; }}
        h1 {{ color: #38bdf8; font-size: 2rem; margin-bottom: 12px; }}
        h2 {{ color: #38bdf8; font-size: 1.4rem; margin-top: 24px; margin-bottom: 12px; }}
        p {{ color: #94a3b8; margin-bottom: 16px; }}
        .metadata {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 16px;
            margin-bottom: 24px;
        }}
        .metadata-item {{
            background: #1e293b;
            padding: 12px 16px;
            border-radius: 6px;
            border-left: 3px solid #38bdf8;
        }}
        .metadata-item strong {{ color: #64748b; font-size: 0.75rem; text-transform: uppercase; display: block; }}
        .metadata-item span {{ color: #e2e8f0; font-size: 1.1rem; }}
        .status {{
            padding: 16px;
            border-radius: 6px;
            margin-bottom: 24px;
            font-weight: bold;
            font-size: 1.2rem;
        }}
        .status.ok {{
            background: rgba(34, 197, 94, 0.1);
            color: #22c55e;
            border: 1px solid #22c55e;
        }}
        .status.fail {{
            background: rgba(239, 68, 68, 0.1);
            color: #ef4444;
            border: 1px solid #ef4444;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 24px;
            background: #1e293b;
            border-radius: 6px;
            overflow: hidden;
        }}
        th {{
            background: #0f172a;
            color: #64748b;
            padding: 12px 16px;
            text-align: left;
            font-size: 0.75rem;
            text-transform: uppercase;
            font-weight: 600;
        }}
        td {{
            padding: 12px 16px;
            border-top: 1px solid #334155;
        }}
        tr:hover {{ background: #0f172a; }}
        .metric-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: 16px;
            margin-bottom: 24px;
        }}
        .metric-box {{
            background: #1e293b;
            padding: 16px;
            border-radius: 6px;
            border: 1px solid #334155;
            text-align: center;
        }}
        .metric-label {{
            color: #94a3b8;
            font-size: 0.85rem;
            text-transform: uppercase;
            margin-bottom: 8px;
        }}
        .metric-value {{
            color: #38bdf8;
            font-size: 1.8rem;
            font-weight: bold;
        }}
        .footer {{
            color: #64748b;
            margin-top: 32px;
            padding-top: 16px;
            border-top: 1px solid #334155;
            font-size: 0.85rem;
        }}
        .checks-status {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 16px;
            margin-bottom: 24px;
        }}
        .check-item {{
            background: #1e293b;
            padding: 16px;
            border-radius: 6px;
            border-left: 3px solid #22c55e;
        }}
        .check-item.fail {{
            border-left-color: #ef4444;
        }}
    </style>
</head>
<body>
    <div class="container">
        <h1>📊 Dashboard K6 - {scenario_name.upper()}</h1>
        <p>Run #{run_num} | {ttype.upper()} Test | {datetime.datetime.utcnow().strftime('%Y-%m-%d %H:%M UTC')}</p>

        <div class="metadata">
            <div class="metadata-item">
                <strong>Target URL</strong>
                <span>{url}</span>
            </div>
            <div class="metadata-item">
                <strong>Virtual Users (VUs)</strong>
                <span>{vus}</span>
            </div>
            <div class="metadata-item">
                <strong>Total Requisições</strong>
                <span>{total:,}</span>
            </div>
            <div class="metadata-item">
                <strong>Taxa de Erro</strong>
                <span>{err_rate}%</span>
            </div>
        </div>

        <div class="status {status_class}">
            {status_text}
        </div>

        <h2>Métricas de Performance</h2>
        <div class="metric-grid">
            <div class="metric-box">
                <div class="metric-label">p50</div>
                <div class="metric-value">{p50}<span style="font-size: 0.6em;">ms</span></div>
            </div>
            <div class="metric-box">
                <div class="metric-label">p95</div>
                <div class="metric-value">{p95}<span style="font-size: 0.6em;">ms</span></div>
            </div>
            <div class="metric-box">
                <div class="metric-label">p99</div>
                <div class="metric-value">{p99}<span style="font-size: 0.6em;">ms</span></div>
            </div>
            <div class="metric-box">
                <div class="metric-label">Média</div>
                <div class="metric-value">{avg}<span style="font-size: 0.6em;">ms</span></div>
            </div>
            <div class="metric-box">
                <div class="metric-label">Taxa</div>
                <div class="metric-value">{rps}<span style="font-size: 0.6em;">req/s</span></div>
            </div>
            <div class="metric-box">
                <div class="metric-label">Erro</div>
                <div class="metric-value">{err_rate}<span style="font-size: 0.6em;">%</span></div>
            </div>
        </div>

        <h2>Checks</h2>
        <div class="checks-status">
            <div class="check-item">
                <strong>✅ Passou</strong><br>
                <span style="font-size: 1.5rem; color: #22c55e;">{checks_pass}</span>
            </div>
            <div class="check-item {'fail' if checks_fail > 0 else ''}">
                <strong>❌ Falhou</strong><br>
                <span style="font-size: 1.5rem; color: {'#ef4444' if checks_fail > 0 else '#22c55e'};">{checks_fail}</span>
            </div>
        </div>

        {thresholds_table}

        <div class="footer">
            <p>💾 Dados armazenados automaticamente | 🔗 <a href="https://comprai.2.25.122.11.nip.io/k6" style="color: #38bdf8;">Ver dashboard completo</a></p>
        </div>
    </div>
</body>
</html>"""
    return html

for sc in scenarios:
    src = pathlib.Path(f'dashboards/{sc}')
    if not src.exists():
        print(f'⚠ Diretório dashboards/{sc} não encontrado')
        continue

    p = src / 'summary.json'
    if not p.exists():
        print(f'⚠ Arquivo dashboards/{sc}/summary.json não encontrado')
        continue

    try:
        data = json.loads(p.read_text())

        # Gerar dashboard HTML
        html_content = generate_dashboard_html(data, sc, run)

        # Salvar em dist/{sc}/dashboard.html
        dst_dist = dist / sc
        dst_dist.mkdir(exist_ok=True)
        (dst_dist / 'dashboard.html').write_text(html_content, encoding='utf-8')

        # Salvar em k6/results/{sc}/dashboard.html (para upload como artifact)
        dst_results = results / sc
        dst_results.mkdir(exist_ok=True)
        (dst_results / 'dashboard.html').write_text(html_content, encoding='utf-8')

        # Também copiar summary.json para k6/results/{sc}/
        shutil.copy(p, dst_results / 'summary.json')

        print(f'✓ Dashboard gerado para {sc}: dist/{sc}/dashboard.html')

        # Extrair métricas para agregação
        dur = data.get('metrics', {}).get('http_req_duration', {}).get('values', {})
        reqs = data.get('metrics', {}).get('http_reqs', {}).get('values', {})
        errs = data.get('metrics', {}).get('http_req_failed', {}).get('values', {})
        chks = data.get('metrics', {}).get('checks', {}).get('values', {})

        ok = all(
            (v if isinstance(v, bool) else v.get('ok', False))
            for md in data.get('metrics', {}).values()
            for v in md.get('thresholds', {}).values()
        )

        thresholds = []
        for metric, md in data.get('metrics', {}).items():
            for cond, val in md.get('thresholds', {}).items():
                ok_thr = val if isinstance(val, bool) else val.get('ok', False)
                mvals = md.get('values', {})
                cur = round(mvals.get('p(95)', mvals.get('rate', 0)), 4)
                thresholds.append({'metric': metric, 'condition': cond, 'actual': cur, 'passed': ok_thr})

        rows.append({
            'sc': sc,
            'p50': round(dur.get('med', 0), 1),
            'p95': round(dur.get('p(95)', 0), 1),
            'p99': round(dur.get('p(99)', 0), 1),
            'avg': round(dur.get('avg', 0), 1),
            'rps': round(reqs.get('rate', 0), 2),
            'total': int(reqs.get('count', 0)),
            'err': round(errs.get('rate', 0) * 100, 2),
            'checks_pass': int(chks.get('passes', 0)),
            'checks_fail': int(chks.get('fails', 0)),
            'ok': ok,
            'thresholds': thresholds,
        })
    except Exception as e:
        print(f'✗ Erro ao processar {sc}: {e}')

# Montar payload do run para enviar à API
if rows:
    smoke = next((r for r in rows if r['sc'] == 'smoke'), rows[0])
    all_thr = [t for r in rows for t in r['thresholds']]
else:
    smoke = {}
    all_thr = []

payload = {
    'run': run,
    'meta': {
        'run_number': run,
        'test_type': ttype,
        'target_url': url,
        'vus': vus,
        'run_date': datetime.datetime.utcnow().strftime('%Y-%m-%d %H:%M UTC'),
    },
    'kpis': {
        'p50': smoke.get('p50'),
        'p95': smoke.get('p95'),
        'p99': smoke.get('p99'),
        'avg': smoke.get('avg'),
        'rps': smoke.get('rps'),
        'total_requests': smoke.get('total'),
        'error_rate': round(smoke.get('err', 0) / 100, 4) if smoke else None,
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
except urllib.error.HTTPError as e:
    print(f'✗ Erro HTTP {e.code} ao enviar para API: {e.reason}')
    try:
        error_body = e.read().decode('utf-8')
        print(f'   Resposta: {error_body}')
    except:
        pass
    print(f'   Payload enviado: {json.dumps(payload, indent=2, ensure_ascii=False)[:500]}...')
except urllib.error.URLError as e:
    print(f'✗ Erro de conexão com API: {e.reason}')
    print(f'   Endpoint: {api}/api/k6/runs')
except Exception as e:
    print(f'✗ Erro inesperado ao enviar para API: {type(e).__name__}: {e}')

# Gerar index.html simples para artifact
summary_html = f"""<!doctype html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>K6 Run #{run}</title>
    <style>
        body {{ background: #0f172a; color: #e2e8f0; font-family: system-ui; padding: 32px; }}
        h1 {{ color: #38bdf8; margin-bottom: 16px; }}
        table {{ border-collapse: collapse; width: 100%; margin-bottom: 24px; }}
        th, td {{ padding: 12px; border-bottom: 1px solid #334155; text-align: left; }}
        th {{ color: #64748b; font-size: .8rem; text-transform: uppercase; background: #1e293b; }}
        .ok {{ color: #22c55e; font-weight: bold; }}
        .fail {{ color: #ef4444; font-weight: bold; }}
        a {{ color: #38bdf8; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
    </style>
</head>
<body>
    <h1>📊 K6 Dashboard — Run #{run}</h1>
    <p>{ttype.upper()} | {url} | VUs: {vus}</p>
    <p>👉 <a href="https://comprai.2.25.122.11.nip.io/k6">Abrir dashboard completo na VPS</a></p>
    <table>
        <thead>
            <tr>
                <th>Cenário</th><th>p50</th><th>p95</th><th>p99</th><th>Req/s</th><th>Total</th><th>Erro%</th><th>Status</th>
                <th>Dashboard</th>
            </tr>
        </thead>
        <tbody>
            {"".join(f'<tr><td><strong>{r["sc"].upper()}</strong></td><td>{r["p50"]}ms</td><td>{r["p95"]}ms</td><td>{r["p99"]}ms</td><td>{r["rps"]}</td><td>{r["total"]:,}</td><td>{r["err"]}%</td><td class="{"ok" if r["ok"] else "fail"}">{"✅ OK" if r["ok"] else "❌ FALHOU"}</td><td><a href="{r["sc"]}/dashboard.html">Ver</a></td></tr>' for r in rows)}
        </tbody>
    </table>
</body>
</html>"""

(dist / 'index.html').write_text(summary_html, encoding='utf-8')
print(f'✓ dist/index.html gerado com links para dashboards individuais ({len(rows)} cenários)')

# Gerar index.json em k6/results/ com a lista de resultados para o frontend
results_index = {
    'run': run,
    'sha': sha,
    'test_type': ttype,
    'target_url': url,
    'vus': vus,
    'timestamp': datetime.datetime.utcnow().isoformat(),
    'scenarios': [
        {
            'name': r['sc'],
            'p50': r['p50'],
            'p95': r['p95'],
            'p99': r['p99'],
            'avg': r['avg'],
            'rps': r['rps'],
            'total_requests': r['total'],
            'error_rate': r['err'],
            'checks_passed': r['checks_pass'],
            'checks_failed': r['checks_fail'],
            'status': 'ok' if r['ok'] else 'fail',
        }
        for r in rows
    ],
}

(results / 'index.json').write_text(json.dumps(results_index, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'✓ k6/results/index.json gerado para o frontend ({len(rows)} cenários)')
