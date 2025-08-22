using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace BlockchainMaps
{
    public class StellarBackendSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool setupOnStart = true;
        
        private StellarApiClient apiClient;
        private StellarWalletManager walletManager;
        
        void Start()
        {
            if (setupOnStart)
            {
                SetupBackendIntegration();
            }
        }
        
        [ContextMenu("Setup Backend Integration")]
        public void SetupBackendIntegration()
        {
            Debug.Log("=== Setting up Stellar Backend Integration ===");
            
            // Create StellarApiClient if it doesn't exist
            if (FindFirstObjectByType<StellarApiClient>() == null)
            {
                Debug.Log("Creating StellarApiClient...");
                var apiClientObj = new GameObject("StellarApiClient");
                apiClient = apiClientObj.AddComponent<StellarApiClient>();
                DontDestroyOnLoad(apiClientObj);
                Debug.Log("StellarApiClient created successfully");
            }
            else
            {
                Debug.Log("StellarApiClient already exists");
                apiClient = FindFirstObjectByType<StellarApiClient>();
            }
            
            // Create StellarWalletManager if it doesn't exist
            if (FindFirstObjectByType<StellarWalletManager>() == null)
            {
                Debug.Log("Creating StellarWalletManager...");
                var walletManagerObj = new GameObject("StellarWalletManager");
                walletManager = walletManagerObj.AddComponent<StellarWalletManager>();
                DontDestroyOnLoad(walletManagerObj);
                Debug.Log("StellarWalletManager created successfully");
            }
            else
            {
                Debug.Log("StellarWalletManager already exists");
                walletManager = FindFirstObjectByType<StellarWalletManager>();
            }
            
            Debug.Log("=== Backend Integration Setup Complete ===");
        }
        
        [ContextMenu("Clean Up Backend Components")]
        public void CleanUpBackendComponents()
        {
            Debug.Log("=== Cleaning up Backend Components ===");
            
            var apiClient = FindFirstObjectByType<StellarApiClient>();
            if (apiClient != null)
            {
                Debug.Log("Destroying StellarApiClient...");
                DestroyImmediate(apiClient.gameObject);
            }
            
            var walletManager = FindFirstObjectByType<StellarWalletManager>();
            if (walletManager != null)
            {
                Debug.Log("Destroying StellarWalletManager...");
                DestroyImmediate(walletManager.gameObject);
            }
            
            Debug.Log("=== Backend Components Cleanup Complete ===");
        }
        
        [ContextMenu("Test Backend Connection")]
        public async void TestBackendConnection()
        {
            Debug.Log("=== Testing Backend Connection ===");
            
            var apiClient = FindFirstObjectByType<StellarApiClient>();
            if (apiClient == null)
            {
                Debug.LogError("StellarApiClient not found. Please run Setup Backend Integration first.");
                return;
            }
            
            try
            {
                Debug.Log("Testing backend connection...");
                var result = await apiClient.CreateAccount();
                Debug.Log($"Backend test successful! Created account: {result.publicKey}");
                
                // Also test the wallet manager
                var walletManager = FindFirstObjectByType<StellarWalletManager>();
                if (walletManager != null)
                {
                    Debug.Log("Testing StellarWalletManager...");
                    var wallet = await walletManager.CreateWallet();
                    if (wallet != null)
                    {
                        Debug.Log($"Wallet creation test successful! Wallet: {wallet.publicKey}");
                    }
                    else
                    {
                        Debug.LogError("Wallet creation test failed!");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Backend test failed: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }
    }
} 