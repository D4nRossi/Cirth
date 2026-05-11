-- Cirth — Postgres initialization
-- Executado automaticamente na primeira inicialização do container postgres

CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS unaccent;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- O schema completo é criado via EF Core migrations.
-- Este script garante apenas que as extensões necessárias estejam disponíveis.

-- Search config português customizada que combina com unaccent
CREATE TEXT SEARCH CONFIGURATION pt_unaccent (COPY = portuguese);
ALTER TEXT SEARCH CONFIGURATION pt_unaccent
  ALTER MAPPING FOR hword, hword_part, word
  WITH unaccent, portuguese_stem;
