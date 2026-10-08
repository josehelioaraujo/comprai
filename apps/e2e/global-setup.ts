/**
 * global-setup.ts — Playwright Global Setup
 *
 * Faz login uma única vez antes de todos os testes e salva o storageState
 * (cookies de sessão NextAuth). Os testes reutilizam essa sessão, evitando
 * múltiplas chamadas à API de login e o disparo do rate-limit.
 */
import { chromium, FullConfig } from '@playwright/test';

const BASE_URL     = process.env.E2E_BASE_URL     || 'http://2.25.122.11:3002';
const E2E_EMAIL    = process.env.E2E_USER_EMAIL    || 'e2e@comprai.test';
const E2E_PASSWORD = process.env.E2E_USER_PASSWORD || 'E2eTest@2026!';

async function globalSetup(_config: FullConfig) {
  const browser = await chromium.launch();
  const page    = await browser.newPage();

  console.log('[global-setup] Iniciando login para salvar storageState…');

  await page.goto(`${BASE_URL}/auth/login`);
  await page.waitForLoadState('networkidle');

  // Garante que a aba "Entrar" está ativa
  const loginTab = page.locator('button:has-text("Entrar"), [role="tab"]:has-text("Entrar")').first();
  if (await loginTab.count() > 0) {
    await loginTab.click();
    await page.waitForTimeout(500);
  }

  await page.locator('input[type="email"]').fill(E2E_EMAIL);
  await page.waitForTimeout(300);
  await page.locator('input[type="password"]').fill(E2E_PASSWORD);
  await page.waitForTimeout(300);

  await page.locator('button[type="submit"]').click();
  await page.waitForURL(/\/chat/, { timeout: 30_000 });
  await page.waitForLoadState('networkidle');

  // Salva cookies e localStorage para reuso nos testes
  await page.context().storageState({ path: 'storageState.json' });

  console.log('[global-setup] storageState salvo — sessão pronta para todos os testes.');
  await browser.close();
}

export default globalSetup;
