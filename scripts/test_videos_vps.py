#!/usr/bin/env python3
"""
test_videos_vps.py — Verifica se os 31 vídeos estão acessíveis via HTTP na VPS.

Uso:
  python3 test_videos_vps.py [base_url]

Exemplo:
  python3 test_videos_vps.py http://2.25.122.11
  python3 test_videos_vps.py http://comprai.2.25.122.11.nip.io
"""
import sys, urllib.request, urllib.error, time

BASE = (sys.argv[1] if len(sys.argv) > 1 else 'http://2.25.122.11').rstrip('/')
ROOT = f'{BASE}/opswatch/results/e2e-v2/downloads/videos_latest'

CATS = {
    '001': 'Outros',       '002': 'Outros',
    '003': 'Autenticação', '004': 'Autenticação', '005': 'Autenticação',
    '006': 'Autenticação', '007': 'Autenticação', '008': 'Autenticação', '009': 'Autenticação',
    '010': 'Busca',        '011': 'Busca',        '012': 'Busca',
    '013': 'Carrinho',     '014': 'Carrinho',     '015': 'Carrinho',
    '016': 'Carrinho',     '017': 'Carrinho',
    '018': 'Checkout',     '019': 'Checkout',     '020': 'Checkout',
    '021': 'Pagamento',    '022': 'Pagamento',    '023': 'Pagamento',
    '024': 'Pedidos',      '025': 'Pedidos',      '026': 'Pedidos',     '027': 'Pedidos',
    '028': 'IA / Chat',    '029': 'IA / Chat',    '030': 'IA / Chat',   '031': 'IA / Chat',
}

ok = err = 0
erros = []

print(f'Testando: {ROOT}')
print(f'{"#":<5} {"Cat":<15} {"Status":<8} {"Bytes":>9}  URL')
print('─' * 75)

for num, cat in CATS.items():
    url = f'{ROOT}/videos/{num}.webm'
    try:
        req = urllib.request.Request(url, method='HEAD')
        with urllib.request.urlopen(req, timeout=8) as r:
            size  = int(r.headers.get('Content-Length', 0))
            ctype = r.headers.get('Content-Type', '')
            if size == 0:
                status = '⚠ VAZIO'
                erros.append((num, url, 'Content-Length: 0'))
                err += 1
            elif 'video' not in ctype and 'octet' not in ctype:
                status = '⚠ TIPO?'
                erros.append((num, url, f'Content-Type: {ctype}'))
                err += 1
            else:
                status = '✅ OK'
                ok += 1
            print(f'{num:<5} {cat:<15} {status:<8} {size:>9,}  {url}')
    except urllib.error.HTTPError as e:
        status = f'❌ {e.code}'
        erros.append((num, url, str(e)))
        err += 1
        print(f'{num:<5} {cat:<15} {status:<8} {"—":>9}  {url}')
    except Exception as e:
        status = '❌ ERR'
        erros.append((num, url, str(e)))
        err += 1
        print(f'{num:<5} {cat:<15} {status:<8} {"—":>9}  {url}')

print('─' * 75)
print(f'\n✅ OK: {ok}   ❌ Falhas: {err}   Total: {ok+err}')

if erros:
    print('\nDetalhes das falhas:')
    for num, url, msg in erros:
        print(f'  {num}.webm — {msg}')
    sys.exit(1)
else:
    print('\n🎉 Todos os 31 vídeos acessíveis via HTTP.')
