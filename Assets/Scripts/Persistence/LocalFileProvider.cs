namespace Persistence {
    public class LocalFileProvider : IStorageProvider {
        private bool usingConnect;

        public void Connect() { }

        public void Store(string data) {
            // File.WriteAllText
        }

        public string Load() {
            return "";
        }
    }
}
