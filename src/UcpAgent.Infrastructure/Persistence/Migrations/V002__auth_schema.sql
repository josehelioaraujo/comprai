-- V002: Auth — provider identity + password hash na tabela customer
-- Suporte a múltiplos providers: credentials | google | github | microsoft

ALTER TABLE customer
    ADD COLUMN IF NOT EXISTS provider        VARCHAR(50)   NOT NULL DEFAULT 'credentials',
    ADD COLUMN IF NOT EXISTS provider_id     VARCHAR(255),
    ADD COLUMN IF NOT EXISTS password_hash   VARCHAR(255),
    ADD COLUMN IF NOT EXISTS avatar_url      VARCHAR(500),
    ADD COLUMN IF NOT EXISTS email_verified  BOOLEAN       NOT NULL DEFAULT FALSE;

CREATE UNIQUE INDEX IF NOT EXISTS idx_customer_provider
    ON customer (provider, provider_id)
    WHERE provider_id IS NOT NULL;
