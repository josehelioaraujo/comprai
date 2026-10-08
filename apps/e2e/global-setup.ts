/**
 * global-setup.ts — Playwright Global Setup
 *
 * Faz login uma única vez antes de todos os testes e salva o storageState
 * (cookies de sessão NextAuth). Os testes reutilizam essa sessão, evitando
 * múltiplas chamadas à API de login e o disparo do rate-limit.
 *
 * Retry automático: se o endpoint de login estiver com rate-limit ativo
 * (runs anteriores recentes), aguarda 65 s e tenta novamente até 4x.
 */
import { chromium, FullConfig } from '@playwright/test';

const BASE_URL     = process.env.E2E_BASE_URL     || 'http://2.25.122.11:3002';
const E2E_EMAIL    = process.env.E2E_USER_EMAIL    || 'e2e@comprai.test';
const E2E_PASSWORD = process.env.E2E_USER_PASSWORD || 'E2eTest@2026!';
const MAX_ATTEMPTS = 4;
const RETRY_DELAY  = 65_000; // ms — maior que a janela de 60 s do rate-limit

async function globalSetup(_config: FullConfig) {
  // Respeita executablePath definido via PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
  // (necessário em ambientes sem download, ex.: containers com Chromium pré-instalado)
  const executablePath = process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH;
  const browser = await chromium.launch(executablePath ? { executablePath } : {});

  for (let attempt = 1; attempt <= MAX_ATTEMPTS; attempt++) {
    const context = await browser.newContext();
    const page    = await context.newPage();

    console.log(`[global-setup] Login tentativa ${attempt}/${MAX_ATTEMPTS}…`);

    try {
      await page.goto(`${BASE_URL}/auth/login`);
      await page.waitForLoadState('networkidle');

      const loginTab = page
        .locator('button:has-text("Entrar"), [role="tab"]:has-text("Entrar")')
        .first();
      if (await loginTab.count() > 0) {
        await loginTab.click();
        await page.waitForTimeout(500);
      }

      await page.locator('input[type="email"]').fill(E2E_EMAIL);
      await page.waitForTimeout(300);
      await page.locator('input[type="password"]').fill(E2E_PASSWORD);
      await page.waitForTimeout(300);
      await page.locator('button[type="submit"]').click();

      await page.waitForURL(/\/chat/, { timeout: 35_000 });
      await page.waitForLoadState('networkidle');

      await context.storageState({ path: 'storageState.json' });
      console.log('[global-setup] storageState salvo — sessão pronta para todos os testes.');
      await browser.close();
      return;

    } catch (err) {
      await context.close();
      if (attempt < MAX_ATTEMPTS) {
        console.log(
          `[global-setup] Tentativa ${attempt} falhou (provável rate-limit). ` +
          `Aguardando ${RETRY_DELAY / 1000}s antes da próxima tentativa…`
        );
        await new Promise(r => setTimeout(r, RETRY_DELAY));
      } else {
        await browser.close();
        throw new Error(
          `[global-setup] Não foi possível fazer login após ${MAX_ATTEMPTS} tentativas. ` +
          `Verifique se o app está no ar e se o rate-limit não está esgotado.\n` +
          `Dica rápida: aguarde 60 s e rode novamente, ou adicione ` +
          `RateLimit__Auth__PermitLimit=30 no .env da API e faça rebuild.`
        );
      }
    }
  }
}

export default globalSetup;
