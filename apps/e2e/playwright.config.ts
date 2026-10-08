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
  },
  timeout: 60_000,
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  /* Variáveis disponíveis nos specs via process.env */
  globalSetup: undefined,
});

/* Exporta constantes para uso nos specs */
export { BASE_URL, API_URL };
