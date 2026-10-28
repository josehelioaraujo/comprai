/**
 * full-flow-visual.spec.ts — Demonstração Visual da Jornada do Cliente
 *
 * 8 cenas que contam a história completa do Comprai:
 *   Cena 01 — Tela de Login
 *   Cena 02 — Interface do Chat após autenticação
 *   Cena 03 — Busca inteligente via chat (IA)
 *   Cena 04 — Adicionando produto ao carrinho
 *   Cena 05 — Checkout com dados de entrega
 *   Cena 06 — Pagamento via PIX
 *   Cena 07 — Confirmação do pedido
 *   Cena 08 — Histórico de pedidos na área do cliente
 *
 * Roda com playwright.config.visual.ts (video: 'on', 1280×720).
 * NÃO substitui os testes API T01-T31 — é um arquivo separado para demos.
 *
 * Filosofia: não falha por causa do LLM não estar conectado.
 * Registra evidências visuais independente do estado do agente.
 */
import { test, expect } from '@playwright/test';
import type { Page, APIRequestContext } from '@playwright/test';

const API_URL      = process.env.E2E_API_URL       || 'http://2.25.122.11:5020';
const E2E_EMAIL    = process.env.E2E_USER_EMAIL    || 'e2e@comprai.test';
const E2E_PASSWORD = process.env.E2E_USER_PASSWORD || 'E2eTest@2026!';
const E2E_NAME     = process.env.E2E_USER_NAME     || 'E2E Tester';

// Sessão de carrinho exclusiva para este run
const SESSION_ID = `visual-${Date.now()}`;

const DEMO_PRODUCT = {
  id:    'demo-iphone-001',
  name:  'iPhone 16 Pro — 256 GB — Titânio Preto',
  price: 8999.99,
  image: 'https://dummyjson.com/image/150',
  src:   'DummyJSON',
};

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Pausa para a câmera "respirar" — o espectador vê a tela por uns instantes. */
async function pause(page: Page, ms = 1_500) {
  await page.waitForTimeout(ms);
}

/** Tenta clicar se visível, sem falhar se não encontrar. */
async function tryClick(page: Page, selector: string, timeout = 3_000) {
  const el = page.locator(selector).first();
  if (await el.isVisible({ timeout }).catch(() => false)) {
    await el.click();
    return true;
  }
  return false;
}

/** Adiciona item ao carrinho via API para garantir estado antes de abrir a UI. */
async function addToCartViaAPI(request: APIRequestContext) {
  return request.post(`${API_URL}/api/cart`, {
    data: {
      sessionId:   SESSION_ID,
      productId:   DEMO_PRODUCT.id,
      productName: DEMO_PRODUCT.name,
      price:       DEMO_PRODUCT.price,
      quantity:    1,
      source:      DEMO_PRODUCT.src,
      imageUrl:    DEMO_PRODUCT.image,
    },
    headers: { 'X-Session-Id': SESSION_ID },
  }).catch(e => { console.log('[addToCart] skipped:', e.message); return null; });
}

// ── Suite ─────────────────────────────────────────────────────────────────────

test.describe('Jornada do Cliente', () => {

  test.beforeAll(async ({ request }) => {
    // Garante que o usuário de demonstração existe
    await request.post(`${API_URL}/api/auth/register`, {
      data: { name: E2E_NAME, email: E2E_EMAIL, password: E2E_PASSWORD },
    }).catch(() => { /* já existe — ok */ });
  });

  // ────────────────────────────────────────────────────────────────────────────
  test('01 Tela de Login — Bem-vindo ao Comprai', async ({ page }) => {
    await page.goto('/auth/login');
    await page.waitForLoadState('networkidle');
    await pause(page, 2_000); // câmera vê a tela de login

    const emailInput = page.locator('input[type="email"]').first();
    const passInput  = page.locator('input[type="password"]').first();

    await expect(emailInput).toBeVisible({ timeout: 10_000 });
    await emailInput.fill(E2E_EMAIL);
    await pause(page, 600);
    await passInput.fill(E2E_PASSWORD);
    await pause(page, 800); // campos preenchidos visíveis

    // Não submete — global-setup já garantiu login; essa cena é só visual
  });

  // ────────────────────────────────────────────────────────────────────────────
  test('02 Interface do Chat — Assistente de Compras com IA', async ({ page }) => {
    await page.goto('/chat');
    await page.waitForLoadState('networkidle');
    await pause(page, 2_500); // câmera mostra o chat pronto

    // Verifica URL mas NÃO falha se auth redirecionou — o vídeo deve ser gerado mesmo sem auth
    const onChat = page.url().includes('/chat');
    console.log('URL após goto /chat:', page.url(), '| autenticado:', onChat);

    const chatInput = page.locator('textarea, input[type="text"]').first();
    const hasInput  = await chatInput.isVisible({ timeout: 8_000 }).catch(() => false);
    console.log('Chat input visível:', hasInput);
    await pause(page, 1_500);
  });

  // ────────────────────────────────────────────────────────────────────────────
  test('03 Busca Inteligente — Encontrando o Produto Certo', async ({ page }) => {
    await page.goto('/chat');
    await page.waitForLoadState('networkidle');
    await pause(page, 1_000);

    const chatInput = page.locator('textarea, input[type="text"]').first();
    await chatInput.fill('quero comprar um notebook gamer até 5 mil reais');
    await pause(page, 800); // câmera vê a mensagem digitada

    await chatInput.press('Enter');
    console.log('Mensagem enviada — aguardando resposta do agente IA…');

    // Aguarda resposta (LLM pode demorar; não falha se não responder)
    await page.waitForTimeout(12_000);

    const response = page.locator(
      '[data-testid="product-carousel"], [class*="product"], [class*="carousel"], [class*="message"]'
    ).first();
    const visible = await response.isVisible({ timeout: 3_000 }).catch(() => false);
    console.log('Resposta do agente renderizada:', visible);
    await pause(page, 2_500); // câmera vê o resultado
  });

  // ────────────────────────────────────────────────────────────────────────────
  test('04 Adicionando ao Carrinho — Produto Selecionado', async ({ page, request }) => {
    // Adiciona via API para garantir estado
    await addToCartViaAPI(request);

    await page.goto('/chat');
    await page.waitForLoadState('networkidle');
    await pause(page, 1_000);

    // Tenta abrir o carrinho pela sidebar ou botão flutuante
    const opened = await tryClick(page,
      '[aria-label*="arrinho"], [data-testid*="cart"], button:has-text("Carrinho"), [class*="cart-button"]'
    );

    if (!opened) {
      // Fallback: pede pelo chat
      const chatInput = page.locator('textarea, input[type="text"]').first();
      await chatInput.fill('ver meu carrinho');
      await chatInput.press('Enter');
      await page.waitForTimeout(7_000);
    }

    await pause(page, 2_500); // câmera vê o carrinho aberto
  });

  // ────────────────────────────────────────────────────────────────────────────
  test('05 Checkout — Preenchendo Dados de Entrega', async ({ page, request }) => {
    // Garante item no carrinho
    await addToCartViaAPI(request);

    await page.goto('/chat');
    await page.waitForLoadState('networkidle');
    await pause(page, 1_000);

    const chatInput = page.locator('textarea, input[type="text"]').first();
    await chatInput.fill('quero finalizar minha compra');
    await pause(page, 600);
    await chatInput.press('Enter');

    console.log('Aguardando card de checkout do agente…');
    await page.waitForTimeout(10_000);

    // Tenta preencher formulário se aparecer
    const nameInput = page.locator('input[name="name"], input[placeholder*="nome"], input[placeholder*="Nome"]').first();
    if (await nameInput.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await nameInput.fill(E2E_NAME);
      await page.locator('input[name="phone"], input[placeholder*="telefone"]').first()
        .fill('(11) 99999-9999').catch(() => {});
      await page.locator('input[name="cep"], input[placeholder*="CEP"]').first()
        .fill('01310-100').catch(() => {});
      await pause(page, 1_200);
    }

    await pause(page, 2_500); // câmera vê a tela de checkout
  });

  // ────────────────────────────────────────────────────────────────────────────
  test('06 Pagamento PIX — Código QR Gerado', async ({ page }) => {
    await page.goto('/chat');
    await page.waitForLoadState('networkidle');
    await pause(page, 800);

    const chatInput = page.locator('textarea, input[type="text"]').first();
    await chatInput.fill('pagar com pix');
    await pause(page, 500);
    await chatInput.press('Enter');

    console.log('Aguardando card PIX do agente…');
    await page.waitForTimeout(10_000);

    const pixCard = page.locator('[class*="pix"], [class*="Pix"], :text("Pix"), :text("QR")').first();
    const hasPix  = await pixCard.isVisible({ timeout: 3_000 }).catch(() => false);
    console.log('Card PIX visível:', hasPix);
    await pause(page, 3_000); // câmera vê o QR code / instruções PIX
  });

  // ────────────────────────────────────────────────────────────────────────────
  test('07 Pedido Confirmado — Compra Realizada com Sucesso', async ({ page, request }) => {
    // Tenta criar pedido de demonstração via API
    const checkoutRes = await request.post(`${API_URL}/api/checkout`, {
      data: {
        sessionId: SESSION_ID,
        email:     E2E_EMAIL,
        customer: {
          name:           E2E_NAME,
          email:          E2E_EMAIL,
          phone:          '(11) 99999-9999',
          document:       '123.456.789-09',
          cep:            '01310-100',
          street:         'Av. Paulista',
          number:         '1000',
          complement:     '',
          neighborhood:   'Bela Vista',
          city:           'São Paulo',
          state:          'SP',
          shippingMethod: 'standard',
        },
      },
      headers: { 'X-Session-Id': SESSION_ID },
    }).catch(() => null);

    const orderId = await checkoutRes?.json()
      .then(b => b?.orderId || b?.id || b?.data?.orderId)
      .catch(() => null);

    console.log('orderId criado:', orderId || '(não disponível)');

    await page.goto('/chat');
    await page.waitForLoadState('networkidle');
    await pause(page, 1_000);

    const chatInput = page.locator('textarea, input[type="text"]').first();
    await chatInput.fill('meu pedido foi confirmado?');
    await chatInput.press('Enter');

    await page.waitForTimeout(7_000);
    await pause(page, 3_000); // câmera vê a confirmação do pedido
  });

  // ────────────────────────────────────────────────────────────────────────────
  test('08 Histórico de Pedidos — Área do Cliente', async ({ page }) => {
    // Tenta navegar diretamente para /account ou /profile
    await page.goto('/account');
    await page.waitForLoadState('networkidle').catch(() => {});
    await pause(page, 1_000);

    const url = page.url();
    const isOnAccount = url.includes('/account') || url.includes('/profile');

    if (!isOnAccount) {
      // Fallback: chat
      await page.goto('/chat');
      await page.waitForLoadState('networkidle');
      const chatInput = page.locator('textarea, input[type="text"]').first();
      await chatInput.fill('mostrar histórico dos meus pedidos');
      await chatInput.press('Enter');
      await page.waitForTimeout(8_000);
    }

    await pause(page, 3_000); // câmera vê o histórico de pedidos
    console.log('URL final da cena 08:', page.url());
  });

});
