/**
 * fixtures.ts — Playwright custom fixtures
 *
 * Sobrescreve o fixture `context` para gravar um arquivo HAR por teste
 * (nomeado com o título do teste). Preserva storageState, viewport,
 * userAgent e vídeo conforme configurado em playwright.config.ts.
 */
import { test as base, expect, BrowserContextOptions } from '@playwright/test';
import * as path from 'path';
import * as fs from 'fs';

export const test = base.extend({
  // Sobrescreve context para adicionar HAR por teste
  context: async ({ browser }, use, testInfo) => {
    // Garante que o diretório har-evidence/ existe
    const harDir = path.join(process.cwd(), 'har-evidence');
    fs.mkdirSync(harDir, { recursive: true });

    // Nome do arquivo: título do teste → slug seguro para filesystem
    const safeName = testInfo.title
      .replace(/[^a-z0-9]+/gi, '-')
      .replace(/^-+|-+$/g, '')
      .slice(0, 80);
    const harPath = path.join(harDir, `${safeName}-chromium.har`);

    const projectUse = testInfo.project.use as any;

    // Monta BrowserContextOptions a partir das opções do projeto
    const contextOptions: BrowserContextOptions = {};

    if (projectUse.storageState !== undefined)      contextOptions.storageState      = projectUse.storageState;
    if (projectUse.viewport !== undefined)          contextOptions.viewport          = projectUse.viewport;
    if (projectUse.userAgent !== undefined)         contextOptions.userAgent         = projectUse.userAgent;
    if (projectUse.baseURL !== undefined)           contextOptions.baseURL           = projectUse.baseURL;
    if (projectUse.ignoreHTTPSErrors !== undefined) contextOptions.ignoreHTTPSErrors = projectUse.ignoreHTTPSErrors;
    if (projectUse.extraHTTPHeaders !== undefined)  contextOptions.extraHTTPHeaders  = projectUse.extraHTTPHeaders;
    if (projectUse.colorScheme !== undefined)       contextOptions.colorScheme       = projectUse.colorScheme;
    if (projectUse.locale !== undefined)            contextOptions.locale            = projectUse.locale;
    if (projectUse.geolocation !== undefined)       contextOptions.geolocation       = projectUse.geolocation;
    if (projectUse.permissions !== undefined)       contextOptions.permissions       = projectUse.permissions;
    if (projectUse.offline !== undefined)           contextOptions.offline           = projectUse.offline;
    if (projectUse.httpCredentials !== undefined)   contextOptions.httpCredentials   = projectUse.httpCredentials;

    // Converte video: 'on' → recordVideo para preservar gravação de vídeo
    if (projectUse.video && projectUse.video !== 'off') {
      contextOptions.recordVideo = { dir: testInfo.outputDir };
    }

    // HAR recording por teste (causa raiz: path global sem diretório não grava)
    contextOptions.recordHar = {
      path:        harPath,
      content:     'embed',
      omitContent: false,
    };

    const context = await browser.newContext(contextOptions);
    await use(context);
    await context.close(); // flush HAR no disco
  },
});

export { expect } from '@playwright/test';
export type { Page } from '@playwright/test';
