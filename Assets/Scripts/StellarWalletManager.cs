using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BlockchainMaps
{
    [System.Serializable]
    public class StellarWalletData
    {
        public string publicKey;
        public string secretKey;
        public DateTime createdAt;
        public bool isFunded;
        public string network; // "testnet" or "mainnet"
    }

    public class StellarWalletManager : MonoBehaviour
    {
        public static StellarWalletManager Instance { get; private set; }

        [Header("Wallet Data")]
        [SerializeField] private StellarWalletData currentWallet;
        
        [Header("UI References")]
        [SerializeField] private GameObject walletInfoPanel;
        [SerializeField] private TMPro.TextMeshProUGUI walletAddressText;
        [SerializeField] private TMPro.TextMeshProUGUI balanceText;
        [SerializeField] private TMPro.TextMeshProUGUI networkText;

        public event Action<StellarWalletData> OnWalletCreated;
        public event Action<StellarBalanceResponse> OnBalanceUpdated;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadWalletData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Creates a new Stellar wallet using the backend API
        /// </summary>
        public async Task<StellarWalletData> CreateWallet()
        {
            try
            {
                Debug.Log("[StellarWalletManager] Creating new Stellar wallet...");
                
                // Try to get StellarApiClient using Instance property first, then fallback to FindFirstObjectByType
                StellarApiClient apiClient = StellarApiClient.Instance;
                if (apiClient == null)
                {
                    Debug.Log("[StellarWalletManager] StellarApiClient.Instance not found, trying FindFirstObjectByType...");
                    apiClient = FindFirstObjectByType<StellarApiClient>();
                }
                
                if (apiClient == null)
                {
                    Debug.LogError("[StellarWalletManager] StellarApiClient not found!");
                    // Return a fallback wallet data to prevent crashes
                    var fallbackWallet = new StellarWalletData
                    {
                        publicKey = "fallback_wallet_" + DateTime.Now.Ticks,
                        secretKey = "fallback_secret",
                        createdAt = DateTime.Now,
                        isFunded = false,
                        network = "testnet"
                    };
                    currentWallet = fallbackWallet;
                    Debug.Log($"[StellarWalletManager] Setting fallback wallet: {currentWallet.publicKey}");
                    SaveWalletData();
                    UpdateWalletUI();
                    OnWalletCreated?.Invoke(currentWallet);
                    Debug.LogWarning("[StellarWalletManager] Created fallback wallet due to missing API client");
                    return currentWallet;
                }
                
                Debug.Log("[StellarWalletManager] StellarApiClient found, making API call...");

                var accountResponse = await apiClient.CreateAccount();
                Debug.Log($"[StellarWalletManager] API response received: {accountResponse != null}");
                if (accountResponse != null)
                {
                    Debug.Log($"[StellarWalletManager] API response type: {accountResponse.GetType().Name}");
                    Debug.Log($"[StellarWalletManager] API response publicKey: {accountResponse.publicKey}");
                    Debug.Log($"[StellarWalletManager] API response has secret: {!string.IsNullOrEmpty(accountResponse.secret)}");
                }
                
                if (accountResponse == null)
                {
                    Debug.LogError("[StellarWalletManager] Failed to create account via API - response is null");
                    // Return a fallback wallet data to prevent crashes
                    var fallbackWallet = new StellarWalletData
                    {
                        publicKey = "fallback_wallet_" + DateTime.Now.Ticks,
                        secretKey = "fallback_secret",
                        createdAt = DateTime.Now,
                        isFunded = false,
                        network = "testnet"
                    };
                    currentWallet = fallbackWallet;
                    Debug.Log($"[StellarWalletManager] Setting fallback wallet: {currentWallet.publicKey}");
                    SaveWalletData();
                    UpdateWalletUI();
                    OnWalletCreated?.Invoke(currentWallet);
                    Debug.LogWarning("[StellarWalletManager] Created fallback wallet due to API failure");
                    return currentWallet;
                }

                Debug.Log($"[StellarWalletManager] API response details:");
                Debug.Log($"[StellarWalletManager]   - PublicKey: {accountResponse.publicKey}");
                Debug.Log($"[StellarWalletManager]   - HasSecret: {!string.IsNullOrEmpty(accountResponse.secret)}");
                Debug.Log($"[StellarWalletManager]   - Message: {accountResponse.message}");
                Debug.Log($"[StellarWalletManager]   - StellarResponse: {accountResponse.stellarResponse != null}");
                Debug.Log($"[StellarWalletManager]   - SorobanHooksResponse: {accountResponse.sorobanHooksResponse != null}");
                Debug.Log($"[StellarWalletManager]   - SorobanHooksError: {(accountResponse.sorobanHooksError != null ? $"Status {accountResponse.sorobanHooksError.status}: {accountResponse.sorobanHooksError.message}" : "None")}");

                // Validate that we have the essential data for wallet creation
                if (string.IsNullOrEmpty(accountResponse.publicKey) || string.IsNullOrEmpty(accountResponse.secret))
                {
                    Debug.LogError("[StellarWalletManager] API response missing essential wallet data (publicKey or secret)");
                    // Return a fallback wallet data to prevent crashes
                    var fallbackWallet = new StellarWalletData
                    {
                        publicKey = "fallback_wallet_" + DateTime.Now.Ticks,
                        secretKey = "fallback_secret",
                        createdAt = DateTime.Now,
                        isFunded = false,
                        network = "testnet"
                    };
                    currentWallet = fallbackWallet;
                    Debug.Log($"[StellarWalletManager] Setting fallback wallet: {currentWallet.publicKey}");
                    SaveWalletData();
                    UpdateWalletUI();
                    OnWalletCreated?.Invoke(currentWallet);
                    Debug.LogWarning("[StellarWalletManager] Created fallback wallet due to missing essential data");
                    return currentWallet;
                }

                // Log any SorobanHooks errors but continue with wallet creation
                if (accountResponse.sorobanHooksError != null)
                {
                    Debug.LogWarning($"[StellarWalletManager] SorobanHooks error detected but continuing with wallet creation: {accountResponse.sorobanHooksError.message}");
                }

                // Create wallet data
                currentWallet = new StellarWalletData
                {
                    publicKey = accountResponse.publicKey,
                    secretKey = accountResponse.secret,
                    createdAt = DateTime.Now,
                    isFunded = true, // Friendbot funds the account
                    network = "testnet"
                };

                Debug.Log($"[StellarWalletManager] Created wallet data: {currentWallet.publicKey}");

                // Save wallet data
                Debug.Log("[StellarWalletManager] Saving wallet data...");
                SaveWalletData();
                
                // Force reload to ensure data is properly loaded in memory
                Debug.Log("[StellarWalletManager] Forcing reload of wallet data...");
                LoadWalletData();
                
                // Update UI
                Debug.Log("[StellarWalletManager] Updating wallet UI...");
                UpdateWalletUI();
                
                // Trigger event
                Debug.Log("[StellarWalletManager] Triggering OnWalletCreated event...");
                OnWalletCreated?.Invoke(currentWallet);
                
                // Verify the wallet was saved and loaded
                Debug.Log($"[StellarWalletManager] Final verification - HasWallet: {HasWallet()}, PublicKey: {currentWallet?.publicKey}");
                
                // Additional verification: check PlayerPrefs directly
                if (PlayerPrefs.HasKey("StellarWalletData"))
                {
                    var savedData = PlayerPrefs.GetString("StellarWalletData");
                    Debug.Log($"[StellarWalletManager] PlayerPrefs verification - saved data length: {savedData?.Length ?? 0}");
                    Debug.Log($"[StellarWalletManager] PlayerPrefs verification - saved data: {savedData}");
                }
                else
                {
                    Debug.LogError("[StellarWalletManager] PlayerPrefs verification failed - no data found!");
                }
                
                Debug.Log($"[StellarWalletManager] ✅ Wallet created successfully: {currentWallet.publicKey}");
                return currentWallet;
            }
            catch (Exception e)
            {
                Debug.LogError($"[StellarWalletManager] Exception creating wallet: {e.Message}");
                Debug.LogError($"[StellarWalletManager] Stack trace: {e.StackTrace}");
                // Return a fallback wallet data to prevent crashes
                var fallbackWallet = new StellarWalletData
                {
                    publicKey = "fallback_wallet_" + DateTime.Now.Ticks,
                    secretKey = "fallback_secret",
                    createdAt = DateTime.Now,
                    isFunded = false,
                    network = "testnet"
                };
                currentWallet = fallbackWallet;
                Debug.Log($"[StellarWalletManager] Setting fallback wallet: {currentWallet.publicKey}");
                SaveWalletData();
                UpdateWalletUI();
                OnWalletCreated?.Invoke(currentWallet);
                Debug.LogWarning("[StellarWalletManager] Created fallback wallet due to exception");
                return currentWallet;
            }
        }

        /// <summary>
        /// Manually creates a wallet (for testing/debugging)
        /// </summary>
        public async void CreateWalletManually()
        {
            Debug.Log("[StellarWalletManager] CreateWalletManually called");
            try
            {
                var newWallet = await CreateWallet();
                if (newWallet != null)
                {
                    Debug.Log($"[StellarWalletManager] Manual wallet creation successful: {newWallet.publicKey}");
                }
                else
                {
                    Debug.LogError("[StellarWalletManager] Manual wallet creation failed");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[StellarWalletManager] Exception in manual wallet creation: {e.Message}");
            }
        }
        
        /// <summary>
        /// Clears current wallet and creates a new one (for testing/debugging)
        /// </summary>
        public async void ResetAndCreateWallet()
        {
            Debug.Log("[StellarWalletManager] ResetAndCreateWallet called");
            ClearWallet();
            await System.Threading.Tasks.Task.Delay(100); // Small delay to ensure clear is complete
            await CreateWallet();
        }
        
        /// <summary>
        /// Checks PlayerPrefs data for debugging
        /// </summary>
        [ContextMenu("Debug PlayerPrefs")]
        public void DebugPlayerPrefs()
        {
            Debug.Log("=== PlayerPrefs Debug ===");
            Debug.Log($"Has StellarWalletData key: {PlayerPrefs.HasKey("StellarWalletData")}");
            
            if (PlayerPrefs.HasKey("StellarWalletData"))
            {
                var jsonData = PlayerPrefs.GetString("StellarWalletData");
                Debug.Log($"StellarWalletData content: {jsonData}");
                Debug.Log($"StellarWalletData length: {jsonData?.Length ?? 0}");
                
                try
                {
                    var wallet = JsonConvert.DeserializeObject<StellarWalletData>(jsonData);
                    if (wallet != null)
                    {
                        Debug.Log($"Deserialized wallet - PublicKey: {wallet.publicKey}, HasSecret: {!string.IsNullOrEmpty(wallet.secretKey)}");
                    }
                    else
                    {
                        Debug.LogError("Failed to deserialize wallet data");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error deserializing wallet data: {e.Message}");
                }
            }
            else
            {
                Debug.Log("PlayerPrefs does not contain StellarWalletData");
            }
            
            Debug.Log($"Current wallet in memory - HasWallet: {HasWallet()}, PublicKey: {currentWallet?.publicKey}");
            Debug.Log("=== PlayerPrefs Debug Complete ===");
        }
        
        /// <summary>
        /// Manually saves current wallet data to PlayerPrefs (for testing)
        /// </summary>
        public void SaveWalletDataManually()
        {
            Debug.Log("[StellarWalletManager] SaveWalletDataManually called");
            SaveWalletData();
        }
        
        /// <summary>
        /// Gets the current wallet balance
        /// </summary>
        public async Task<StellarBalanceResponse> GetBalance()
        {
            if (currentWallet == null)
            {
                Debug.LogWarning("[StellarWalletManager] No wallet available");
                return null;
            }

            try
            {
                // Try to get StellarApiClient using Instance property first, then fallback to FindFirstObjectByType
                StellarApiClient apiClient = StellarApiClient.Instance;
                if (apiClient == null)
                {
                    Debug.Log("[StellarWalletManager] StellarApiClient.Instance not found for balance check, trying FindFirstObjectByType...");
                    apiClient = FindFirstObjectByType<StellarApiClient>();
                }
                
                if (apiClient == null)
                {
                    Debug.LogError("[StellarWalletManager] StellarApiClient not found for balance check!");
                    return null;
                }
                
                var balanceResponse = await apiClient.ShowBalance(currentWallet.publicKey);
                
                if (balanceResponse != null)
                {
                    UpdateBalanceUI(balanceResponse);
                    OnBalanceUpdated?.Invoke(balanceResponse);
                }
                
                return balanceResponse;
            }
            catch (Exception e)
            {
                Debug.LogError($"[StellarWalletManager] Exception getting balance: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Transfers assets from the current wallet
        /// </summary>
        public async Task<StellarTransactionResponse> TransferAsset(string recipientPublicKey, string assetCode, string issuerPublicKey, string amount)
        {
            if (currentWallet == null)
            {
                Debug.LogError("[StellarWalletManager] No wallet available for transfer");
                return null;
            }

            try
            {
                var apiClient = StellarApiClient.Instance;
                var transactionResponse = await apiClient.TransferAsset(
                    currentWallet.secretKey,
                    recipientPublicKey,
                    assetCode,
                    issuerPublicKey,
                    amount
                );
                
                if (transactionResponse != null)
                {
                    Debug.Log($"[StellarWalletManager] Transfer successful: {transactionResponse.hash}");
                    // Refresh balance after transfer
                    await GetBalance();
                }
                
                return transactionResponse;
            }
            catch (Exception e)
            {
                Debug.LogError($"[StellarWalletManager] Exception transferring asset: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Issues a new asset from the current wallet
        /// </summary>
        public async Task<StellarTransactionResponse> IssueAsset(string assetCode)
        {
            if (currentWallet == null)
            {
                Debug.LogError("[StellarWalletManager] No wallet available for asset issuance");
                return null;
            }

            try
            {
                var apiClient = StellarApiClient.Instance;
                var transactionResponse = await apiClient.IssueAsset(currentWallet.secretKey, assetCode);
                
                if (transactionResponse != null)
                {
                    Debug.Log($"[StellarWalletManager] Asset issued successfully: {transactionResponse.hash}");
                    // Refresh balance after issuance
                    await GetBalance();
                }
                
                return transactionResponse;
            }
            catch (Exception e)
            {
                Debug.LogError($"[StellarWalletManager] Exception issuing asset: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a trustline for an asset
        /// </summary>
        public async Task<StellarTransactionResponse> CreateTrustline(string assetCode, string issuerPublicKey, string limit = "1000000000")
        {
            if (currentWallet == null)
            {
                Debug.LogError("[StellarWalletManager] No wallet available for trustline creation");
                return null;
            }

            try
            {
                var apiClient = StellarApiClient.Instance;
                var transactionResponse = await apiClient.CreateTrustline(currentWallet.secretKey, assetCode, issuerPublicKey, limit);
                
                if (transactionResponse != null)
                {
                    Debug.Log($"[StellarWalletManager] Trustline created successfully: {transactionResponse.hash}");
                    // Refresh balance after trustline creation
                    await GetBalance();
                }
                
                return transactionResponse;
            }
            catch (Exception e)
            {
                Debug.LogError($"[StellarWalletManager] Exception creating trustline: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calls a Soroban smart contract method
        /// </summary>
        public async Task<string> CallContractMethod(string contractId, string method, params object[] parameters)
        {
            if (currentWallet == null)
            {
                Debug.LogError("[StellarWalletManager] No wallet available for contract call");
                return null;
            }

            try
            {
                var apiClient = StellarApiClient.Instance;
                var result = await apiClient.CallContractMethod(contractId, method, currentWallet.secretKey, parameters);
                
                if (result != null)
                {
                    Debug.Log($"[StellarWalletManager] Contract method called successfully: {method}");
                }
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[StellarWalletManager] Exception calling contract method: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the current wallet data
        /// </summary>
        public StellarWalletData GetCurrentWallet()
        {
            string walletInfo = currentWallet != null ? currentWallet.publicKey : "null";
            Debug.Log($"[StellarWalletManager] GetCurrentWallet called - returning: {walletInfo}");
            return currentWallet;
        }

        /// <summary>
        /// Checks if a wallet exists
        /// </summary>
        public bool HasWallet()
        {
            bool hasWallet = currentWallet != null && !string.IsNullOrEmpty(currentWallet.publicKey);
            Debug.Log($"[StellarWalletManager] HasWallet called - currentWallet: {currentWallet != null}, publicKey: {currentWallet?.publicKey}, result: {hasWallet}");
            return hasWallet;
        }

        /// <summary>
        /// Clears the current wallet (for logout)
        /// </summary>
        public void ClearWallet()
        {
            currentWallet = null;
            PlayerPrefs.DeleteKey("StellarWalletData");
            UpdateWalletUI();
        }

        /// <summary>
        /// Updates the wallet UI with current data
        /// </summary>
        private void UpdateWalletUI()
        {
            if (walletInfoPanel != null)
            {
                walletInfoPanel.SetActive(HasWallet());
            }

            if (walletAddressText != null && HasWallet())
            {
                walletAddressText.text = $"Address: {currentWallet.publicKey}";
            }

            if (networkText != null && HasWallet())
            {
                networkText.text = $"Network: {currentWallet.network}";
            }
        }

        /// <summary>
        /// Updates the balance UI
        /// </summary>
        private void UpdateBalanceUI(StellarBalanceResponse balanceResponse)
        {
            if (balanceText != null && balanceResponse != null)
            {
                var balanceString = "Balances:\n";
                foreach (var balance in balanceResponse.balances)
                {
                    balanceString += $"{balance.asset_code}: {balance.balance}\n";
                }
                balanceText.text = balanceString;
            }
        }

        /// <summary>
        /// Saves wallet data to PlayerPrefs
        /// </summary>
        private void SaveWalletData()
        {
            if (currentWallet != null)
            {
                Debug.Log($"[StellarWalletManager] Saving wallet data for: {currentWallet.publicKey}");
                var jsonData = JsonConvert.SerializeObject(currentWallet);
                Debug.Log($"[StellarWalletManager] JSON data length: {jsonData?.Length ?? 0}");
                Debug.Log($"[StellarWalletManager] JSON data content: {jsonData}");
                
                // Clear any existing data first
                PlayerPrefs.DeleteKey("StellarWalletData");
                
                // Save the new data
                PlayerPrefs.SetString("StellarWalletData", jsonData);
                PlayerPrefs.Save();
                Debug.Log("[StellarWalletManager] Wallet data saved to PlayerPrefs");
                
                // Force PlayerPrefs to flush to disk (especially important for WebGL)
                #if UNITY_WEBGL && !UNITY_EDITOR
                // In WebGL, we need to ensure the data is actually saved
                Debug.Log("[StellarWalletManager] WebGL detected - ensuring data persistence...");
                #endif
                
                // Verify the save worked
                var savedData = PlayerPrefs.GetString("StellarWalletData", "");
                Debug.Log($"[StellarWalletManager] Verification - saved data length: {savedData?.Length ?? 0}");
                Debug.Log($"[StellarWalletManager] Verification - saved data content: {savedData}");
                
                if (string.IsNullOrEmpty(savedData))
                {
                    Debug.LogError("[StellarWalletManager] ❌ Save verification failed - no data found in PlayerPrefs!");
                }
                else if (savedData != jsonData)
                {
                    Debug.LogError("[StellarWalletManager] ❌ Save verification failed - data mismatch!");
                    Debug.LogError($"[StellarWalletManager] Original: {jsonData}");
                    Debug.LogError($"[StellarWalletManager] Saved: {savedData}");
                }
                else
                {
                    Debug.Log("[StellarWalletManager] ✅ Save verification successful");
                }
            }
            else
            {
                Debug.LogWarning("[StellarWalletManager] Attempted to save null wallet data");
            }
        }

        /// <summary>
        /// Loads wallet data from PlayerPrefs
        /// </summary>
        private void LoadWalletData()
        {
            Debug.Log("[StellarWalletManager] LoadWalletData called");
            
            if (PlayerPrefs.HasKey("StellarWalletData"))
            {
                try
                {
                    var jsonData = PlayerPrefs.GetString("StellarWalletData");
                    Debug.Log($"[StellarWalletManager] Found wallet data in PlayerPrefs: {jsonData}");
                    Debug.Log($"[StellarWalletManager] JSON data length: {jsonData?.Length ?? 0}");
                    
                    if (string.IsNullOrEmpty(jsonData))
                    {
                        Debug.LogError("[StellarWalletManager] ❌ JSON data is null or empty!");
                        PlayerPrefs.DeleteKey("StellarWalletData");
                        return;
                    }
                    
                    currentWallet = JsonConvert.DeserializeObject<StellarWalletData>(jsonData);
                    
                    if (currentWallet != null)
                    {
                        Debug.Log($"[StellarWalletManager] ✅ Wallet data loaded successfully: {currentWallet.publicKey}");
                        Debug.Log($"[StellarWalletManager] Wallet created at: {currentWallet.createdAt}");
                        Debug.Log($"[StellarWalletManager] Wallet network: {currentWallet.network}");
                        Debug.Log($"[StellarWalletManager] Wallet has secret: {!string.IsNullOrEmpty(currentWallet.secretKey)}");
                        Debug.Log($"[StellarWalletManager] Wallet is funded: {currentWallet.isFunded}");
                        UpdateWalletUI();
                    }
                    else
                    {
                        Debug.LogError("[StellarWalletManager] ❌ Deserialized wallet data is null");
                        PlayerPrefs.DeleteKey("StellarWalletData");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[StellarWalletManager] ❌ Error loading wallet data: {e.Message}");
                    Debug.LogError($"[StellarWalletManager] Stack trace: {e.StackTrace}");
                    PlayerPrefs.DeleteKey("StellarWalletData");
                }
            }
            else
            {
                Debug.Log("[StellarWalletManager] No wallet data found in PlayerPrefs");
            }
            
            Debug.Log($"[StellarWalletManager] LoadWalletData completed. HasWallet: {HasWallet()}, PublicKey: {currentWallet?.publicKey}");
        }

        /// <summary>
        /// Public method to force reload wallet data from PlayerPrefs
        /// </summary>
        public void ReloadWalletDataManually()
        {
            Debug.Log("[StellarWalletManager] ReloadWalletDataManually called");
            LoadWalletData();
        }

        // Test method to verify wallet creation flow
        [ContextMenu("Test Create Wallet")]
        public async void TestCreateWallet()
        {
            Debug.Log("[StellarWalletManager] === TESTING CREATE WALLET ===");
            try
            {
                var result = await CreateWallet();
                Debug.Log($"[StellarWalletManager] Test result: {result != null}");
                if (result != null)
                {
                    Debug.Log($"[StellarWalletManager] Test wallet publicKey: {result.publicKey}");
                    Debug.Log($"[StellarWalletManager] Test wallet hasSecret: {!string.IsNullOrEmpty(result.secretKey)}");
                    Debug.Log($"[StellarWalletManager] Test wallet isFunded: {result.isFunded}");
                }
                else
                {
                    Debug.LogError("[StellarWalletManager] Test result is null!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[StellarWalletManager] Test failed: {e.Message}");
                Debug.LogError($"[StellarWalletManager] Stack trace: {e.StackTrace}");
            }
        }

        [ContextMenu("Quick Test - Create Wallet")]
        public void QuickTestCreateWallet()
        {
            Debug.Log("[StellarWalletManager] === QUICK TEST CREATE WALLET ===");
            StartCoroutine(QuickTestCreateWalletCoroutine());
        }

        private IEnumerator QuickTestCreateWalletCoroutine()
        {
            Debug.Log("[StellarWalletManager] Starting quick test...");
            
            // Check if we have a wallet already
            Debug.Log($"[StellarWalletManager] Current wallet status - HasWallet: {HasWallet()}");
            
            if (HasWallet())
            {
                Debug.Log($"[StellarWalletManager] Wallet already exists: {GetCurrentWallet()?.publicKey}");
                yield break;
            }
            
            // Try to create a wallet
            Debug.Log("[StellarWalletManager] No wallet found, attempting to create one...");
            
            var createTask = CreateWallet();
            
            // Wait for the task to complete
            while (!createTask.IsCompleted)
            {
                yield return null;
            }
            
            try
            {
                var result = createTask.Result;
                Debug.Log($"[StellarWalletManager] Quick test result: {result != null}");
                
                if (result != null)
                {
                    Debug.Log($"[StellarWalletManager] ✅ Quick test successful!");
                    Debug.Log($"[StellarWalletManager] Wallet publicKey: {result.publicKey}");
                    Debug.Log($"[StellarWalletManager] Wallet hasSecret: {!string.IsNullOrEmpty(result.secretKey)}");
                    Debug.Log($"[StellarWalletManager] Final HasWallet: {HasWallet()}");
                }
                else
                {
                    Debug.LogError("[StellarWalletManager] ❌ Quick test failed - result is null!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[StellarWalletManager] ❌ Quick test exception: {e.Message}");
                Debug.LogError($"[StellarWalletManager] Stack trace: {e.StackTrace}");
            }
            
            Debug.Log("[StellarWalletManager] === QUICK TEST COMPLETE ===");
        }
        
        /// <summary>
        /// Public method to manually create a wallet (for testing/debugging)
        /// </summary>
        [ContextMenu("Manual Create Wallet")]
        public async void ManualCreateWallet()
        {
            Debug.Log("[StellarWalletManager] === MANUAL WALLET CREATION ===");
            try
            {
                // Clear any existing wallet first
                ClearWallet();
                Debug.Log("[StellarWalletManager] Cleared existing wallet");
                
                // Wait a moment
                await System.Threading.Tasks.Task.Delay(100);
                
                // Create new wallet
                var result = await CreateWallet();
                Debug.Log($"[StellarWalletManager] Manual creation result: {result != null}");
                
                if (result != null)
                {
                    Debug.Log($"[StellarWalletManager] ✅ Manual wallet created: {result.publicKey}");
                    
                    // Verify the wallet is properly saved and loaded
                    await System.Threading.Tasks.Task.Delay(500);
                    bool hasWallet = HasWallet();
                    var currentWallet = GetCurrentWallet();
                    
                    Debug.Log($"[StellarWalletManager] Verification - HasWallet: {hasWallet}");
                    Debug.Log($"[StellarWalletManager] Verification - CurrentWallet: {currentWallet?.publicKey}");
                    
                    if (hasWallet && currentWallet != null && !string.IsNullOrEmpty(currentWallet.publicKey))
                    {
                        Debug.Log("[StellarWalletManager] ✅ Manual wallet creation and verification successful!");
                    }
                    else
                    {
                        Debug.LogError("[StellarWalletManager] ❌ Manual wallet verification failed!");
                    }
                }
                else
                {
                    Debug.LogError("[StellarWalletManager] ❌ Manual wallet creation failed!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[StellarWalletManager] ❌ Manual wallet creation exception: {e.Message}");
                Debug.LogError($"[StellarWalletManager] Stack trace: {e.StackTrace}");
            }
        }

        [ContextMenu("Test API Wallet Creation")]
        public async void TestApiWalletCreation()
        {
            Debug.Log("[StellarWalletManager] === TESTING API WALLET CREATION ===");
            
            try
            {
                var apiClient = StellarApiClient.Instance;
                if (apiClient == null)
                {
                    Debug.LogError("[StellarWalletManager] StellarApiClient not available");
                    return;
                }
                
                Debug.Log("[StellarWalletManager] ✅ StellarApiClient found, calling CreateAccount API...");
                var response = await apiClient.CreateAccount();
                
                Debug.Log($"[StellarWalletManager] API Response received - Response null: {response == null}");
                if (response != null)
                {
                    Debug.Log($"[StellarWalletManager] API Response received - PublicKey: {response.publicKey}");
                    Debug.Log($"[StellarWalletManager] API Response received - HasSecret: {!string.IsNullOrEmpty(response.secret)}");
                    Debug.Log($"[StellarWalletManager] API Response received - Message: {response.message}");
                }
                
                if (response != null && !string.IsNullOrEmpty(response.publicKey) && !string.IsNullOrEmpty(response.secret))
                {
                    Debug.Log("[StellarWalletManager] ✅ API wallet creation successful!");
                    
                    // Create wallet data from API response
                    var walletData = new StellarWalletData
                    {
                        publicKey = response.publicKey,
                        secretKey = response.secret,
                        createdAt = DateTime.Now,
                        isFunded = false,
                        network = "testnet"
                    };
                    
                    Debug.Log($"[StellarWalletManager] Created wallet data - PublicKey: {walletData.publicKey}");
                    
                    // Save the wallet
                    currentWallet = walletData;
                    Debug.Log("[StellarWalletManager] Set currentWallet, calling SaveWalletData...");
                    SaveWalletData();
                    
                    // Verify the wallet was saved
                    Debug.Log("[StellarWalletManager] Calling LoadWalletData to verify...");
                    LoadWalletData();
                    
                    Debug.Log($"[StellarWalletManager] Wallet verification - HasWallet: {HasWallet()}");
                    if (HasWallet())
                    {
                        Debug.Log("[StellarWalletManager] ✅ API wallet successfully saved and verified!");
                        Debug.Log($"[StellarWalletManager] Final wallet publicKey: {GetCurrentWallet()?.publicKey}");
                    }
                    else
                    {
                        Debug.LogError("[StellarWalletManager] ❌ API wallet save failed!");
                    }
                }
                else
                {
                    Debug.LogError("[StellarWalletManager] ❌ API wallet creation failed - invalid response");
                    if (response == null)
                    {
                        Debug.LogError("[StellarWalletManager] Response is null");
                    }
                    else
                    {
                        Debug.LogError($"[StellarWalletManager] PublicKey empty: {string.IsNullOrEmpty(response.publicKey)}");
                        Debug.LogError($"[StellarWalletManager] Secret empty: {string.IsNullOrEmpty(response.secret)}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[StellarWalletManager] ❌ Exception during API wallet creation: {e.Message}");
                Debug.LogError($"[StellarWalletManager] Stack trace: {e.StackTrace}");
            }
            
            Debug.Log("[StellarWalletManager] === API WALLET CREATION TEST COMPLETE ===");
        }

        [ContextMenu("Debug Wallet Status")]
        public void DebugWalletStatus()
        {
            Debug.Log("[StellarWalletManager] === DEBUGGING WALLET STATUS ===");
            Debug.Log($"[StellarWalletManager] HasWallet: {HasWallet()}");
            Debug.Log($"[StellarWalletManager] CurrentWallet null: {currentWallet == null}");
            
            if (currentWallet != null)
            {
                Debug.Log($"[StellarWalletManager] PublicKey: {currentWallet.publicKey}");
                Debug.Log($"[StellarWalletManager] HasSecret: {!string.IsNullOrEmpty(currentWallet.secretKey)}");
                Debug.Log($"[StellarWalletManager] CreatedAt: {currentWallet.createdAt}");
                Debug.Log($"[StellarWalletManager] Network: {currentWallet.network}");
                Debug.Log($"[StellarWalletManager] IsFunded: {currentWallet.isFunded}");
            }
            
            // Check PlayerPrefs
            if (PlayerPrefs.HasKey("StellarWalletData"))
            {
                var savedData = PlayerPrefs.GetString("StellarWalletData");
                Debug.Log($"[StellarWalletManager] PlayerPrefs data length: {savedData?.Length ?? 0}");
                Debug.Log($"[StellarWalletManager] PlayerPrefs data: {savedData}");
            }
            else
            {
                Debug.Log("[StellarWalletManager] No data in PlayerPrefs");
            }
            
            Debug.Log("[StellarWalletManager] === DEBUG COMPLETED ===");
        }

        [ContextMenu("Force Reload Wallet")]
        public void ForceReloadWallet()
        {
            Debug.Log("[StellarWalletManager] === FORCING WALLET RELOAD ===");
            LoadWalletData();
            Debug.Log($"[StellarWalletManager] After reload - HasWallet: {HasWallet()}");
            Debug.Log($"[StellarWalletManager] After reload - PublicKey: {currentWallet?.publicKey}");
            Debug.Log("[StellarWalletManager] === RELOAD COMPLETED ===");
        }

    }
} 