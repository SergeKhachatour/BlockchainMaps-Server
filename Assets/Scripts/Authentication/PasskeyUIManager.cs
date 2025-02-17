using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

namespace BlockchainMaps.Authentication
{
    public class PasskeyUIManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject authPanel;
        public Button authenticateButton;
        public TextMeshProUGUI statusText;
        public Image statusIcon;
        public Button logOffButton;
        
        [Header("Status Colors")]
        [SerializeField] private Color authenticatedColor = Color.green;
        [SerializeField] private Color unauthenticatedColor = Color.red;
        [SerializeField] private Color processingColor = Color.yellow;

        private PasskeyManager passkeyManager;
        private bool isInitialized = false;
        private static bool isBeingSetup = false;

        public static void StartSetup()
        {
            isBeingSetup = true;
        }

        public static void EndSetup()
        {
            isBeingSetup = false;
        }

        private void Awake()
        {
            Debug.Log("PasskeyUIManager Awake");
            InitializeUI();
        }

        private void OnEnable()
        {
            Debug.Log("[PasskeyUIManager] OnEnable");
            InitializeUI();
        }

        private void InitializeUI()
        {
            if (isInitialized) return;
            
            Debug.Log("[PasskeyUIManager] Initializing UI...");
            
            // Verify button setup
            if (authenticateButton != null)
            {
                Debug.Log("[PasskeyUIManager] Setting up authenticate button");
                authenticateButton.onClick.RemoveAllListeners();
                authenticateButton.onClick.AddListener(OnAuthenticateButtonClicked);
                
                // Ensure the button image has raycast target enabled
                Image buttonImage = authenticateButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.raycastTarget = true;
                    Debug.Log("[PasskeyUIManager] Button raycastTarget enabled");
                }
            }
            else
            {
                Debug.LogError("[PasskeyUIManager] Authenticate button reference is missing!");
            }

            // Set up log off button
            if (logOffButton != null)
            {
                Debug.Log("[PasskeyUIManager] Setting up log off button");
                logOffButton.onClick.RemoveAllListeners();
                logOffButton.onClick.AddListener(OnLogOffButtonClicked);
                logOffButton.gameObject.SetActive(false); // Hide initially
            }
            else
            {
                Debug.LogError("[PasskeyUIManager] Log off button reference is missing!");
            }

            isInitialized = true;
        }

        private void Start()
        {
            Debug.Log("[PasskeyUIManager] Start");
            
            // Ensure PasskeyManager exists
            passkeyManager = PasskeyManager.Instance;
            if (passkeyManager == null)
            {
                Debug.LogError("[PasskeyUIManager] Failed to get PasskeyManager instance!");
                return;
            }
            
            Debug.Log($"[PasskeyUIManager] Found PasskeyManager: {passkeyManager.gameObject.name}");
            UpdateUI();
        }

        private void OnAuthenticateButtonClicked()
        {
            Debug.Log("Authenticate button clicked!");
            HandleAuthenticationClick();
        }

        private void OnLogOffButtonClicked()
        {
            Debug.Log("[PasskeyUIManager] Log off button clicked!");
            LogOff();
        }

        private void LogOff()
        {
            if (passkeyManager != null)
            {
                passkeyManager.LogOff();
                UpdateUI();
                Show(); // Show the auth panel again
            }
        }

        private async void HandleAuthenticationClick()
        {
            if (passkeyManager == null)
            {
                Debug.LogError("[PasskeyUIManager] PasskeyManager is null!");
                return;
            }

            Debug.Log("[PasskeyUIManager] Starting authentication process...");
            SetProcessingState();
            bool success = await passkeyManager.Authenticate("user");
            
            if (success)
            {
                ShowMessage("Authentication successful!");
                Hide(); // Hide the auth panel
                if (logOffButton != null)
                {
                    logOffButton.gameObject.SetActive(true); // Show log off button
                }
            }
            else
            {
                ShowMessage("Authentication failed. Please try again.");
                UpdateUI();
            }
        }

        private void SetProcessingState()
        {
            Debug.Log("Setting processing state...");
            if (statusText != null) statusText.text = "Processing...";
            if (statusIcon != null) statusIcon.color = processingColor;
            if (authenticateButton != null) authenticateButton.interactable = false;
        }

        private void UpdateUI()
        {
            if (passkeyManager == null)
            {
                Debug.LogError("[PasskeyUIManager] PasskeyManager is null during UI update!");
                return;
            }

            bool isAuthenticated = passkeyManager.IsAuthenticated();
            Debug.Log($"[PasskeyUIManager] Updating UI - Authentication status: {isAuthenticated}");
            
            if (statusText != null)
            {
                statusText.text = isAuthenticated ? "Authenticated" : "Not Authenticated";
            }

            if (statusIcon != null)
            {
                statusIcon.color = isAuthenticated ? authenticatedColor : unauthenticatedColor;
            }

            if (authenticateButton != null)
            {
                authenticateButton.interactable = !isAuthenticated;
            }

            if (logOffButton != null)
            {
                logOffButton.gameObject.SetActive(isAuthenticated);
            }

            if (authPanel != null)
            {
                authPanel.SetActive(!isAuthenticated);
            }
        }

        private void ShowMessage(string message)
        {
            Debug.Log($"UI Message: {message}");
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        public void Show()
        {
            Debug.Log("Showing PasskeyUI");
            if (authPanel != null)
            {
                authPanel.SetActive(true);
                UpdateUI();
            }
        }

        public void Hide()
        {
            Debug.Log("Hiding PasskeyUI");
            if (authPanel != null)
            {
                authPanel.SetActive(false);
            }
        }

        private void OnValidate()
        {
            // Skip validation during setup
            if (isBeingSetup) return;

            // Only show errors if we're in edit mode and the component is enabled
            if (!Application.isPlaying && enabled)
            {
                // Don't show errors during initialization
                if (gameObject.scene.isLoaded)
                {
                    if (authenticateButton == null)
                        Debug.LogError("[PasskeyUIManager] Authenticate Button reference is missing!", this);
                    if (statusText == null)
                        Debug.LogError("[PasskeyUIManager] Status Text reference is missing!", this);
                    if (statusIcon == null)
                        Debug.LogError("[PasskeyUIManager] Status Icon reference is missing!", this);
                    if (authPanel == null)
                        Debug.LogError("[PasskeyUIManager] Auth Panel reference is missing!", this);
                    if (logOffButton == null)
                        Debug.LogError("[PasskeyUIManager] Log Off Button reference is missing!", this);
                }
            }
        }
    }
} 