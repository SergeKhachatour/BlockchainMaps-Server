using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;

namespace BlockchainMaps
{
    [System.Serializable]
    public class PaymentRequest
    {
        public string RecipientAddress;
        public string AssetCode;
        public string IssuerPublicKey;
        public decimal Amount;
        public string Memo;
    }

    [System.Serializable]
    public class TransferAssetRequest
    {
        public string senderSecret;
        public string recipientPublicKey;
        public string assetCode;
        public string issuerPublicKey;
        public string amount;
        public string memo;
    }

    public class QRPaymentProcessor : MonoBehaviour
    {
        [Header("Payment UI")]
        [SerializeField] private GameObject paymentPanel;
        [SerializeField] private TMP_InputField amountInput;
        [SerializeField] private TMP_InputField memoInput;
        [SerializeField] private TextMeshProUGUI recipientText;
        [SerializeField] private TextMeshProUGUI senderText; // Shows the sender wallet address
        [SerializeField] private TextMeshProUGUI qrCodeText; // Shows the scanned QR code data
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("Confirmation UI")]
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmPaymentButton;
        [SerializeField] private Button backButton;
        
        [Header("Configuration")]
        [SerializeField] private float autoHideDelay = 3f;
        
        private StellarApiClient apiClient;
        private StellarWalletManager walletManager;
        private PaymentRequest currentPayment;
        private bool isProcessing = false;
        
        public static QRPaymentProcessor Instance { get; private set; }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        
        void Start()
        {
            StartCoroutine(InitializeWithDelay());
        }
        
        private System.Collections.IEnumerator InitializeWithDelay()
        {
            yield return new WaitForSeconds(1f);
            
            // Find required components
            apiClient = FindFirstObjectByType<StellarApiClient>();
            if (apiClient == null)
            {
                Debug.LogWarning("[QRPaymentProcessor] StellarApiClient not found, creating one...");
                var apiClientObj = new GameObject("StellarApiClient");
                apiClient = apiClientObj.AddComponent<StellarApiClient>();
                DontDestroyOnLoad(apiClientObj);
            }
            
            walletManager = FindFirstObjectByType<StellarWalletManager>();
            if (walletManager == null)
            {
                Debug.LogWarning("[QRPaymentProcessor] StellarWalletManager not found, creating one...");
                var walletManagerObj = new GameObject("StellarWalletManager");
                walletManager = walletManagerObj.AddComponent<StellarWalletManager>();
                DontDestroyOnLoad(walletManagerObj);
            }
            
            // Debug wallet manager state
            Debug.Log($"[QRPaymentProcessor] Wallet manager found: {walletManager != null}");
            if (walletManager != null)
            {
                Debug.Log($"[QRPaymentProcessor] Has wallet: {walletManager.HasWallet()}");
                if (walletManager.HasWallet())
                {
                    var wallet = walletManager.GetCurrentWallet();
                    Debug.Log($"[QRPaymentProcessor] Current wallet public key: {wallet?.publicKey}");
                }
                else
                {
                    Debug.Log("[QRPaymentProcessor] No wallet found, attempting to create one...");
                    // Try to create a wallet if none exists
                    _ = CreateWalletIfNeeded(); // Fire and forget async task
                }
            }
            
            // Create UI if it doesn't exist
            if (paymentPanel == null)
            {
                CreatePaymentUI();
            }
            
            // Setup UI event handlers
            SetupPaymentUI();
            
            // Update sender text to reflect current wallet state
            UpdateSenderText();
            
            Debug.Log("[QRPaymentProcessor] Initialization complete");
        }
        
        private async System.Threading.Tasks.Task CreateWalletIfNeeded()
        {
            Debug.Log("[QRPaymentProcessor] CreateWalletIfNeeded task started");
            
            // Wait a bit for any other initialization to complete
            await System.Threading.Tasks.Task.Delay(500);
            
            if (walletManager == null)
            {
                Debug.LogError("[QRPaymentProcessor] WalletManager is null in CreateWalletIfNeeded");
                return;
            }
            
            // Check if wallet was created by another process
            if (walletManager.HasWallet())
            {
                Debug.Log("[QRPaymentProcessor] Wallet already exists, no need to create");
                // Update UI even if wallet already exists
                UpdateSenderText();
                return;
            }
            
            Debug.Log("[QRPaymentProcessor] Attempting to create new wallet...");
            
            try
            {
                // Try to create a new wallet
                var newWallet = await walletManager.CreateWallet();
                
                if (newWallet != null && !string.IsNullOrEmpty(newWallet.publicKey))
                {
                    Debug.Log($"[QRPaymentProcessor] New wallet created successfully: {newWallet.publicKey}");
                    
                    // Update the UI to reflect the new wallet
                    UpdateSenderText();
                }
                else
                {
                    Debug.LogError("[QRPaymentProcessor] Failed to create new wallet or wallet has no public key");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[QRPaymentProcessor] Exception while creating wallet: {ex.Message}");
            }
            
            Debug.Log("[QRPaymentProcessor] CreateWalletIfNeeded task completed");
        }
        
        private void UpdateSenderText()
        {
            if (senderText != null && walletManager != null)
            {
                if (walletManager.HasWallet())
                {
                    var wallet = walletManager.GetCurrentWallet();
                    if (wallet != null && !string.IsNullOrEmpty(wallet.publicKey))
                    {
                        senderText.text = $"From: {wallet.publicKey}";
                        Debug.Log($"[QRPaymentProcessor] Updated sender text to: {senderText.text}");
                    }
                    else
                    {
                        senderText.text = "From: Invalid wallet data";
                        Debug.LogWarning("[QRPaymentProcessor] Wallet exists but has invalid data");
                    }
                }
                else
                {
                    senderText.text = "From: No wallet";
                    Debug.LogWarning("[QRPaymentProcessor] No wallet available for sender text");
                }
            }
            else
            {
                Debug.LogWarning("[QRPaymentProcessor] Cannot update sender text - senderText or walletManager is null");
            }
        }
        
        private void SetupPaymentUI()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(ShowConfirmation);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(HidePaymentUI);
            }
            
            if (confirmPaymentButton != null)
            {
                confirmPaymentButton.onClick.RemoveAllListeners();
                confirmPaymentButton.onClick.AddListener(ProcessPayment);
            }
            
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(ShowPaymentPanel);
            }
            
            // Add input validation
            if (amountInput != null)
            {
                amountInput.onValueChanged.RemoveAllListeners();
                amountInput.onValueChanged.AddListener(OnAmountChanged);
            }
        }
        
        private void OnAmountChanged(string value)
        {
            if (decimal.TryParse(value, out decimal amount))
            {
                // Validate amount
                if (amount <= 0)
                {
                    confirmButton.interactable = false;
                    statusText.text = "Amount must be greater than 0";
                    statusText.color = Color.red;
                }
                else if (amount > 1000000) // Reasonable upper limit
                {
                    confirmButton.interactable = false;
                    statusText.text = "Amount too high";
                    statusText.color = Color.red;
                }
                else
                {
                    confirmButton.interactable = true;
                    statusText.text = "";
                }
            }
            else
            {
                confirmButton.interactable = false;
                statusText.text = "Invalid amount format";
                statusText.color = Color.red;
            }
        }
        
        public void HandleQRCodeScanned(string qrData)
        {
            Debug.Log($"[QRPaymentProcessor] QR code scanned: {qrData}");
            Debug.Log($"[QRPaymentProcessor] Starting to process QR data...");
            
            try
            {
                var recipientAddress = ParseQRData(qrData);
                Debug.Log($"[QRPaymentProcessor] ParseQRData result: {recipientAddress}");
                
                if (!string.IsNullOrEmpty(recipientAddress))
                {
                    Debug.Log($"[QRPaymentProcessor] Creating payment request for: {recipientAddress}");
                    
                    // Create payment request
                    currentPayment = new PaymentRequest
                    {
                        RecipientAddress = recipientAddress,
                        AssetCode = "XLM",
                        IssuerPublicKey = "native",
                        Amount = 0,
                        Memo = ""
                    };
                    
                    // Store the original QR data for display
                    currentPayment.Memo = qrData;
                    
                    Debug.Log($"[QRPaymentProcessor] About to call ShowPaymentUI...");
                    ShowPaymentUI(currentPayment);
                    Debug.Log($"[QRPaymentProcessor] ShowPaymentUI completed");
                }
                else
                {
                    Debug.LogError("[QRPaymentProcessor] Failed to parse recipient address from QR data");
                    ShowStatus("Invalid QR code format", Color.red);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[QRPaymentProcessor] Error processing QR code: {e.Message}");
                Debug.LogError($"[QRPaymentProcessor] Stack trace: {e.StackTrace}");
                ShowStatus($"QR processing error: {e.Message}", Color.red);
            }
        }
        
        private string ParseQRData(string qrData)
        {
            // Handle different QR code formats
            if (qrData.StartsWith("web+stellar:"))
            {
                // Parse Stellar URI format
                var uri = qrData.Substring("web+stellar:".Length);
                var parts = uri.Split('?');
                var address = parts[0];
                
                // Extract memo if present
                if (parts.Length > 1)
                {
                    var query = parts[1];
                    var memoMatch = System.Text.RegularExpressions.Regex.Match(query, @"memo=([^&]+)");
                    if (memoMatch.Success)
                    {
                        // Use Unity's built-in URL decoding method instead of System.Web.HttpUtility
                        currentPayment.Memo = UnityWebRequest.UnEscapeURL(memoMatch.Groups[1].Value);
                    }
                }
                
                return address;
            }
            else if (qrData.Length == 56 && qrData.StartsWith("G"))
            {
                // Direct Stellar public key
                return qrData;
            }
            else
            {
                // Try to extract Stellar address from other formats
                var match = System.Text.RegularExpressions.Regex.Match(qrData, @"G[A-Z0-9]{55}");
                if (match.Success)
                {
                    return match.Value;
                }
            }
            
            return null;
        }
        
        public void ShowPaymentUI(PaymentRequest request)
        {
            Debug.Log($"[QRPaymentProcessor] ShowPaymentUI called with request: {request?.RecipientAddress}");
            currentPayment = request;
            
            if (paymentPanel != null)
            {
                Debug.Log($"[QRPaymentProcessor] Payment panel found, activating...");
                paymentPanel.SetActive(true);
                
                // Pre-fill recipient
                if (recipientText != null)
                {
                    recipientText.text = $"To: {request.RecipientAddress}";
                    Debug.Log($"[QRPaymentProcessor] Set recipient text: {recipientText.text}");
                }
                else
                {
                    Debug.LogWarning("[QRPaymentProcessor] recipientText is null!");
                }
                
                // Show sender address
                if (senderText != null)
                {
                    UpdateSenderText();
                }
                else
                {
                    Debug.LogWarning("[QRPaymentProcessor] senderText is null!");
                }
                
                // Show scanned QR code data
                if (qrCodeText != null)
                {
                    // Hide QR schema from the user
                    qrCodeText.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("[QRPaymentProcessor] qrCodeText is null!");
                }
                
                // Reset form
                if (amountInput != null)
                    amountInput.text = "";
                if (memoInput != null)
                    memoInput.text = "";
                
                // Clear status
                if (statusText != null)
                    statusText.text = "";
                
                // Update balance
                UpdateBalanceDisplay();
                // Reset confirm buttons state for fresh scan
                if (confirmButton != null) confirmButton.interactable = true;
                if (confirmPaymentButton != null) confirmPaymentButton.interactable = true;
                if (statusText != null) statusText.text = "";
                
                Debug.Log($"[QRPaymentProcessor] Payment UI shown for recipient: {request.RecipientAddress}");
            }
            else
            {
                Debug.LogError("[QRPaymentProcessor] Payment panel not found!");
                Debug.LogError("[QRPaymentProcessor] This means CreatePaymentUI() was never called or failed!");
            }
        }
        
        public void HidePaymentUI()
        {
            if (paymentPanel != null)
                paymentPanel.SetActive(false);
            
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
            
            currentPayment = null;
            isProcessing = false;
            
            Debug.Log("[QRPaymentProcessor] Payment UI hidden");
        }
        
        public void ShowConfirmation()
        {
            if (string.IsNullOrEmpty(amountInput.text) || !decimal.TryParse(amountInput.text, out decimal amount))
            {
                ShowStatus("Please enter a valid amount", Color.red);
                return;
            }
            
            if (amount <= 0)
            {
                ShowStatus("Amount must be greater than 0", Color.red);
                return;
            }
            
            // Validate wallet availability
            if (!walletManager.HasWallet())
            {
                ShowStatus("No wallet available. Please authenticate first.", Color.red);
                return;
            }
            
            // Show confirmation panel
            if (confirmationPanel != null)
            {
                string senderAddress = walletManager.HasWallet() ? walletManager.GetCurrentWallet().publicKey : "No wallet";
                confirmationText.text = $"Confirm Payment\n\n" +
                                       $"From: {senderAddress}\n" +
                                       $"To: {currentPayment.RecipientAddress}\n" +
                                       $"Amount: {amount} XLM\n" +
                                       $"Memo: {memoInput?.text ?? ""}\n\n" +
                                       $"Fee: ~0.00001 XLM";
                
                confirmationPanel.SetActive(true);
                paymentPanel.SetActive(false);
            }
        }
        
        public void ShowPaymentPanel()
        {
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
            
            if (paymentPanel != null)
                paymentPanel.SetActive(true);
        }
        
        public async void ProcessPayment()
        {
            if (isProcessing) return;
            
            try
            {
                isProcessing = true;
                ShowStatus("Processing payment...", Color.yellow);
                
                if (confirmPaymentButton != null)
                    confirmPaymentButton.interactable = false;
                
                var amount = decimal.Parse(amountInput.text);
                var memo = memoInput?.text ?? "";
                
                // Get current wallet
                var wallet = walletManager.GetCurrentWallet();
                if (wallet == null)
                {
                    Debug.LogError("[QRPaymentProcessor] No wallet found. User needs to authenticate with passkey first.");
                    ShowStatus("No wallet available. Please authenticate with passkey first to create a wallet.", Color.red);
                    return;
                }
                
                if (string.IsNullOrEmpty(wallet.publicKey) || string.IsNullOrEmpty(wallet.secretKey))
                {
                    Debug.LogError("[QRPaymentProcessor] Wallet data is invalid or incomplete.");
                    ShowStatus("Wallet data is invalid. Please re-authenticate with passkey.", Color.red);
                    return;
                }
                
                Debug.Log($"[QRPaymentProcessor] Using wallet for payment: {wallet.publicKey}");
                
                // Create transfer request
                var transferRequest = new TransferAssetRequest
                {
                    senderSecret = wallet.secretKey,
                    recipientPublicKey = currentPayment.RecipientAddress,
                    assetCode = "XLM",
                    issuerPublicKey = "native", // For XLM
                    amount = amount.ToString(),
                    memo = memo
                };
                
                // Process payment
                var result = await apiClient.TransferAsset(transferRequest);
                
                if (result != null && !string.IsNullOrEmpty(result.hash))
                {
                    // Show a proper confirmation view
                    if (confirmationPanel != null)
                    {
                        confirmationText.text =
                            $"Payment Sent\n\n" +
                            $"From: {wallet.publicKey}\n" +
                            $"To: {currentPayment.RecipientAddress}\n" +
                            $"Amount: {amount} XLM\n" +
                            $"Memo: {memo}\n\n" +
                            $"Tx: {result.hash}";
                        confirmationPanel.SetActive(true);
                        if (paymentPanel != null) paymentPanel.SetActive(false);
                    }
                    
                    ShowStatus("Payment successful!", Color.green);
                    
                    // Refresh on-screen balance
                    StartCoroutine(RefreshBalance());
                    
                    // Also refresh top-right balance label if available
                    var uiMgr = FindFirstObjectByType<BlockchainMaps.Authentication.PasskeyUIManager>();
                    if (uiMgr != null)
                    {
                        uiMgr.RefreshTopRightStatus();
                    }
                    
                    // Hide UI after delay
                    Invoke(nameof(HidePaymentUI), autoHideDelay);
                }
                else
                {
                    ShowStatus("Payment failed - no transaction hash received", Color.red);
                    if (confirmationPanel != null) confirmationPanel.SetActive(false);
                    if (paymentPanel != null) paymentPanel.SetActive(true);
                    if (confirmPaymentButton != null) confirmPaymentButton.interactable = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[QRPaymentProcessor] Payment error: {e.Message}");
                ShowStatus($"Payment failed: {e.Message}", Color.red);
                if (confirmationPanel != null) confirmationPanel.SetActive(false);
                if (paymentPanel != null) paymentPanel.SetActive(true);
                if (confirmPaymentButton != null) confirmPaymentButton.interactable = true;
            }
            finally
            {
                isProcessing = false;
                if (confirmPaymentButton != null)
                    confirmPaymentButton.interactable = true;
            }
        }
        
        private void ShowStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
            
            Debug.Log($"[QRPaymentProcessor] Payment Status: {message}");
        }
        
        private void UpdateBalanceDisplay()
        {
            if (walletManager == null)
            {
                Debug.LogError("[QRPaymentProcessor] WalletManager is null!");
                if (balanceText != null)
                    balanceText.text = "Wallet manager not available";
                return;
            }
            
            if (walletManager.HasWallet())
            {
                var wallet = walletManager.GetCurrentWallet();
                if (wallet != null && !string.IsNullOrEmpty(wallet.publicKey))
                {
                    Debug.Log($"[QRPaymentProcessor] Updating balance for wallet: {wallet.publicKey}");
                    StartCoroutine(RefreshBalance());
                }
                else
                {
                    Debug.LogWarning("[QRPaymentProcessor] Wallet exists but data is invalid");
                    if (balanceText != null)
                        balanceText.text = "Wallet data invalid - re-authenticate";
                }
            }
            else
            {
                Debug.LogWarning("[QRPaymentProcessor] No wallet found - user needs to authenticate");
                if (balanceText != null)
                    balanceText.text = "No wallet - authenticate with passkey";
            }
        }
        
        private System.Collections.IEnumerator RefreshBalance()
        {
            if (balanceText != null)
                balanceText.text = "Balance: Checking...";
            
            // Call backend via walletManager -> apiClient.ShowBalance
            yield return new WaitForSeconds(0.1f);
            
            if (walletManager == null)
            {
                if (balanceText != null) balanceText.text = "Balance unavailable";
                yield break;
            }
            
            string publicKey = null;
            if (walletManager.HasWallet())
            {
                var wallet = walletManager.GetCurrentWallet();
                publicKey = wallet?.publicKey;
            }
            
            if (string.IsNullOrEmpty(publicKey))
            {
                if (balanceText != null) balanceText.text = "No wallet";
                yield break;
            }
            
            var task = apiClient != null ? apiClient.ShowBalance(publicKey) : null;
            if (task == null)
            {
                if (balanceText != null) balanceText.text = "Balance unavailable";
                yield break;
            }
            
            while (!task.IsCompleted)
                yield return null;
            
            try
            {
                var result = task.Result;
                if (result != null && result.balances != null)
                {
                    string xlm = null;
                    foreach (var b in result.balances)
                    {
                        if (string.Equals(b.asset_code ?? b.asset_type, "XLM", StringComparison.OrdinalIgnoreCase) || string.Equals(b.asset_type, "native", StringComparison.OrdinalIgnoreCase))
                        {
                            xlm = b.balance;
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(xlm) && result.balances.Count > 0)
                        xlm = result.balances[0].balance;
                    
                    if (balanceText != null)
                        balanceText.text = !string.IsNullOrEmpty(xlm) ? $"Balance: {xlm} XLM" : "Balance unavailable";
                }
                else
                {
                    if (balanceText != null) balanceText.text = "Balance unavailable";
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[QRPaymentProcessor] Failed to refresh balance: {e.Message}");
                if (balanceText != null) balanceText.text = "Balance unavailable";
            }
        }
        
        private void CreatePaymentUI()
        {
            // Create main payment panel
            paymentPanel = new GameObject("PaymentPanel");
            paymentPanel.transform.SetParent(transform);
            
            // Add Canvas and CanvasScaler for proper UI scaling
            var canvas = paymentPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var scaler = paymentPanel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            var raycaster = paymentPanel.AddComponent<GraphicRaycaster>();
            
            // Create background
            var background = CreateImage(paymentPanel, "Background", new Color(0, 0, 0, 0.9f));
            var rectTransform = background.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.2f, 0.2f);
            rectTransform.anchorMax = new Vector2(0.8f, 0.8f);
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Create title
            var titleText = CreateText(paymentPanel, "Send Payment", 24, Color.white);
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.85f);
            titleRect.anchorMax = new Vector2(0.9f, 0.95f);
            
            // Create recipient display
            recipientText = CreateText(paymentPanel, "To: ", 18, Color.white);
            var recipientRect = recipientText.GetComponent<RectTransform>();
            recipientRect.anchorMin = new Vector2(0.1f, 0.74f);
            recipientRect.anchorMax = new Vector2(0.9f, 0.84f);
            
            // Create sender display
            senderText = CreateText(paymentPanel, "From: ", 18, Color.white);
            var senderRect = senderText.GetComponent<RectTransform>();
            senderRect.anchorMin = new Vector2(0.1f, 0.70f);
            senderRect.anchorMax = new Vector2(0.9f, 0.78f);
            
            // Create QR code data display
            qrCodeText = CreateText(paymentPanel, "Scanned: ", 16, Color.cyan);
            var qrCodeRect = qrCodeText.GetComponent<RectTransform>();
            qrCodeRect.anchorMin = new Vector2(0.1f, 0.55f);
            qrCodeRect.anchorMax = new Vector2(0.9f, 0.65f);
            
            // Create amount input
            amountInput = CreateAmountInput(paymentPanel);
            var amountRect = amountInput.GetComponent<RectTransform>();
            amountRect.anchorMin = new Vector2(0.1f, 0.45f);
            amountRect.anchorMax = new Vector2(0.6f, 0.55f);
            
            // Create memo input
            memoInput = CreateMemoInput(paymentPanel);
            var memoRect = memoInput.GetComponent<RectTransform>();
            memoRect.anchorMin = new Vector2(0.1f, 0.30f);
            memoRect.anchorMax = new Vector2(0.6f, 0.40f);
            
            // Create balance display
            balanceText = CreateText(paymentPanel, "Balance: ", 16, Color.green);
            var balanceRect = balanceText.GetComponent<RectTransform>();
            balanceRect.anchorMin = new Vector2(0.1f, 0.2f);
            balanceRect.anchorMax = new Vector2(0.9f, 0.27f);
            
            // Create status text
            statusText = CreateText(paymentPanel, "", 14, Color.white);
            var statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.1f, 0.1f);
            statusRect.anchorMax = new Vector2(0.9f, 0.17f);
            
            // Create buttons
            confirmButton = CreateButton(paymentPanel, "Continue", Color.green);
            var confirmRect = confirmButton.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.1f, 0.1f);
            confirmRect.anchorMax = new Vector2(0.45f, 0.2f);
            
            cancelButton = CreateButton(paymentPanel, "Cancel", Color.red);
            var cancelRect = cancelButton.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.55f, 0.1f);
            cancelRect.anchorMax = new Vector2(0.9f, 0.2f);
            
            // Create confirmation panel
            CreateConfirmationPanel();
            
            // Initially hide the UI
            paymentPanel.SetActive(false);
            
            Debug.Log("[QRPaymentProcessor] Payment UI created successfully");
        }
        
        private void CreateConfirmationPanel()
        {
            confirmationPanel = new GameObject("ConfirmationPanel");
            confirmationPanel.transform.SetParent(transform);
            
            var canvas = confirmationPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 101;
            
            var scaler = confirmationPanel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            var raycaster = confirmationPanel.AddComponent<GraphicRaycaster>();
            
            // Create background
            var background = CreateImage(confirmationPanel, "Background", new Color(0, 0, 0, 0.95f));
            var rectTransform = background.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.15f, 0.15f);
            rectTransform.anchorMax = new Vector2(0.85f, 0.85f);
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Create confirmation text
            confirmationText = CreateText(confirmationPanel, "", 20, Color.white);
            var textRect = confirmationText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.3f);
            textRect.anchorMax = new Vector2(0.9f, 0.7f);
            
            // Create buttons
            confirmPaymentButton = CreateButton(confirmationPanel, "Confirm Payment", Color.green);
            var confirmRect = confirmPaymentButton.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.1f, 0.1f);
            confirmRect.anchorMax = new Vector2(0.45f, 0.2f);
            
            backButton = CreateButton(confirmationPanel, "Back", Color.gray);
            var backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.55f, 0.1f);
            backRect.anchorMax = new Vector2(0.9f, 0.2f);
            
            // Initially hide the confirmation panel
            confirmationPanel.SetActive(false);
        }
        
        private TextMeshProUGUI CreateText(GameObject parent, string text, int fontSize, Color color)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform);
            
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = color;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            var rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            
            return textComponent;
        }
        
        private TMP_InputField CreateAmountInput(GameObject parent)
        {
            var inputObj = new GameObject("AmountInput");
            inputObj.transform.SetParent(parent.transform);
            
            // Background on parent (raycast target)
            var bgImage = inputObj.AddComponent<Image>();
            bgImage.color = Color.white;
            
            var rectTransform = inputObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0, 24);
            
            // Child for text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "";
            textComponent.fontSize = 14;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(8, 2);
            textRect.offsetMax = new Vector2(-8, -2);
            
            // Child for placeholder
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);
            var placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = "Enter amount (XLM)";
            placeholder.fontSize = 14;
            placeholder.color = Color.gray;
            var placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0, 0);
            placeholderRect.anchorMax = new Vector2(1, 1);
            placeholderRect.offsetMin = new Vector2(8, 2);
            placeholderRect.offsetMax = new Vector2(-8, -2);
            
            // Input field
            var inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholder;
            inputField.caretColor = Color.black;
            inputField.selectionColor = new Color(0, 0, 0, 0.2f);
            
            return inputField;
        }
        
        private TMP_InputField CreateMemoInput(GameObject parent)
        {
            var inputObj = new GameObject("MemoInput");
            inputObj.transform.SetParent(parent.transform);
            
            // Background on parent (raycast target)
            var bgImage = inputObj.AddComponent<Image>();
            bgImage.color = Color.white;
            
            var rectTransform = inputObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0, 24);
            
            // Child for text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "";
            textComponent.fontSize = 14;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(8, 2);
            textRect.offsetMax = new Vector2(-8, -2);
            
            // Child for placeholder
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);
            var placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = "Memo (optional)";
            placeholder.fontSize = 14;
            placeholder.color = Color.gray;
            var placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0, 0);
            placeholderRect.anchorMax = new Vector2(1, 1);
            placeholderRect.offsetMin = new Vector2(8, 2);
            placeholderRect.offsetMax = new Vector2(-8, -2);
            
            // Input field
            var inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholder;
            inputField.caretColor = Color.black;
            inputField.selectionColor = new Color(0, 0, 0, 0.2f);
            
            return inputField;
        }
        
        private Button CreateButton(GameObject parent, string text, Color color)
        {
            var buttonObj = new GameObject("Button");
            buttonObj.transform.SetParent(parent.transform);
            
            var button = buttonObj.AddComponent<Button>();
            var image = buttonObj.AddComponent<Image>();
            image.color = color;
            
            var textComponent = CreateText(buttonObj, text, 16, Color.white);
            var textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            
            var rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            
            return button;
        }
        
        private Image CreateImage(GameObject parent, string name, Color color)
        {
            var imageObj = new GameObject(name);
            imageObj.transform.SetParent(parent.transform);
            
            var image = imageObj.AddComponent<Image>();
            image.color = color;
            
            var rectTransform = imageObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            
            return image;
        }
        
        void Update()
        {
            // Manual wallet creation for testing (F5 key)
            if (Input.GetKeyDown(KeyCode.F5))
            {
                Debug.Log("[QRPaymentProcessor] F5 pressed - manually creating wallet");
                if (walletManager != null)
                {
                    walletManager.CreateWalletManually();
                }
                else
                {
                    Debug.LogWarning("[QRPaymentProcessor] WalletManager is null, cannot create wallet");
                }
            }
            
            // Manual sender text refresh for testing (F6 key)
            if (Input.GetKeyDown(KeyCode.F6))
            {
                Debug.Log("[QRPaymentProcessor] F6 pressed - manually refreshing sender text");
                UpdateSenderText();
            }
            
            // Manual wallet state check for debugging (F7 key)
            if (Input.GetKeyDown(KeyCode.F7))
            {
                Debug.Log("[QRPaymentProcessor] F7 pressed - checking wallet state");
                if (walletManager != null)
                {
                    Debug.Log($"[QRPaymentProcessor] WalletManager found: {walletManager != null}");
                    Debug.Log($"[QRPaymentProcessor] HasWallet: {walletManager.HasWallet()}");
                    if (walletManager.HasWallet())
                    {
                        var wallet = walletManager.GetCurrentWallet();
                        Debug.Log($"[QRPaymentProcessor] Current wallet: {wallet?.publicKey}");
                    }
                }
                else
                {
                    Debug.LogWarning("[QRPaymentProcessor] WalletManager is null");
                }
            }
            
            // Manual wallet reset and recreation for testing (F8 key)
            if (Input.GetKeyDown(KeyCode.F8))
            {
                Debug.Log("[QRPaymentProcessor] F8 pressed - resetting and recreating wallet");
                if (walletManager != null)
                {
                    walletManager.ResetAndCreateWallet();
                }
                else
                {
                    Debug.LogWarning("[QRPaymentProcessor] WalletManager is null, cannot reset wallet");
                }
            }
            
            // Manual CreateWalletIfNeeded trigger for testing (F9 key)
            if (Input.GetKeyDown(KeyCode.F9))
            {
                Debug.Log("[QRPaymentProcessor] F9 pressed - manually triggering CreateWalletIfNeeded");
                _ = CreateWalletIfNeeded();
            }
            
            // Manual PlayerPrefs debug check for testing (F10 key)
            if (Input.GetKeyDown(KeyCode.F10))
            {
                Debug.Log("[QRPaymentProcessor] F10 pressed - checking PlayerPrefs data");
                if (walletManager != null)
                {
                    walletManager.DebugPlayerPrefs();
                }
                else
                {
                    Debug.LogWarning("[QRPaymentProcessor] WalletManager is null, cannot check PlayerPrefs");
                }
            }
            
            // Manual wallet data save for testing (F11 key)
            if (Input.GetKeyDown(KeyCode.F11))
            {
                Debug.Log("[QRPaymentProcessor] F11 pressed - manually saving wallet data");
                if (walletManager != null)
                {
                    walletManager.SaveWalletDataManually();
                }
                else
                {
                    Debug.LogWarning("[QRPaymentProcessor] WalletManager is null, cannot save wallet data");
                }
            }
            
            // Manual wallet data reload for testing (F12 key)
            if (Input.GetKeyDown(KeyCode.F12))
            {
                Debug.Log("[QRPaymentProcessor] F12 pressed - manually reloading wallet data");
                if (walletManager != null)
                {
                    walletManager.ReloadWalletDataManually();
                    // Update the UI after reloading
                    UpdateSenderText();
                }
                else
                {
                    Debug.LogWarning("[QRPaymentProcessor] WalletManager is null, cannot reload wallet data");
                }
            }
        }
        
        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
} 