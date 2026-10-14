-- =============================================================================
-- COMPRAI — Clean Database (apaga todos os dados, mantém estrutura)
-- Uso: psql -h <host> -U <user> -d <db> -f clean.sql
-- ATENCAO: irreversivel — use apenas em dev/homolog
-- =============================================================================

BEGIN;

TRUNCATE TABLE
    refund,
    payment,
    order_item,
    order_history,
    fulfillment_event,
    order_outbox,
    payment_outbox,
    notification_outbox,
    notification,
    webhook_event,
    "order",
    cart_snapshot,
    search_log,
    idempotency_key,
    customer_address,
    session,
    customer
CASCADE;

COMMIT;

SELECT 'Base limpa.' AS status;