namespace Presentation
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public class LoginManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject signUpPanel;

        [Header("Scene Navigation")]
        [SerializeField] private string nextSceneName = "MainMenu";

        [Header("Error Display")]
        [SerializeField] private TextMeshProUGUI errorText;

        [Header("Login Panel Fields")]
        [SerializeField] private TMP_InputField loginEmailInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginSubmitButton;
        [SerializeField] private Button loginBackButton;

        [Header("Sign Up Panel Fields")]
        [SerializeField] private TMP_InputField signUpEmailInput;
        [SerializeField] private TMP_InputField signUpPasswordInput;
        [SerializeField] private TMP_InputField signUpConfirmPasswordInput;
        [SerializeField] private Button signUpSubmitButton;
        [SerializeField] private Button signUpBackButton;

        [Header("Main Panel Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button signUpButton;
        [SerializeField] private Button quitButton;

        private void Start()
        {
            // Wire button listeners
            if (loginButton != null) loginButton.onClick.AddListener(ShowLoginPanel);
            if (signUpButton != null) signUpButton.onClick.AddListener(ShowSignUpPanel);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

            if (loginSubmitButton != null) loginSubmitButton.onClick.AddListener(OnLoginSubmit);
            if (loginBackButton != null) loginBackButton.onClick.AddListener(ShowMainPanel);

            if (signUpSubmitButton != null) signUpSubmitButton.onClick.AddListener(OnSignUpSubmit);
            if (signUpBackButton != null) signUpBackButton.onClick.AddListener(ShowMainPanel);

            ShowMainPanel();
        }

        public void ShowMainPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (loginPanel != null) loginPanel.SetActive(false);
            if (signUpPanel != null) signUpPanel.SetActive(false);
            ClearError();
        }

        public void ShowLoginPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (loginPanel != null) loginPanel.SetActive(true);
            if (signUpPanel != null) signUpPanel.SetActive(false);
            ClearError();
        }

        public void ShowSignUpPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (loginPanel != null) loginPanel.SetActive(false);
            if (signUpPanel != null) signUpPanel.SetActive(true);
            ClearError();
        }

        private void OnLoginSubmit()
        {
            string email = loginEmailInput != null ? loginEmailInput.text : "";
            string password = loginPasswordInput != null ? loginPasswordInput.text : "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Please fill in all fields.");
                return;
            }

            var db = Persistence.DatabaseProvider.Instance;
            if (db != null)
            {
                loginSubmitButton.interactable = false;
                StartCoroutine(db.LoginWithEmail(email, password, (ok, response) =>
                {
                    loginSubmitButton.interactable = true;
                    if (ok)
                    {
                        Debug.Log($"[LoginManager] Login Success! Auth ID: {db.AuthUserId}");
                        // Auto-create/fetch a player record matching their email username
                        StartCoroutine(db.UpsertPlayer(email, (playerJson) =>
                        {
                            ShowError("Login successful! Loading game...");
                            if (!string.IsNullOrEmpty(nextSceneName))
                                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
                            else
                                ShowMainPanel();
                        }));
                    }
                    else
                    {
                        ShowError($"Login Failed: {response}");
                    }
                }));
            }
            else
            {
                ShowError("DatabaseProvider not found.");
            }
        }

        private void OnSignUpSubmit()
        {
            string email = signUpEmailInput != null ? signUpEmailInput.text : "";
            string password = signUpPasswordInput != null ? signUpPasswordInput.text : "";
            string confirm = signUpConfirmPasswordInput != null ? signUpConfirmPasswordInput.text : "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirm))
            {
                ShowError("Please fill in all fields.");
                return;
            }

            if (password != confirm)
            {
                ShowError("Passwords do not match.");
                return;
            }

            var db = Persistence.DatabaseProvider.Instance;
            if (db != null)
            {
                signUpSubmitButton.interactable = false;
                StartCoroutine(db.SignUpWithEmail(email, password, (ok, response) =>
                {
                    signUpSubmitButton.interactable = true;
                    if (ok)
                    {
                        Debug.Log($"[LoginManager] Sign Up Success! Auth ID: {db.AuthUserId}");
                        ShowError("Sign up successful! Please login.");
                        ShowLoginPanel();
                    }
                    else
                    {
                        ShowError($"Sign Up Failed: {response}");
                    }
                }));
            }
            else
            {
                ShowError("DatabaseProvider not found.");
            }
        }

        private void OnQuit()
        {
            Debug.Log("[LoginManager] Quit called.");
            #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
            #else
                        Application.Quit();
            #endif
        }

        public void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.gameObject.SetActive(true);
                errorText.text = message;
            }
        }

        private void ClearError()
        {
            if (errorText != null)
            {
                errorText.text = "";
                errorText.gameObject.SetActive(false);
            }
        }
    }
}
