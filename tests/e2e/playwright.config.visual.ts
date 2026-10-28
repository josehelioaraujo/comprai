/**
 * playwright.config.visual.ts — Config para testes de demonstração visual
 *
 * Usado pelo full-flow-visual.spec.ts para gerar vídeos da jornada do cliente.
 * Cada teste captura uma cena que é exibida no OpsWatch e em apresentações
 * comerciais / reviews de produto.
 *
 * Diferenças do config padrão:
 *   - testMatch: apenas full-flow-visual.spec.ts
 *   - video: 'on' — grava todos os testes
 *   - screenshot: 'on' — captura tela em todos os testes
 *   - outputDir: test-results-visual (separado dos resultados API)
 *   - viewport: 1280×720 (padrão para apresentações)
 *   - globalSetup: faz login antes de todos os testes
 *   - storageState: reutiliza sessão salva pelo global-setup
 */
import { defineConfig, devices } from '@playwright/test';

const BASE_URL = process.env.E2E_BASE_URL || 'http://2.25.122.11:3002';
const API_URL  = process.env.E2E_API_URL  || 'http://2.25.122.11:5020';

export default defineConfig({
  testDir:   './tests',
  testMatch: ['**/full-flow-visual.spec.ts'],
  fullyParallel: false,
  forbidOnly:    !!process.env.CI,
  retries:       0,   // Vídeos de retry são confusos; melhor falhar limpo
  workers:       1,
  reporter: [
    ['list'],
    ['json', { outputFile: 'playwright-results-visual.json' }],
  ],
  use: {
    baseURL:           BASE_URL,
    video:             'on',
    screenshot:        'on',
    trace:             'off',
    actionTimeout:     20_000,
    navigationTimeout: 40_000,
    storageState:      'storageState.json',
  },
  outputDir: 'test-results-visual',
  timeout:   90_000,
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1280, height: 720 },
        launchOptions: {
          ...(process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
            ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH }
            : {}),
          // --no-sandbox é obrigatório em CI Linux sem user namespace (GitHub Actions, Docker)
          // Sem isso, Chromium pode crashar silenciosamente e NÃO gerar os .webm
          args: process.env.CI
            ? ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage']
            : [],
        },
      },
    },
  ],
  /* Login único antes da suite — versão suave que NUNCA aborta os testes visuais */
  globalSetup: './global-setup-visual',
});

export { BASE_URL, API_URL };
