#!/usr/bin/env python3
"""
Patch do index.html do OpsWatch — adiciona aba Acesso no grupo Observabilidade.
Executar na VPS: python3 /tmp/patch_access_log.py
"""
import re

PATH = '/var/www/html/k6/dashboard/index.html'

with open(PATH, encoding='utf-8') as f:
    html = f.read()

# ── 1. NAV_SECTIONS — adicionar obsAcessoBlock após obsLogsBlock ──────────────
html = html.replace(
    "'obsLogsBlock','obsTracesBlock'",
    "'obsLogsBlock','obsAcessoBlock','obsTracesBlock'"
)

# ── 2. showSection trigger — adicionar obsAcessoBlock ────────────────────────
html = html.replace(
    "if (id === 'obsLogsBlock')    { requestAnimationFrame(function(){ setTimeout(function(){ obsLoad('logs');",
    "if (id === 'obsAcessoBlock')  { requestAnimationFrame(function(){ setTimeout(function(){ obsLoadAccess(); }, 100); }); }\n      if (id === 'obsLogsBlock')    { requestAnimationFrame(function(){ setTimeout(function(){ obsLoad('logs');"
)

# ── 3. Nav item — adicionar após nav-obs-logs ─────────────────────────────────
html = html.replace(
    '''        <div class="nav-item" id="nav-obs-traces" onclick="showSection('obsTracesBlock', this)" title="Traces Jaeger">''',
    '''        <div class="nav-item" id="nav-obs-acesso" onclick="showSection('obsAcessoBlock', this)" title="Logs de Acesso">
          <span class="nav-icon">&#128272;</span><span class="nav-text">Acesso</span>
        </div>
        <div class="nav-item" id="nav-obs-traces" onclick="showSection('obsTracesBlock', this)" title="Traces Jaeger">'''
)

# ── 4. Bloco HTML — inserir antes do bloco Logs ───────────────────────────────
access_block = '''
    <!-- ── Observabilidade: Acesso ─────────────────────────────────────────── -->
    <div class="section-block" id="obsAcessoBlock" style="display:none">
      <div class="section-header" onclick="toggleSection(this)">
        <span class="section-title-text" style="flex:1">&#128272; Acesso — Access Log</span>
        <span class="obs-refresh-badge" id="obs-access-refresh"><span class="obs-refresh-dot"></span>live</span>
        <span class="section-chevron">&#9660;</span>
      </div>
      <div id="obs-access-content" style="padding:16px">
        <!-- Toolbar de filtros -->
        <div class="obs-toolbar" style="display:flex;gap:8px;flex-wrap:wrap;margin-bottom:12px;align-items:center">
          <select id="acc-method" style="background:var(--surface2);color:var(--text);border:1px solid var(--border);border-radius:6px;padding:4px 8px;font-size:12px">
            <option value="">Todos métodos</option>
            <option value="GET">GET</option>
            <option value="POST">POST</option>
            <option value="PUT">PUT</option>
            <option value="DELETE">DELETE</option>
          </select>
          <select id="acc-status" style="background:var(--surface2);color:var(--text);border:1px solid var(--border);border-radius:6px;padding:4px 8px;font-size:12px">
            <option value="">Todos status</option>
            <option value="200">2xx OK</option>
            <option value="400">4xx Erro cliente</option>
            <option value="500">5xx Erro servidor</option>
          </select>
          <input id="acc-path" type="text" placeholder="Filtrar path..." style="background:var(--surface2);color:var(--text);border:1px solid var(--border);border-radius:6px;padding:4px 8px;font-size:12px;flex:1;min-width:120px">
          <button class="obs-range-btn" onclick="obsLoadAccess()" style="margin-left:auto">&#8635; Atualizar</button>
        </div>
        <!-- KPIs rápidos -->
        <div id="acc-kpis" style="display:grid;grid-template-columns:repeat(4,1fr);gap:8px;margin-bottom:14px"></div>
        <!-- Tabela -->
        <div style="overflow-x:auto">
          <table style="width:100%;border-collapse:collapse;font-size:12px" id="acc-table">
            <thead>
              <tr style="background:var(--surface2);color:var(--text-muted)">
                <th style="padding:6px 8px;text-align:left;white-space:nowrap">Hora</th>
                <th style="padding:6px 8px;text-align:left">Método</th>
                <th style="padding:6px 8px;text-align:left">Path</th>
                <th style="padding:6px 8px;text-align:center">Status</th>
                <th style="padding:6px 8px;text-align:right">Ms</th>
                <th style="padding:6px 8px;text-align:left">IP</th>
                <th style="padding:6px 8px;text-align:left">UA</th>
              </tr>
            </thead>
            <tbody id="acc-tbody"><tr><td colspan="7" style="padding:20px;text-align:center;color:var(--text-muted)">Carregando...</td></tr></tbody>
          </table>
        </div>
        <div id="acc-state" style="margin-top:8px;font-size:11px;color:var(--text-muted)"></div>
      </div>
    </div>

'''

html = html.replace(
    '    <!-- ── Observabilidade: Logs ───────────────────────────────────────── -->',
    access_block + '    <!-- ── Observabilidade: Logs ───────────────────────────────────────── -->'
)

# ── 5. JS — função obsLoadAccess ──────────────────────────────────────────────
js_func = r'''
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
      // KPIs
      var kpis = document.getElementById('acc-kpis');
      if (kpis) {
        var total  = entries.length;
        var ok     = entries.filter(function(e){ return String(e.status).startsWith('2'); }).length;
        var err4   = entries.filter(function(e){ return String(e.status).startsWith('4'); }).length;
        var err5   = entries.filter(function(e){ return String(e.status).startsWith('5'); }).length;
        var avgMs  = total > 0 ? Math.round(entries.reduce(function(s,e){ return s + (parseInt(e.ms)||0); },0) / total) : 0;
        kpis.innerHTML =
          '<div class="obs-kpi-card"><div class="obs-kpi-label">&#128200; Total</div><div class="obs-kpi-value">'+total+'</div><div class="obs-kpi-sub">requisicoes</div></div>' +
          '<div class="obs-kpi-card"><div class="obs-kpi-label" style="color:var(--success)">&#9989; 2xx</div><div class="obs-kpi-value" style="color:var(--success)">'+ok+'</div><div class="obs-kpi-sub">sucesso</div></div>' +
          '<div class="obs-kpi-card"><div class="obs-kpi-label" style="color:var(--warning)">&#9888; 4xx</div><div class="obs-kpi-value" style="color:var(--warning)">'+err4+'</div><div class="obs-kpi-sub">erro cliente</div></div>' +
          '<div class="obs-kpi-card"><div class="obs-kpi-label" style="color:var(--danger)">&#10060; 5xx</div><div class="obs-kpi-value" style="color:var(--danger)">'+err5+'</div><div class="obs-kpi-sub">erro servidor</div></div>';
      }
      // Tabela
      if (!tbody) return;
      if (!entries.length) { tbody.innerHTML = '<tr><td colspan="7" style="padding:20px;text-align:center;color:var(--text-muted)">Sem registros no periodo.</td></tr>'; return; }
      var rows = entries.map(function(e) {
        var sc   = parseInt(e.status) || 0;
        var scColor = sc >= 500 ? 'var(--danger)' : sc >= 400 ? 'var(--warning)' : 'var(--success)';
        var msBadge = parseInt(e.ms) > 1000 ? 'color:var(--danger)' : parseInt(e.ms) > 300 ? 'color:var(--warning)' : '';
        var ts   = e.timestamp ? new Date(e.timestamp).toLocaleTimeString('pt-BR') : '-';
        var ua   = (e.ua || '-').substring(0, 40) + ((e.ua||'').length > 40 ? '...' : '');
        return '<tr style="border-bottom:1px solid var(--border)">' +
          '<td style="padding:5px 8px;white-space:nowrap;color:var(--text-muted)">'+ts+'</td>' +
          '<td style="padding:5px 8px"><span style="font-weight:600;font-size:11px">'+e.method+'</span></td>' +
          '<td style="padding:5px 8px;max-width:220px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap" title="'+e.path+'">'+e.path+'</td>' +
          '<td style="padding:5px 8px;text-align:center"><span style="background:'+scColor+'22;color:'+scColor+';border-radius:4px;padding:2px 7px;font-weight:700;font-size:11px">'+e.status+'</span></td>' +
          '<td style="padding:5px 8px;text-align:right;'+msBadge+'">'+e.ms+'ms</td>' +
          '<td style="padding:5px 8px;font-family:monospace;font-size:11px">'+e.ip+'</td>' +
          '<td style="padding:5px 8px;color:var(--text-muted);font-size:11px" title="'+(e.ua||'')+'">'+ua+'</td>' +
          '</tr>';
      });
      tbody.innerHTML = rows.join('');
      var state = document.getElementById('acc-state');
      if (state) state.textContent = entries.length + ' registros — últimas 6h';
    })
    .catch(function(err) {
      if (tbody) tbody.innerHTML = '<tr><td colspan="7" style="padding:20px;text-align:center;color:var(--danger)">Erro: ' + err.message + '</td></tr>';
    });
}

'''

# Inserir antes do fechamento do script principal (antes de </script> do bloco principal)
# Encontrar um ponto seguro — antes da função obsLoad
html = html.replace(
    '\n// ── Access Log ────────────────────────────────────────────────────────────────\nfunction obsLoadAccess()',
    '\nfunction obsLoadAccess()'  # evitar duplicata se rodar 2x
)
html = html.replace(
    'function obsLoad(type) {',
    js_func + 'function obsLoad(type) {'
)

with open(PATH, 'w', encoding='utf-8') as f:
    f.write(html)

print("Patch aplicado com sucesso!")
print("Verificando:")
import subprocess
result = subprocess.run(['grep', '-c', 'obsAcessoBlock', PATH], capture_output=True, text=True)
print(f"  obsAcessoBlock aparece {result.stdout.strip()}x")
result2 = subprocess.run(['grep', '-c', 'obsLoadAccess', PATH], capture_output=True, text=True)
print(f"  obsLoadAccess aparece {result2.stdout.strip()}x")
