-- =============================================================================
-- COMPRAI — Elevar usuário existente para role=admin
--
-- Uso:
--   docker exec -it comprai-db psql -U comprai -d comprai \
--     -c "UPDATE customer SET role='admin' WHERE email='SEU_EMAIL@exemplo.com';"
--
-- Ou passando este arquivo:
--   docker exec -i comprai-db psql -U comprai -d comprai < seed/set-admin-role.sql
--
-- ⚠️  Substitua o e-mail abaixo antes de executar.
-- =============================================================================

-- Substitua o e-mail pelo e-mail do administrador real:
UPDATE customer
   SET role = 'admin'
 WHERE email = 'SEU_EMAIL@exemplo.com';

-- Confirmar:
SELECT id, name, email, role FROM customer WHERE role = 'admin';
