
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
