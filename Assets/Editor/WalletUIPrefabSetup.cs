using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using BlockchainMaps.Wallet;

namespace BlockchainMaps.Wallet.Editor
{
    public class WalletUIPrefabSetup
    {
        private const string PREFAB_PATH = "Assets/Prefabs/WalletUI.prefab";

        [MenuItem("Tools/Create Wallet UI")]
        public static void CreateWalletUI()
        {
            // Create Canvas
            GameObject canvas = new GameObject("WalletCanvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();

            // Create main panel
            GameObject mainPanel = CreateUIElement("MainPanel", canvas);
            RectTransform mainPanelRect = mainPanel.GetComponent<RectTransform>();
            mainPanelRect.anchorMin = new Vector2(1, 1);
            mainPanelRect.anchorMax = new Vector2(1, 1);
            mainPanelRect.pivot = new Vector2(1, 1);
            mainPanelRect.anchoredPosition = new Vector2(-20, -20);
            mainPanelRect.sizeDelta = new Vector2(300, 400);

            Image mainPanelImage = mainPanel.AddComponent<Image>();
            mainPanelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Create UI elements
            GameObject balanceText = CreateTextElement("BalanceText", mainPanel, "Balance: 0 XLM");
            GameObject addressText = CreateTextElement("AddressText", mainPanel, "Not Connected");
            GameObject statusText = CreateTextElement("StatusText", mainPanel, "");
            
            // Create buttons
            GameObject connectButton = CreateButton("ConnectButton", mainPanel, "Connect Wallet");
            GameObject disconnectButton = CreateButton("DisconnectButton", mainPanel, "Disconnect");
            GameObject generateQRButton = CreateButton("GenerateQRButton", mainPanel, "Generate QR");
            GameObject scanQRButton = CreateButton("ScanQRButton", mainPanel, "Scan QR");

            // Create QR display
            GameObject qrDisplay = CreateUIElement("QRDisplay", mainPanel);
            RawImage qrImage = qrDisplay.AddComponent<RawImage>();
            qrImage.color = Color.white;
            RectTransform qrRect = qrDisplay.GetComponent<RectTransform>();
            qrRect.sizeDelta = new Vector2(200, 200);

            // Create payment panel
            GameObject paymentPanel = CreateUIElement("PaymentPanel", mainPanel);
            Image paymentPanelImage = paymentPanel.AddComponent<Image>();
            paymentPanelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            RectTransform paymentRect = paymentPanel.GetComponent<RectTransform>();
            paymentRect.sizeDelta = new Vector2(280, 200);

            // Payment panel elements
            GameObject paymentAmountText = CreateTextElement("PaymentAmountText", paymentPanel, "Amount: 0 XLM");
            GameObject confirmButton = CreateButton("ConfirmButton", paymentPanel, "Confirm");
            GameObject cancelButton = CreateButton("CancelButton", paymentPanel, "Cancel");

            // Layout elements
            float padding = 10f;
            float buttonHeight = 40f;
            float textHeight = 30f;

            // Position elements
            RectTransform balanceRect = balanceText.GetComponent<RectTransform>();
            RectTransform addressRect = addressText.GetComponent<RectTransform>();
            RectTransform statusRect = statusText.GetComponent<RectTransform>();
            RectTransform connectRect = connectButton.GetComponent<RectTransform>();
            RectTransform disconnectRect = disconnectButton.GetComponent<RectTransform>();
            RectTransform generateQRRect = generateQRButton.GetComponent<RectTransform>();
            RectTransform scanQRRect = scanQRButton.GetComponent<RectTransform>();

            balanceRect.anchoredPosition = new Vector2(padding, -padding);
            addressRect.anchoredPosition = new Vector2(padding, -(padding + textHeight));
            statusRect.anchoredPosition = new Vector2(padding, -(padding + 2 * textHeight));
            connectRect.anchoredPosition = new Vector2(padding, -(padding + 3 * textHeight));
            disconnectRect.anchoredPosition = new Vector2(padding + 150, -(padding + 3 * textHeight));
            generateQRRect.anchoredPosition = new Vector2(padding, -(padding + 4 * textHeight));
            scanQRRect.anchoredPosition = new Vector2(padding + 150, -(padding + 4 * textHeight));
            qrRect.anchoredPosition = new Vector2(padding + 100, -(padding + 6 * textHeight));

            // Add WalletUIManager component
            WalletUIManager uiManager = canvas.AddComponent<WalletUIManager>();
            
            // Assign references
            uiManager.qrCodeDisplay = qrImage;
            uiManager.balanceText = balanceText.GetComponent<TextMeshProUGUI>();
            uiManager.addressText = addressText.GetComponent<TextMeshProUGUI>();
            uiManager.statusText = statusText.GetComponent<TextMeshProUGUI>();
            uiManager.connectWalletButton = connectButton.GetComponent<Button>();
            uiManager.disconnectWalletButton = disconnectButton.GetComponent<Button>();
            uiManager.generateQRButton = generateQRButton.GetComponent<Button>();
            uiManager.scanQRButton = scanQRButton.GetComponent<Button>();

            // Create prefab
            if (!System.IO.Directory.Exists("Assets/Prefabs"))
            {
                System.IO.Directory.CreateDirectory("Assets/Prefabs");
            }

            // Save the prefab
            #if UNITY_2018_3_OR_NEWER
            bool success = PrefabUtility.SaveAsPrefabAsset(canvas, PREFAB_PATH);
            #else
            bool success = PrefabUtility.CreatePrefab(PREFAB_PATH, canvas) != null;
            #endif

            if (success)
            {
                Debug.Log("WalletUI prefab created successfully at " + PREFAB_PATH);
            }
            else
            {
                Debug.LogError("Failed to create WalletUI prefab!");
            }

            // Cleanup
            Object.DestroyImmediate(canvas);
        }

        private static GameObject CreateUIElement(string name, GameObject parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            return obj;
        }

        private static GameObject CreateTextElement(string name, GameObject parent, string defaultText)
        {
            GameObject obj = CreateUIElement(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = defaultText;
            tmp.color = Color.white;
            tmp.fontSize = 14;
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 30);
            return obj;
        }

        private static GameObject CreateButton(string name, GameObject parent, string text)
        {
            GameObject obj = CreateUIElement(name, parent);
            Image image = obj.AddComponent<Image>();
            Button button = obj.AddComponent<Button>();
            
            // Create text child
            GameObject textObj = CreateUIElement(name + "Text", obj);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = Color.white;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;

            // Position text
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Set button size
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140, 40);

            // Set button colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1);
            button.colors = colors;

            return obj;
        }
    }
} 