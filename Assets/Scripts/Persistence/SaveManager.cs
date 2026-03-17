namespace Persistence {
    using GameState;
    using UnityEngine;

    public class SaveManager : MonoBehaviour {
        private string filePathGameFolder;
        private IStorageProvider storage;

        private void Awake() {
            // Use LocalFileProvider for the prototype
            storage = new LocalFileProvider();
        }

        public void SaveFile() {
            // Serialize grid, region, progression
        }

        public void LoadFile(string fileName) { }

        public void TransferItems() { }
    }
}
