using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using BlockchainMaps;

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
        public TextMeshProUGUI walletAddressText; // New field for wallet address display
        
        [Header("Top-Right Status Indicator")]
        [SerializeField] private GameObject topRightStatusPanel;
        [SerializeField] private TextMeshProUGUI topRightStatusText;
        [SerializeField] private Image topRightStatusIcon;
        [SerializeField] private TextMeshProUGUI topRightWalletText;
        [SerializeField] private TextMeshProUGUI topRightBalanceText;
        
        [Header("Status Colors")]
        [SerializeField] private Color authenticatedColor = Color.green;
        [SerializeField] private Color unauthenticatedColor = Color.red;
        [SerializeField] private Color processingColor = Color.yellow;

        private PasskeyManager passkeyManager;
        private bool isInitialized = false;
        private static bool isBeingSetup = false;
        private Coroutine topRightMonitor;
        private string lastWalletPublicKey;
        private float lastBalanceFetchTime;
        private const float BalanceRefreshIntervalSeconds = 10f;
        
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
            // Start monitor to keep the top-right status in sync without hard assembly refs
            if (topRightMonitor == null)
            {
                topRightMonitor = StartCoroutine(TopRightStatusMonitor());
            }
            // Attempt an immediate refresh if wallet already exists
            TryRefreshTopRightWalletAndBalance();
        }

        private void OnDisable()
        {
            if (topRightMonitor != null)
            {
                StopCoroutine(topRightMonitor);
                topRightMonitor = null;
            }
        }

        private void InitializeUI()
        {
            if (isInitialized) return;
            
            Debug.Log("[PasskeyUIManager] Initializing UI...");
            
            // Create top-right status indicator if it doesn't exist
            CreateTopRightStatusIndicator();
            
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
        
        private void CreateTopRightStatusIndicator()
        {
            // Check if top-right status panel already exists
            if (topRightStatusPanel != null)
            {
                Debug.Log("[PasskeyUIManager] Top-right status panel already exists");
                return;
            }
            
            Debug.Log("[PasskeyUIManager] Creating top-right status indicator...");
            
            // Find or create Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.Log("[PasskeyUIManager] No Canvas found, creating one...");
                GameObject canvasObj = new GameObject("StatusCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // Ensure it's on top
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            // Create top-right status panel
            topRightStatusPanel = new GameObject("TopRightStatusPanel");
            topRightStatusPanel.transform.SetParent(canvas.transform, false);
            
            // Add RectTransform and configure for top-right positioning
            RectTransform panelRect = topRightStatusPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.sizeDelta = new Vector2(300, 110);
            panelRect.anchoredPosition = new Vector2(-20, -20); // 20px from top-right corner
            
            // Add background image
            Image panelImage = topRightStatusPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            // Create status text
            GameObject statusTextObj = new GameObject("TopRightStatusText");
            statusTextObj.transform.SetParent(topRightStatusPanel.transform, false);
            topRightStatusText = statusTextObj.AddComponent<TextMeshProUGUI>();
            topRightStatusText.text = "Not Authenticated";
            topRightStatusText.color = unauthenticatedColor;
            topRightStatusText.fontSize = 16;
            topRightStatusText.alignment = TextAlignmentOptions.Right;
            
            RectTransform statusTextRect = statusTextObj.GetComponent<RectTransform>();
            statusTextRect.anchorMin = new Vector2(0, 0.6f);
            statusTextRect.anchorMax = new Vector2(1, 0.95f);
            statusTextRect.offsetMin = new Vector2(10, 0);
            statusTextRect.offsetMax = new Vector2(-10, 0);
            
            // Create status icon
            GameObject statusIconObj = new GameObject("TopRightStatusIcon");
            statusIconObj.transform.SetParent(topRightStatusPanel.transform, false);
            topRightStatusIcon = statusIconObj.AddComponent<Image>();
            topRightStatusIcon.color = unauthenticatedColor;
            topRightStatusIcon.sprite = CreateCircleSprite();
            
            RectTransform statusIconRect = statusIconObj.GetComponent<RectTransform>();
            statusIconRect.anchorMin = new Vector2(0, 0.6f);
            statusIconRect.anchorMax = new Vector2(0, 0.95f);
            statusIconRect.sizeDelta = new Vector2(16, 16);
            statusIconRect.anchoredPosition = new Vector2(15, 0);
            
            // Create wallet address text
            GameObject walletTextObj = new GameObject("TopRightWalletText");
            walletTextObj.transform.SetParent(topRightStatusPanel.transform, false);
            topRightWalletText = walletTextObj.AddComponent<TextMeshProUGUI>();
            topRightWalletText.text = "";
            topRightWalletText.color = Color.white;
            topRightWalletText.fontSize = 12;
            topRightWalletText.alignment = TextAlignmentOptions.Right;
            
            RectTransform walletTextRect = walletTextObj.GetComponent<RectTransform>();
            walletTextRect.anchorMin = new Vector2(0, 0.3f);
            walletTextRect.anchorMax = new Vector2(1, 0.58f);
            walletTextRect.offsetMin = new Vector2(10, 0);
            walletTextRect.offsetMax = new Vector2(-10, 0);
            
            // Create balance text
            GameObject balanceTextObj = new GameObject("TopRightBalanceText");
            balanceTextObj.transform.SetParent(topRightStatusPanel.transform, false);
            topRightBalanceText = balanceTextObj.AddComponent<TextMeshProUGUI>();
            topRightBalanceText.text = "";
            topRightBalanceText.color = Color.white;
            topRightBalanceText.fontSize = 12;
            topRightBalanceText.alignment = TextAlignmentOptions.Right;
            
            RectTransform balanceTextRect = balanceTextObj.GetComponent<RectTransform>();
            balanceTextRect.anchorMin = new Vector2(0, 0.05f);
            balanceTextRect.anchorMax = new Vector2(1, 0.3f);
            balanceTextRect.offsetMin = new Vector2(10, 0);
            balanceTextRect.offsetMax = new Vector2(-10, 0);
            
            // Initially hide the panel
            topRightStatusPanel.SetActive(false);
            
            Debug.Log("[PasskeyUIManager] Top-right status indicator created successfully");
        }
        
        private Sprite CreateCircleSprite()
        {
            // Create a simple circle sprite for the status icon
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            
            Vector2 center = new Vector2(16, 16);
            float radius = 14f;
            
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * 32 + x] = distance <= radius ? Color.white : Color.clear;
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }

        private void Start()
        {
            Debug.Log("[PasskeyUIManager] Start");
            passkeyManager = FindFirstObjectByType<PasskeyManager>();
            if (passkeyManager != null)
            {
                Debug.Log($"[PasskeyUIManager] Found PasskeyManager: {passkeyManager.gameObject.name}");
            }
            else
            {
                Debug.LogError("[PasskeyUIManager] PasskeyManager not found in scene");
            }
            
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
                
                // Clear wallet address display
                if (walletAddressText != null)
                {
                    walletAddressText.text = "";
                    walletAddressText.gameObject.SetActive(false);
                }
                
                UpdateUI();
                Show(); // Show the auth panel again
            }
        }

        private async void HandleAuthenticationClick()
        {
            if (isBeingSetup)
            {
                Debug.Log("[PasskeyUIManager] Setup already in progress, skipping...");
                return;
            }

            isBeingSetup = true;
            SetProcessingState();

            try
            {
                Debug.Log("[PasskeyUIManager] Starting authentication process...");
                
                // Check if user is new or existing
                bool isNewUser = CheckIfNewUser();
                
                if (isNewUser)
                {
                    ShowMessage("Creating new passkey and wallet...");
                    Debug.Log("[PasskeyUIManager] New user - will create passkey and wallet");
                    
                    // Authenticate with passkey
                    bool authResult = await passkeyManager.Authenticate("user");
                    
                    if (authResult)
                    {
                        ShowMessage($"Authentication successful! Creating Stellar wallet...");
                        Debug.Log($"[PasskeyUIManager] Authentication successful, creating backend wallet...");
                        
                        try
                        {
                            Debug.Log("[PasskeyUIManager] Starting wallet creation process...");
                            
                            // Step 1: Ensure AutoStellarSetup exists and runs
                            var autoSetupType = System.Type.GetType("BlockchainMaps.AutoStellarSetup");
                            if (autoSetupType != null)
                            {
                                var autoSetup = FindFirstObjectByType(autoSetupType) as MonoBehaviour;
                                if (autoSetup == null)
                                {
                                    Debug.Log("[PasskeyUIManager] Creating AutoStellarSetup...");
                                    var setupObj = new GameObject("AutoStellarSetup");
                                    autoSetup = setupObj.AddComponent(autoSetupType) as MonoBehaviour;
                                    DontDestroyOnLoad(setupObj);
                                    
                                    // Force the setup to run immediately
                                    var setupMethod = autoSetupType.GetMethod("SetupStellarBackend", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    if (setupMethod != null)
                                    {
                                        setupMethod.Invoke(autoSetup, null);
                                        Debug.Log("[PasskeyUIManager] AutoStellarSetup completed");
                                    }
                                }
                            }

                            // Step 2: Ensure StellarApiClient exists
                            var apiClientType = System.Type.GetType("BlockchainMaps.StellarApiClient");
                            MonoBehaviour apiClient = null;
                            if (apiClientType != null)
                            {
                                apiClient = FindFirstObjectByType(apiClientType) as MonoBehaviour;
                                if (apiClient == null)
                                {
                                    Debug.Log("[PasskeyUIManager] Creating StellarApiClient...");
                                    var apiClientObj = new GameObject("StellarApiClient");
                                    apiClient = apiClientObj.AddComponent(apiClientType) as MonoBehaviour;
                                    DontDestroyOnLoad(apiClientObj);
                                }
                            }

                            // Step 3: Ensure StellarWalletManager exists
                            MonoBehaviour walletManager = null;
                            
                            // Find existing StellarWalletManager by type name
                            var existingWalletManagers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                            foreach (var manager in existingWalletManagers)
                            {
                                if (manager != null && manager.GetType().Name == "StellarWalletManager")
                                {
                                    walletManager = manager;
                                    break;
                                }
                            }
                            
                            // If not found, try to create one
                            if (walletManager == null)
                            {
                                Debug.Log("[PasskeyUIManager] Creating StellarWalletManager...");
                                try
                                {
                                    // Try to find the type at runtime
                                    var walletManagerType = System.Type.GetType("StellarWalletManager");
                                    if (walletManagerType == null)
                                    {
                                        // Try with full namespace
                                        walletManagerType = System.Type.GetType("BlockchainMaps.StellarWalletManager");
                                    }
                                    
                                    if (walletManagerType != null)
                                    {
                                        var walletManagerObj = new GameObject("StellarWalletManager");
                                        walletManager = walletManagerObj.AddComponent(walletManagerType) as MonoBehaviour;
                                        DontDestroyOnLoad(walletManagerObj);
                                    }
                                    else
                                    {
                                        Debug.LogError("[PasskeyUIManager] Could not find StellarWalletManager type");
                                    }
                                }
                                catch (System.Exception e)
                                {
                                    Debug.LogError($"[PasskeyUIManager] Error creating StellarWalletManager: {e.Message}");
                                }
                            }
                            
                            if (walletManager != null)
                            {
                                Debug.Log("[PasskeyUIManager] All components ready, creating wallet...");
                                
                                // Update UI to show authenticated state
                                if (statusIcon != null) statusIcon.color = authenticatedColor;
                                if (authenticateButton != null) authenticateButton.interactable = false;
                                if (logOffButton != null) logOffButton.gameObject.SetActive(true);
                                
                                // Show a placeholder wallet address
                                if (walletAddressText != null)
                                {
                                    walletAddressText.text = "Backend wallet creation in progress...";
                                    walletAddressText.gameObject.SetActive(true);
                                }
                                
                                // Update UI and hide the auth panel
                                UpdateUI();
                                Hide();
                                
                                // Create wallet asynchronously (after UI updates to prevent blocking)
                                try
                                {
                                    Debug.Log("[PasskeyUIManager] Starting wallet creation process...");
                                    await CreateWalletAsync(walletManager);
                                    Debug.Log("[PasskeyUIManager] Wallet creation process completed");
                                    
                                    // Verify wallet was created by checking HasWallet
                                    var hasWalletMethod = walletManager.GetType().GetMethod("HasWallet");
                                    if (hasWalletMethod != null)
                                    {
                                        var hasWallet = (bool)hasWalletMethod.Invoke(walletManager, null);
                                        Debug.Log($"[PasskeyUIManager] Wallet verification - HasWallet: {hasWallet}");
                                        
                                        if (hasWallet)
                                        {
                                            Debug.Log("[PasskeyUIManager] ✅ Wallet created successfully!");
                                            ShowMessage("Authentication successful! Wallet created.");
                                        }
                                        else
                                        {
                                            Debug.LogError("[PasskeyUIManager] ❌ Wallet creation failed - HasWallet returns false");
                                            ShowMessage("Authentication successful! (Wallet creation failed)");
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning("[PasskeyUIManager] Could not verify wallet creation - HasWallet method not found");
                                        ShowMessage("Authentication successful! (Wallet verification failed)");
                                    }
                                }
                                catch (System.Exception e)
                                {
                                    Debug.LogError($"[PasskeyUIManager] Exception during wallet creation: {e.Message}");
                                    Debug.LogError($"[PasskeyUIManager] Stack trace: {e.StackTrace}");
                                    ShowMessage("Authentication successful! (Wallet creation failed)");
                                }
                            }
                            else
                            {
                                Debug.LogError("[PasskeyUIManager] Failed to create StellarWalletManager");
                                ShowMessage("Authentication successful! (Backend not available)");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[PasskeyUIManager] Error during wallet creation: {e.Message}");
                            ShowMessage("Authentication successful! (Backend wallet creation failed)");
                        }
                    }
                    else
                    {
                        ShowMessage("Authentication failed. Please try again.");
                        Debug.LogError("[PasskeyUIManager] Authentication failed");
                    }
                }
                else
                {
                    ShowMessage("Authenticating with existing passkey...");
                    Debug.Log("[PasskeyUIManager] Existing user - will authenticate with passkey");
                    
                    // Authenticate with existing passkey
                    bool authResult = await passkeyManager.Authenticate("user");
                    
                    if (authResult)
                    {
                        ShowMessage("Authentication successful!");
                        Debug.Log("[PasskeyUIManager] Authentication successful");
                        
                        // Get existing wallet address
                        string walletAddress = await GetWalletAddress();
                        if (!string.IsNullOrEmpty(walletAddress))
                        {
                            if (walletAddressText != null)
                            {
                                walletAddressText.text = $"Wallet: {walletAddress}";
                                walletAddressText.gameObject.SetActive(true);
                            }
                        }
                        
                        // Update UI to show authenticated state
                        if (statusIcon != null) statusIcon.color = authenticatedColor;
                        if (authenticateButton != null) authenticateButton.interactable = false;
                        if (logOffButton != null) logOffButton.gameObject.SetActive(true);
                        
                        // Update UI and hide the auth panel
                        UpdateUI();
                        Hide();
                    }
                    else
                    {
                        ShowMessage("Authentication failed. Please try again.");
                        Debug.LogError("[PasskeyUIManager] Authentication failed");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyUIManager] Error during authentication: {e.Message}");
                ShowMessage($"Authentication error: {e.Message}");
            }
            finally
            {
                isBeingSetup = false;
            }
        }

        private bool CheckIfNewUser()
        {
            #if !UNITY_EDITOR && UNITY_WEBGL
            // Bridge-based wallet storage disabled; treat as new user to ensure wallet creation
            return true;
            #else
            return true; // Simulate new user in editor
            #endif
        }

        private async Task<string> GetWalletAddress()
        {
            #if !UNITY_EDITOR && UNITY_WEBGL
            // Bridge storage disabled; return empty to force backend wallet path
            await Task.Delay(50);
            return string.Empty;
            #else
            // Simulate async operation in editor
            await Task.Delay(100);
            return "Editor_Test_Wallet_" + DateTime.Now.Ticks.ToString().Substring(0, 8);
            #endif
        }

        #if !UNITY_EDITOR && UNITY_WEBGL
        // Disabled direct JS interop to avoid null function mismatch in WebGL when plugin not present
        // private static extern string GetStoredWalletAddress(string username);
        #endif

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
            
            // Update main UI elements
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
                // Hide the authenticate button completely when authenticated
                authenticateButton.gameObject.SetActive(!isAuthenticated);
            }

            if (logOffButton != null)
            {
                logOffButton.gameObject.SetActive(isAuthenticated);
            }

            // Hide wallet address text when not authenticated
            if (walletAddressText != null)
            {
                walletAddressText.gameObject.SetActive(isAuthenticated);
            }

            // Hide the entire auth panel when authenticated
            if (authPanel != null)
            {
                authPanel.SetActive(!isAuthenticated);
            }
            
            // Also hide individual auth elements that might be outside the panel
            if (statusText != null)
            {
                statusText.gameObject.SetActive(!isAuthenticated);
            }
            
            if (statusIcon != null)
            {
                statusIcon.gameObject.SetActive(!isAuthenticated);
            }
            
            // Update top-right status indicator
            UpdateTopRightStatusIndicator(isAuthenticated);
        }
        
        private void UpdateTopRightStatusIndicator(bool isAuthenticated)
        {
            if (topRightStatusPanel == null)
            {
                Debug.LogWarning("[PasskeyUIManager] Top-right status panel is null, creating it...");
                CreateTopRightStatusIndicator();
            }
            
            if (topRightStatusPanel != null)
            {
                // Show the panel when authenticated, hide when not
                topRightStatusPanel.SetActive(isAuthenticated);
                
                if (isAuthenticated)
                {
                    // Update status text and icon
                    if (topRightStatusText != null)
                    {
                        topRightStatusText.text = "Authenticated";
                        topRightStatusText.color = authenticatedColor;
                    }
                    
                    if (topRightStatusIcon != null)
                    {
                        topRightStatusIcon.color = authenticatedColor;
                    }
                    
                    // Also try immediate refresh of wallet/balance
                    TryRefreshTopRightWalletAndBalance();
                }
                else
                {
                    // Update for unauthenticated state
                    if (topRightStatusText != null)
                    {
                        topRightStatusText.text = "Not Authenticated";
                        topRightStatusText.color = unauthenticatedColor;
                    }
                    
                    if (topRightStatusIcon != null)
                    {
                        topRightStatusIcon.color = unauthenticatedColor;
                    }
                    
                    if (topRightWalletText != null)
                    {
                        topRightWalletText.text = "";
                    }
                    if (topRightBalanceText != null)
                    {
                        topRightBalanceText.text = "";
                    }
                }
            }
        }
        
        /// <summary>
        /// Public method to manually refresh the top-right status indicator
        /// </summary>
        [ContextMenu("Refresh Top-Right Status")]
        public void RefreshTopRightStatus()
        {
            if (passkeyManager != null)
            {
                bool isAuthenticated = passkeyManager.IsAuthenticated();
                UpdateTopRightStatusIndicator(isAuthenticated);
                Debug.Log($"[PasskeyUIManager] Manually refreshed top-right status. Authenticated: {isAuthenticated}");
            }
            else
            {
                Debug.LogWarning("[PasskeyUIManager] PasskeyManager is null, cannot refresh status");
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
            
            // Show the main auth panel
            if (authPanel != null)
            {
                authPanel.SetActive(true);
            }
            
            // Show individual authentication UI elements
            if (authenticateButton != null)
            {
                authenticateButton.gameObject.SetActive(true);
            }
            
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
            }
            
            if (statusIcon != null)
            {
                statusIcon.gameObject.SetActive(true);
            }
            
            // Hide the log off button when showing auth UI
            if (logOffButton != null)
            {
                logOffButton.gameObject.SetActive(false);
            }
            
            UpdateUI();
        }

        public void Hide()
        {
            Debug.Log("Hiding PasskeyUI");
            
            // Hide the main auth panel
            if (authPanel != null)
            {
                authPanel.SetActive(false);
            }
            
            // Hide individual authentication UI elements
            if (authenticateButton != null)
            {
                authenticateButton.gameObject.SetActive(false);
            }
            
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }
            
            if (statusIcon != null)
            {
                statusIcon.gameObject.SetActive(false);
            }
            
            // Show the log off button when hiding auth UI
            if (logOffButton != null)
            {
                logOffButton.gameObject.SetActive(true);
            }
            
            // Ensure the game can start by enabling any game start buttons
            EnableGameStartButtons();
        }
        
        private void EnableGameStartButtons()
        {
            Debug.Log("[PasskeyUIManager] Enabling game start buttons...");
            
            // Find all buttons with "Start" in their name
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            
            foreach (var button in allButtons)
            {
                if (button != null && button.name.ToLower().Contains("start"))
                {
                    button.interactable = true;
                    Debug.Log($"[PasskeyUIManager] Enabled button: {button.name}");
                }
            }
            
            Debug.Log("[PasskeyUIManager] Game start buttons enabled");
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
                    // Wallet address text is optional - can be created automatically by WalletAddressDisplay
                    // if (walletAddressText == null)
                    //     Debug.LogError("[PasskeyUIManager] Wallet Address Text reference is missing!", this);
                }
            }
        }
        
        /// <summary>
        /// Async method to create wallet using runtime type checking (WebGL compatible)
        /// </summary>
        private async Task CreateWalletAsync(MonoBehaviour walletManager)
        {
            Debug.Log("[PasskeyUIManager] Starting CreateWalletAsync method...");
            
            try
            {
                // Check if the walletManager is actually a StellarWalletManager by type name
                if (walletManager != null && walletManager.GetType().Name == "StellarWalletManager")
                {
                    Debug.Log("[PasskeyUIManager] StellarWalletManager found, calling CreateWallet via reflection...");
                    
                    // Use reflection to call CreateWallet method
                    var createWalletMethod = walletManager.GetType().GetMethod("CreateWallet");
                    if (createWalletMethod != null)
                    {
                        Debug.Log("[PasskeyUIManager] CreateWallet method found, invoking...");
                        var task = createWalletMethod.Invoke(walletManager, null);
                        Debug.Log($"[PasskeyUIManager] CreateWallet method returned: {task?.GetType().FullName}");
                        
                        // Handle Task<WalletData> result via reflection
                        if (task is System.Threading.Tasks.Task walletTask)
                        {
                            Debug.Log("[PasskeyUIManager] Task detected, awaiting completion...");
                            
                            // Wait for the task to complete
                            await walletTask;
                            Debug.Log("[PasskeyUIManager] Task completed successfully");
                            
                            // Get the result using reflection
                            var resultProperty = walletTask.GetType().GetProperty("Result");
                            if (resultProperty != null)
                            {
                                var walletData = resultProperty.GetValue(walletTask);
                                Debug.Log($"[PasskeyUIManager] Task result type: {walletData?.GetType().FullName}");
                                
                                if (walletData != null)
                                {
                                    var t = walletData.GetType();
                                    var publicKeyField = t.GetField("publicKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                                    var publicKeyProperty = t.GetProperty("publicKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                                    var publicKey = publicKeyField != null ? publicKeyField.GetValue(walletData) as string : (publicKeyProperty != null ? publicKeyProperty.GetValue(walletData) as string : null);
                                    
                                    Debug.Log($"[PasskeyUIManager] Wallet public key: {publicKey}");
                                    
                                    if (!string.IsNullOrEmpty(publicKey))
                                    {
                                        // Force a longer delay to ensure wallet data is saved
                                        Debug.Log("[PasskeyUIManager] Waiting for wallet data to be saved...");
                                        await System.Threading.Tasks.Task.Delay(1000);
                                        
                                        // Multiple verification attempts
                                        bool walletVerified = false;
                                        int verificationAttempts = 0;
                                        const int maxAttempts = 10;
                                        
                                        while (!walletVerified && verificationAttempts < maxAttempts)
                                        {
                                            verificationAttempts++;
                                            Debug.Log($"[PasskeyUIManager] Verification attempt {verificationAttempts}/{maxAttempts}");
                                            
                                            // Verify the wallet was actually saved by checking HasWallet
                                            var hasWalletMethod = walletManager.GetType().GetMethod("HasWallet");
                                            if (hasWalletMethod != null)
                                            {
                                                var hasWallet = (bool)hasWalletMethod.Invoke(walletManager, null);
                                                Debug.Log($"[PasskeyUIManager] HasWallet check: {hasWallet}");
                                                
                                                if (hasWallet)
                                                {
                                                    // Double-check by getting the wallet data
                                                    var getCurrentWalletMethod = walletManager.GetType().GetMethod("GetCurrentWallet");
                                                    if (getCurrentWalletMethod != null)
                                                    {
                                                        var currentWallet = getCurrentWalletMethod.Invoke(walletManager, null);
                                                        if (currentWallet != null)
                                                        {
                                                            var ct = currentWallet.GetType();
                                                            var currentPublicKeyField = ct.GetField("publicKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                                                            var currentPublicKeyProperty = ct.GetProperty("publicKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                                                            var currentPublicKey = currentPublicKeyField != null ? currentPublicKeyField.GetValue(currentWallet) as string : (currentPublicKeyProperty != null ? currentPublicKeyProperty.GetValue(currentWallet) as string : null);
                                                            Debug.Log($"[PasskeyUIManager] Current wallet public key: {currentPublicKey}");
                                                            
                                                            if (!string.IsNullOrEmpty(currentPublicKey))
                                                            {
                                                                walletVerified = true;
                                                                Debug.Log("[PasskeyUIManager] Wallet verified successfully!");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            
                                            if (!walletVerified && verificationAttempts < maxAttempts)
                                            {
                                                Debug.Log($"[PasskeyUIManager] Wallet not verified, waiting 500ms before retry...");
                                                await System.Threading.Tasks.Task.Delay(500);
                                                
                                                // Try to force reload the wallet data
                                                var reloadMethod = walletManager.GetType().GetMethod("ReloadWalletDataManually");
                                                if (reloadMethod != null)
                                                {
                                                    reloadMethod.Invoke(walletManager, null);
                                                    Debug.Log("[PasskeyUIManager] Forced wallet data reload");
                                                }
                                            }
                                        }
                                        
                                        if (walletVerified)
                                        {
                                            Debug.Log("[PasskeyUIManager] ✅ Wallet creation and verification completed successfully!");
                                            ShowMessage("Wallet created successfully!");
                                            
                                            // Update the UI to show the wallet address
                                            if (walletAddressText != null)
                                            {
                                                walletAddressText.text = $"Wallet: {publicKey.Substring(0, 8)}...{publicKey.Substring(publicKey.Length - 4)}";
                                            }
                                            
                                            // Update top-right status
                                            UpdateTopRightStatusIndicator(true);
                                        }
                                        else
                                        {
                                            Debug.LogError("[PasskeyUIManager] ❌ Wallet verification failed after all attempts");
                                            ShowMessage("Wallet created but verification failed. Please try again.");
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogError("[PasskeyUIManager] ❌ Wallet data has no public key");
                                        ShowMessage("Wallet creation failed - no public key generated.");
                                    }
                                }
                                else
                                {
                                    Debug.LogError("[PasskeyUIManager] ❌ Wallet data is null");
                                    ShowMessage("Wallet creation failed - no wallet data received.");
                                }
                            }
                            else
                            {
                                Debug.LogError("[PasskeyUIManager] ❌ Could not get task result property");
                                ShowMessage("Wallet creation failed - could not retrieve result.");
                            }
                        }
                        else
                        {
                            Debug.LogError($"[PasskeyUIManager] ❌ CreateWallet did not return a Task: {task?.GetType().FullName}");
                            ShowMessage("Wallet creation failed - invalid return type.");
                        }
                    }
                    else
                    {
                        Debug.LogError("[PasskeyUIManager] ❌ CreateWallet method not found");
                        ShowMessage("Wallet creation failed - method not found.");
                    }
                }
                else
                {
                    Debug.LogError("[PasskeyUIManager] ❌ StellarWalletManager not found");
                    ShowMessage("Wallet creation failed - wallet manager not found.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyUIManager] ❌ Exception in CreateWalletAsync: {e.Message}");
                Debug.LogError($"[PasskeyUIManager] Stack trace: {e.StackTrace}");
                ShowMessage($"Wallet creation failed: {e.Message}");
            }
            
            Debug.Log("[PasskeyUIManager] CreateWalletAsync method completed");
        }

        // Test method to verify wallet creation flow
        [ContextMenu("Test Wallet Creation")]
        public async void TestWalletCreation()
        {
            Debug.Log("[PasskeyUIManager] === TESTING WALLET CREATION ===");
            try
            {
                // Find StellarWalletManager using reflection
                var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                MonoBehaviour walletManager = null;
                
                foreach (var mb in allMonoBehaviours)
                {
                    if (mb.GetType().Name == "StellarWalletManager")
                    {
                        walletManager = mb;
                        break;
                    }
                }
                
                if (walletManager == null)
                {
                    Debug.LogError("[PasskeyUIManager] StellarWalletManager not found!");
                    return;
                }
                
                Debug.Log("[PasskeyUIManager] StellarWalletManager found, testing wallet creation...");
                await CreateWalletAsync(walletManager);
                Debug.Log("[PasskeyUIManager] Wallet creation test completed");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyUIManager] Test failed: {e.Message}");
                Debug.LogError($"[PasskeyUIManager] Stack trace: {e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Public method to manually trigger wallet creation (for testing/debugging)
        /// </summary>
        [ContextMenu("Manual Trigger Wallet Creation")]
        public async void ManualTriggerWalletCreation()
        {
            Debug.Log("[PasskeyUIManager] === MANUAL WALLET CREATION TRIGGER ===");
            try
            {
                // Find StellarWalletManager using reflection
                var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                MonoBehaviour walletManager = null;
                
                foreach (var mb in allMonoBehaviours)
                {
                    if (mb.GetType().Name == "StellarWalletManager")
                    {
                        walletManager = mb;
                        break;
                    }
                }
                
                if (walletManager == null)
                {
                    Debug.LogError("[PasskeyUIManager] StellarWalletManager not found!");
                    return;
                }
                
                Debug.Log("[PasskeyUIManager] StellarWalletManager found, manually triggering wallet creation...");
                
                // Simulate the authentication success flow
                ShowMessage("Manual wallet creation triggered...");
                
                // Update UI to show authenticated state
                if (statusIcon != null) statusIcon.color = authenticatedColor;
                if (authenticateButton != null) authenticateButton.interactable = false;
                if (logOffButton != null) logOffButton.gameObject.SetActive(true);
                
                // Update UI and hide the auth panel
                UpdateUI();
                Hide();
                
                // Create wallet asynchronously
                await CreateWalletAsync(walletManager);
                Debug.Log("[PasskeyUIManager] Manual wallet creation trigger completed");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyUIManager] Manual trigger failed: {e.Message}");
                Debug.LogError($"[PasskeyUIManager] Stack trace: {e.StackTrace}");
            }
        }

        private IEnumerator TopRightStatusMonitor()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
                try
                {
                    // Find StellarWalletManager via reflection to avoid assembly coupling
                    MonoBehaviour walletManager = null;
                    foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                    {
                        if (mb != null && mb.GetType().Name == "StellarWalletManager")
                        {
                            walletManager = mb;
                            break;
                        }
                    }

                    if (walletManager != null)
                    {
                        var type = walletManager.GetType();
                        var hasWalletMethod = type.GetMethod("HasWallet");
                        var getCurrentWalletMethod = type.GetMethod("GetCurrentWallet");
                        var getBalanceMethod = type.GetMethod("GetBalance");

                        if (hasWalletMethod != null && getCurrentWalletMethod != null)
                        {
                            var hasWallet = (bool)hasWalletMethod.Invoke(walletManager, null);
                            if (hasWallet)
                            {
                                var wallet = getCurrentWalletMethod.Invoke(walletManager, null);
                                if (wallet != null)
                                {
                                    var wType = wallet.GetType();
                                    var pkField = wType.GetField("publicKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                                    var pkProp = wType.GetProperty("publicKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                                    var publicKey = pkField != null ? pkField.GetValue(wallet) as string : (pkProp != null ? pkProp.GetValue(wallet) as string : null);

                                    if (!string.IsNullOrEmpty(publicKey))
                                    {
                                        if (publicKey != lastWalletPublicKey)
                                        {
                                            lastWalletPublicKey = publicKey;
                                            UpdateTopRightWalletLabel(publicKey);
                                            lastBalanceFetchTime = 0f; // force immediate fetch
                                        }

                                        // Periodically refresh balance
                                        if (Time.unscaledTime - lastBalanceFetchTime > BalanceRefreshIntervalSeconds)
                                        {
                                            lastBalanceFetchTime = Time.unscaledTime;
                                            _ = RequestBalanceAsync(walletManager, getBalanceMethod);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PasskeyUIManager] TopRightStatusMonitor error: {e.Message}");
                }

                yield return wait;
            }
        }

        private async Task RequestBalanceAsync(MonoBehaviour walletManager, System.Reflection.MethodInfo getBalanceMethod)
        {
            if (walletManager == null || getBalanceMethod == null) return;
            try
            {
                var taskObj = getBalanceMethod.Invoke(walletManager, null);
                if (taskObj is System.Threading.Tasks.Task balanceTask)
                {
                    await balanceTask;
                    var resultProp = balanceTask.GetType().GetProperty("Result");
                    var result = resultProp != null ? resultProp.GetValue(balanceTask) : null;
                    if (result != null)
                    {
                        // Reflect balances list
                        var rType = result.GetType();
                        var balancesProp = rType.GetProperty("balances");
                        var balances = balancesProp != null ? balancesProp.GetValue(result) as System.Collections.IEnumerable : null;
                        string xlm = null;
                        if (balances != null)
                        {
                            foreach (var b in balances)
                            {
                                var bType = b.GetType();
                                var assetCodeProp = bType.GetProperty("asset_code");
                                var assetTypeProp = bType.GetProperty("asset_type");
                                var balanceProp = bType.GetProperty("balance");
                                var assetCode = assetCodeProp != null ? assetCodeProp.GetValue(b) as string : null;
                                var assetType = assetTypeProp != null ? assetTypeProp.GetValue(b) as string : null;
                                var bal = balanceProp != null ? balanceProp.GetValue(b) as string : null;
                                if (string.Equals(assetCode ?? assetType, "XLM", StringComparison.OrdinalIgnoreCase) || string.Equals(assetType, "native", StringComparison.OrdinalIgnoreCase))
                                {
                                    xlm = bal;
                                    break;
                                }
                                if (xlm == null && string.Equals(assetType, "native", StringComparison.OrdinalIgnoreCase))
                                {
                                    xlm = bal;
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(xlm) && balances is System.Collections.ICollection coll && coll.Count > 0)
                        {
                            foreach (var b in balances)
                            {
                                var balanceProp = b.GetType().GetProperty("balance");
                                xlm = balanceProp != null ? balanceProp.GetValue(b) as string : null;
                                if (!string.IsNullOrEmpty(xlm)) break;
                            }
                        }
                        UpdateTopRightBalanceLabel(string.IsNullOrEmpty(xlm) ? string.Empty : $"Balance: {xlm} XLM");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyUIManager] Error requesting balance via reflection: {e.Message}");
            }
        }

        private void TryRefreshTopRightWalletAndBalance()
        {
            MonoBehaviour walletManager = null;
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb != null && mb.GetType().Name == "StellarWalletManager")
                {
                    walletManager = mb;
                    break;
                }
            }
            if (walletManager == null) return;

            var type = walletManager.GetType();
            var hasWalletMethod = type.GetMethod("HasWallet");
            var getCurrentWalletMethod = type.GetMethod("GetCurrentWallet");
            var getBalanceMethod = type.GetMethod("GetBalance");
            if (hasWalletMethod == null || getCurrentWalletMethod == null) return;

            try
            {
                var hasWallet = (bool)hasWalletMethod.Invoke(walletManager, null);
                if (!hasWallet) return;
                var wallet = getCurrentWalletMethod.Invoke(walletManager, null);
                if (wallet == null) return;
                var wType = wallet.GetType();
                var pkField = wType.GetField("publicKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                var pkProp = wType.GetProperty("publicKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                var publicKey = pkField != null ? pkField.GetValue(wallet) as string : (pkProp != null ? pkProp.GetValue(wallet) as string : null);
                if (!string.IsNullOrEmpty(publicKey))
                {
                    UpdateTopRightWalletLabel(publicKey);
                    if (getBalanceMethod != null)
                    {
                        _ = RequestBalanceAsync(walletManager, getBalanceMethod);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PasskeyUIManager] Failed to refresh wallet/balance: {e.Message}");
            }
        }

        private void UpdateTopRightWalletLabel(string publicKey)
        {
            if (topRightWalletText == null) return;
            if (string.IsNullOrEmpty(publicKey))
            {
                topRightWalletText.text = "Wallet: Creating...";
                return;
            }
            string shortAddress = publicKey.Length > 12 ? publicKey.Substring(0, 6) + "..." + publicKey.Substring(publicKey.Length - 6) : publicKey;
            topRightWalletText.text = $"Wallet: {shortAddress}";
        }

        private void UpdateTopRightBalanceLabel(string text)
        {
            if (topRightBalanceText == null) return;
            topRightBalanceText.text = text ?? string.Empty;
        }
    }
} 