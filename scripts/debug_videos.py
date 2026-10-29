#!/usr/bin/env python3
"""
debug_videos.py — Gera debug.html com todos os vídeos da pasta videos_latest.

Uso:
  python3 debug_videos.py [caminho/para/videos_latest]

Se o caminho não for passado, busca automaticamente.
"""
import json, os, sys, glob

# ─── Localizar videos_latest ─────────────────────────────────────────────────
def find_videos_dir(arg=None):
    if arg:
        return arg
    candidates = [
        'e2e-v2-output/downloads/videos_latest',
        '/opt/comprai/e2e-v2-output/downloads/videos_latest',
        os.path.expanduser('~/comprai/e2e-v2-output/downloads/videos_latest'),
    ]
    # busca dinâmica por playlist.json
    for pattern in ['/opt/**/videos_latest', '/var/www/**/videos_latest', '/home/**/videos_latest']:
        found = glob.glob(pattern, recursive=True)
        candidates.extend(found)
    for c in candidates:
        if os.path.isdir(c) and os.path.exists(os.path.join(c, 'playlist.json')):
            return c
    return None

arg = sys.argv[1] if len(sys.argv) > 1 else None
videos_dir = find_videos_dir(arg)

if not videos_dir:
    print('ERRO: pasta videos_latest com playlist.json não encontrada.')
    print('Passe o caminho como argumento: python3 debug_videos.py /caminho/videos_latest')
    sys.exit(1)

print(f'Usando: {videos_dir}')

# ─── Ler playlist.json ───────────────────────────────────────────────────────
playlist_path = os.path.join(videos_dir, 'playlist.json')
with open(playlist_path, encoding='utf-8') as f:
    data = json.load(f)

all_items = data.get('ALL', [])
videos    = [x for x in all_items if x.get('hasVideo')]
api_items = [x for x in all_items if not x.get('hasVideo')]

print(f'Total: {len(all_items)} itens ({len(api_items)} API, {len(videos)} visuais)')
for v in videos:
    webm = v.get('src','').split('/')[-1]
    path = os.path.join(videos_dir, 'videos', webm)
    size = f'{os.path.getsize(path)/1024:.0f} KB' if os.path.exists(path) else '❌ não existe'
    print(f'  [{v.get("cat","?"):15s}] {webm}  {size}  {v.get("name","")[:60]}')

# ─── Gerar debug.html ────────────────────────────────────────────────────────
CAT_COLORS = {
    'Autenticação': '#6366f1', 'IA / Chat': '#8b5cf6',
    'Busca': '#0ea5e9',        'Carrinho': '#f59e0b',
    'Checkout': '#f97316',     'Pagamento': '#22c55e',
    'Pedidos': '#ec4899',      'Outros': '#94a3b8',
}

cards = ''
for v in videos:
    src  = v.get('src', '')
    name = v.get('name', '')
    cat  = v.get('cat', 'Outros')
    webm = src.split('/')[-1]
    col  = CAT_COLORS.get(cat, '#94a3b8')
    local_idx = v.get('idx', '')
    cards += f'''
  <div class="card" id="card-{webm}">
    <video src="{src}" controls preload="metadata"
           onloadedmetadata="mark('{webm}',this.duration,true)"
           onerror="mark('{webm}',0,false)"></video>
    <div class="info">
      <span class="cat" style="color:{col}">{cat}</span>
      <span class="name" title="{name}">{name}</span>
      <span class="meta" id="meta-{webm}">⏳ carregando…</span>
    </div>
  </div>'''

html = f'''<!DOCTYPE html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Debug Vídeos — {len(videos)} cenas</title>
<style>
*{{box-sizing:border-box;margin:0;padding:0}}
body{{background:#0f172a;color:#e2e8f0;font-family:system-ui,sans-serif;padding:20px}}
h1{{color:#3b82f6;font-size:18px;margin-bottom:6px}}
.sub{{color:#64748b;font-size:12px;margin-bottom:20px}}
.grid{{display:grid;grid-template-columns:repeat(auto-fill,minmax(320px,1fr));gap:16px}}
.card{{background:#1e293b;border-radius:10px;overflow:hidden;border:2px solid #334155;transition:border-color .2s}}
.card.ok{{border-color:#22c55e}}.card.err{{border-color:#ef4444}}
video{{width:100%;display:block;background:#000;max-height:220px}}
.info{{padding:10px;display:flex;flex-direction:column;gap:4px}}
.cat{{font-size:10px;font-weight:700;text-transform:uppercase;letter-spacing:.06em}}
.name{{font-size:12px;color:#94a3b8;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}}
.meta{{font-size:11px;color:#475569}}
</style>
</head>
<body>
<h1>🎬 Debug Vídeos</h1>
<div class="sub">{len(videos)} cenas visuais · {len(api_items)} testes API · gerado de: {videos_dir}</div>
<div class="grid">
{cards}
</div>
<script>
function mark(f,dur,ok){{
  var el=document.getElementById('meta-'+f);
  var card=document.getElementById('card-'+f);
  if(ok){{
    el.innerHTML='<span style="color:#22c55e">✅ '+Math.round(dur)+'s</span> — '+f;
    card.classList.add('ok');
  }}else{{
    el.innerHTML='<span style="color:#ef4444">❌ erro ao carregar</span> — '+f;
    card.classList.add('err');
  }}
}}
</script>
</body>
</html>'''

out_path = os.path.join(videos_dir, 'debug.html')
with open(out_path, 'w', encoding='utf-8') as f:
    f.write(html)

print(f'\n✅ Gerado: {out_path}')
print(f'   Acesse: /opswatch/results/e2e-v2/downloads/videos_latest/debug.html')
