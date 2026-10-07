-- V003: unique index em customer_address para ON CONFLICT no SaveAddressAsync
CREATE UNIQUE INDEX IF NOT EXISTS idx_customer_address_unique
    ON customer_address (customer_id, zip_code, COALESCE(number, ''));
