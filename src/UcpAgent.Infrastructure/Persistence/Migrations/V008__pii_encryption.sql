-- =============================================================================
-- V008__pii_encryption.sql
-- Fase 2 Segurança: ampliar colunas PII para suportar texto criptografado AES-256-GCM
-- Campos afetados: customer.phone, customer.document
--                  customer_address.street, number, complement, neighborhood
--
-- O formato criptografado é: "enc:<base64>" (~160-400 chars dependendo do original)
-- Dados legado (plaintext) coexistem sem alteração — IEncryptionService faz fallback.
-- =============================================================================

-- customer
ALTER TABLE customer
    ALTER COLUMN phone    TYPE TEXT,
    ALTER COLUMN document TYPE TEXT;

-- customer_address
ALTER TABLE customer_address
    ALTER COLUMN street       TYPE TEXT,
    ALTER COLUMN number       TYPE TEXT,
    ALTER COLUMN complement   TYPE TEXT,
    ALTER COLUMN neighborhood TYPE TEXT;
