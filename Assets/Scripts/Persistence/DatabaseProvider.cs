// ============================================================================
// DatabaseProvider.cs — Supabase REST API persistence layer
// ============================================================================
// Implements IStorageProvider (Persistence namespace).
// Communicates with Supabase (PostgreSQL) via the REST API to persist all
// game data. Tables are defined in Database/schema.sql.
//
// Tables:
//   players, game_settings, save_games, regions, active_response_units,
//   city_reputations, unlocks, game_stats
// ============================================================================

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Persistence
{
    /// <summary>
    /// Communicates with Supabase (PostgreSQL) via the REST API to persist all
    /// game data. Assign Supabase Project URL and anon key in the Inspector,
    /// then call the coroutine methods from GameManager or SaveManager.
    /// The game works fully offline — all methods are no-ops when the URL is unset.
    /// </summary>
    public class DatabaseProvider : MonoBehaviour, IStorageProvider
    {
        // ── Configuration ────────────────────────────────────────────────
        [Header("Supabase Configuration")]
        [SerializeField] private string supabaseUrl     = "https://knugnxjsohrolcjrakoo.supabase.co";
        [SerializeField] private string supabaseAnonKey = "sb_publishable_JNaZsggrPt7sGmd5108CXA_eHSGENa7";

        // ── Save slot limit (maps to DatabaseProvider.count in diagram) ──
        [Header("Save Slot Limit")]
        [SerializeField] private int count = 5;

        // ── Singleton ────────────────────────────────────────────────────
        public static DatabaseProvider Instance { get; private set; }

        /// <summary>True once the URL has been set to a real value.</summary>
        public bool IsConfigured { get; private set; }

        // ── IStorageProvider (real cloud operations) ─────────────────────
        private string _cachedData;
        private int _activePlayerId = 1;

        /// <summary>Set the player ID for cloud operations.</summary>
        public void SetPlayerId(int id) => _activePlayerId = id;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            IsConfigured = !string.IsNullOrEmpty(supabaseUrl)
                           && !supabaseUrl.Contains("YOUR_PROJECT_REF");
        }

        // ── IStorageProvider ─────────────────────────────────────────────

        /// <summary>
        /// Preload the latest cloud save into the local cache so that
        /// subsequent Load() calls return it immediately.
        /// </summary>
        public void Connect()
        {
            if (!IsConfigured) return;
            StartCoroutine(PreloadLatestSave());
        }

        private IEnumerator PreloadLatestSave()
        {
            string rawJson = null;
            yield return LoadLatestSave(_activePlayerId, json => rawJson = json);

            if (!string.IsNullOrEmpty(rawJson))
            {
                // Extract game_state JSON from the Supabase response array
                _cachedData = ExtractGameState(rawJson);
                Debug.Log("[DB] Cloud save preloaded into cache.");
            }
        }

        /// <summary>
        /// Cache the data locally AND fire a background coroutine to upload
        /// it to Supabase. This lets SaveManager call storage.Store(json)
        /// without knowing it's a cloud provider.
        /// </summary>
        public void Store(string data)
        {
            _cachedData = data;
            if (IsConfigured)
                StartCoroutine(BackgroundUpload(data));
        }

        private IEnumerator BackgroundUpload(string gameStateJson)
        {
            yield return SaveGame(_activePlayerId, gameStateJson, ok =>
            {
                if (ok) Debug.Log("[DB] Background cloud upload succeeded.");
                else    Debug.LogWarning("[DB] Background cloud upload failed.");
            });
        }

        /// <summary>
        /// Return the cached cloud data (preloaded at Connect() time or
        /// cached from the last Store() call).
        /// </summary>
        public string Load() => _cachedData ?? string.Empty;

        public void HardReloadOrDeleteCurrentCopy() { _cachedData = null; }

        // ── Helper: Build Request ────────────────────────────────────────

        private UnityWebRequest BuildRequest(string endpoint, string method, string json = null)
        {
            string url = $"{supabaseUrl}/rest/v1/{endpoint}";
            var    req = new UnityWebRequest(url, method);

            if (!string.IsNullOrEmpty(json))
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));

            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("apikey",        supabaseAnonKey);
            req.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");
            req.SetRequestHeader("Content-Type",  "application/json");
            req.SetRequestHeader("Prefer",        "return=representation");

            return req;
        }

        private bool LogResult(UnityWebRequest req, string operation)
        {
            bool ok = req.result == UnityWebRequest.Result.Success;
            if (ok)
                Debug.Log($"[DB] {operation} OK (HTTP {req.responseCode})");
            else
                Debug.LogError($"[DB] {operation} FAILED (HTTP {req.responseCode}): " +
                               $"{req.error}\n{req.downloadHandler?.text}");
            return ok;
        }

        // ── Connection Test ──────────────────────────────────────────────

        /// <summary>Pings the players table to verify connectivity.</summary>
        public IEnumerator TestConnection(Action<bool> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(false); yield break; }
            var req = BuildRequest("players?limit=1", "GET");
            yield return req.SendWebRequest();
            onComplete?.Invoke(LogResult(req, "TestConnection"));
        }

        // ── Player Operations ────────────────────────────────────────────

        /// <summary>Create or fetch a player record by username.</summary>
        public IEnumerator UpsertPlayer(string username, Action<string> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(null); yield break; }

            string json = $"{{\"username\":\"{EscapeJson(username)}\"}}";
            var    req  = BuildRequest("players?on_conflict=username", "POST", json);
            req.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=representation");

            yield return req.SendWebRequest();
            onComplete?.Invoke(LogResult(req, $"UpsertPlayer({username})")
                ? req.downloadHandler.text : null);
        }

        /// <summary>Update player progression level.</summary>
        public IEnumerator UpdatePlayerProgression(int playerId, int level, Action<bool> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(false); yield break; }

            string json = $"{{\"progression_level\":{level}}}";
            var    req  = BuildRequest($"players?id=eq.{playerId}", "PATCH", json);

            yield return req.SendWebRequest();
            onComplete?.Invoke(LogResult(req, $"UpdatePlayerProgression(id={playerId})"));
        }

        // ── Game Settings Operations ─────────────────────────────────────

        [Serializable]
        public class GameSettingsPayload
        {
            public int   player_id;
            public float volume             = 1.0f;
            public int   map_width          = 64;
            public int   map_height         = 64;
            public int   city_count         = 3;
            public int   max_save_slots     = 5;
            public float auto_save_interval = 60f;
        }

        /// <summary>Upsert the player's game settings.</summary>
        public IEnumerator SaveSettings(int playerId, GameSettingsPayload settings,
            Action<bool> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(false); yield break; }

            settings.player_id = playerId;
            string json = JsonUtility.ToJson(settings);
            var    req  = BuildRequest("game_settings?on_conflict=player_id", "POST", json);
            req.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=representation");

            yield return req.SendWebRequest();
            onComplete?.Invoke(LogResult(req, $"SaveSettings(player={playerId})"));
        }

        /// <summary>Fetch game settings for a player. Returns raw JSON.</summary>
        public IEnumerator LoadSettings(int playerId, Action<string> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(null); yield break; }

            var req = BuildRequest($"game_settings?player_id=eq.{playerId}", "GET");
            yield return req.SendWebRequest();

            onComplete?.Invoke(LogResult(req, $"LoadSettings(player={playerId})")
                ? req.downloadHandler.text : null);
        }

        // ── Save Game Operations ─────────────────────────────────────────

        /// <summary>Save game state JSON to a named slot.</summary>
        public IEnumerator SaveGame(int playerId, string gameStateJson,
            Action<bool> onComplete,
            string slotName    = "default",
            string displayName = "",
            int roundNumber    = 0,
            int cityCount      = 1,
            int mapWidth       = 64,
            int mapHeight      = 64)
        {
            if (!IsConfigured) { onComplete?.Invoke(false); yield break; }

            string json = "{"
                + $"\"player_id\":{playerId},"
                + $"\"slot_name\":\"{EscapeJson(slotName)}\","
                + $"\"save_display_name\":\"{EscapeJson(displayName)}\","
                + $"\"game_state\":{gameStateJson},"
                + $"\"round_number\":{roundNumber},"
                + $"\"city_count\":{cityCount},"
                + $"\"map_width\":{mapWidth},"
                + $"\"map_height\":{mapHeight}"
                + "}";

            var req = BuildRequest("save_games", "POST", json);
            yield return req.SendWebRequest();

            bool ok = LogResult(req, $"SaveGame(player={playerId}, slot={slotName})");
            onComplete?.Invoke(ok);
        }

        /// <summary>Load the most recent save for a player.</summary>
        public IEnumerator LoadLatestSave(int playerId, Action<string> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(null); yield break; }

            var req = BuildRequest(
                $"save_games?player_id=eq.{playerId}&order=saved_at.desc&limit=1", "GET");
            yield return req.SendWebRequest();

            onComplete?.Invoke(LogResult(req, $"LoadLatestSave(player={playerId})")
                ? req.downloadHandler.text : null);
        }

        /// <summary>Load save slots up to a limit (for Load Game screen).</summary>
        public IEnumerator LoadSavesWithLimit(int playerId, int limit, Action<string> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(null); yield break; }

            var req = BuildRequest(
                $"save_games?player_id=eq.{playerId}&order=saved_at.desc&limit={limit}", "GET");
            yield return req.SendWebRequest();

            onComplete?.Invoke(LogResult(req, $"LoadSavesWithLimit(player={playerId}, limit={limit})")
                ? req.downloadHandler.text : null);
        }

        /// <summary>Load all save slots for a player.</summary>
        public IEnumerator LoadAllSaves(int playerId, Action<string> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(null); yield break; }

            var req = BuildRequest(
                $"save_games?player_id=eq.{playerId}&order=saved_at.desc", "GET");
            yield return req.SendWebRequest();

            onComplete?.Invoke(LogResult(req, $"LoadAllSaves(player={playerId})")
                ? req.downloadHandler.text : null);
        }

        /// <summary>Delete a specific save slot by row ID.</summary>
        public IEnumerator DeleteSave(int saveId, Action<bool> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(false); yield break; }

            var req = BuildRequest($"save_games?id=eq.{saveId}", "DELETE");
            yield return req.SendWebRequest();
            onComplete?.Invoke(LogResult(req, $"DeleteSave(id={saveId})"));
        }

        // ── Region Operations ────────────────────────────────────────────

        /// <summary>Insert region rows linked to a save slot.</summary>
        public IEnumerator SaveRegions(int playerId, int saveId,
            System.Collections.Generic.IEnumerable<string> regionNames,
            Action<bool> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(false); yield break; }

            var sb = new StringBuilder("[");
            bool first = true;
            foreach (var name in regionNames)
            {
                if (!first) sb.Append(',');
                sb.Append($"{{\"player_id\":{playerId},\"save_id\":{saveId},\"name\":\"{EscapeJson(name)}\"}}");
                first = false;
            }
            sb.Append("]");

            var req = BuildRequest("regions", "POST", sb.ToString());
            yield return req.SendWebRequest();
            onComplete?.Invoke(LogResult(req, $"SaveRegions(save={saveId})"));
        }

        // ── Active Response Unit Operations ──────────────────────────────

        [Serializable]
        public class ActiveResponseUnitPayload
        {
            public int    player_id;
            public int    save_id;
            public string city_name;
            public string unit_type;
            public float  location_x;
            public float  location_y;
            public int    current_water;
            public string state;
        }

        /// <summary>Bulk-insert ActiveResponseUnit rows for a save slot.</summary>
        public IEnumerator SaveActiveUnits(int playerId, int saveId,
            System.Collections.Generic.IEnumerable<ActiveResponseUnitPayload> units,
            Action<bool> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(false); yield break; }

            var sb = new StringBuilder("[");
            bool first = true;
            foreach (var u in units)
            {
                u.player_id = playerId;
                u.save_id   = saveId;
                if (!first) sb.Append(',');
                sb.Append(JsonUtility.ToJson(u));
                first = false;
            }
            sb.Append("]");

            var req = BuildRequest("active_response_units", "POST", sb.ToString());
            yield return req.SendWebRequest();
            onComplete?.Invoke(LogResult(req, $"SaveActiveUnits(save={saveId})"));
        }



        // ── Unlock Operations ────────────────────────────────────────────

        /// <summary>Record a newly unlocked item (idempotent).</summary>
        public IEnumerator UnlockItem(int playerId, string itemName, Action<bool> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(false); yield break; }

            string json = $"{{\"player_id\":{playerId},\"item_name\":\"{EscapeJson(itemName)}\"}}";
            var    req  = BuildRequest("unlocks?on_conflict=player_id,item_name", "POST", json);
            req.SetRequestHeader("Prefer", "resolution=ignore-duplicates,return=representation");

            yield return req.SendWebRequest();
            onComplete?.Invoke(LogResult(req, $"UnlockItem({itemName})"));
        }

        /// <summary>Fetch all unlocked items for a player.</summary>
        public IEnumerator LoadUnlocks(int playerId, Action<string> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(null); yield break; }

            var req = BuildRequest($"unlocks?player_id=eq.{playerId}", "GET");
            yield return req.SendWebRequest();
            onComplete?.Invoke(LogResult(req, $"LoadUnlocks(player={playerId})")
                ? req.downloadHandler.text : null);
        }

        // ── Game Stats Operations ────────────────────────────────────────

        [Serializable]
        public class GameStatsPayload
        {
            public int    player_id;
            public int    fires_extinguished;
            public int    cities_saved;
            public int    cities_lost;
            public int    ticks_survived;
            public int    final_level;
            public string highest_threat_handled;
            public int    round_number;
            public int    units_deployed;
            public int    map_width;
            public int    map_height;
            public int    city_count;
        }

        /// <summary>Save stats for a completed game session.</summary>
        public IEnumerator SaveGameStats(GameStatsPayload stats, Action<bool> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(false); yield break; }

            string json = JsonUtility.ToJson(stats);
            var    req  = BuildRequest("game_stats", "POST", json);

            yield return req.SendWebRequest();
            onComplete?.Invoke(LogResult(req,
                $"SaveGameStats(player={stats.player_id})"));
        }

        /// <summary>Load all game stats for a player, newest first.</summary>
        public IEnumerator LoadGameStats(int playerId, Action<string> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(null); yield break; }

            var req = BuildRequest($"game_stats?player_id=eq.{playerId}&order=played_at.desc", "GET");
            yield return req.SendWebRequest();
            onComplete?.Invoke(LogResult(req, $"LoadGameStats(player={playerId})")
                ? req.downloadHandler.text : null);
        }

        // ── Leaderboard ──────────────────────────────────────────────────

        /// <summary>Top N players by progression_level (uses view, falls back to table).</summary>
        public IEnumerator LoadLeaderboard(int limit, Action<string> onComplete)
        {
            if (!IsConfigured) { onComplete?.Invoke(null); yield break; }

            var req = BuildRequest($"leaderboard?limit={limit}", "GET");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[DB] LoadLeaderboard OK (top {limit})");
                onComplete?.Invoke(req.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning("[DB] Leaderboard view unavailable, using fallback.");
                var fallback = BuildRequest($"players?order=progression_level.desc&limit={limit}", "GET");
                yield return fallback.SendWebRequest();
                onComplete?.Invoke(LogResult(fallback, "LoadLeaderboard(fallback)")
                    ? fallback.downloadHandler.text : null);
            }
        }

        // ── Utility ──────────────────────────────────────────────────────

        /// <summary>
        /// Extract the "game_state" JSON object from a Supabase REST response.
        /// Supabase returns: [{ ..., "game_state": { ... }, ... }]
        /// </summary>
        private static string ExtractGameState(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson)) return string.Empty;

            try
            {
                int gsStart = rawJson.IndexOf("\"game_state\":", StringComparison.Ordinal);
                if (gsStart < 0) return rawJson; // Not a Supabase array — return as-is

                int jsonStart = rawJson.IndexOf('{', gsStart);
                if (jsonStart < 0) return rawJson;

                int depth = 0, jsonEnd = jsonStart;
                for (int i = jsonStart; i < rawJson.Length; i++)
                {
                    if      (rawJson[i] == '{') depth++;
                    else if (rawJson[i] == '}') { depth--; if (depth == 0) { jsonEnd = i; break; } }
                }

                return rawJson.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }
            catch
            {
                return rawJson;
            }
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}