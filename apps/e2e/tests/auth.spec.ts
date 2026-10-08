import { test, expect } from '@playwright/test';

const API_URL  = process.env.E2E_API_URL  || 'http://2.25.122.11:5020';
const E2E_EMAIL    = process.env.E2E_USER_EMAIL    || `e2e-${Date.now()}@comprai-test.local`;
const E2E_PASSWORD = process.env.E2E_USER_PASSWORD || 'E2eTest@2024!';
const E2E_NAME     = process.env.E2E_USER_NAME     || 'E2E Tester';

test.describe('Autenticacao', () => {
  test.beforeAll(async ({ request }) => {
    // Garante que o usuario de teste existe
    await request.post(`${API_URL}/api/auth/register`, {
      data: { name: E2E_NAME, email: E2E_EMAIL, password: E2E_PASSWORD },
    });
    // Ignora falha de conflito (usuario ja existe)
  });

  test('redireciona para login quando nao autenticado', async ({ browser }) => {
    // Contexto limpo sem storageState (ignora sessão do global-setup)
    const ctx  = await browser.newContext({ storageState: { cookies: [], origins: [] } });
    const page = await ctx.newPage();
    await page.goto('/chat');
    await expect(page).toHaveURL(/\/auth\/login/);
    await ctx.close();
  });

  test('exibe formulario de login', async ({ page }) => {
    await page.goto('/auth/login');
    await expect(page.locator('input[type="email"]')).toBeVisible();
    await expect(page.locator('input[type="password"]')).toBeVisible();
  });

  test('rejeita credenciais invalidas', async ({ page }) => {
    await page.goto('/auth/login');
    await page.locator('input[type="email"]').fill(E2E_EMAIL);
    await page.locator('input[type="password"]').fill('SenhaErrada!123');
    await page.locator('button[type="submit"]').click();
    // Deve permanecer na tela de login (sem redirecionar)
    await expect(page).toHaveURL(/\/auth\/login/);
  });

  test('efetua login com credentials validas', async ({ page }) => {
    await page.goto('/auth/login');

    // Ativa aba "Entrar" se existir
    const loginTab = page.getByRole('button', { name: 'Entrar', exact: true });
    if (await loginTab.count() > 0) await loginTab.click();

    await page.locator('input[type="email"]').fill(E2E_EMAIL);
    await page.locator('input[type="password"]').fill(E2E_PASSWORD);
    await page.locator('button[type="submit"]').click();

    // Aguarda redirecionamento para /chat
    await page.waitForURL(/\/chat/, { timeout: 20_000 });
    await expect(page).toHaveURL(/\/chat/);
  });

  test('efetua logout com sucesso', async ({ page }) => {
    // Login primeiro
    await page.goto('/auth/login');
    const loginTab = page.getByRole('button', { name: 'Entrar', exact: true });
    if (await loginTab.count() > 0) await loginTab.click();
    await page.locator('input[type="email"]').fill(E2E_EMAIL);
    await page.locator('input[type="password"]').fill(E2E_PASSWORD);
    await page.locator('button[type="submit"]').click();
    await page.waitForURL(/\/chat/, { timeout: 20_000 });

    // Logout — botao "Sair" ou link com texto
    const sairBtn = page.locator('text=Sair').first();
    if (await sairBtn.isVisible()) {
      await sairBtn.click();
    } else {
      await page.goto('/api/auth/signout');
      const confirmBtn = page.locator('button[type="submit"]');
      if (await confirmBtn.isVisible()) await confirmBtn.click();
    }

    // Deve voltar para login
    await page.waitForURL(/\/auth\/login|^\/$/, { timeout: 15_000 });
  });

  test('exibe pagina de verificacao de email apos registro', async ({ page }) => {
    // Testa fluxo de cadastro (novo email)
    const newEmail = `e2e-new-${Date.now()}@comprai-test.local`;
    await page.goto('/auth/login');

    const cadastrarTab = page.locator('text=Cadastrar');
    if (await cadastrarTab.count() > 0) {
      await cadastrarTab.click();
      const nameInput = page.locator('input[name="name"], input[placeholder*="nome"], input[placeholder*="Nome"]').first();
      const emailInput = page.locator('input[type="email"]').first();
      const passInput  = page.locator('input[type="password"]').first();
      if (await nameInput.isVisible()) await nameInput.fill(E2E_NAME);
      await emailInput.fill(newEmail);
      await passInput.fill(E2E_PASSWORD);
      await page.locator('button[type="submit"]').click();
      // Deve redirecionar para /chat ou /auth/verify-email
      await page.waitForURL(/\/chat|\/auth\/verify-email/, { timeout: 20_000 });
    } else {
      test.skip();
    }
  });
});
