#!/bin/bash
set -e

REPO_DIR=/home/projetos/comprai
WEB_DIR=$REPO_DIR/apps/web
IMAGE=comprai-web

echo "=== Comprai Web — Setup VPS ==="

# 1. atualiza repo
cd $REPO_DIR
git pull origin main

# 2. build da imagem
cd $WEB_DIR
echo "=== Build Docker ==="
docker build \
  --build-arg NEXT_PUBLIC_API_URL=https://comprai.2.25.122.11.nip.io \
  -t $IMAGE:latest \
  .

# 3. para container anterior se existir
docker stop comprai-web 2>/dev/null || true
docker rm   comprai-web 2>/dev/null || true

# 4. sobe novo container
echo "=== Subindo container ==="
docker run -d \
  --name comprai-web \
  --restart unless-stopped \
  --network comprai-network \
  -p 3002:3002 \
  -e NODE_ENV=production \
  -e NEXT_PUBLIC_API_URL=https://comprai.2.25.122.11.nip.io \
  $IMAGE:latest

# 5. smoke test
echo "=== Aguardando start... ==="
sleep 8
if curl -sf http://localhost:3002/ > /dev/null; then
  echo ""
  echo "✅ comprai-web rodando em http://2.25.122.11:3002"
else
  echo "❌ Falha no smoke test — veja: docker logs comprai-web"
  exit 1
fi
