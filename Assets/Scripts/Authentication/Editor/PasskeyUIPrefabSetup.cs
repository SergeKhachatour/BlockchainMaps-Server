using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.EventSystems;

namespace BlockchainMaps.Authentication
{
    public class PasskeyUIPrefabSetup
    {
        [MenuItem("GameObject/UI/Blockchain Maps/Passkey Authentication Panel", false, 10)]
        public static void CreatePasskeyAuthPanel()
        {
            // Create the main panel
            GameObject authPanel = new GameObject("PasskeyAuthPanel");
            
            // Add required components
            RectTransform panelRect = authPanel.GetComponent<RectTransform>();
            if (panelRect == null) panelRect = authPanel.AddComponent<RectTransform>();
            
            Canvas canvas = authPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767; // Maximum sorting order to ensure it's on top
            canvas.vertexColorAlwaysGammaSpace = true;

            // Add CanvasGroup to handle input properly
            CanvasGroup canvasGroup = authPanel.AddComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1;

            CanvasScaler scaler = authPanel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GraphicRaycaster raycaster = authPanel.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            raycaster.enabled = true;

            // Set panel to cover entire screen
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;

            // Create background
            GameObject background = CreateUIElement("Background", authPanel);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);
            bgImage.raycastTarget = false;
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Create container directly in the panel
            GameObject container = CreateUIElement("Container", authPanel);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(300, 200);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.localScale = Vector3.one;

            // Create button
            GameObject buttonObj = CreateUIElement("AuthenticateButton", container);
            Button button = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            buttonImage.raycastTarget = true;

            // Add CanvasGroup to button for better interaction handling
            CanvasGroup buttonGroup = buttonObj.AddComponent<CanvasGroup>();
            buttonGroup.interactable = true;
            buttonGroup.blocksRaycasts = true;
            buttonGroup.alpha = 1;

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.2f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.2f);
            buttonRect.sizeDelta = new Vector2(200, 40);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.localScale = Vector3.one;

            // Create button text
            GameObject buttonText = CreateUIElement("Text", buttonObj);
            TextMeshProUGUI btnText = buttonText.AddComponent<TextMeshProUGUI>();
            btnText.text = "Authenticate";
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontSize = 20;
            btnText.raycastTarget = false;
            RectTransform btnTextRect = buttonText.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            btnTextRect.localScale = Vector3.one;

            // Create status icon
            GameObject statusIcon = CreateUIElement("StatusIcon", container);
            Image icon = statusIcon.AddComponent<Image>();
            icon.color = Color.red;
            icon.raycastTarget = false;
            RectTransform iconRect = statusIcon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1);
            iconRect.anchorMax = new Vector2(0.5f, 1);
            iconRect.anchoredPosition = new Vector2(0, -30);
            iconRect.sizeDelta = new Vector2(20, 20);

            // Create status text
            GameObject statusText = CreateUIElement("StatusText", container);
            TextMeshProUGUI text = statusText.AddComponent<TextMeshProUGUI>();
            text.text = "Not Authenticated";
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 24;
            text.raycastTarget = false;
            RectTransform textRect = statusText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0.6f);
            textRect.anchorMax = new Vector2(1, 0.8f);
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            // Set up button colors and transition
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            colors.selectedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = buttonImage;
            button.interactable = true;

            // Add test click handler with more detailed logging
            button.onClick.AddListener(() => {
                Debug.Log($"[PasskeyUI] Button clicked at {Time.time}");
                Debug.Log($"[PasskeyUI] Button state: interactable={button.interactable}, raycastTarget={buttonImage.raycastTarget}");
                Debug.Log($"[PasskeyUI] CanvasGroup state: interactable={buttonGroup.interactable}, blocksRaycasts={buttonGroup.blocksRaycasts}");
            });

            // Ensure we have an EventSystem
            EventSystem existingEventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (existingEventSystem == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                var inputModule = eventSystem.AddComponent<StandaloneInputModule>();
                inputModule.enabled = true;
            }
            else
            {
                Debug.Log($"[PasskeyUI] Using existing EventSystem: {existingEventSystem.gameObject.name}");
                var inputModule = existingEventSystem.GetComponent<StandaloneInputModule>();
                if (inputModule != null)
                {
                    inputModule.enabled = true;
                    Debug.Log($"[PasskeyUI] StandaloneInputModule is {(inputModule.enabled ? "enabled" : "disabled")}");
                }
            }

            // Create the UI manager
            PasskeyUIManager uiManager = authPanel.AddComponent<PasskeyUIManager>();
            
            // Use SerializedObject to set references
            var serializedManager = new SerializedObject(uiManager);
            serializedManager.Update();

            var authPanelProp = serializedManager.FindProperty("authPanel");
            var buttonProp = serializedManager.FindProperty("authenticateButton");
            var textProp = serializedManager.FindProperty("statusText");
            var iconProp = serializedManager.FindProperty("statusIcon");

            authPanelProp.objectReferenceValue = background;
            buttonProp.objectReferenceValue = button;
            textProp.objectReferenceValue = text;
            iconProp.objectReferenceValue = icon;

            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            // Select the created panel
            Selection.activeGameObject = authPanel;
            
            Debug.Log("[PasskeyUI] Authentication Panel created successfully!");
            Debug.Log($"[PasskeyUI] Button setup complete: {buttonObj.name}, Interactable: {button.interactable}");
        }

        static GameObject CreateUIElement(string name, GameObject parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }
    }

    public class TempReferenceHolder : ScriptableObject
    {
        public GameObject authPanel;
        public Button authenticateButton;
        public TextMeshProUGUI statusText;
        public Image statusIcon;
    }
} 