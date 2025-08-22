using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace BlockchainMaps.Authentication
{
    /// <summary>
    /// Helper script to add a wallet address display to the authentication UI
    /// </summary>
    public class WalletAddressDisplay : MonoBehaviour
    {
        [Header("UI Setup")]
        [SerializeField] private TextMeshProUGUI walletAddressText;
        [SerializeField] private PasskeyUIManager passkeyUIManager;
        
        void Start()
        {
            // Find the PasskeyUIManager if not assigned
            if (passkeyUIManager == null)
            {
                passkeyUIManager = FindFirstObjectByType<PasskeyUIManager>();
            }
            
            // Create wallet address text if it doesn't exist
            if (walletAddressText == null)
            {
                CreateWalletAddressText();
            }
            
            // Assign the wallet address text to the PasskeyUIManager
            if (passkeyUIManager != null && walletAddressText != null)
            {
                // Use reflection to set the walletAddressText field
                var field = passkeyUIManager.GetType().GetField("walletAddressText", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(passkeyUIManager, walletAddressText);
                    Debug.Log("Wallet address text assigned to PasskeyUIManager");
                }
            }
        }
        
        private void CreateWalletAddressText()
        {
            // Find the auth panel
            var authPanel = passkeyUIManager?.authPanel;
            if (authPanel == null)
            {
                Debug.LogError("Auth panel not found!");
                return;
            }
            
            // Create a new GameObject for the wallet address text
            GameObject walletTextObj = new GameObject("WalletAddressText");
            walletTextObj.transform.SetParent(authPanel.transform);
            
            // Add TextMeshProUGUI component
            walletAddressText = walletTextObj.AddComponent<TextMeshProUGUI>();
            walletAddressText.text = "";
            walletAddressText.fontSize = 14;
            walletAddressText.color = Color.white;
            walletAddressText.alignment = TextAlignmentOptions.Center;
            walletAddressText.gameObject.SetActive(false); // Hidden initially
            
            // Set up RectTransform
            RectTransform rectTransform = walletTextObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.sizeDelta = new Vector2(0, 30);
            rectTransform.anchoredPosition = new Vector2(0, 10); // Position below log off button
            
            Debug.Log("Wallet address text created successfully");
        }
    }
} 