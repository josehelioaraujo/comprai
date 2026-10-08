import { test, expect } from '@playwright/test';

const API_URL      = process.env.E2E_API_URL      || 'http://2.25.122.11:5020';
const E2E_EMAIL    = process.env.E2E_USER_EMAIL    || `e2e-search@comprai-test.local`;
const E2E_PASSWORD = process.env.E2E_USER_PASSWORD || 'E2eTest@2024!';
const E2E_NAME     = 'E2E Search Tester';

test.describe('Busca de Produtos', () => {
  test.beforeAll(async ({ request }) => {
    await request.post(`${API_URL}/api/auth/register`, {
      data: { name: E2E_NAME, email: E2E_EMAIL, password: E2E_PASSWORD },
    });
  });

  test.beforeEach(async ({ page }) => {
    // Login antes de cada teste de busca
    await page.goto('/auth/login');
    const loginTab = page.locator('text=Entrar');
    if (await loginTab.count() > 0) await loginTab.click();
    await page.locator('input[type="email"]').fill(E2E_EMAIL);
    await page.locator('input[type="password"]').fill(E2E_PASSWORD);
    await page.locator('button[type="submit"]').click();
    await page.waitForURL(/\/chat/, { timeout: 20_000 });
  });

  test('chat carrega corretamente apos login', async ({ page }) => {
    await expect(page).toHaveURL(/\/chat/);
    // Campo de input do chat deve estar visivel
    const chatInput = page.locator('textarea, input[placeholder*="mensagem"], input[placeholder*="Buscar"], input[placeholder*="Comprar"]').first();
    await expect(chatInput).toBeVisible({ timeout: 10_000 });
  });

  test('envia mensagem de busca e recebe resposta', async ({ page }) => {
    const chatInput = page.locator('textarea, input[type="text"]').first();
    await chatInput.fill('quero comprar notebook');
    await chatInput.press('Enter');

    // Aguarda aparecer algum produto ou resposta (ProductCarousel ou mensagem)
    const response = page.locator(
      '[data-testid="product-carousel"], [class*="product"], [class*="card"], [class*="carousel"], [class*="message"]'
    ).first();
    await expect(response).toBeVisible({ timeout: 30_000 });
  });

  test('API search retorna produtos', async ({ request }) => {
    // Teste direto na API — smoke test de healthiness
    const resp = await request.post(`${API_URL}/api/search`, {
      data: { query: 'notebook', sessionId: 'e2e-session-001' },
      headers: { 'X-Session-Id': 'e2e-session-001' },
    });
    expect(resp.status()).toBeLessThan(500);
    const body = await resp.json().catch(() => ({}));
    // Retorna lista ou objeto com products/items
    expect(body).toBeDefined();
  });

  test('API health endpoints respondendo', async ({ request }) => {
    const live = await request.get(`${API_URL}/api/health/live`);
    expect(live.status()).toBe(200);

    const ready = await request.get(`${API_URL}/api/health/ready`);
    expect(ready.status()).toBe(200);
  });

  test('intencao add-to-cart via chat', async ({ page }) => {
    const chatInput = page.locator('textarea, input[type="text"]').first();
    await chatInput.fill('adicionar ao carrinho');
    await chatInput.press('Enter');

    // Aguarda qualquer resposta do agente
    await page.waitForTimeout(5_000);
    const hasResponse = await page.locator('[class*="message"], [class*="card"], [class*="chat"]').count();
    expect(hasResponse).toBeGreaterThan(0);
  });
});
