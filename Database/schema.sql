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

    -- ── 2. GAME_SETTINGS ─────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS game_settings (
    player_id          INT             PRIMARY KEY REFERENCES players(id) ON DELETE CASCADE,
    volume             FLOAT           NOT NULL DEFAULT 1.0 CHECK (volume BETWEEN 0.0 AND 1.0),
    map_width          INT             NOT NULL DEFAULT 64,
    map_height         INT             NOT NULL DEFAULT 64,
    city_count         INT             NOT NULL DEFAULT 3 CHECK (city_count >= 1),
    max_save_slots     INT             NOT NULL DEFAULT 5,
    auto_save_interval FLOAT           NOT NULL DEFAULT 60.0,
    updated_at         TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE OR REPLACE TRIGGER game_settings_updated_at
    BEFORE UPDATE ON game_settings
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();