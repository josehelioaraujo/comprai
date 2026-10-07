-- =============================================================================
-- V001__initial_schema.sql
-- Comprai — Schema inicial completo
-- Convenções:
--   • Todos os campos de data/hora: TIMESTAMPTZ (UTC nativo Npgsql)
--   • IdempotencyKey em tabelas com escrita externa ou retentável
--   • Outbox Pattern: order_outbox, payment_outbox, notification_outbox
-- =============================================================================

-- =============================================================================
-- 1. CLIENTES E SESSÕES
-- =============================================================================

CREATE TABLE IF NOT EXISTS customer (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    name             VARCHAR(200) NOT NULL,
    email            VARCHAR(200) NOT NULL,
    phone            VARCHAR(20),
    document         VARCHAR(20),
    channel          VARCHAR(20) NOT NULL DEFAULT 'web', -- web | whatsapp | mobile
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_customer_email ON customer (email);

-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS customer_address (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id      UUID        NOT NULL REFERENCES customer (id) ON DELETE CASCADE,
    label            VARCHAR(50),                          -- casa, trabalho, etc
    zip_code         VARCHAR(10) NOT NULL,
    street           VARCHAR(200) NOT NULL,
    number           VARCHAR(20),
    complement       VARCHAR(100),
    neighborhood     VARCHAR(100),
    city             VARCHAR(100) NOT NULL,
    state            CHAR(2)     NOT NULL,
    country          CHAR(2)     NOT NULL DEFAULT 'BR',
    is_default       BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_customer_address_customer ON customer_address (customer_id);

-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS session (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id      UUID        REFERENCES customer (id) ON DELETE SET NULL,
    channel          VARCHAR(20) NOT NULL DEFAULT 'web', -- web | whatsapp | mobile
    metadata         JSONB,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at       TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_session_customer ON session (customer_id);
CREATE INDEX IF NOT EXISTS idx_session_expires  ON session (expires_at);

-- =============================================================================
-- 2. PEDIDOS E ITENS
-- =============================================================================

CREATE TABLE IF NOT EXISTS "order" (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    idempotency_key     UUID        NOT NULL,
    session_id          UUID        REFERENCES session (id) ON DELETE SET NULL,
    customer_id         UUID        REFERENCES customer (id) ON DELETE SET NULL,
    status              VARCHAR(30) NOT NULL DEFAULT 'pending',
    -- pending | confirmed | processing | shipped | delivered | cancelled | returned
    total_amount        NUMERIC(12,2) NOT NULL,
    currency            CHAR(3)     NOT NULL DEFAULT 'BRL',
    -- endereço snapshot imutável
    shipping_zip        VARCHAR(10),
    shipping_street     VARCHAR(200),
    shipping_number     VARCHAR(20),
    shipping_complement VARCHAR(100),
    shipping_city       VARCHAR(100),
    shipping_state      CHAR(2),
    shipping_country    CHAR(2)     DEFAULT 'BR',
    -- rastreio
    tracking_code       VARCHAR(100),
    cancel_reason       TEXT,
    delivered_at        TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_order_idempotency ON "order" (idempotency_key);
CREATE INDEX IF NOT EXISTS idx_order_customer         ON "order" (customer_id);
CREATE INDEX IF NOT EXISTS idx_order_status           ON "order" (status);
CREATE INDEX IF NOT EXISTS idx_order_created          ON "order" (created_at DESC);

-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS order_item (
    id               UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id         UUID          NOT NULL REFERENCES "order" (id) ON DELETE CASCADE,
    product_id       VARCHAR(200)  NOT NULL,
    product_title    VARCHAR(500)  NOT NULL,
    sku              VARCHAR(100),
    quantity         INT           NOT NULL CHECK (quantity > 0),
    unit_price       NUMERIC(12,2) NOT NULL,
    original_price   NUMERIC(12,2),
    source           VARCHAR(50),  -- MercadoLivre | Shopify | FakeCatalog
    image_url        TEXT,
    product_url      TEXT,
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_order_item_order ON order_item (order_id);

-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS order_history (
    id               UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id         UUID          NOT NULL REFERENCES "order" (id) ON DELETE CASCADE,
    customer_id      UUID          REFERENCES customer (id) ON DELETE SET NULL,
    status           VARCHAR(30)   NOT NULL, -- delivered | cancelled | returned
    total_amount     NUMERIC(12,2) NOT NULL,
    items_snapshot   JSONB         NOT NULL, -- snapshot dos itens no momento final
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_order_history_order ON order_history (order_id);
CREATE INDEX IF NOT EXISTS idx_order_history_customer     ON order_history (customer_id);

-- =============================================================================
-- 3. FULFILLMENT — APPEND-ONLY
-- =============================================================================

CREATE TABLE IF NOT EXISTS fulfillment_event (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id         UUID        NOT NULL REFERENCES "order" (id) ON DELETE CASCADE,
    status           VARCHAR(50) NOT NULL,
    location         VARCHAR(200),
    tracking_code    VARCHAR(100),
    description      TEXT,
    occurred_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
    -- NUNCA UPDATE/DELETE — append-only por design
);

CREATE INDEX IF NOT EXISTS idx_fulfillment_order      ON fulfillment_event (order_id);
CREATE INDEX IF NOT EXISTS idx_fulfillment_occurred   ON fulfillment_event (occurred_at DESC);

-- =============================================================================
-- 4. PAGAMENTO
-- =============================================================================

CREATE TABLE IF NOT EXISTS payment (
    id               UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    idempotency_key  UUID          NOT NULL,
    order_id         UUID          NOT NULL REFERENCES "order" (id) ON DELETE CASCADE,
    provider         VARCHAR(30)   NOT NULL, -- mock | stripe | efi
    method           VARCHAR(20)   NOT NULL, -- pix | credit_card | debit_card
    status           VARCHAR(20)   NOT NULL DEFAULT 'pending',
    -- pending | processing | confirmed | failed | refunded
    amount           NUMERIC(12,2) NOT NULL,
    currency         CHAR(3)       NOT NULL DEFAULT 'BRL',
    -- Pix
    pix_key          VARCHAR(200),
    pix_qr_code      TEXT,
    pix_expires_at   TIMESTAMPTZ,
    -- Cartão
    card_last4       CHAR(4),
    card_brand       VARCHAR(20),
    -- Controle
    external_id      VARCHAR(200), -- ID no provider (Stripe/Efi)
    paid_at          TIMESTAMPTZ,
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_payment_idempotency ON payment (idempotency_key);
CREATE INDEX IF NOT EXISTS idx_payment_order             ON payment (order_id);
CREATE INDEX IF NOT EXISTS idx_payment_status            ON payment (status);

-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS refund (
    id               UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_id       UUID          NOT NULL REFERENCES payment (id) ON DELETE CASCADE,
    amount           NUMERIC(12,2) NOT NULL,
    reason           TEXT,
    status           VARCHAR(20)   NOT NULL DEFAULT 'pending',
    -- pending | processing | completed | failed
    external_id      VARCHAR(200),
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_refund_payment ON refund (payment_id);

-- =============================================================================
-- 5. WEBHOOKS
-- =============================================================================

CREATE TABLE IF NOT EXISTS webhook_event (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    provider         VARCHAR(30) NOT NULL, -- stripe | efi
    external_id      VARCHAR(200) NOT NULL,
    event_type       VARCHAR(100) NOT NULL,
    payload          JSONB        NOT NULL,
    processed_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_webhook_provider_external ON webhook_event (provider, external_id);

-- =============================================================================
-- 6. OUTBOX PATTERN
-- =============================================================================

CREATE TABLE IF NOT EXISTS order_outbox (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    idempotency_key  UUID        NOT NULL,
    topic            VARCHAR(200) NOT NULL, -- ucp.order.created | ucp.order.updated | ucp.order.cancelled
    payload          JSONB        NOT NULL,
    status           VARCHAR(20)  NOT NULL DEFAULT 'pending', -- pending | sent | failed
    retry_count      INT          NOT NULL DEFAULT 0,
    last_error       TEXT,
    next_retry_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    sent_at          TIMESTAMPTZ,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_order_outbox_idempotency ON order_outbox (idempotency_key);
CREATE INDEX IF NOT EXISTS idx_order_outbox_pending
    ON order_outbox (next_retry_at)
    WHERE status IN ('pending', 'failed');

-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS payment_outbox (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    idempotency_key  UUID        NOT NULL,
    topic            VARCHAR(200) NOT NULL, -- ucp.payment.confirmed | ucp.payment.failed | ucp.payment.refunded
    payload          JSONB        NOT NULL,
    status           VARCHAR(20)  NOT NULL DEFAULT 'pending',
    retry_count      INT          NOT NULL DEFAULT 0,
    last_error       TEXT,
    next_retry_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    sent_at          TIMESTAMPTZ,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_payment_outbox_idempotency ON payment_outbox (idempotency_key);
CREATE INDEX IF NOT EXISTS idx_payment_outbox_pending
    ON payment_outbox (next_retry_at)
    WHERE status IN ('pending', 'failed');

-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS notification_outbox (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    idempotency_key  UUID        NOT NULL,
    queue            VARCHAR(200) NOT NULL, -- notifications.email | notifications.whatsapp
    payload          JSONB        NOT NULL,
    status           VARCHAR(20)  NOT NULL DEFAULT 'pending',
    retry_count      INT          NOT NULL DEFAULT 0,
    last_error       TEXT,
    next_retry_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    sent_at          TIMESTAMPTZ,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_notification_outbox_idempotency ON notification_outbox (idempotency_key);
CREATE INDEX IF NOT EXISTS idx_notification_outbox_pending
    ON notification_outbox (next_retry_at)
    WHERE status IN ('pending', 'failed');

-- =============================================================================
-- 7. NOTIFICAÇÕES
-- =============================================================================

CREATE TABLE IF NOT EXISTS notification (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id         UUID        REFERENCES "order" (id) ON DELETE SET NULL,
    channel          VARCHAR(20) NOT NULL, -- email | whatsapp | sms
    type             VARCHAR(50) NOT NULL, -- order_confirmed | payment_confirmed | shipped | delivered
    recipient        VARCHAR(200) NOT NULL,
    subject          VARCHAR(500),
    body             TEXT,
    status           VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending | sent | failed
    retry_count      INT         NOT NULL DEFAULT 0,
    sent_at          TIMESTAMPTZ,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_notification_order  ON notification (order_id);
CREATE INDEX IF NOT EXISTS idx_notification_status ON notification (status);

-- =============================================================================
-- 8. ANALYTICS E CONTROLE
-- =============================================================================

CREATE TABLE IF NOT EXISTS search_log (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id       UUID        REFERENCES session (id) ON DELETE SET NULL,
    query            TEXT        NOT NULL,
    results_count    INT         NOT NULL DEFAULT 0,
    sources          TEXT[],
    duration_ms      INT,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_search_log_session ON search_log (session_id);
CREATE INDEX IF NOT EXISTS idx_search_log_created ON search_log (created_at DESC);

-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS cart_snapshot (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id       UUID        NOT NULL REFERENCES session (id) ON DELETE CASCADE,
    items            JSONB       NOT NULL,
    total_amount     NUMERIC(12,2),
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_cart_snapshot_session ON cart_snapshot (session_id);

-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS idempotency_key (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    endpoint         VARCHAR(200) NOT NULL,
    key              UUID         NOT NULL,
    response_status  INT          NOT NULL,
    response_body    JSONB,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    expires_at       TIMESTAMPTZ  NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_idempotency_endpoint_key ON idempotency_key (endpoint, key);
CREATE INDEX IF NOT EXISTS idx_idempotency_expires             ON idempotency_key (expires_at);
