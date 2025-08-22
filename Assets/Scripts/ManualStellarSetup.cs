using UnityEngine;

namespace BlockchainMaps
{
    public class ManualStellarSetup : MonoBehaviour
    {
        [Header("Setup Options")]
        [SerializeField] private bool createOnStart = true;
        [SerializeField] private bool createTestUI = true;

        void Start()
        {
            if (createOnStart)
            {
                SetupStellarComponents();
            }
        }

        [ContextMenu("Setup Stellar Components")]
        public void SetupStellarComponents()
        {
            Debug.Log("[ManualStellarSetup] Setting up Stellar components...");

            // Create StellarApiClient
            if (FindFirstObjectByType<StellarApiClient>() == null)
            {
                GameObject apiClientObj = new GameObject("StellarApiClient");
                apiClientObj.AddComponent<StellarApiClient>();
                Debug.Log("[ManualStellarSetup] Created StellarApiClient");
            }
            else
            {
                Debug.Log("[ManualStellarSetup] StellarApiClient already exists");
            }

            // Create StellarWalletManager
            if (FindFirstObjectByType<StellarWalletManager>() == null)
            {
                GameObject walletManagerObj = new GameObject("StellarWalletManager");
                walletManagerObj.AddComponent<StellarWalletManager>();
                Debug.Log("[ManualStellarSetup] Created StellarWalletManager");
            }
            else
            {
                Debug.Log("[ManualStellarSetup] StellarWalletManager already exists");
            }

            // Create test UI if requested
            if (createTestUI && FindFirstObjectByType<StellarBackendTester>() == null)
            {
                CreateSimpleTestUI();
            }

            Debug.Log("[ManualStellarSetup] Setup complete!");
        }

        private void CreateSimpleTestUI()
        {
            Debug.Log("[ManualStellarSetup] Creating simple test UI...");

            // Create Canvas if needed
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("TestCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // Create test panel
            GameObject testPanel = new GameObject("TestPanel");
            testPanel.transform.SetParent(canvas.transform, false);
            
            var panelRect = testPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 300);
            panelRect.anchoredPosition = Vector2.zero;

            var panelImage = testPanel.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0, 0, 0, 0.9f);

            // Create title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(testPanel.transform, false);
            var titleText = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
            titleText.text = "Stellar Backend Test";
            titleText.fontSize = 20;
            titleText.color = Color.white;
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.8f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Create test button
            GameObject buttonObj = new GameObject("TestButton");
            buttonObj.transform.SetParent(testPanel.transform, false);
            var button = buttonObj.AddComponent<UnityEngine.UI.Button>();
            var buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.1f, 0.3f);
            buttonRect.anchorMax = new Vector2(0.9f, 0.5f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            var buttonText = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText == null)
            {
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                buttonText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            }
            
            buttonText.text = "Test Backend Connection";
            buttonText.fontSize = 16;
            buttonText.color = Color.white;
            buttonText.alignment = TMPro.TextAlignmentOptions.Center;
            
            var textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Add click handler
            button.onClick.AddListener(TestBackendConnection);

            Debug.Log("[ManualStellarSetup] Simple test UI created");
        }

        private async void TestBackendConnection()
        {
            Debug.Log("[ManualStellarSetup] Testing backend connection...");
            
            var apiClient = FindFirstObjectByType<StellarApiClient>();
            if (apiClient == null)
            {
                Debug.LogError("[ManualStellarSetup] StellarApiClient not found!");
                return;
            }

            try
            {
                var accountResponse = await apiClient.CreateAccount();
                if (accountResponse != null)
                {
                    Debug.Log($"[ManualStellarSetup] SUCCESS! Account created: {accountResponse.publicKey}");
                }
                else
                {
                    Debug.LogError("[ManualStellarSetup] Failed to create account");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ManualStellarSetup] Error: {e.Message}");
            }
        }
    }
} 