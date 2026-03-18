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

-- ── 3. SAVE_GAMES ────────────────────────────────────────────────────────────
-- Stores the full serialised game state as JSONB.
-- Extra columns let the Load Game screen display meaningful context without
-- deserialising the full JSONB payload.

CREATE TABLE IF NOT EXISTS save_games (
    id                  SERIAL          PRIMARY KEY,
    player_id           INT             NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    slot_name           TEXT            NOT NULL DEFAULT 'default',
    save_display_name   TEXT            NOT NULL DEFAULT '',   -- human-readable label shown in UI
    game_state          JSONB           NOT NULL,
    round_number        INT             NOT NULL DEFAULT 0,    -- current game round at save time
    city_count          INT             NOT NULL DEFAULT 1,    -- number of cities on the map
    map_width           INT             NOT NULL DEFAULT 64,   -- GridSystem.width at save time
    map_height          INT             NOT NULL DEFAULT 64,   -- GridSystem.height at save time
    saved_at            TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_save_games_player_id ON save_games(player_id);
CREATE INDEX IF NOT EXISTS idx_save_games_saved_at  ON save_games(player_id, saved_at DESC);

-- ── Save-slot limit trigger ──────────────────────────────────────────────────
-- After each INSERT into save_games, check if the player has exceeded their
-- max_save_slots limit; if so, delete the oldest save.
-- This enforces the Load Game "Limit to the number of saved games" requirement
-- and corresponds to DatabaseProvider.count in the class diagram.

CREATE OR REPLACE FUNCTION enforce_save_slot_limit()
RETURNS TRIGGER AS $$
DECLARE
    v_max   INT;
    v_count INT;
BEGIN
    -- Resolve the player's save-slot limit (fall back to 5 if no settings row)
    SELECT max_save_slots INTO v_max
    FROM game_settings
    WHERE player_id = NEW.player_id;

    IF v_max IS NULL THEN v_max := 5; END IF;

    SELECT COUNT(*) INTO v_count
    FROM save_games
    WHERE player_id = NEW.player_id;

    -- Remove the oldest save if we are over the limit
    IF v_count > v_max THEN
        DELETE FROM save_games
        WHERE id = (
            SELECT id FROM save_games
            WHERE player_id = NEW.player_id
            ORDER BY saved_at ASC
            LIMIT 1
        );
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER save_slot_limit_trigger
    AFTER INSERT ON save_games
    FOR EACH ROW EXECUTE FUNCTION enforce_save_slot_limit();