using UnityEngine;

namespace BlockchainMaps
{
    [DefaultExecutionOrder(-200)] // Execute very early
    public class AutoStellarSetup : MonoBehaviour
    {
        private static bool isSetupComplete = false;
        
        void Awake()
        {
            if (!isSetupComplete)
            {
                SetupStellarBackend();
                isSetupComplete = true;
            }
        }
        
        private void SetupStellarBackend()
        {
            Debug.Log("=== Auto Stellar Setup Starting ===");
            
            // Create StellarApiClient if it doesn't exist
            if (StellarApiClient.Instance == null)
            {
                Debug.Log("Creating StellarApiClient...");
                var apiClientObj = new GameObject("StellarApiClient");
                apiClientObj.AddComponent<StellarApiClient>();
                DontDestroyOnLoad(apiClientObj);
                Debug.Log("StellarApiClient created successfully");
            }
            else
            {
                Debug.Log("StellarApiClient already exists");
            }
            
            // Create StellarWalletManager if it doesn't exist
            if (StellarWalletManager.Instance == null)
            {
                Debug.Log("Creating StellarWalletManager...");
                var walletManagerObj = new GameObject("StellarWalletManager");
                walletManagerObj.AddComponent<StellarWalletManager>();
                DontDestroyOnLoad(walletManagerObj);
                Debug.Log("StellarWalletManager created successfully");
            }
            else
            {
                Debug.Log("StellarWalletManager already exists");
            }
            
            // Create QRPaymentProcessor if it doesn't exist
            var existingProcessor = FindFirstObjectByType<MonoBehaviour>();
            bool processorFound = false;
            
            if (existingProcessor != null && existingProcessor.GetType().Name == "QRPaymentProcessor")
            {
                Debug.Log("QRPaymentProcessor already exists");
                processorFound = true;
            }
            
            if (!processorFound)
            {
                Debug.Log("Creating QRPaymentProcessor...");
                var processorObj = new GameObject("QRPaymentProcessor");
                processorObj.AddComponent<QRPaymentProcessor>();
                DontDestroyOnLoad(processorObj);
                Debug.Log("QRPaymentProcessor created successfully");
            }
            
            Debug.Log("=== Auto Stellar Setup Complete ===");
        }
    }
} 