-- =============================================================================
-- V009__postgres_least_privilege.sql
-- Fase 4 Segurança: redução de privilégios Postgres (least-privilege)
--
-- Criamos um role somente-leitura "comprai_readonly" para uso futuro por
-- ferramentas de BI, relatórios e health-checks, sem acesso de escrita.
-- O role "comprai" mantém DML, mas perde permissões de superusuário/DDL
-- que não são necessárias em produção.
--
-- ATENÇÃO: este script assume que o banco "comprai" e o usuário "comprai"
-- já existem (criados pelo docker-compose via POSTGRES_USER/POSTGRES_DB).
-- =============================================================================

-- 1. Role somente-leitura para BI/relatórios
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'comprai_readonly') THEN
        CREATE ROLE comprai_readonly NOLOGIN;
    END IF;
END
$$;

-- 2. Garantir que comprai_readonly possa conectar ao banco
GRANT CONNECT ON DATABASE comprai TO comprai_readonly;

-- 3. Grant SELECT em todas as tabelas existentes
GRANT USAGE  ON SCHEMA public        TO comprai_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO comprai_readonly;

-- 4. Propagar para tabelas criadas no futuro
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT ON TABLES TO comprai_readonly;

-- 5. Revogar CREATE no schema public do usuário comprai
--    (migrações rodam como superusuário de migration, não como comprai)
REVOKE CREATE ON SCHEMA public FROM PUBLIC;

-- 6. Garantir que comprai tem apenas DML (SELECT, INSERT, UPDATE, DELETE)
--    nas tabelas existentes — sem necessidade de CREATE TABLE / DROP TABLE
GRANT USAGE  ON SCHEMA public                        TO comprai;
GRANT SELECT, INSERT, UPDATE, DELETE
      ON ALL TABLES IN SCHEMA public                 TO comprai;
GRANT USAGE, SELECT
      ON ALL SEQUENCES IN SCHEMA public              TO comprai;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES   TO comprai;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES                 TO comprai;
