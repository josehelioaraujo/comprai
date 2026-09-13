#!/usr/bin/env python3
import json, os, pathlib, shutil

run   = os.environ.get('RUN_NUMBER', '?')
ttype = os.environ.get('TEST_TYPE', '?')
url   = os.environ.get('TARGET_URL', '?')
vus   = os.environ.get('VUS_OVERRIDE', 'padrao do script')

scenarios = ['smoke', 'load', 'stress', 'spike', 'soak']
rows = []

dist = pathlib.Path('dist')
dist.mkdir(exist_ok=True)

# Copiar dashboards individuais
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
        # Thresholds detalhados
        thresholds = []
        for metric, md in s.get('metrics', {}).items():
            for cond, val in md.get('thresholds', {}).items():
                ok_thr = val if isinstance(val, bool) else val.get('ok', False)
                mvals  = md.get('values', {})
                if 'p(95)' in cond:
                    cur = round(mvals.get('p(95)', 0), 2)
                elif 'p(99)' in cond:
                    cur = round(mvals.get('p(99)', 0), 2)
                elif 'rate' in cond:
                    cur = round(mvals.get('rate', 0), 4)
                else:
                    cur = '-'
                thresholds.append({'metric': metric, 'condition': cond, 'current': cur, 'ok': ok_thr})
        rows.append({
            'sc': sc, 'p50': round(dur.get('med', 0), 1),
            'p95': round(dur.get('p(95)', 0), 1),
            'p99': round(dur.get('p(99)', 0), 1),
            'avg': round(dur.get('avg', 0), 1),
            'rps': round(reqs.get('rate', 0), 2),
            'total': int(reqs.get('count', 0)),
            'err': round(errs.get('rate', 0) * 100, 2),
            'checks_pass': int(chks.get('passes', 0)),
            'checks_fail': int(chks.get('fails', 0)),
            'ok': ok,
            'thresholds': thresholds
        })
    except Exception as e:
        print(f'Erro ao processar {sc}: {e}')

# Montar __DASHBOARD_DATA__
dashboard_data = json.dumps({
    'run': run,
    'type': ttype,
    'url': url,
    'vus': vus,
    'scenarios': rows
}, ensure_ascii=False)

# Ler template e substituir APENAS o bloco de dados
template_path = pathlib.Path('k6/dashboard/index.html')
if not template_path.exists():
    print('ERRO: k6/dashboard/index.html não encontrado')
    exit(1)

html = template_path.read_text(encoding='utf-8')

# Substituir título
html = html.replace('<title>K6 Dashboard</title>',
                    f'<title>K6 Dashboard — Run #{run}</title>')

# Substituir bloco de dados (único ponto de injeção)
old_block = '''<script>
// __DASHBOARD_DATA__ will be replaced by the CI step with real JSON
// Fallback for local preview:
var DASHBOARD_DATA = __DASHBOARD_DATA__;
</script>'''

new_block = f'''<script>
var DASHBOARD_DATA = {dashboard_data};
window.__DASHBOARD_DATA__ = DASHBOARD_DATA;
</script>'''

if old_block in html:
    html = html.replace(old_block, new_block, 1)
    print(f'✓ __DASHBOARD_DATA__ injetado ({len(dashboard_data)} bytes, {len(rows)} cenários)')
else:
    print('AVISO: placeholder não encontrado no template')

(dist / 'index.html').write_text(html, encoding='utf-8')
print(f'✓ index.html gerado: {len(html)} bytes')
