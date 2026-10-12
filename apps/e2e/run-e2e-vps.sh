#!/usr/bin/env bash
# run-e2e-vps.sh — Roda todos os testes E2E e gera um único vídeo consolidado
# Uso: bash run-e2e-vps.sh [caminho-do-repo]
# Ex : bash run-e2e-vps.sh /opt/comprai

set -euo pipefail

REPO_DIR="${1:-$(pwd)}"
E2E_DIR="$REPO_DIR/apps/e2e"
OUTPUT_VIDEO="$E2E_DIR/e2e-full-$(date +%Y%m%d-%H%M%S).mp4"

# ── Chromium ──────────────────────────────────────────────────────────────────
CHROMIUM_PATH=""
for candidate in \
  /usr/bin/chromium-browser \
  /usr/bin/chromium \
  /snap/bin/chromium \
  /usr/bin/google-chrome-stable \
  /usr/bin/google-chrome; do
  if [ -x "$candidate" ]; then
    CHROMIUM_PATH="$candidate"
    break
  fi
done

if [ -z "$CHROMIUM_PATH" ]; then
  echo "❌  Chromium não encontrado. Instale com: apt-get install -y chromium-browser"
  exit 1
fi
echo "✅  Chromium: $CHROMIUM_PATH"

# ── Deps ──────────────────────────────────────────────────────────────────────
echo "📦  Instalando dependências..."
cd "$E2E_DIR"
npm ci --silent

# ── Limpa runs anteriores ─────────────────────────────────────────────────────
rm -rf test-results playwright-report playwright-results.json storageState.json

# ── Roda testes ───────────────────────────────────────────────────────────────
echo ""
echo "🎬  Iniciando testes E2E (vídeo por teste ativado)..."
echo "────────────────────────────────────────────────────"

set +e
PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH="$CHROMIUM_PATH" \
E2E_BASE_URL="${E2E_BASE_URL:-http://localhost:3002}" \
E2E_API_URL="${E2E_API_URL:-http://localhost:5020}" \
  npx playwright test --reporter=list
TEST_EXIT=$?
set -e

echo "────────────────────────────────────────────────────"
[ $TEST_EXIT -eq 0 ] && echo "✅  Todos os testes passaram!" || echo "⚠️  Alguns testes falharam (exit $TEST_EXIT)"

# ── Concatena vídeos ──────────────────────────────────────────────────────────
echo ""
echo "🎞️  Gerando vídeo único..."

# Coleta todos os .webm em ordem de criação
mapfile -t VIDEOS < <(find test-results -name "*.webm" -printf '%T@ %p\n' 2>/dev/null | sort -n | awk '{print $2}')

if [ ${#VIDEOS[@]} -eq 0 ]; then
  echo "⚠️  Nenhum vídeo encontrado em test-results/. Verifique se video: 'on' está no playwright.config.ts"
  exit $TEST_EXIT
fi

echo "   Encontrados ${#VIDEOS[@]} vídeo(s)."

# Verifica se ffmpeg está disponível
if ! command -v ffmpeg &>/dev/null; then
  echo "⚠️  ffmpeg não encontrado. Instale com: apt-get install -y ffmpeg"
  echo "   Vídeos individuais disponíveis em: test-results/"
  exit $TEST_EXIT
fi

# Cria lista de concatenação
CONCAT_LIST=$(mktemp /tmp/e2e-concat-XXXX.txt)
for v in "${VIDEOS[@]}"; do
  echo "file '$(realpath "$v")'" >> "$CONCAT_LIST"
done

# Converte webm → mp4 e concatena
ffmpeg -y -f concat -safe 0 -i "$CONCAT_LIST" \
  -c:v libx264 -preset fast -crf 23 -movflags +faststart \
  -an \
  "$OUTPUT_VIDEO" \
  -loglevel error

rm -f "$CONCAT_LIST"

echo ""
echo "🎬  Vídeo gerado: $OUTPUT_VIDEO"
echo "   Tamanho: $(du -sh "$OUTPUT_VIDEO" | cut -f1)"
echo ""

exit $TEST_EXIT
