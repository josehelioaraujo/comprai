#!/usr/bin/env python3
import json, os, pathlib, shutil

run   = os.environ.get('RUN_NUMBER', '?')
ttype = os.environ.get('TEST_TYPE', '?')
url   = os.environ.get('TARGET_URL', '?')

scenarios = ['smoke', 'load', 'stress', 'spike', 'soak']
rows = []

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
        ok   = all(
            (v if isinstance(v, bool) else v.get('ok', False))
            for md in s.get('metrics', {}).values()
            for v in md.get('thresholds', {}).values()
        )
        rows.append({
            'sc': sc, 'p50': dur.get('med', 0), 'p95': dur.get('p(95)', 0),
            'p99': dur.get('p(99)', 0), 'rps': reqs.get('rate', 0),
            'total': int(reqs.get('count', 0)), 'err': errs.get('rate', 0) * 100,
            'ok': ok
        })
    except Exception as e:
        print(f'Erro ao processar {sc}: {e}')

metric_rows = ''
for r in rows:
    badge = '#22c55e' if r['ok'] else '#ef4444'
    lbl   = 'PASS' if r['ok'] else 'FAIL'
    metric_rows += (
        '<tr>'
        + '<td><button class="sc-link" onclick="showTab(\'' + r['sc'] + '\')">' + r['sc'].upper() + '</button></td>'
        + '<td>' + str(round(r['p50'])) + '</td>'
        + '<td>' + str(round(r['p95'])) + '</td>'
        + '<td>' + str(round(r['p99'])) + '</td>'
        + '<td>' + f"{r['rps']:.2f}" + '</td>'
        + '<td>' + f"{r['total']:,}" + '</td>'
        + '<td>' + f"{r['err']:.2f}" + '%</td>'
        + '<td><span style="background:' + badge + ';color:#fff;padding:2px 8px;border-radius:4px;font-size:12px">' + lbl + '</span></td>'
        + '</tr>\n'
    )

tab_btns  = ''
tab_panes = ''
for r in rows:
    sc   = r['sc']
    icon = '\u2705' if r['ok'] else '\u274c'
    tab_btns  += '<button class="tab-btn" onclick="showTab(\'' + sc + '\')"><span class="sc-label">' + sc.upper() + '</span> ' + icon + '</button>\n'
    tab_panes += '<div id="pane-' + sc + '" class="pane" style="display:none"><iframe src="' + sc + '/dashboard.html" frameborder="0" width="100%" height="900"></iframe></div>\n'

first_sc = rows[0]['sc'] if rows else ''

html = """<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<title>K6 Dashboard - Run """ + run + """</title>
<style>
*{box-sizing:border-box;margin:0;padding:0}
body{background:#0f172a;color:#e2e8f0;font-family:system-ui,sans-serif;padding:24px}
h1{font-size:1.6rem;color:#38bdf8;margin-bottom:4px}
.meta{color:#94a3b8;font-size:.85rem;margin-bottom:24px}
.card{background:#1e293b;border-radius:8px;padding:20px;margin-bottom:20px;border:1px solid #334155}
h2{font-size:.85rem;color:#64748b;margin-bottom:14px;text-transform:uppercase;letter-spacing:.08em}
table{width:100%;border-collapse:collapse;font-size:.88rem}
th{color:#64748b;text-align:left;padding:6px 10px;border-bottom:1px solid #334155;white-space:nowrap}
td{padding:8px 10px;border-bottom:1px solid #0f172a}
tr:hover td{background:#263547}
.tabs{display:flex;gap:8px;margin-bottom:16px;flex-wrap:wrap}
.tab-btn{background:#0f172a;border:1px solid #334155;color:#e2e8f0;padding:8px 18px;border-radius:6px;cursor:pointer;display:inline-flex;align-items:center;gap:6px;font-size:.9rem}
.tab-btn:hover{background:#334155}
.tab-btn.active{background:#0ea5e9;border-color:#0ea5e9;color:#fff}
.sc-label{font-weight:700}
.sc-link{background:none;border:none;color:#38bdf8;cursor:pointer;font-weight:600;font-size:.88rem;padding:0}
.pane{border-radius:6px;overflow:hidden;border:1px solid #334155}
</style>
</head>
<body>
<h1>&#x1F4CA; K6 Performance Dashboard &mdash; Run #""" + run + """</h1>
<p class="meta">Cenario: """ + ttype.upper() + """ &nbsp;|&nbsp; URL: """ + url + """</p>
<div class="card">
<h2>Resumo de Metricas</h2>
<div style="overflow-x:auto">
<table>
<thead><tr><th>Cenario</th><th>p50 ms</th><th>p95 ms</th><th>p99 ms</th><th>Req/s</th><th>Total</th><th>Erro %</th><th>Status</th></tr></thead>
<tbody>""" + metric_rows + """</tbody>
</table>
</div>
</div>
<div class="card">
<h2>Dashboards por Cenario</h2>
<div class="tabs">""" + tab_btns + """</div>
""" + tab_panes + """
</div>
<script>
function showTab(sc){
  document.querySelectorAll('.pane').forEach(function(p){p.style.display='none';});
  document.querySelectorAll('.tab-btn').forEach(function(b){b.classList.remove('active');});
  var p=document.getElementById('pane-'+sc); if(p) p.style.display='block';
  document.querySelectorAll('.tab-btn').forEach(function(b){
    if(b.querySelector('.sc-label')&&b.querySelector('.sc-label').textContent===sc.toUpperCase()) b.classList.add('active');
  });
}
if('""" + first_sc + """') showTab('""" + first_sc + """');
</script>
</body>
</html>"""

(dist / 'index.html').write_text(html, encoding='utf-8')
print(f'index.html gerado: {len(rows)} cenarios, {len(html)} bytes')
