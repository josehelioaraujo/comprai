import { defineConfig, devices } from '@playwright/test';

const BASE_URL = process.env.E2E_BASE_URL || 'http://2.25.122.11:3002';
const API_URL  = process.env.E2E_API_URL  || 'http://2.25.122.11:5020';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: [
    ['list'],
    ['html',  { outputFolder: 'playwright-report', open: 'never' }],
    ['json',  { outputFile: 'playwright-results.json' }],
  ],
  use: {
    baseURL: BASE_URL,
    video: 'on',
    screenshot: 'only-on-failure',
    trace: 'on-first-retry',
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    // Reutiliza sessão salva pelo global-setup (evita rate-limit de login)
    storageState: 'storageState.json',
    // Captura tráfego HTTP por teste para exibição de request/response
    recordHar: {
      path: 'har-evidence',
      content: 'embed',
      omitContent: false,
    },
  },
  timeout: 60_000,
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        // Usa Chromium pré-instalado em ambientes sem download (ex.: containers CI)
        ...(process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
          ? { launchOptions: { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH } }
          : {}),
      },
    },
  ],
  /* Login único antes da suite; cookies reutilizados por todos os testes */
  globalSetup: './global-setup',
});

/* Exporta constantes para uso nos specs */
export { BASE_URL, API_URL };
