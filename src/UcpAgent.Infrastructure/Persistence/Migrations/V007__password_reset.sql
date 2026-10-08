-- V007: Recuperação de senha
-- Armazena hash SHA-256 do token UUID (não o token bruto) + expiração

ALTER TABLE customer
    ADD COLUMN IF NOT EXISTS password_reset_token_hash TEXT,
    ADD COLUMN IF NOT EXISTS password_reset_expires_at  TIMESTAMPTZ;

CREATE INDEX IF NOT EXISTS idx_customer_password_reset_token_hash
    ON customer (password_reset_token_hash)
    WHERE password_reset_token_hash IS NOT NULL;
