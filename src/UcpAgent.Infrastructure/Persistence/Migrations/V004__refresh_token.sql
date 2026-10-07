-- V004: suporte a refresh token com rotação
-- access_token: 15min | refresh_token: 7 dias, opaque, armazenado como hash SHA-256

ALTER TABLE customer
    ADD COLUMN IF NOT EXISTS refresh_token_hash      TEXT,
    ADD COLUMN IF NOT EXISTS refresh_token_expires_at TIMESTAMPTZ;

-- índice para lookup rápido no endpoint POST /api/auth/refresh
CREATE INDEX IF NOT EXISTS idx_customer_refresh_token_hash
    ON customer (refresh_token_hash)
    WHERE refresh_token_hash IS NOT NULL;
