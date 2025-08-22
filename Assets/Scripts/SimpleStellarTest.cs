using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlockchainMaps
{
    public class SimpleStellarTest : MonoBehaviour
    {
        [Header("Test UI")]
        [SerializeField] private Button testButton;
        [SerializeField] private TMPro.TextMeshProUGUI resultText;

        void Start()
        {
            // Create test button if not assigned
            if (testButton == null)
            {
                CreateTestUI();
            }
        }

        private void CreateTestUI()
        {
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
            GameObject testPanel = new GameObject("SimpleTestPanel");
            testPanel.transform.SetParent(canvas.transform, false);
            
            var panelRect = testPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(300, 200);
            panelRect.anchoredPosition = Vector2.zero;

            var panelImage = testPanel.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0, 0, 0, 0.9f);

            // Create test button
            GameObject buttonObj = new GameObject("TestButton");
            buttonObj.transform.SetParent(testPanel.transform, false);
            testButton = buttonObj.AddComponent<UnityEngine.UI.Button>();
            var buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            var buttonRect = testButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.1f, 0.6f);
            buttonRect.anchorMax = new Vector2(0.9f, 0.8f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            var buttonText = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText == null)
            {
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                buttonText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            }
            
            buttonText.text = "Test Stellar Backend";
            buttonText.fontSize = 16;
            buttonText.color = Color.white;
            buttonText.alignment = TMPro.TextAlignmentOptions.Center;
            
            var textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Create result text
            GameObject resultObj = new GameObject("ResultText");
            resultObj.transform.SetParent(testPanel.transform, false);
            resultText = resultObj.AddComponent<TMPro.TextMeshProUGUI>();
            resultText.text = "Click button to test backend connection...";
            resultText.fontSize = 12;
            resultText.color = Color.white;
            resultText.alignment = TMPro.TextAlignmentOptions.Left;
            resultText.textWrappingMode = TMPro.TextWrappingModes.Normal;
            
            var resultRect = resultText.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0.1f, 0.1f);
            resultRect.anchorMax = new Vector2(0.9f, 0.5f);
            resultRect.offsetMin = Vector2.zero;
            resultRect.offsetMax = Vector2.zero;

            // Add click handler
            testButton.onClick.AddListener(TestBackendConnection);
        }

        public async void TestBackendConnection()
        {
            if (resultText != null)
                resultText.text = "Testing backend connection...";

            Debug.Log("[SimpleStellarTest] Testing backend connection...");

            // Create API client if it doesn't exist
            var apiClient = FindFirstObjectByType<StellarApiClient>();
            if (apiClient == null)
            {
                GameObject apiClientObj = new GameObject("StellarApiClient");
                apiClient = apiClientObj.AddComponent<StellarApiClient>();
                Debug.Log("[SimpleStellarTest] Created StellarApiClient");
            }

            try
            {
                var accountResponse = await apiClient.CreateAccount();
                if (accountResponse != null)
                {
                    string successMessage = $"SUCCESS!\nAccount: {accountResponse.publicKey}\nSecret: {accountResponse.secret.Substring(0, 8)}...";
                    Debug.Log($"[SimpleStellarTest] {successMessage}");
                    if (resultText != null)
                        resultText.text = successMessage;
                }
                else
                {
                    string errorMessage = "ERROR: Failed to create account";
                    Debug.LogError($"[SimpleStellarTest] {errorMessage}");
                    if (resultText != null)
                        resultText.text = errorMessage;
                }
            }
            catch (System.Exception e)
            {
                string errorMessage = $"ERROR: {e.Message}";
                Debug.LogError($"[SimpleStellarTest] {errorMessage}");
                if (resultText != null)
                    resultText.text = errorMessage;
            }
        }
    }
} 