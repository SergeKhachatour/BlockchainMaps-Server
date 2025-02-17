using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockchainMaps.Authentication
{
    [DefaultExecutionOrder(-1)] // Ensure this runs before other scripts
    public class UIInteractionChecker : MonoBehaviour
    {
        void Awake()
        {
            // Check for EventSystem
            if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
            {
                Debug.Log("Creating missing EventSystem");
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            // Check Canvas and GraphicRaycaster
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    Debug.Log("Adding missing GraphicRaycaster");
                    raycaster = gameObject.AddComponent<GraphicRaycaster>();
                }
            }

            // Verify button setup
            Button[] buttons = GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    if (!buttonImage.raycastTarget)
                    {
                        Debug.Log($"Fixing raycastTarget for button: {button.gameObject.name}");
                        buttonImage.raycastTarget = true;
                    }
                }
            }
        }
    }
} 