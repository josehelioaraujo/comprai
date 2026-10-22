/**
 * playwright.config.v2.ts — Config exclusivo para E2E v2 (T01-T31)
 *
 * Diferenças do config padrão:
 *   - Sem globalSetup: v2 usa apenas fixture `request` (API-level), sem UI
 *   - Sem storageState: não requer login via browser
 *   - Video off: sem UI para gravar
 */
import { defineConfig, devices } from '@playwright/test';

const API_URL = process.env.E2E_API_URL || 'http://2.25.122.11:5020';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: [
    ['list'],
    ['json', { outputFile: 'playwright-results.json' }],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
  ],
  use: {
    actionTimeout: 15_000,
    // Sem storageState — v2 testa apenas via API (fixture `request`)
    // Sem video/screenshot — sem UI
  },
  timeout: 60_000,
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        ...(process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
          ? { launchOptions: { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH } }
          : {}),
      },
    },
  ],
  // Sem globalSetup — v2 não depende de sessão autenticada via UI
});

export { API_URL };
