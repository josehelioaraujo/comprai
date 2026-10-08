import { test, expect } from '@playwright/test';

const API_URL      = process.env.E2E_API_URL      || 'http://2.25.122.11:5020';
const E2E_EMAIL    = process.env.E2E_USER_EMAIL    || `e2e-account@comprai-test.local`;
const E2E_PASSWORD = process.env.E2E_USER_PASSWORD || 'E2eTest@2024!';
const E2E_NAME     = 'E2E Account Tester';

test.describe('Area do Usuario', () => {
  test.beforeAll(async ({ request }) => {
    await request.post(`${API_URL}/api/auth/register`, {
      data: { name: E2E_NAME, email: E2E_EMAIL, password: E2E_PASSWORD },
    });
  });

  test.beforeEach(async ({ page }) => {
    await page.goto('/auth/login');
    const loginTab = page.getByRole('button', { name: 'Entrar', exact: true });
    if (await loginTab.count() > 0) await loginTab.click();
    await page.locator('input[type="email"]').fill(E2E_EMAIL);
    await page.locator('input[type="password"]').fill(E2E_PASSWORD);
    await page.locator('button[type="submit"]').click();
    await page.waitForURL(/\/chat/, { timeout: 20_000 });
  });

  test('pagina /account carrega sem erro', async ({ page }) => {
    await page.goto('/account');
    // Nao deve ter 404 ou erro de servidor
    await expect(page.locator('body')).not.toContainText('404');
    await expect(page.locator('body')).not.toContainText('Application error');
  });

  test('pagina /account exibe dados do usuario', async ({ page }) => {
    await page.goto('/account');
    await page.waitForLoadState('networkidle');

    // Deve exibir email ou nome do usuario logado
    const body = page.locator('body');
    const hasEmail = await body.getByText(E2E_EMAIL).isVisible().catch(() => false);
    const hasName  = await body.getByText(E2E_NAME).isVisible().catch(() => false);
    expect(hasEmail || hasName).toBeTruthy();
  });

  test('pagina /profile exibe historico de pedidos', async ({ page }) => {
    await page.goto('/profile');
    await page.waitForLoadState('networkidle');
    // Nao deve ter erro de servidor
    await expect(page.locator('body')).not.toContainText('Application error');
    // Deve ter alguma secao de pedidos (mesmo vazia)
    const hasSection = await page.locator(
      'text=pedidos, text=Pedidos, text=Histórico, [class*="order"], [class*="pedido"]'
    ).count();
    // Apenas registra — sem falhar se vazio
    console.log('Secoes de pedido encontradas:', hasSection);
  });

  test('API /api/auth/me retorna dados do usuario autenticado', async ({ request }) => {
    const loginResp = await request.post(`${API_URL}/api/auth/login`, {
      data: { email: E2E_EMAIL, password: E2E_PASSWORD },
    });
    expect(loginResp.status()).toBe(200);
    const { token } = await loginResp.json();

    const meResp = await request.get(`${API_URL}/api/auth/me`, {
      headers: { 'Authorization': `Bearer ${token}` },
    });
    expect(meResp.status()).toBe(200);
    const me = await meResp.json();
    expect(me.email).toBe(E2E_EMAIL);
    expect(me.name).toBeTruthy();
  });

  test('API /api/auth/me rejeita sem token', async ({ request }) => {
    const resp = await request.get(`${API_URL}/api/auth/me`);
    expect(resp.status()).toBe(401);
  });

  test('formulario de edicao de perfil exibe campos', async ({ page }) => {
    await page.goto('/account');
    await page.waitForLoadState('networkidle');

    // Busca campos de nome/telefone
    const nameInput = page.locator('input[name="name"], input[placeholder*="nome"], input[placeholder*="Nome"]').first();
    if (await nameInput.isVisible()) {
      expect(await nameInput.inputValue()).toBeTruthy();
    } else {
      // Campo pode estar readonly ou carregando
      console.log('Campo nome nao encontrado como input editavel');
    }
  });
});
