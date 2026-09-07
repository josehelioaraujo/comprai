# Renovação Automática do Token MercadoLivre no CI

## Problema
O access_token do ML expira em 6h. O teste `GetMlOrders_WithRealToken_Returns200Or204`
falha com 401 quando o token expirou.

## Solução — Refresh Token Automático no CI

### Passo 1 — Capturar o refresh_token (fazer uma vez)
1. Subir ngrok na VPS: `ngrok http 5020`
2. Atualizar Redirect URI no ML Developer para a URL do ngrok
3. Acessar `http://2.25.122.11:5020/api/ml/auth` → fazer login ML
4. No callback, logar o refresh_token recebido
5. Salvar como secret no GitHub: `ML_REFRESH_TOKEN`

### Passo 2 — Adicionar step no GitHub Actions (.github/workflows/ci.yml)

Adicionar ANTES do job de integration-tests:

```yaml
- name: Renovar token MercadoLivre
  id: ml_token
  run: |
    RESPONSE=$(curl -s -X POST https://api.mercadolibre.com/oauth/token \
      -H "Content-Type: application/x-www-form-urlencoded" \
      -d "grant_type=refresh_token" \
      -d "client_id=${{ secrets.ML_CLIENT_ID }}" \
      -d "client_secret=${{ secrets.ML_CLIENT_SECRET }}" \
      -d "refresh_token=${{ secrets.ML_REFRESH_TOKEN }}")
    ACCESS_TOKEN=$(echo $RESPONSE | jq -r '.access_token')
    NEW_REFRESH=$(echo $RESPONSE | jq -r '.refresh_token')
    echo "ML_ACCESS_TOKEN=$ACCESS_TOKEN" >> $GITHUB_ENV
    # Atualizar o refresh_token no secret via API do GitHub (opcional)
    echo "Novo token obtido com sucesso"
```

### Secrets necessários no GitHub
- `ML_CLIENT_ID` = 2786639248653015
- `ML_CLIENT_SECRET` = (pegar no ML Developer)
- `ML_REFRESH_TOKEN` = (capturar no Passo 1)

### Referência
- App ML: ucp-compra.mercadolivre
- Client ID: 2786639248653015
- Docs: https://developers.mercadolibre.com.br/documentacao/autorizacao-oauth
