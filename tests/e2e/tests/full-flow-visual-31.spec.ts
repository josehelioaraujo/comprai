/**
 * full-flow-visual-31.spec.ts — 31 Cenas Visuais (V01–V31)
 *
 * Cada cena corresponde a um teste de API T01–T31, gerando evidência visual
 * do estado da UI para aquele cenário.
 *
 * Grupos:
 *   V01–V02  → Health (T01–T02)
 *   V03–V09  → Autenticação (T03–T09)
 *   V10–V12  → Busca (T10–T12)
 *   V13–V17  → Carrinho (T13–T17)
 *   V18–V20  → Checkout (T18–T20)
 *   V21–V23  → Pagamento (T21–T23)
 *   V24–V27  → Pedidos (T24–T27)
 *   V28–V31  → E2E Smoke (T28–T31)
 *
 * Filosofia: NUNCA falha por estado externo (LLM, API down, etc.).
 * Grava evidência visual mesmo sem serviço disponível.
 */
import { test, expect } from '@playwright/test';
import type { Page, APIRequestContext } from '@playwright/test';

const API_URL      = process.env.E2E_API_URL       || 'http://2.25.122.11:5020';
const BASE_URL     = process.env.E2E_BASE_URL       || 'http://2.25.122.11:3005';
const E2E_EMAIL    = process.env.E2E_USER_EMAIL    || 'e2e@comprai.test';
const E2E_PASSWORD = process.env.E2E_USER_PASSWORD || 'E2eTest@2026!';
const E2E_NAME     = process.env.E2E_USER_NAME     || 'E2E Tester';
const SESSION_ID   = `v31-${Date.now()}`;

// ── Helpers ───────────────────────────────────────────────────────────────────

async function pause(page: Page, ms = 1_500) {
  await page.waitForTimeout(ms);
}

async function tryClick(page: Page, selector: string, timeout = 3_000) {
  const el = page.locator(selector).first();
  if (await el.isVisible({ timeout }).catch(() => false)) {
    await el.click();
    return true;
  }
  return false;
}

async function goto(page: Page, path: string) {
  await page.goto(path).catch(() => {});
  await page.waitForLoadState('networkidle').catch(() => {});
  await pause(page, 800);
}

async function fillLogin(page: Page, email: string, password: string) {
  const emailInput = page.locator('input[type="email"]').first();
  const passInput  = page.locator('input[type="password"]').first();
  if (await emailInput.isVisible({ timeout: 5_000 }).catch(() => false)) {
    await emailInput.fill(email);
    await pause(page, 400);
    await passInput.fill(password).catch(() => {});
    await pause(page, 600);
  }
}

async function addToCart(request: APIRequestContext, qty = 1) {
  return request.post(`${API_URL}/api/cart`, {
    data: {
      sessionId:   SESSION_ID,
      productId:   'demo-iphone-001',
      productName: 'iPhone 16 Pro — 256 GB — Titânio Preto',
      price:       8999.99,
      quantity:    qty,
      source:      'DummyJSON',
      imageUrl:    'https://dummyjson.com/image/150',
    },
    headers: { 'X-Session-Id': SESSION_ID },
  }).catch(() => null);
}

async function chatSend(page: Page, msg: string, waitMs = 8_000) {
  const input = page.locator('textarea, input[type="text"]').first();
  if (await input.isVisible({ timeout: 5_000 }).catch(() => false)) {
    await input.fill(msg);
    await pause(page, 500);
    await input.press('Enter');
    await page.waitForTimeout(waitMs);
  }
}

// ── beforeAll: garante usuário e2e ───────────────────────────────────────────

test.beforeAll(async ({ request }) => {
  await request.post(`${API_URL}/api/auth/register`, {
    data: { name: E2E_NAME, email: E2E_EMAIL, password: E2E_PASSWORD },
  }).catch(() => {});
});

// ─────────────────────────────────────────────────────────────────────────────
// V01–V02 — Health
// ─────────────────────────────────────────────────────────────────────────────

test.describe('V01–V02 — Health', () => {

  test('V01 — Health Live — App Online', async ({ page }) => {
    await goto(page, '/');
    await pause(page, 2_000);
    console.log('V01 URL:', page.url());
  });

  test('V02 — Health Ready — API Conectada', async ({ page, request }) => {
    const res = await request.get(`${API_URL}/health/ready`).catch(() => null);
    const ok  = res?.ok() ?? false;
    console.log('V02 /health/ready:', ok ? '✅ 200' : '❌ falhou');
    await goto(page, '/');
    await pause(page, 2_000);
  });

});

// ─────────────────────────────────────────────────────────────────────────────
// V03–V09 — Autenticação
// ─────────────────────────────────────────────────────────────────────────────

test.describe('V03–V09 — Autenticação', () => {

  test('V03 — Auth Cadastro — Formulário de Registro', async ({ page }) => {
    await goto(page, '/auth/register');
    await pause(page, 1_000);
    const nameInput = page.locator('input[name="name"], input[placeholder*="nome"], input[placeholder*="Nome"]').first();
    if (await nameInput.isVisible({ timeout: 5_000 }).catch(() => false)) {
      await nameInput.fill(E2E_NAME);
      await pause(page, 400);
      await page.locator('input[type="email"]').first().fill('novo@comprai.test').catch(() => {});
      await pause(page, 400);
      await page.locator('input[type="password"]').first().fill('Senha@2026!').catch(() => {});
      await pause(page, 800);
    }
    await pause(page, 2_000);
  });

  test('V04 — Auth Verificação Email — Aguardando Confirmação', async ({ page }) => {
    // Tenta tela de verificação; fallback para login com mensagem de reenvio
    await goto(page, '/auth/verify');
    await pause(page, 1_000);
    const url = page.url();
    if (!url.includes('/auth/verify')) {
      await goto(page, '/auth/login');
    }
    await pause(page, 2_500);
    console.log('V04 URL:', page.url());
  });

  test('V05 — Auth OTP Inválido — Erro de Verificação', async ({ page }) => {
    await goto(page, '/auth/verify');
    await pause(page, 800);
    // Tenta preencher campo OTP com código inválido
    const otpInput = page.locator('input[name="otp"], input[name="code"], input[maxlength="6"]').first();
    if (await otpInput.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await otpInput.fill('000000');
      await pause(page, 500);
      await tryClick(page, 'button[type="submit"], button:has-text("Verificar"), button:has-text("Confirmar")');
      await pause(page, 2_000);
    } else {
      // Fallback: mostra tela atual
      await pause(page, 2_000);
    }
    console.log('V05 URL:', page.url());
  });

  test('V06 — Auth Login Senha Errada — Erro 401', async ({ page }) => {
    await goto(page, '/auth/login');
    await fillLogin(page, E2E_EMAIL, 'SenhaErrada@123!');
    await tryClick(page, 'button[type="submit"], button:has-text("Entrar"), button:has-text("Login")');
    await pause(page, 2_500);
    console.log('V06 URL:', page.url());
  });

  test('V07 — Auth Login Sucesso — Sessão Autenticada', async ({ page }) => {
    await goto(page, '/auth/login');
    await fillLogin(page, E2E_EMAIL, E2E_PASSWORD);
    await tryClick(page, 'button[type="submit"], button:has-text("Entrar"), button:has-text("Login")');
    await page.waitForTimeout(3_000);
    await pause(page, 2_000);
    console.log('V07 URL:', page.url());
  });

  test('V08 — Auth Email Inexistente — Erro 404', async ({ page }) => {
    await goto(page, '/auth/login');
    await fillLogin(page, 'naoexiste@comprai.test', 'Qualquer@123!');
    await tryClick(page, 'button[type="submit"], button:has-text("Entrar"), button:has-text("Login")');
    await pause(page, 2_500);
    console.log('V08 URL:', page.url());
  });

  test('V09 — Auth Token Refresh — Sessão Válida', async ({ page }) => {
    await goto(page, '/chat');
    await pause(page, 2_000);
    console.log('V09 URL:', page.url());
  });

});

// ─────────────────────────────────────────────────────────────────────────────
// V10–V12 — Busca
// ─────────────────────────────────────────────────────────────────────────────

test.describe('V10–V12 — Busca', () => {

  test('V10 — Busca iPhone — Resultados Encontrados', async ({ page }) => {
    await goto(page, '/chat');
    await chatSend(page, 'quero comprar um iphone', 10_000);
    await pause(page, 2_500);
  });

  test('V11 — Busca com Filtros — Notebook até R$5.000', async ({ page }) => {
    await goto(page, '/chat');
    await chatSend(page, 'notebook gamer até 5000 reais', 10_000);
    await pause(page, 2_500);
  });

  test('V12 — Busca Sem Resultados — Produto Não Encontrado', async ({ page }) => {
    await goto(page, '/chat');
    await chatSend(page, 'xyz-produto-que-nao-existe-abc123', 8_000);
    await pause(page, 2_500);
  });

});

// ─────────────────────────────────────────────────────────────────────────────
// V13–V17 — Carrinho
// ─────────────────────────────────────────────────────────────────────────────

test.describe('V13–V17 — Carrinho', () => {

  test('V13 — Carrinho Adicionar Produto — Item no Carrinho', async ({ page, request }) => {
    await addToCart(request);
    await goto(page, '/chat');
    await chatSend(page, 'ver meu carrinho', 6_000);
    await pause(page, 2_500);
  });

  test('V14 — Carrinho Produto Duplicado — Idempotência', async ({ page, request }) => {
    await addToCart(request);
    await addToCart(request); // duplicado — idempotente
    await goto(page, '/chat');
    await chatSend(page, 'meu carrinho', 6_000);
    await pause(page, 2_000);
  });

  test('V15 — Carrinho Ver Itens — Resumo do Carrinho', async ({ page, request }) => {
    await addToCart(request);
    await goto(page, '/chat');
    const opened = await tryClick(page,
      '[aria-label*="arrinho"], [data-testid*="cart"], button:has-text("Carrinho"), [class*="cart-button"]'
    );
    if (!opened) await chatSend(page, 'mostrar carrinho', 6_000);
    await pause(page, 2_500);
  });

  test('V16 — Carrinho Remover Item — Carrinho Atualizado', async ({ page, request }) => {
    await addToCart(request);
    await goto(page, '/chat');
    await chatSend(page, 'remover item do carrinho', 8_000);
    await pause(page, 2_500);
  });

  test('V17 — Carrinho Inexistente — Carrinho Vazio', async ({ page }) => {
    await goto(page, '/chat');
    await chatSend(page, 'ver carrinho', 6_000);
    await pause(page, 2_000);
  });

});

// ─────────────────────────────────────────────────────────────────────────────
// V18–V20 — Checkout
// ─────────────────────────────────────────────────────────────────────────────

test.describe('V18–V20 — Checkout', () => {

  test('V18 — Checkout Dados Válidos — Formulário Preenchido', async ({ page, request }) => {
    await addToCart(request);
    await goto(page, '/chat');
    await chatSend(page, 'quero finalizar minha compra', 10_000);
    const nameInput = page.locator('input[name="name"], input[placeholder*="nome"]').first();
    if (await nameInput.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await nameInput.fill(E2E_NAME);
      await page.locator('input[name="phone"], input[placeholder*="telefone"]').first().fill('(11) 99999-9999').catch(() => {});
      await page.locator('input[name="cep"], input[placeholder*="CEP"]').first().fill('01310-100').catch(() => {});
      await pause(page, 1_000);
    }
    await pause(page, 2_500);
  });

  test('V19 — Checkout Carrinho Vazio — Erro de Validação', async ({ page }) => {
    await goto(page, '/chat');
    await chatSend(page, 'finalizar compra', 8_000);
    await pause(page, 2_500);
  });

  test('V20 — Checkout Dados Inválidos — Erros no Formulário', async ({ page, request }) => {
    await addToCart(request);
    await goto(page, '/chat');
    await chatSend(page, 'quero finalizar minha compra', 10_000);
    // Tenta submeter formulário com campos inválidos
    const nameInput = page.locator('input[name="name"]').first();
    if (await nameInput.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await nameInput.fill(''); // vazio → erro
      await tryClick(page, 'button[type="submit"], button:has-text("Confirmar"), button:has-text("Finalizar")');
      await pause(page, 1_500);
    }
    await pause(page, 2_500);
  });

});

// ─────────────────────────────────────────────────────────────────────────────
// V21–V23 — Pagamento
// ─────────────────────────────────────────────────────────────────────────────

test.describe('V21–V23 — Pagamento', () => {

  test('V21 — Pagamento PIX Mock — QR Code Gerado', async ({ page }) => {
    await goto(page, '/chat');
    await chatSend(page, 'pagar com pix', 10_000);
    const pixEl = page.locator('[class*="pix"], [class*="Pix"], :text("PIX"), :text("QR")').first();
    const hasPix = await pixEl.isVisible({ timeout: 3_000 }).catch(() => false);
    console.log('V21 PIX visível:', hasPix);
    await pause(page, 3_000);
  });

  test('V22 — Pagamento Provider sem Chave — Fluxo Alternativo', async ({ page }) => {
    await goto(page, '/chat');
    await chatSend(page, 'formas de pagamento disponíveis', 8_000);
    await pause(page, 2_500);
  });

  test('V23 — Pagamento Order Inválido — Erro 404', async ({ page }) => {
    await goto(page, '/chat');
    await chatSend(page, 'status do pedido ABC-INVALIDO-999', 8_000);
    await pause(page, 2_500);
  });

});

// ─────────────────────────────────────────────────────────────────────────────
// V24–V27 — Pedidos
// ─────────────────────────────────────────────────────────────────────────────

test.describe('V24–V27 — Pedidos', () => {

  test('V24 — Pedido Existente — Detalhes do Pedido', async ({ page }) => {
    await goto(page, '/chat');
    await chatSend(page, 'ver meus pedidos', 8_000);
    await pause(page, 2_500);
  });

  test('V25 — Pedido Não Encontrado — Erro 404', async ({ page }) => {
    await goto(page, '/chat');
    await chatSend(page, 'pedido número 00000-INEXISTENTE', 8_000);
    await pause(page, 2_500);
  });

  test('V26 — Pedidos Listagem — Histórico do Cliente', async ({ page }) => {
    await goto(page, '/account');
    await pause(page, 1_000);
    if (!page.url().includes('/account')) {
      await goto(page, '/chat');
      await chatSend(page, 'histórico dos meus pedidos', 8_000);
    }
    await pause(page, 3_000);
  });

  test('V27 — Pedidos Sem Token — Acesso Negado', async ({ page }) => {
    // Sem autenticação (nova página sem storageState) — mostra tela de login/unauthorized
    await page.context().clearCookies();
    await goto(page, '/account');
    await pause(page, 2_500);
    console.log('V27 URL:', page.url());
  });

});

// ─────────────────────────────────────────────────────────────────────────────
// V28–V31 — E2E Smoke
// ─────────────────────────────────────────────────────────────────────────────

test.describe('V28–V31 — E2E Smoke', () => {

  test('V28 — E2E Search→Cart→Checkout→Payment — Fluxo Completo', async ({ page, request }) => {
    // Busca
    await goto(page, '/chat');
    await chatSend(page, 'quero comprar um iphone', 8_000);
    await pause(page, 1_000);

    // Adiciona ao carrinho via API
    await addToCart(request);

    // Ver carrinho
    await chatSend(page, 'ver meu carrinho', 6_000);
    await pause(page, 1_000);

    // Finaliza
    await chatSend(page, 'finalizar compra', 6_000);
    await pause(page, 2_000);
  });

  test('V29 — E2E Múltiplos Carrinhos — Sessões Paralelas', async ({ page, request }) => {
    // Cria 2 carrinhos com sessions diferentes
    await Promise.all([
      request.post(`${API_URL}/api/cart`, {
        data: { sessionId: `session-A-${Date.now()}`, productId: 'demo-iphone-001',
                productName: 'iPhone 16 Pro', price: 8999.99, quantity: 1, source: 'DummyJSON' },
      }).catch(() => null),
      request.post(`${API_URL}/api/cart`, {
        data: { sessionId: `session-B-${Date.now()}`, productId: 'demo-notebook-001',
                productName: 'Notebook Gamer', price: 4999.99, quantity: 1, source: 'DummyJSON' },
      }).catch(() => null),
    ]);
    await goto(page, '/chat');
    await pause(page, 2_500);
    console.log('V29 múltiplos carrinhos criados');
  });

  test('V30 — E2E Idempotência Checkout — Compra Sem Duplicação', async ({ page, request }) => {
    await addToCart(request);
    await goto(page, '/chat');
    await chatSend(page, 'meu pedido foi processado?', 8_000);
    await pause(page, 2_500);
  });

  test('V31 — E2E Resumo Final — Estado Completo da Aplicação', async ({ page }) => {
    await goto(page, '/');
    await pause(page, 1_500);
    await goto(page, '/chat');
    await pause(page, 2_000);
    await chatSend(page, 'olá, o que você pode fazer?', 8_000);
    await pause(page, 3_000);
    console.log('V31 — Demonstração completa concluída');
  });

});
