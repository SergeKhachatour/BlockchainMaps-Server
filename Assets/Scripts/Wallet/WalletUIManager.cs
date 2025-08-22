using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;
using BlockchainMaps.Authentication;
using Newtonsoft.Json;

namespace BlockchainMaps.Wallet
{
    public class WalletUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject walletPanel;
        [SerializeField] public RawImage qrCodeDisplay;
        [SerializeField] public TextMeshProUGUI balanceText;
        [SerializeField] public TextMeshProUGUI addressText;
        [SerializeField] public TextMeshProUGUI statusText;
        [SerializeField] public Button connectWalletButton;
        [SerializeField] public Button disconnectWalletButton;
        [SerializeField] public Button generateQRButton;
        [SerializeField] public Button scanQRButton;
        
        [Header("Payment UI")]
        [SerializeField] public GameObject paymentPanel;
        [SerializeField] public TextMeshProUGUI paymentAmountText;
        [SerializeField] public TextMeshProUGUI paymentAddressText;
        [SerializeField] public TextMeshProUGUI paymentMemoText;
        [SerializeField] public Button confirmPaymentButton;
        [SerializeField] public Button cancelPaymentButton;

        [Header("Blockchain Logos")]
        [SerializeField] public Image blockchainLogo;
        [SerializeField] public Sprite stellarLogo;
        [SerializeField] public Sprite circleLogo;

        [Header("QR Code Display")]
        [SerializeField] private float qrDisplayDuration = 10f;
        private Coroutine qrDisplayCoroutine;

        private StellarAccountManager accountManager;
        private QRPaymentManager qrPaymentManager;
        private PasskeyManager passkeyManager;
        private PaymentQRData pendingPayment;
        private bool isConnecting = false;

        private void Start()
        {
            accountManager = StellarAccountManager.Instance;
            qrPaymentManager = QRPaymentManager.Instance;
            passkeyManager = PasskeyManager.Instance;

            // Subscribe to events
            accountManager.OnWalletConnected += HandleWalletConnected;
            accountManager.OnWalletDisconnected += HandleWalletDisconnected;
            accountManager.OnBalanceUpdated += HandleBalanceUpdated;
            accountManager.OnError += HandleError;

            qrPaymentManager.OnQRCodeScanned += HandleQRCodeScanned;
            qrPaymentManager.OnPaymentSuccess += HandlePaymentSuccess;
            qrPaymentManager.OnPaymentError += HandleError;

            // Set up button listeners
            connectWalletButton.onClick.AddListener(ConnectWallet);
            disconnectWalletButton.onClick.AddListener(DisconnectWallet);
            generateQRButton.onClick.AddListener(GenerateQRCode);
            scanQRButton.onClick.AddListener(ToggleQRScanner);
            confirmPaymentButton.onClick.AddListener(ConfirmPayment);
            cancelPaymentButton.onClick.AddListener(CancelPayment);

            // Initial UI state
            UpdateUIState(false);
            UpdateBlockchainLogo("Stellar"); // Default to Stellar

            // Additional event subscriptions
            if (qrPaymentManager != null)
            {
                qrPaymentManager.OnScanningStateChanged += HandleScanningStateChanged;
            }
        }

        private void UpdateUIState(bool isConnected)
        {
            walletPanel.SetActive(isConnected);
            connectWalletButton.gameObject.SetActive(!isConnected && !isConnecting);
            disconnectWalletButton.gameObject.SetActive(isConnected);
            generateQRButton.gameObject.SetActive(isConnected);
            scanQRButton.gameObject.SetActive(isConnected);
            qrCodeDisplay.gameObject.SetActive(false);
            paymentPanel.SetActive(false);
            blockchainLogo.gameObject.SetActive(true); // Always show blockchain logo

            if (!isConnected)
            {
                balanceText.text = "Balance: --";
                addressText.text = "Not Connected";
                UpdateBlockchainLogo("Stellar"); // Reset to Stellar when disconnected
            }
        }

        private void UpdateBlockchainLogo(string blockchain)
        {
            if (blockchainLogo != null)
            {
                blockchainLogo.sprite = blockchain.ToLower() == "circle" ? circleLogo : stellarLogo;
            }
        }

        private async void ConnectWallet()
        {
            if (isConnecting) return;
            isConnecting = true;

            try
            {
                UpdateUIState(false);
                statusText.text = "Connecting wallet...";
                connectWalletButton.interactable = false;

                // Initialize PasskeyKit if needed
                if (!passkeyManager.IsInitialized())
                {
                    passkeyManager.InitializePasskeyKit();
                    await Task.Delay(1000); // Wait for initialization
                }

                // Authenticate with PasskeyKit
                bool authenticated = await passkeyManager.Authenticate("player");
                if (authenticated)
                {
                    string walletAddress = await GetWalletAddress();
                    accountManager.ConnectWallet(walletAddress);
                }
                else
                {
                    throw new Exception("Authentication failed");
                }
            }
            catch (Exception e)
            {
                HandleError(e.Message);
                UpdateUIState(false);
            }
            finally
            {
                isConnecting = false;
                connectWalletButton.interactable = true;
            }
        }

        private async Task<string> GetWalletAddress()
        {
            // In a real implementation, this would get the wallet address from PasskeyKit
            // For now, we'll generate a test address
            await Task.Delay(500); // Simulate network delay
            return "TEST_WALLET_" + DateTime.Now.Ticks.ToString().Substring(0, 8);
        }

        private void DisconnectWallet()
        {
            passkeyManager.LogOff();
            accountManager.DisconnectWallet();
        }

        private void GenerateQRCode()
        {
            try
            {
                var qrTexture = qrPaymentManager.GeneratePaymentQR(
                    accountManager.CurrentWalletAddress,
                    "Player Payment"
                );
                qrCodeDisplay.texture = qrTexture;
                qrCodeDisplay.gameObject.SetActive(true);
            }
            catch (Exception e)
            {
                HandleError(e.Message);
            }
        }

        private void ToggleQRScanner()
        {
            bool isScanning = !scanQRButton.interactable;
            scanQRButton.interactable = !isScanning;
            qrPaymentManager.ToggleScanning(!isScanning);
            statusText.text = isScanning ? "Scanning..." : "";
        }

        private void HandleWalletConnected(string address)
        {
            UpdateUIState(true);
            addressText.text = $"Wallet: {address.Substring(0, 8)}...";
            statusText.text = "Connected";
        }

        private void HandleWalletDisconnected()
        {
            UpdateUIState(false);
            statusText.text = "Disconnected";
        }

        private void HandleBalanceUpdated(decimal balance)
        {
            balanceText.text = $"Balance: {balance} XLM";
        }

        private void HandleQRCodeScanned(PaymentQRData paymentData)
        {
            pendingPayment = paymentData;
            paymentPanel.SetActive(true);
            
            // Update payment UI with marker information
            paymentAddressText.text = $"To: {paymentData.label}\n{paymentData.address.Substring(0, 8)}...";
            paymentAmountText.text = $"Amount: {paymentData.amount} {(paymentData.blockchain == "Circle" ? "USDC" : "XLM")}";
            paymentMemoText.text = $"Memo: {paymentData.memo}";

            // Update blockchain logo
            UpdateBlockchainLogo(paymentData.blockchain);
        }

        private async void ConfirmPayment()
        {
            if (pendingPayment == null) return;

            confirmPaymentButton.interactable = false;
            cancelPaymentButton.interactable = false;
            statusText.text = "Processing payment...";

            try
            {
                await qrPaymentManager.ProcessScannedQRCode(
                    JsonConvert.SerializeObject(pendingPayment)
                );
            }
            catch (Exception e)
            {
                HandleError(e.Message);
            }
            finally
            {
                confirmPaymentButton.interactable = true;
                cancelPaymentButton.interactable = true;
                CancelPayment();
            }
        }

        private void CancelPayment()
        {
            pendingPayment = null;
            paymentPanel.SetActive(false);
            statusText.text = "";
            UpdateBlockchainLogo("Stellar"); // Reset to Stellar when payment is cancelled
        }

        private void HandlePaymentSuccess(string txHash)
        {
            statusText.text = $"Payment successful!\nTx: {txHash.Substring(0, 8)}...";
            Invoke(nameof(ClearStatus), 5f);
            
            // Reset UI state
            CancelPayment();
            UpdateBlockchainLogo("Stellar");
        }

        private void HandleError(string error)
        {
            statusText.text = $"Error: {error}";
            Invoke(nameof(ClearStatus), 5f);
        }

        private void ClearStatus()
        {
            if (statusText != null)
            {
                statusText.text = "";
            }
        }

        private void HandleScanningStateChanged(bool isScanning)
        {
            scanQRButton.interactable = !isScanning;
            statusText.text = isScanning ? "Scanning for QR code..." : "";
        }

        public void ShowMarkerQRCode(string publicKey, string label)
        {
            try
            {
                var qrTexture = qrPaymentManager.GeneratePaymentQR(publicKey, label);
                if (qrTexture != null)
                {
                    qrCodeDisplay.texture = qrTexture;
                    qrCodeDisplay.gameObject.SetActive(true);

                    // Update blockchain logo based on address
                    string blockchain = publicKey.StartsWith("0x") ? "Circle" : "Stellar";
                    UpdateBlockchainLogo(blockchain);

                    // Auto-hide QR code after duration
                    if (qrDisplayCoroutine != null)
                    {
                        StopCoroutine(qrDisplayCoroutine);
                    }
                    qrDisplayCoroutine = StartCoroutine(AutoHideQRCode());
                }
            }
            catch (Exception e)
            {
                HandleError(e.Message);
            }
        }

        private System.Collections.IEnumerator AutoHideQRCode()
        {
            yield return new WaitForSeconds(qrDisplayDuration);
            HideQRCode();
        }

        private void HideQRCode()
        {
            if (qrCodeDisplay != null)
            {
                qrCodeDisplay.gameObject.SetActive(false);
                qrCodeDisplay.texture = null;
            }
            UpdateBlockchainLogo("Stellar"); // Reset to default
        }

        private void OnDestroy()
        {
            if (accountManager != null)
            {
                accountManager.OnWalletConnected -= HandleWalletConnected;
                accountManager.OnWalletDisconnected -= HandleWalletDisconnected;
                accountManager.OnBalanceUpdated -= HandleBalanceUpdated;
                accountManager.OnError -= HandleError;
            }

            if (qrPaymentManager != null)
            {
                qrPaymentManager.OnQRCodeScanned -= HandleQRCodeScanned;
                qrPaymentManager.OnPaymentSuccess -= HandlePaymentSuccess;
                qrPaymentManager.OnPaymentError -= HandleError;
                qrPaymentManager.OnScanningStateChanged -= HandleScanningStateChanged;
            }

            if (qrDisplayCoroutine != null)
            {
                StopCoroutine(qrDisplayCoroutine);
            }
        }
    }
} 