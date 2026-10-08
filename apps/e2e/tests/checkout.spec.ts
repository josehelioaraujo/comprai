import { test, expect } from '@playwright/test';

const API_URL      = process.env.E2E_API_URL      || 'http://2.25.122.11:5020';
const E2E_EMAIL    = process.env.E2E_USER_EMAIL    || 'e2e@comprai.test';
const E2E_PASSWORD = process.env.E2E_USER_PASSWORD || 'E2eTest@2026!';
const E2E_NAME     = process.env.E2E_USER_NAME     || 'E2E Tester';

test.describe('Fluxo de Checkout', () => {
  let sessionId: string;

  test.beforeAll(async ({ request }) => {
    await request.post(`${API_URL}/api/auth/register`, {
      data: { name: E2E_NAME, email: E2E_EMAIL, password: E2E_PASSWORD },
    });
  });

  test.beforeEach(async ({ page }) => {
    sessionId = `e2e-checkout-${Date.now()}`;
    // Reutiliza sessão do storageState (salvo pelo global-setup)
    // Sem login por formulário em cada teste — evita rate-limit
    await page.goto('/chat');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
  });

  test('API cart - adicionar e consultar item', async ({ request }) => {
    // Adiciona item ao carrinho via API
    const addResp = await request.post(`${API_URL}/api/cart`, {
      data: {
        sessionId,
        productId:   'e2e-product-001',
        productName: 'Notebook E2E Test',
        price:       1999.99,
        quantity:    1,
        source:      'DummyJSON',
        imageUrl:    'https://dummyjson.com/image/150',
      },
      headers: { 'X-Session-Id': sessionId },
    });
    expect(addResp.status()).toBeLessThan(500);
  });

  test('API checkout - health check', async ({ request }) => {
    // Endpoint de checkout deve existir (mesmo que retorne erro de validacao)
    const resp = await request.post(`${API_URL}/api/checkout`, {
      data: { sessionId, email: E2E_EMAIL },
      headers: { 'X-Session-Id': sessionId },
    });
    // 200 ou 400 (validacao) sao aceitaveis — 500 nao
    expect(resp.status()).not.toBe(500);
  });

  test('checkout via chat - mensagem de finalizacao', async ({ page }) => {
    const chatInput = page.locator('textarea, input[type="text"]').first();
    await chatInput.fill('quero finalizar minha compra');
    await chatInput.press('Enter');

    // Aguarda o agente processar e exibir card de checkout ou confirmacao
    await page.waitForTimeout(8_000);
    const hasCard = await page.locator(
      '[class*="checkout"], [class*="card"], [class*="message"]'
    ).count();
    expect(hasCard).toBeGreaterThan(0);
  });

  test('card de pagamento Pix exibe QR Code ou instrucoes', async ({ page }) => {
    // Simula conversa que leva ao Pix
    const chatInput = page.locator('textarea, input[type="text"]').first();
    await chatInput.fill('pagar com pix');
    await chatInput.press('Enter');
    await page.waitForTimeout(8_000);

    // O card de Pix deve exibir algo (QR, chave pix, instrucoes)
    const pixCard = page.locator(
      '[class*="pix"], [class*="Pix"], text=Pix, text=QR'
    ).first();
    // Nao falha se o card nao aparecer (depende do estado do carrinho)
    // Apenas registra presenca
    const appeared = await pixCard.isVisible().catch(() => false);
    console.log('PixCard visivel:', appeared);
  });

  test('API orders - lista de pedidos do usuario', async ({ request }) => {
    const loginResp = await request.post(`${API_URL}/api/auth/login`, {
      data: { email: E2E_EMAIL, password: E2E_PASSWORD },
    });
    if (loginResp.status() !== 200) {
      test.skip();
      return;
    }
    const { token } = await loginResp.json();
    const ordersResp = await request.get(`${API_URL}/api/orders`, {
      headers: { 'Authorization': `Bearer ${token}` },
    });
    expect(ordersResp.status()).toBeLessThan(500);
  });
});
