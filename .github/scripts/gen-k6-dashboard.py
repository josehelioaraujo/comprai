#!/usr/bin/env python3
import json, os, pathlib, shutil

run   = os.environ.get('RUN_NUMBER', '?')
ttype = os.environ.get('TEST_TYPE', '?')
url   = os.environ.get('TARGET_URL', '?')
vus   = os.environ.get('VUS_OVERRIDE', 'padrao do script')
dur   = os.environ.get('DURATION_OVERRIDE', 'padrao do script')

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
        dura = s.get('metrics',{}).get('http_req_duration',{}).get('values',{})
        reqs = s.get('metrics',{}).get('http_reqs',{}).get('values',{})
        errs = s.get('metrics',{}).get('http_req_failed',{}).get('values',{})
        ok   = all(
            (v if isinstance(v, bool) else v.get('ok', False))
            for md in s.get('metrics', {}).values()
            for v in md.get('thresholds', {}).values()
        )
        rows.append({
            'sc': sc, 'p50': dura.get('med', 0), 'p95': dura.get('p(95)', 0),
            'p99': dura.get('p(99)', 0), 'rps': reqs.get('rate', 0),
            'total': int(reqs.get('count', 0)), 'err': errs.get('rate', 0) * 100,
            'ok': ok, 'raw': s
        })
    except Exception as e:
        print(f'Erro ao processar {sc}: {e}')

# ── Montar blocos de dados dinâmicos ──────────────────────────────────────────

# Tabela de métricas
metric_rows = ''
for r in rows:
    badge = '#22c55e' if r['ok'] else '#ef4444'
    lbl   = 'OK' if r['ok'] else 'FAIL'
    metric_rows += (
        '<tr>'
        + '<td><button class="sc-link" onclick="showTab(\'' + r['sc'] + '\')">' + r['sc'] + '</button></td>'
        + '<td>' + str(round(r['p50'], 1)) + '</td>'
        + '<td>' + str(round(r['p95'], 1)) + '</td>'
        + '<td>' + str(round(r['p99'], 1)) + '</td>'
        + '<td>' + f"{r['rps']:.2f}" + '</td>'
        + '<td>' + f"{r['total']:,}" + '</td>'
        + '<td>' + f"{r['err']:.2f}" + '%</td>'
        + '<td><span style="background:' + badge + ';color:#fff;padding:2px 8px;border-radius:4px;font-size:12px">' + lbl + '</span></td>'
        + '</tr>\n'
    )

# Tabs dos cenários
tab_btns  = ''
tab_panes = ''
for r in rows:
    sc   = r['sc']
    icon = '\u2705' if r['ok'] else '\u274c'
    tab_btns  += '<button class="tab-btn" onclick="showTab(\'' + sc + '\')"><span class="sc-label">' + sc.upper() + '</span> ' + icon + '</button>\n'
    tab_panes += '<div id="pane-' + sc + '" class="pane" style="display:none"><iframe src="' + sc + '/dashboard.html" frameborder="0" width="100%" height="900"></iframe></div>\n'

first_sc = rows[0]['sc'] if rows else ''

# Thresholds
thr_rows = ''
for r in rows:
    for metric, md in r['raw'].get('metrics', {}).items():
        for cond, val in md.get('thresholds', {}).items():
            ok_thr = val if isinstance(val, bool) else val.get('ok', False)
            cur_val = md.get('values', {})
            if 'p(95)' in cond:
                cur = round(cur_val.get('p(95)', 0), 2)
            elif 'rate' in cond:
                cur = round(cur_val.get('rate', 0), 4)
            else:
                cur = '-'
            color = '#22c55e' if ok_thr else '#ef4444'
            thr_rows += (
                '<tr>'
                + '<td>' + r['sc'] + '</td>'
                + '<td>' + metric + '</td>'
                + '<td><code>' + cond + '</code></td>'
                + '<td><span style="color:' + color + '">' + str(cur) + '</span></td>'
                + '</tr>\n'
            )

# __DASHBOARD_DATA__ para o chat LLM
dashboard_data = json.dumps({
    'run': run, 'type': ttype, 'url': url,
    'scenarios': [{k: v for k, v in r.items() if k != 'raw'} for r in rows]
}, ensure_ascii=False)

# ── Ler template base ──────────────────────────────────────────────────────────
template_path = pathlib.Path('k6/dashboard/index.html')
if template_path.exists():
    html = template_path.read_text(encoding='utf-8')

    # Injetar metadados no <title>
    html = html.replace('<title>K6 Dashboard</title>',
                        f'<title>K6 Dashboard - Run #{run}</title>')

    # Substituir placeholder __DASHBOARD_DATA__ no template (linha 281)
    html = html.replace('__DASHBOARD_DATA__', dashboard_data, 1)



    # Injetar tabela de métricas e tabs via placeholder ou após </head>
    metrics_block = f"""
  <!-- ── Dados injetados pelo gen-k6-dashboard.py ── -->
  <div class="section-title">&#x1F4CA; Resultados — Run #{run}
    <span style="font-size:12px;font-weight:400;color:var(--muted);margin-left:12px">
      {ttype.upper()} &nbsp;|&nbsp; {url} &nbsp;|&nbsp; VUs: {vus}
    </span>
  </div>
  <div class="card" style="overflow-x:auto">
    <table>
      <thead><tr><th>Tipo</th><th>p50 ms</th><th>p95 ms</th><th>p99 ms</th><th>Req/s</th><th>Total</th><th>Erro %</th><th>Status</th></tr></thead>
      <tbody>{metric_rows}</tbody>
    </table>
  </div>

  <div class="section-title" style="margin-top:24px">&#x1F4C8; Thresholds</div>
  <div class="card" style="overflow-x:auto">
    <table>
      <thead><tr><th>Cenário</th><th>Métrica</th><th>Condição</th><th>Valor Atual</th></tr></thead>
      <tbody>{thr_rows if thr_rows else '<tr><td colspan="4" style="color:var(--muted)">Sem dados de threshold</td></tr>'}</tbody>
    </table>
  </div>

  <div class="section-title" style="margin-top:24px">&#x1F4F9; Dashboards por Cenário</div>
  <div class="card">
    <div class="tabs">{tab_btns}</div>
    {tab_panes}
  </div>
  <script>
  function showTab(sc){{
    document.querySelectorAll('.pane').forEach(function(p){{p.style.display='none';}});
    document.querySelectorAll('.tab-btn').forEach(function(b){{b.classList.remove('active');}});
    var p=document.getElementById('pane-'+sc); if(p) p.style.display='block';
    document.querySelectorAll('.tab-btn').forEach(function(b){{
      if(b.querySelector('.sc-label')&&b.querySelector('.sc-label').textContent===sc.toUpperCase()) b.classList.add('active');
    }});
  }}
  if('{first_sc}') showTab('{first_sc}');
  </script>
"""
    # Injetar antes da seção do chat (antes de <!-- ── K6 AI Chat)
    html = html.replace('  <!-- ── K6 AI Chat', metrics_block + '\n  <!-- ── K6 AI Chat', 1)

else:
    print('AVISO: k6/dashboard/index.html nao encontrado, usando HTML basico')
    html = f'<html><body><h1>Run #{run} — template nao encontrado</h1></body></html>'

(dist / 'index.html').write_text(html, encoding='utf-8')
print(f'index.html gerado: {len(rows)} cenarios, {len(html)} bytes')
