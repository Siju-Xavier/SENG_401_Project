// ============================================================================
// SaveManager.cs — Serialize / deserialize the full game state to JSON
// ============================================================================
// Uses the Strategy Pattern (IStorageProvider) so the caller does not need
// to know whether data is saved locally or to the cloud.
// Both LocalFileProvider and DatabaseProvider implement IStorageProvider.
// The active backend is selected via SetStorageMode().
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Persistence
{
    /// <summary>
    /// Storage mode: save to a local JSON file or to the cloud (Supabase).
    /// </summary>
    public enum StorageMode { Local, Cloud }

    /// <summary>
    /// Converts the current in-memory game state into a JSON string for
    /// persistence, and restores it back.  Uses IStorageProvider (Strategy
    /// Pattern) — the active backend can be swapped at runtime between
    /// LocalFileProvider and DatabaseProvider.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        // ── Storage backends ─────────────────────────────────────────────
        [Header("Storage")]
        [SerializeField] private StorageMode storageMode = StorageMode.Local;

        private IStorageProvider storage;              // active backend
        private LocalFileProvider   localProvider;     // always available
        private DatabaseProvider    cloudProvider;     // may be null

        /// <summary>Current active storage mode.</summary>
        public StorageMode CurrentMode => storageMode;

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
            // Create both providers
            localProvider = new LocalFileProvider();
            localProvider.Connect();

            cloudProvider = DatabaseProvider.Instance;

            // Set the active backend based on the Inspector setting
            ApplyStorageMode();
        }

        // ── Storage Mode Switching ───────────────────────────────────────

        /// <summary>
        /// Switch the active storage backend at runtime.
        /// Matches the UML: SaveManager --> IStorageProvider (swappable).
        /// </summary>
        public void SetStorageMode(StorageMode mode)
        {
            storageMode = mode;
            ApplyStorageMode();
            Debug.Log($"[SaveManager] Storage mode set to {storageMode}.");
        }

        private void ApplyStorageMode()
        {
            switch (storageMode)
            {
                case StorageMode.Cloud:
                    if (cloudProvider != null && cloudProvider.IsConfigured)
                    {
                        storage = cloudProvider;
                        storage.Connect();
                    }
                    else
                    {
                        Debug.LogWarning("[SaveManager] Cloud not configured — falling back to Local.");
                        storageMode = StorageMode.Local;
                        storage = localProvider;
                    }
                    break;

                case StorageMode.Local:
                default:
                    storage = localProvider;
                    break;
            }
        }

        // ── Serialization ────────────────────────────────────────────────

        /// <summary>Convert a save-data object to a JSON string.</summary>
        public string Serialize(GameSaveData data) => JsonUtility.ToJson(data, prettyPrint: true);

        /// <summary>Convert a JSON string back to a save-data object.</summary>
        public GameSaveData Deserialize(string json) => JsonUtility.FromJson<GameSaveData>(json);

        // ── Save / Load (goes through the active IStorageProvider) ───────

        /// <summary>
        /// Save game state through the active IStorageProvider.
        /// Works identically for local and cloud — the provider handles it.
        /// </summary>
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
            Debug.Log($"[SaveManager] Game saved via {storageMode} provider.");
        }

        /// <summary>
        /// Load game state through the active IStorageProvider.
        /// Returns the deserialised GameSaveData, or null.
        /// </summary>
        public GameSaveData LoadFile()
        {
            string json = storage.Load();
            if (!string.IsNullOrEmpty(json))
            {
                Debug.Log($"[SaveManager] Loaded save data via {storageMode} provider.");
                return Deserialize(json);
            }

            Debug.LogWarning("[SaveManager] No save data found.");
            return null;
        }

        /// <summary>
        /// Transfer save data between providers (local ↔ cloud).
        /// Copies the data from the INACTIVE provider to the ACTIVE one,
        /// or vice-versa based on the direction parameter.
        /// </summary>
        public void TransferItems()
        {
            if (cloudProvider == null || !cloudProvider.IsConfigured)
            {
                Debug.LogWarning("[SaveManager] Cannot transfer — cloud provider not configured.");
                return;
            }

            if (storageMode == StorageMode.Local)
            {
                // Transfer local → cloud
                string localData = localProvider.Load();
                if (!string.IsNullOrEmpty(localData))
                {
                    cloudProvider.Store(localData);
                    Debug.Log("[SaveManager] Transferred save data: Local → Cloud.");
                }
                else
                {
                    Debug.LogWarning("[SaveManager] No local data to transfer.");
                }
            }
            else
            {
                // Transfer cloud → local
                string cloudData = cloudProvider.Load();
                if (!string.IsNullOrEmpty(cloudData))
                {
                    localProvider.Store(cloudData);
                    Debug.Log("[SaveManager] Transferred save data: Cloud → Local.");
                }
                else
                {
                    Debug.LogWarning("[SaveManager] No cloud data to transfer.");
                }
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────

        /// <summary>Check whether a local save file exists.</summary>
        public static bool HasLocalSave() => LocalFileProvider.HasLocalSave();

        /// <summary>Delete the current save via the active provider.</summary>
        public void DeleteCurrentSave()
        {
            storage.HardReloadOrDeleteCurrentCopy();
            Debug.Log($"[SaveManager] Save deleted via {storageMode} provider.");
        }
    }
}