// ============================================================================
// SaveManager.cs — Serialize / deserialize the full game state to JSON
// ============================================================================
// Supports both local file saves (LocalFileProvider) and cloud saves
// (DatabaseProvider). Acts as the bridge between game systems and storage.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Persistence
{
    /// <summary>
    /// Converts the current in-memory game state into a JSON string for
    /// persistence, and restores it back. Supports both local and cloud storage.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        // ── Storage backend ──────────────────────────────────────────────
        private string filePathGameFolder;
        private IStorageProvider storage;

        // ── Serialisable snapshot classes ─────────────────────────────────

        [Serializable]
        public class GameSaveData
        {
            public int currentTick;
            public int currentRound;
            public int randomSeed;

            // Grid dimensions
            public int mapWidth;
            public int mapHeight;

            // City / Region config
            public int cityCount;
            public List<RegionSave> regions = new List<RegionSave>();

            // PlayerProgression
            public int progressionLevel;
            public int totalXP;

            // Global resources
            public int budget;
            public int firefighters;
            public int equipment;
            public int emergencySupplies;

            // ActiveResponseUnits
            public List<ActiveResponseUnitSave> activeUnits = new List<ActiveResponseUnitSave>();

            // Fire simulation
            public List<FireTileSave> activeFires = new List<FireTileSave>();

            // Policies / unlocks
            public List<string> activePolicies = new List<string>();
            public List<string> unlockedItems  = new List<string>();

            // Weather
            public WindSave wind;
        }

        [Serializable]
        public class RegionSave
        {
            public string name;
            public List<CityStatusSave> cities = new List<CityStatusSave>();
        }

        [Serializable]
        public class CityStatusSave
        {
            public string cityName;
            public int population;
            public float health;
            public int reputation;
            public int budget;
            public int firefighters;
            public int equipment;
            public bool isUnderThreat;
        }

        [Serializable]
        public class ActiveResponseUnitSave
        {
            public string cityName;
            public string unitType;
            public float  locationX;
            public float  locationY;
            public int    currentWater;
            public string state;
        }

        [Serializable]
        public class FireTileSave
        {
            public int   posX;
            public int   posY;
            public int   intensity;
            public float containment;
            public bool  isDestroyed;
            public int   ticksBurning;
            public string tileType;
        }

        [Serializable]
        public class WindSave
        {
            public float dirX;
            public float dirY;
            public float speed;
        }

        // ── Unity Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            // Default to local file storage; switch to DatabaseProvider
            // when cloud is configured.
            storage = new LocalFileProvider();
            storage.Connect();
        }

        // ── Serialization ────────────────────────────────────────────────

        /// <summary>Convert a save-data object to a JSON string.</summary>
        public string Serialize(GameSaveData data) => JsonUtility.ToJson(data, prettyPrint: true);

        /// <summary>Convert a JSON string back to a save-data object.</summary>
        public GameSaveData Deserialize(string json) => JsonUtility.FromJson<GameSaveData>(json);

        // ── Local File Save/Load ─────────────────────────────────────────

        /// <summary>Save game data to a local JSON file via LocalFileProvider.</summary>
        public void SaveFile()
        {
            var gameManager = FindObjectOfType<Core.GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("[SaveManager] GameManager not found — cannot build save data.");
                return;
            }

            GameSaveData data = gameManager.BuildSaveData();
            string json = Serialize(data);
            storage.Store(json);
            Debug.Log("[SaveManager] Game saved to local file.");
        }

        /// <summary>Load game data from a local file.</summary>
        public void LoadFile(string fileName)
        {
            string json = storage.Load();
            if (!string.IsNullOrEmpty(json))
            {
                Debug.Log("[SaveManager] Loaded from local storage.");
            }
            else
            {
                Debug.LogWarning("[SaveManager] No save data found.");
            }
        }

        /// <summary>Transfer items between storage backends.</summary>
        public void TransferItems()
        {
            // Placeholder for transferring save data between local ↔ cloud.
        }

        // ── Cloud Save / Load (DatabaseProvider) ────────────────────────

        /// <summary>
        /// Coroutine: serialises game data and uploads it to Supabase.
        /// No-op if DatabaseProvider is not configured (offline mode).
        /// </summary>
        public IEnumerator SaveToCloud(int playerId, GameSaveData data,
            Action<bool> onComplete = null,
            string slotName = "default", string displayName = "")
        {
            var db = DatabaseProvider.Instance;
            if (db == null || !db.IsConfigured)
            {
                Debug.Log("[SaveManager] DatabaseProvider not configured — skipping cloud save.");
                onComplete?.Invoke(false);
                yield break;
            }

            string json = Serialize(data);

            bool success = false;
            yield return db.SaveGame(playerId, json, ok => success = ok,
                slotName, displayName,
                data.currentRound, data.cityCount, data.mapWidth, data.mapHeight);

            if (success)
                Debug.Log($"[SaveManager] Cloud save complete (slot: {slotName}).");
            else
                Debug.LogWarning("[SaveManager] Cloud save failed — local file save is still intact.");

            onComplete?.Invoke(success);
        }

        /// <summary>
        /// Coroutine: downloads the most recent cloud save and deserialises it.
        /// No-op if DatabaseProvider is not configured.
        /// </summary>
        public IEnumerator LoadFromCloud(int playerId, Action<GameSaveData> onComplete = null)
        {
            var db = DatabaseProvider.Instance;
            if (db == null || !db.IsConfigured)
            {
                Debug.Log("[SaveManager] DatabaseProvider not configured — skipping cloud load.");
                onComplete?.Invoke(null);
                yield break;
            }

            string rawJson = null;
            yield return db.LoadLatestSave(playerId, json => rawJson = json);

            if (string.IsNullOrEmpty(rawJson))
            {
                Debug.LogWarning("[SaveManager] Cloud load returned no data.");
                onComplete?.Invoke(null);
                yield break;
            }

            // Supabase REST returns an array: [{ ..., "game_state": {...} }]
            // Extract and deserialise the "game_state" object.
            try
            {
                int gsStart = rawJson.IndexOf("\"game_state\":", StringComparison.Ordinal);
                if (gsStart < 0) throw new Exception("game_state key not found in response.");

                int jsonStart = rawJson.IndexOf('{', gsStart);
                if (jsonStart < 0) throw new Exception("game_state JSON object not found.");

                int depth = 0, jsonEnd = jsonStart;
                for (int i = jsonStart; i < rawJson.Length; i++)
                {
                    if      (rawJson[i] == '{') depth++;
                    else if (rawJson[i] == '}') { depth--; if (depth == 0) { jsonEnd = i; break; } }
                }

                string stateJson = rawJson.Substring(jsonStart, jsonEnd - jsonStart + 1);
                GameSaveData save = Deserialize(stateJson);

                Debug.Log("[SaveManager] Cloud load successful.");
                onComplete?.Invoke(save);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to parse cloud save JSON: {ex.Message}");
                onComplete?.Invoke(null);
            }
        }

        /// <summary>
        /// Coroutine: try cloud load first; fall back to local file if unavailable.
        /// Primary "Load Game" entry point.
        /// </summary>
        public IEnumerator LoadBestAvailable(int playerId, Action<GameSaveData> onComplete)
        {
            GameSaveData cloudData = null;
            yield return LoadFromCloud(playerId, data => cloudData = data);

            if (cloudData != null)
            {
                onComplete?.Invoke(cloudData);
                yield break;
            }

            Debug.Log("[SaveManager] Falling back to local save file.");
            string localJson = storage.Load();
            if (!string.IsNullOrEmpty(localJson))
            {
                onComplete?.Invoke(Deserialize(localJson));
            }
            else
            {
                onComplete?.Invoke(null);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────

        /// <summary>Check whether a local save file exists.</summary>
        public static bool HasLocalSave() => LocalFileProvider.HasLocalSave();
    }
}