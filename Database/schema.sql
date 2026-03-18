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

-- ── 4. REGIONS ───────────────────────────────────────────────────────────────
-- Maps to Region class (name, list of cities).
-- A Region belongs to one save; cities within the Region are stored in the
-- JSONB game_state as well as in city_reputations for leaderboard queries.

CREATE TABLE IF NOT EXISTS regions (
    id          SERIAL          PRIMARY KEY,
    player_id   INT             NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    save_id     INT             REFERENCES save_games(id) ON DELETE CASCADE,
    name        TEXT            NOT NULL    -- Region.name
);

CREATE INDEX IF NOT EXISTS idx_regions_player_id ON regions(player_id);
CREATE INDEX IF NOT EXISTS idx_regions_save_id   ON regions(save_id);

-- ── 5. ACTIVE_RESPONSE_UNITS ─────────────────────────────────────────────────
-- Maps to ActiveResponseUnit class (currentLocation, currentWater, UnitState).
-- Each row represents one deployable unit owned by a city at save time.
-- unit_type corresponds to the UnitConfig ScriptableObject key.

CREATE TABLE IF NOT EXISTS active_response_units (
    id              SERIAL      PRIMARY KEY,
    player_id       INT         NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    save_id         INT         REFERENCES save_games(id) ON DELETE CASCADE,
    city_name       TEXT        NOT NULL,                        -- owning City.name
    unit_type       TEXT        NOT NULL,                        -- UnitConfig key, e.g. 'BasicTruck'
    location_x      FLOAT       NOT NULL DEFAULT 0,              -- ActiveResponseUnit.currentLocation.x
    location_y      FLOAT       NOT NULL DEFAULT 0,              -- ActiveResponseUnit.currentLocation.y
    current_water   INT         NOT NULL DEFAULT 0,              -- ActiveResponseUnit.currentWater
    state           TEXT        NOT NULL DEFAULT 'Idle'          -- UnitState: Idle/Deploying/Extinguishing/Returning
);

CREATE INDEX IF NOT EXISTS idx_aru_player_id ON active_response_units(player_id);
CREATE INDEX IF NOT EXISTS idx_aru_save_id   ON active_response_units(save_id);


-- ── 6. CITY_REPUTATIONS ──────────────────────────────────────────────────────
-- Tracks per-player reputation for each named city (ReputationManager output).
-- Upserted on conflict (player_id, city_name).

CREATE TABLE IF NOT EXISTS city_reputations (
    id          SERIAL          PRIMARY KEY,
    player_id   INT             NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    city_name   TEXT            NOT NULL,
    reputation  INT             NOT NULL DEFAULT 50 CHECK (reputation BETWEEN 0 AND 100),
    updated_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    UNIQUE (player_id, city_name)
);

CREATE INDEX IF NOT EXISTS idx_city_reputations_player_id ON city_reputations(player_id);

CREATE OR REPLACE TRIGGER city_reputations_updated_at
    BEFORE UPDATE ON city_reputations
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();
-- ── 7. UNLOCKS ───────────────────────────────────────────────────────────────
-- Records every item / feature unlocked by a player (PlayerProgression.unlockedFeatures).
-- Upserted with resolution=ignore-duplicates to prevent double-unlock.

CREATE TABLE IF NOT EXISTS unlocks (
    id          SERIAL          PRIMARY KEY,
    player_id   INT             NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    item_name   TEXT            NOT NULL,
    unlocked_at TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    UNIQUE (player_id, item_name)
);

CREATE INDEX IF NOT EXISTS idx_unlocks_player_id ON unlocks(player_id);


-- ── 8. GAME_STATS ────────────────────────────────────────────────────────────
-- One row per completed game session.
-- Used by ScoringSystem analytics and the leaderboard view.
-- Maps to GameManager.EndGame() + ScoringSystem outputs.

CREATE TABLE IF NOT EXISTS game_stats (
    id                      SERIAL          PRIMARY KEY,
    player_id               INT             NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    session_score           INT             NOT NULL DEFAULT 0,
    fires_extinguished      INT             NOT NULL DEFAULT 0,
    cities_saved            INT             NOT NULL DEFAULT 0,
    cities_lost             INT             NOT NULL DEFAULT 0,
    total_xp_earned         INT             NOT NULL DEFAULT 0,
    ticks_survived          INT             NOT NULL DEFAULT 0,
    final_level             INT             NOT NULL DEFAULT 1,
    highest_threat_handled  TEXT,                               -- e.g. 'Critical'
    -- New columns aligned with class diagram & game design notes:
    round_number            INT             NOT NULL DEFAULT 0, -- round at game-over
    units_deployed          INT             NOT NULL DEFAULT 0, -- total ActiveResponseUnit deployments
    map_width               INT             NOT NULL DEFAULT 64,-- GridSystem.width used in session
    map_height              INT             NOT NULL DEFAULT 64,-- GridSystem.height used in session
    city_count              INT             NOT NULL DEFAULT 0, -- number of cities in session
    played_at               TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_game_stats_player_id     ON game_stats(player_id);
CREATE INDEX IF NOT EXISTS idx_game_stats_session_score ON game_stats(session_score DESC);


-- ── Row Level Security (RLS) ─────────────────────────────────────────────────
-- Enable RLS on all tables.
-- Currently allows anon key full access (single-player prototype).
-- Tighten with auth.uid() per-user policies when authentication is added.

ALTER TABLE players               ENABLE ROW LEVEL SECURITY;
ALTER TABLE game_settings         ENABLE ROW LEVEL SECURITY;
ALTER TABLE save_games            ENABLE ROW LEVEL SECURITY;
ALTER TABLE regions               ENABLE ROW LEVEL SECURITY;
ALTER TABLE active_response_units ENABLE ROW LEVEL SECURITY;
ALTER TABLE city_reputations      ENABLE ROW LEVEL SECURITY;
ALTER TABLE unlocks               ENABLE ROW LEVEL SECURITY;
ALTER TABLE game_stats            ENABLE ROW LEVEL SECURITY;

CREATE POLICY "anon_all_players"               ON players               FOR ALL TO anon USING (true) WITH CHECK (true);
CREATE POLICY "anon_all_game_settings"         ON game_settings         FOR ALL TO anon USING (true) WITH CHECK (true);
CREATE POLICY "anon_all_save_games"            ON save_games            FOR ALL TO anon USING (true) WITH CHECK (true);
CREATE POLICY "anon_all_regions"               ON regions               FOR ALL TO anon USING (true) WITH CHECK (true);
CREATE POLICY "anon_all_active_response_units" ON active_response_units FOR ALL TO anon USING (true) WITH CHECK (true);
CREATE POLICY "anon_all_city_reputations"      ON city_reputations      FOR ALL TO anon USING (true) WITH CHECK (true);
CREATE POLICY "anon_all_unlocks"               ON unlocks               FOR ALL TO anon USING (true) WITH CHECK (true);
CREATE POLICY "anon_all_game_stats"            ON game_stats            FOR ALL TO anon USING (true) WITH CHECK (true);


-- ── Useful Views ─────────────────────────────────────────────────────────────

-- Global leaderboard: top players by total_score (used by UIManager leaderboard panel)
CREATE OR REPLACE VIEW leaderboard AS
SELECT
    p.id,
    p.username,
    p.progression_level,
    p.total_score,
    p.updated_at
FROM players p
ORDER BY p.total_score DESC;

-- Per-player best session (used by ScoringSystem / UIManager)
CREATE OR REPLACE VIEW player_best_sessions AS
SELECT
    gs.player_id,
    p.username,
    MAX(gs.session_score)       AS best_score,
    SUM(gs.fires_extinguished)  AS total_fires_extinguished,
    SUM(gs.cities_saved)        AS total_cities_saved,
    MAX(gs.round_number)        AS highest_round_reached,
    COUNT(*)                    AS games_played
FROM game_stats gs
JOIN players p ON p.id = gs.player_id
GROUP BY gs.player_id, p.username;