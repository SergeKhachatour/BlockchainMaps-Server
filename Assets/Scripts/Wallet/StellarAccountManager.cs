using UnityEngine;
using System;
using System.Threading.Tasks;
using BlockchainMaps.Soroban;

namespace BlockchainMaps.Wallet
{
    public class StellarAccountManager : MonoBehaviour
    {
        private static StellarAccountManager instance;
        public static StellarAccountManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<StellarAccountManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("StellarAccountManager");
                        instance = go.AddComponent<StellarAccountManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        [SerializeField] private SorobanConfig sorobanConfig;
        private SorobanManager sorobanManager;
        private string currentWalletAddress;
        private bool isInitialized = false;
        private decimal currentBalance = 0;

        public string CurrentWalletAddress => currentWalletAddress;
        public bool IsInitialized => isInitialized;
        public event Action<string> OnWalletConnected;
        public event Action OnWalletDisconnected;
        public event Action<decimal> OnBalanceUpdated;
        public event Action<string> OnTransactionCompleted;
        public event Action<string> OnError;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            sorobanManager = SorobanManager.Instance;
            if (sorobanConfig == null)
            {
                sorobanConfig = Resources.Load<SorobanConfig>("SorobanConfig");
            }
            Initialize();
        }

        private async void Initialize()
        {
            try
            {
                // The SorobanManager.Instance now handles initialization automatically
                // Just wait for it to be ready
                int maxWaitTime = 10000; // 10 seconds
                int waitTime = 0;
                while (!sorobanManager.IsInitialized() && waitTime < maxWaitTime)
                {
                    await Task.Delay(100);
                    waitTime += 100;
                }
                
                if (!sorobanManager.IsInitialized())
                {
                    Debug.LogWarning("SorobanManager failed to initialize within timeout period, continuing with limited functionality");
                    // Continue with limited functionality instead of throwing an exception
                }
                
                isInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize StellarAccountManager: {e.Message}");
                OnError?.Invoke("Failed to initialize wallet manager");
            }
        }

        public async Task<decimal> GetAccountBalance(string publicKey = null)
        {
            try
            {
                string address = publicKey ?? currentWalletAddress;
                if (string.IsNullOrEmpty(address))
                {
                    throw new Exception("No wallet address provided");
                }

                // Call Soroban to get account info
                string response = await sorobanManager.ExecuteContract(
                    sorobanConfig.tokenContractId,
                    "balance",
                    new object[] { address }
                );

                // Parse balance from response
                decimal balance = decimal.Parse(response);
                if (publicKey == null || publicKey == currentWalletAddress)
                {
                    currentBalance = balance;
                    OnBalanceUpdated?.Invoke(balance);
                }
                return balance;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get account balance: {e.Message}");
                OnError?.Invoke("Failed to get account balance");
                return 0;
            }
        }

        public async Task<string> SendPayment(string toAddress, decimal amount, string memo = "")
        {
            try
            {
                if (!IsWalletConnected())
                {
                    throw new Exception("Wallet not connected");
                }

                if (amount <= 0)
                {
                    throw new Exception("Invalid payment amount");
                }

                if (amount > currentBalance)
                {
                    throw new Exception("Insufficient balance");
                }

                // Prepare payment parameters
                var args = new object[]
                {
                    currentWalletAddress, // from
                    toAddress,           // to
                    amount.ToString(),   // amount
                    memo                 // memo
                };

                // Execute payment through Soroban
                string response = await sorobanManager.ExecuteContract(
                    sorobanConfig.tokenContractId,
                    "transfer",
                    args
                );

                // Update balances
                await GetAccountBalance();
                
                OnTransactionCompleted?.Invoke(response);
                return response;
            }
            catch (Exception e)
            {
                Debug.LogError($"Payment failed: {e.Message}");
                OnError?.Invoke($"Payment failed: {e.Message}");
                throw;
            }
        }

        public void ConnectWallet(string walletAddress)
        {
            if (string.IsNullOrEmpty(walletAddress))
            {
                OnError?.Invoke("Invalid wallet address");
                return;
            }

            currentWalletAddress = walletAddress;
            OnWalletConnected?.Invoke(walletAddress);
            _ = GetAccountBalance(); // Fire and forget balance update
        }

        public void DisconnectWallet()
        {
            currentWalletAddress = null;
            currentBalance = 0;
            OnWalletDisconnected?.Invoke();
        }

        public bool IsWalletConnected()
        {
            return !string.IsNullOrEmpty(currentWalletAddress);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
} 