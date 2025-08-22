using UnityEngine;
using System.Threading.Tasks;

namespace BlockchainMaps
{
    public class WalletTestHelper : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool verboseLogging = true;
        
        public bool VerboseLogging => verboseLogging;

        [ContextMenu("Test Complete Wallet Flow")]
        public async void TestCompleteWalletFlow()
        {
            Debug.Log("=== TESTING COMPLETE WALLET FLOW ===");
            
            try
            {
                // Test 1: Check if StellarApiClient exists
                Debug.Log("1. Checking StellarApiClient...");
                var apiClient = StellarApiClient.Instance;
                if (apiClient == null)
                {
                    Debug.LogError("❌ StellarApiClient.Instance is null");
                    return;
                }
                Debug.Log("✅ StellarApiClient found");

                // Test 2: Check if StellarWalletManager exists
                Debug.Log("2. Checking StellarWalletManager...");
                var walletManager = StellarWalletManager.Instance;
                if (walletManager == null)
                {
                    Debug.LogError("❌ StellarWalletManager.Instance is null");
                    return;
                }
                Debug.Log("✅ StellarWalletManager found");

                // Test 3: Test account creation
                Debug.Log("3. Testing account creation...");
                var accountResponse = await apiClient.CreateAccount();
                if (accountResponse == null)
                {
                    Debug.LogError("❌ Account creation failed - response is null");
                    return;
                }
                
                if (string.IsNullOrEmpty(accountResponse.publicKey) || string.IsNullOrEmpty(accountResponse.secret))
                {
                    Debug.LogError("❌ Account creation failed - missing publicKey or secret");
                    Debug.LogError($"   PublicKey: {accountResponse.publicKey}");
                    Debug.LogError($"   HasSecret: {!string.IsNullOrEmpty(accountResponse.secret)}");
                    return;
                }
                Debug.Log($"✅ Account created successfully: {accountResponse.publicKey}");

                // Test 4: Test wallet creation
                Debug.Log("4. Testing wallet creation...");
                var walletData = await walletManager.CreateWallet();
                if (walletData == null)
                {
                    Debug.LogError("❌ Wallet creation failed - wallet data is null");
                    return;
                }
                
                if (string.IsNullOrEmpty(walletData.publicKey))
                {
                    Debug.LogError("❌ Wallet creation failed - missing public key");
                    return;
                }
                Debug.Log($"✅ Wallet created successfully: {walletData.publicKey}");

                // Test 5: Verify wallet persistence
                Debug.Log("5. Testing wallet persistence...");
                bool hasWallet = walletManager.HasWallet();
                if (!hasWallet)
                {
                    Debug.LogError("❌ Wallet persistence failed - HasWallet returns false");
                    return;
                }
                Debug.Log("✅ Wallet persistence verified");

                Debug.Log("=== ALL TESTS PASSED ===");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Test failed with exception: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }

        [ContextMenu("Test API Only")]
        public async void TestApiOnly()
        {
            Debug.Log("=== TESTING API ONLY ===");
            
            try
            {
                var apiClient = StellarApiClient.Instance;
                if (apiClient == null)
                {
                    Debug.LogError("❌ StellarApiClient.Instance is null");
                    return;
                }

                var accountResponse = await apiClient.CreateAccount();
                if (accountResponse == null)
                {
                    Debug.LogError("❌ API call failed - response is null");
                    return;
                }
                
                Debug.Log($"✅ API call successful: {accountResponse.publicKey}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ API test failed: {e.Message}");
            }
        }

        [ContextMenu("Test Wallet Manager Only")]
        public async void TestWalletManagerOnly()
        {
            Debug.Log("=== TESTING WALLET MANAGER ONLY ===");
            
            try
            {
                var walletManager = StellarWalletManager.Instance;
                if (walletManager == null)
                {
                    Debug.LogError("❌ StellarWalletManager.Instance is null");
                    return;
                }

                var walletData = await walletManager.CreateWallet();
                if (walletData == null)
                {
                    Debug.LogError("❌ Wallet creation failed - wallet data is null");
                    return;
                }
                
                Debug.Log($"✅ Wallet creation successful: {walletData.publicKey}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Wallet manager test failed: {e.Message}");
            }
        }

        [ContextMenu("Clear All Wallet Data")]
        public void ClearAllWalletData()
        {
            Debug.Log("=== CLEARING ALL WALLET DATA ===");
            
            try
            {
                // Clear PlayerPrefs
                PlayerPrefs.DeleteKey("StellarWalletData");
                PlayerPrefs.Save();
                Debug.Log("✅ PlayerPrefs cleared");

                // Clear wallet manager
                var walletManager = StellarWalletManager.Instance;
                if (walletManager != null)
                {
                    var clearMethod = walletManager.GetType().GetMethod("ClearWallet");
                    if (clearMethod != null)
                    {
                        clearMethod.Invoke(walletManager, null);
                        Debug.Log("✅ Wallet manager cleared");
                    }
                }

                Debug.Log("=== ALL WALLET DATA CLEARED ===");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to clear wallet data: {e.Message}");
            }
        }
    }
} 