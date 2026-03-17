-- schema.sql — Fire Rescue Supabase (PostgreSQL) Database Schema

-- Shared trigger function: auto-update updated_at
CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

--1. PLAYERS 
CREATE TABLE IF NOT EXISTS players (
    id                  SERIAL          PRIMARY KEY,
    username            TEXT            NOT NULL UNIQUE,
    progression_level   INT             NOT NULL DEFAULT 1,
    total_score         INT             NOT NULL DEFAULT 0,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE OR REPLACE TRIGGER players_updated_at
    BEFORE UPDATE ON players
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();