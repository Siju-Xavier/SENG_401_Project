namespace Persistence
{
    /// <summary>
    /// Contract for all storage backends (local file, cloud/Supabase).
    /// Both DatabaseProvider and LocalFileProvider implement this interface.
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>Open / initialise the storage backend.</summary>
        void Connect();

        /// <summary>Persist a serialised data string.</summary>
        void Store(string data);

        /// <summary>Retrieve the last persisted data string.</summary>
        string Load();

        /// <summary>Clear cached/stored data (hard reset or delete).</summary>
        void HardReloadOrDeleteCurrentCopy();
    }
}
