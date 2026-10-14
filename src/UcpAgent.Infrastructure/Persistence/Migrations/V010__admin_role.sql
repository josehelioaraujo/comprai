-- V010: Admin Role — campo role na tabela customer
-- Valores: 'customer' (padrão) | 'admin'

ALTER TABLE customer
    ADD COLUMN IF NOT EXISTS role VARCHAR(20) NOT NULL DEFAULT 'customer';

CREATE INDEX IF NOT EXISTS idx_customer_role ON customer (role);
