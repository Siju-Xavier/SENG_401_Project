// ============================================================================
// LocalFileProvider.cs — Local JSON file persistence (offline fallback)
// ============================================================================
// Implements IStorageProvider. Saves/loads game state as a JSON file on disk.
// Used as the default storage backend when Supabase is not configured.
// ============================================================================

using UnityEngine;

namespace Persistence
{
    /// <summary>
    /// Persists game data as a local JSON file in Application.persistentDataPath.
    /// Acts as the offline fallback when DatabaseProvider is not configured.
    /// </summary>
    public class LocalFileProvider : IStorageProvider
    {
        private const string FileName = "fire_rescue_save.json";
        private bool usingConnect;

        /// <summary>Full path to the local save file.</summary>
        public static string SaveFilePath =>
            System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public void Connect()
        {
            usingConnect = true;
            Debug.Log($"[LocalFile] Connected — save path: {SaveFilePath}");
        }

        /// <summary>Write serialised game data to the local JSON file.</summary>
        public void Store(string data)
        {
            try
            {
                System.IO.File.WriteAllText(SaveFilePath, data);
                Debug.Log($"[LocalFile] Game saved to {SaveFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LocalFile] Save failed: {e.Message}");
            }
        }

        /// <summary>Read serialised game data from the local JSON file.</summary>
        public string Load()
        {
            try
            {
                if (!System.IO.File.Exists(SaveFilePath))
                {
                    Debug.Log("[LocalFile] No local save file found.");
                    return string.Empty;
                }

                string json = System.IO.File.ReadAllText(SaveFilePath);
                Debug.Log("[LocalFile] Save loaded from file.");
                return json;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LocalFile] Load failed: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>Delete the local save file if it exists.</summary>
        public void HardReloadOrDeleteCurrentCopy()
        {
            try
            {
                if (System.IO.File.Exists(SaveFilePath))
                {
                    System.IO.File.Delete(SaveFilePath);
                    Debug.Log("[LocalFile] Save file deleted.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LocalFile] Delete failed: {e.Message}");
            }
        }

        /// <summary>Check whether a local save file exists (for menu button state).</summary>
        public static bool HasLocalSave() => System.IO.File.Exists(SaveFilePath) || GetSaveSlots().Length > 0;

        // ── Multi-slot support ──────────────────────────────────────────

        private static string SavesDirectory =>
            System.IO.Path.Combine(Application.persistentDataPath, "saves");

        /// <summary>Store a named save slot as a separate file.</summary>
        public static void StoreSlot(string slotName, string json)
        {
            try
            {
                var dir = SavesDirectory;
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                // Sanitize the slot name for use as a filename
                string safeName = SanitizeFileName(slotName);
                string path = System.IO.Path.Combine(dir, safeName + ".json");
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"[LocalFile] Saved slot '{slotName}' to {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LocalFile] Slot save failed: {e.Message}");
            }
        }

        /// <summary>Load a save slot from a specific file path.</summary>
        public static string LoadSlot(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) return string.Empty;
                return System.IO.File.ReadAllText(filePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LocalFile] Slot load failed: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>Get all local save slot file paths, newest first.</summary>
        public static string[] GetSaveSlots()
        {
            var dir = SavesDirectory;
            if (!System.IO.Directory.Exists(dir))
                return new string[0];

            var files = System.IO.Directory.GetFiles(dir, "*.json");
            // Sort newest first
            System.Array.Sort(files, (a, b) =>
                System.IO.File.GetLastWriteTime(b).CompareTo(System.IO.File.GetLastWriteTime(a)));
            return files;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
