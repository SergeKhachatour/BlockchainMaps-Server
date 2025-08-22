using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

namespace BlockchainMaps
{
    public class StellarBackendTester : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] public Button createAccountButton;
        [SerializeField] public Button getBalanceButton;
        [SerializeField] public Button transferButton;
        [SerializeField] public Button issueAssetButton;
        [SerializeField] public TMPro.TextMeshProUGUI resultText;
        
        [Header("Test Data")]
        [SerializeField] private string testRecipientPublicKey = "GABC123...";
        [SerializeField] private string testAssetCode = "TEST";
        [SerializeField] private string testIssuerPublicKey = "GXYZ789...";
        [SerializeField] private string testAmount = "100";

        private StellarApiClient apiClient;
        private StellarWalletManager walletManager;

        void Start()
        {
            // Get references
            apiClient = StellarApiClient.Instance;
            walletManager = StellarWalletManager.Instance;

            // Set up button listeners
            if (createAccountButton != null)
                createAccountButton.onClick.AddListener(TestCreateAccount);
            
            if (getBalanceButton != null)
                getBalanceButton.onClick.AddListener(TestGetBalance);
            
            if (transferButton != null)
                transferButton.onClick.AddListener(TestTransfer);
            
            if (issueAssetButton != null)
                issueAssetButton.onClick.AddListener(TestIssueAsset);

            // Check if components are available
            CheckComponents();
        }

        private void CheckComponents()
        {
            if (apiClient == null)
            {
                Debug.LogError("[StellarBackendTester] StellarApiClient not found!");
                LogResult("ERROR: StellarApiClient not found. Make sure it's added to the scene.");
            }
            else
            {
                Debug.Log("[StellarBackendTester] StellarApiClient found");
            }

            if (walletManager == null)
            {
                Debug.LogError("[StellarBackendTester] StellarWalletManager not found!");
                LogResult("ERROR: StellarWalletManager not found. Make sure it's added to the scene.");
            }
            else
            {
                Debug.Log("[StellarBackendTester] StellarWalletManager found");
            }
        }

        public async void TestCreateAccount()
        {
            LogResult("Creating Stellar account...");
            
            if (apiClient == null)
            {
                LogResult("ERROR: StellarApiClient not available");
                return;
            }

            try
            {
                var accountResponse = await apiClient.CreateAccount();
                if (accountResponse != null)
                {
                    LogResult($"SUCCESS: Account created!\nPublic Key: {accountResponse.publicKey}\nSecret: {accountResponse.secret.Substring(0, 8)}...\nMessage: {accountResponse.message}");
                }
                else
                {
                    LogResult("ERROR: Failed to create account");
                }
            }
            catch (System.Exception e)
            {
                LogResult($"ERROR: {e.Message}");
            }
        }

        public async void TestGetBalance()
        {
            if (walletManager == null)
            {
                LogResult("ERROR: StellarWalletManager not available");
                return;
            }

            if (!walletManager.HasWallet())
            {
                LogResult("ERROR: No wallet available. Create an account first.");
                return;
            }

            LogResult("Getting balance...");
            
            try
            {
                var balanceResponse = await walletManager.GetBalance();
                if (balanceResponse != null)
                {
                    var balanceText = "SUCCESS: Balance retrieved!\n";
                    foreach (var balance in balanceResponse.balances)
                    {
                        balanceText += $"{balance.asset_code}: {balance.balance}\n";
                    }
                    LogResult(balanceText);
                }
                else
                {
                    LogResult("ERROR: Failed to get balance");
                }
            }
            catch (System.Exception e)
            {
                LogResult($"ERROR: {e.Message}");
            }
        }

        public async void TestTransfer()
        {
            if (walletManager == null)
            {
                LogResult("ERROR: StellarWalletManager not available");
                return;
            }

            if (!walletManager.HasWallet())
            {
                LogResult("ERROR: No wallet available. Create an account first.");
                return;
            }

            LogResult($"Transferring {testAmount} {testAssetCode} to {testRecipientPublicKey}...");
            
            try
            {
                var transactionResponse = await walletManager.TransferAsset(
                    testRecipientPublicKey,
                    testAssetCode,
                    testIssuerPublicKey,
                    testAmount
                );
                
                if (transactionResponse != null)
                {
                    LogResult($"SUCCESS: Transfer completed!\nTransaction Hash: {transactionResponse.hash}");
                }
                else
                {
                    LogResult("ERROR: Failed to transfer asset");
                }
            }
            catch (System.Exception e)
            {
                LogResult($"ERROR: {e.Message}");
            }
        }

        public async void TestIssueAsset()
        {
            if (walletManager == null)
            {
                LogResult("ERROR: StellarWalletManager not available");
                return;
            }

            if (!walletManager.HasWallet())
            {
                LogResult("ERROR: No wallet available. Create an account first.");
                return;
            }

            LogResult($"Issuing asset: {testAssetCode}...");
            
            try
            {
                var transactionResponse = await walletManager.IssueAsset(testAssetCode);
                
                if (transactionResponse != null)
                {
                    LogResult($"SUCCESS: Asset issued!\nTransaction Hash: {transactionResponse.hash}");
                }
                else
                {
                    LogResult("ERROR: Failed to issue asset");
                }
            }
            catch (System.Exception e)
            {
                LogResult($"ERROR: {e.Message}");
            }
        }

        private void LogResult(string message)
        {
            Debug.Log($"[StellarBackendTester] {message}");
            
            if (resultText != null)
            {
                resultText.text = message;
            }
        }

        void OnDestroy()
        {
            // Clean up button listeners
            if (createAccountButton != null)
                createAccountButton.onClick.RemoveListener(TestCreateAccount);
            
            if (getBalanceButton != null)
                getBalanceButton.onClick.RemoveListener(TestGetBalance);
            
            if (transferButton != null)
                transferButton.onClick.RemoveListener(TestTransfer);
            
            if (issueAssetButton != null)
                issueAssetButton.onClick.RemoveListener(TestIssueAsset);
        }
    }
} 