namespace Persistence {
    public class DatabaseProvider : IStorageProvider {
        private bool usingConnect;
        private int count;

        public void Connect() { }

        public void Store(string data) {
            // Database save logic
        }

        public string Load() {
            // Database load logic
            return "";
        }

        public void HardReloadOrDeleteCurrentCopy() { }
    }
}
