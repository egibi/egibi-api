-- ============================================
-- Egibi PostgreSQL Initialization
-- ============================================
-- This script runs ONCE on first container start
-- (when the data volume is empty).
-- ============================================

-- Enable useful extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";    -- UUID generation
CREATE EXTENSION IF NOT EXISTS "pgcrypto";     -- Crypto functions (backup, not primary encryption)

-- Log confirmation
DO $$
BEGIN
  RAISE NOTICE 'Egibi PostgreSQL initialized successfully';
  RAISE NOTICE 'Database: %', current_database();
  RAISE NOTICE 'Extensions: uuid-ossp, pgcrypto';
END $$;
