#!/usr/bin/env python3
"""
Fix: inserir função obsLoadAccess() no index.html da VPS
"""
PATH = '/var/www/html/k6/dashboard/index.html'

with open(PATH, encoding='utf-8') as f:
    html = f.read()

# Verificar se já existe
if 'function obsLoadAccess()' in html:
    print("AVISO: obsLoadAccess ja existe — removendo duplicata e reinserindo")
    # Remover a existente (pode estar malformada)
    start = html.find('\n// ── Access Log')
    if start == -1:
        start = html.find('function obsLoadAccess()')
        # Voltar para pegar o comentário
        start = html.rfind('\n', 0, start)
    end = html.find('\nfunction obsLoad(type)', start)
    if end == -1:
        end = html.find('\nfunction obsLoad(', start)
    if start > 0 and end > 0:
        html = html[:start] + html[end:]
        print(f"  Removido bloco antigo ({end-start} chars)")

js_func = r"""
// ── Access Log ────────────────────────────────────────────────────────────────
function obsLoadAccess() {
  var method = (document.getElementById('acc-method') || {}).value || '';
  var status = (document.getElementById('acc-status') || {}).value || '';
  var path   = (document.getElementById('acc-path')   || {}).value || '';
  var qs = '?limit=200';
  if (method) qs += '&method=' + encodeURIComponent(method);
  if (status) qs += '&status=' + encodeURIComponent(status);
  if (path)   qs += '&path='   + encodeURIComponent(path);

  var tbody = document.getElementById('acc-tbody');
  if (tbody) tbody.innerHTML = '<tr><td colspan="7" style="padding:20px;text-align:center;color:var(--text-muted)">Carregando...</td></tr>';

  fetch('/api/observability/logs/access' + qs)
    .then(function(r){ return r.json(); })
    .then(function(d) {
      var entries = d.entries || [];
      var kpis = document.getElementById('acc-kpis');
      if (kpis) {
        var total = entries.length;
        var ok    = entries.filter(function(e){ return String(e.status).startsWith('2'); }).length;
        var err4  = entries.filter(function(e){ return String(e.status).startsWith('4'); }).length;
        var err5  = entries.filter(function(e){ return String(e.status).startsWith('5'); }).length;
        kpis.innerHTML =
          '<div class="obs-kpi-card"><div class="obs-kpi-label">Total</div><div class="obs-kpi-value">'+total+'</div><div class="obs-kpi-sub">requisicoes</div></div>' +
          '<div class="obs-kpi-card"><div class="obs-kpi-label" style="color:var(--success)">2xx</div><div class="obs-kpi-value" style="color:var(--success)">'+ok+'</div><div class="obs-kpi-sub">sucesso</div></div>' +
          '<div class="obs-kpi-card"><div class="obs-kpi-label" style="color:var(--warning)">4xx</div><div class="obs-kpi-value" style="color:var(--warning)">'+err4+'</div><div class="obs-kpi-sub">erro cliente</div></div>' +
          '<div class="obs-kpi-card"><div class="obs-kpi-label" style="color:var(--danger)">5xx</div><div class="obs-kpi-value" style="color:var(--danger)">'+err5+'</div><div class="obs-kpi-sub">erro servidor</div></div>';
      }
      if (!tbody) return;
      if (!entries.length) {
        tbody.innerHTML = '<tr><td colspan="7" style="padding:20px;text-align:center;color:var(--text-muted)">Sem registros no periodo.</td></tr>';
        return;
      }
      var rows = entries.map(function(e) {
        var sc = parseInt(e.status) || 0;
        var scColor = sc >= 500 ? 'var(--danger)' : sc >= 400 ? 'var(--warning)' : 'var(--success)';
        var msBadge = parseInt(e.ms) > 1000 ? 'color:var(--danger)' : parseInt(e.ms) > 300 ? 'color:var(--warning)' : '';
        var ts = e.timestamp ? new Date(e.timestamp).toLocaleTimeString('pt-BR') : '-';
        var ua = (e.ua || '-').substring(0, 40) + ((e.ua||'').length > 40 ? '...' : '');
        return '<tr style="border-bottom:1px solid var(--border)">' +
          '<td style="padding:5px 8px;white-space:nowrap;color:var(--text-muted)">'+ts+'</td>' +
          '<td style="padding:5px 8px;font-weight:600;font-size:11px">'+e.method+'</td>' +
          '<td style="padding:5px 8px;max-width:220px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap" title="'+e.path+'">'+e.path+'</td>' +
          '<td style="padding:5px 8px;text-align:center"><span style="background:'+scColor+'22;color:'+scColor+';border-radius:4px;padding:2px 7px;font-weight:700;font-size:11px">'+e.status+'</span></td>' +
          '<td style="padding:5px 8px;text-align:right;'+msBadge+'">'+e.ms+'ms</td>' +
          '<td style="padding:5px 8px;font-family:monospace;font-size:11px">'+e.ip+'</td>' +
          '<td style="padding:5px 8px;color:var(--text-muted);font-size:11px" title="'+(e.ua||'')+'">'+ua+'</td>' +
          '</tr>';
      });
      tbody.innerHTML = rows.join('');
      var state = document.getElementById('acc-state');
      if (state) state.textContent = entries.length + ' registros — ultimas 6h';
    })
    .catch(function(err) {
      if (tbody) tbody.innerHTML = '<tr><td colspan="7" style="padding:20px;text-align:center;color:var(--danger)">Erro: ' + err.message + '</td></tr>';
    });
}

"""

# Inserir antes de function obsLoad(type)
target = 'function obsLoad(type) {'
if target not in html:
    target = 'function obsLoad('

if target in html:
    html = html.replace(target, js_func + target, 1)
    print("Funcao obsLoadAccess inserida com sucesso!")
else:
    print("ERRO: anchor nao encontrado")

# Fix chevron: garantir que section-header do obsAcessoBlock tem onclick=toggleSection
# Verificar se o header esta correto
if 'id="obsAcessoBlock"' in html:
    print("obsAcessoBlock encontrado no HTML")
    # Verificar se tem toggleSection
    import re
    block = html[html.find('id="obsAcessoBlock"'):html.find('id="obsAcessoBlock"')+500]
    if 'toggleSection' in block:
        print("toggleSection OK no header")
    else:
        print("AVISO: toggleSection ausente no header — chevron nao vai funcionar")

with open(PATH, 'w', encoding='utf-8') as f:
    f.write(html)

print("Arquivo salvo.")
