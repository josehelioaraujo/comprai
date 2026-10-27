/**
 * global-setup-visual.ts — Setup suave para testes de demonstração visual
 *
 * Diferente do global-setup.ts (que lança erro se o login falhar), este setup
 * tenta fazer login UMA ÚNICA VEZ e, independente do resultado, sempre salva um
 * storageState.json válido e permite que os testes visuais rodem.
 *
 * Motivo: os testes T01-T31 ativam o rate-limit de auth logo antes deste setup
 * rodar. Em vez de ficar 4 × 65 s tentando e abortar no final (sem vídeos),
 * fazemos uma tentativa rápida e seguimos em frente — os vídeos da jornada
 * visual são gerados de qualquer jeito.
 */
import { chromium, FullConfig } from '@playwright/test';
import * as fs from 'fs';

const BASE_URL     = process.env.E2E_BASE_URL     || 'http://2.25.122.11:3002';
const E2E_EMAIL    = process.env.E2E_USER_EMAIL    || 'e2e@comprai.test';
const E2E_PASSWORD = process.env.E2E_USER_PASSWORD || 'E2eTest@2026!';

async function globalSetup(_config: FullConfig) {
  const executablePath = process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH;
  const browser = await chromium.launch(executablePath ? { executablePath } : {});
  const context = await browser.newContext();
  const page    = await context.newPage();

  console.log('[visual-setup] Tentando login para capturar sessão...');

  try {
    await page.goto(`${BASE_URL}/auth/login`, { timeout: 20_000 });
    await page.waitForLoadState('networkidle', { timeout: 15_000 });

    // Clica na aba "Entrar" se existir (UI com tabs)
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

    await page.waitForURL(/\/chat/, { timeout: 20_000 });
    await page.waitForLoadState('networkidle');

    // Visita /account para garantir que o token NextAuth (v5) seja validado/rotacionado
    // nessa rota protegida ANTES de salvar o storageState. O (user)/layout.tsx usa
    // useSession() que chama /api/auth/session — isso pode rotacionar o cookie.
    // Salvar o storageState APÓS essa visita captura o token mais recente.
    console.log('[visual-setup] Verificando autenticação em /account…');
    await page.goto(`${BASE_URL}/account`, { timeout: 15_000 }).catch(() => {});
    await page.waitForLoadState('networkidle', { timeout: 10_000 }).catch(() => {});
    const accountUrl = page.url();
    if (accountUrl.includes('/account') || accountUrl.includes('/profile')) {
      console.log('[visual-setup] ✅ /account acessível — autenticação confirmada.');
    } else {
      console.warn(`[visual-setup] ⚠️  /account redirecionou para: ${accountUrl}`);
    }

    await context.storageState({ path: 'storageState.json' });
    console.log('[visual-setup] ✅ Login OK — storageState.json salvo com sessão autenticada.');

  } catch (err: any) {
    // Não aborta — salva estado vazio para que os testes rodem sem auth
    console.warn(`[visual-setup] ⚠️  Login falhou (${err.message?.split('\n')[0]})`);
    console.warn('[visual-setup] Salvando storageState vazio — testes visuais rodam sem autenticação.');
    fs.writeFileSync('storageState.json', JSON.stringify({ cookies: [], origins: [] }, null, 2));

  } finally {
    await browser.close();
  }
}

export default globalSetup;
