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
        public static bool HasLocalSave() => System.IO.File.Exists(SaveFilePath);
    }
}
