namespace Persistence {
    public interface IStorageProvider {
        void Store(string data);
        string Load();
    }
}
