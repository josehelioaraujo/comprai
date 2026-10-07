-- V005: email verification OTP (6-char uppercase alphanumeric)
-- Código válido por 15 minutos, armazenado em texto plano (curto e descartável)
-- access: POST /api/auth/send-verification → gera OTP → armazena → envia via Resend
--         POST /api/auth/verify-email      → valida OTP → marca email_verified=TRUE

ALTER TABLE customer
    ADD COLUMN IF NOT EXISTS email_verification_token      VARCHAR(6),
    ADD COLUMN IF NOT EXISTS email_verification_expires_at TIMESTAMPTZ;

-- índice para lookup no endpoint de verificação
CREATE INDEX IF NOT EXISTS idx_customer_email_verification_token
    ON customer (email_verification_token)
    WHERE email_verification_token IS NOT NULL;
