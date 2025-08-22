using UnityEngine;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BlockchainMaps.Wallet
{
    [System.Serializable]
    public class PaymentQRData
    {
        public string address;
        public decimal amount;
        public string memo;
        public string playerName;
        public string blockchain;
        public string label;
    }

    public class QRPaymentManager : MonoBehaviour
    {
        private static QRPaymentManager _instance;
        public static QRPaymentManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<QRPaymentManager>();
                }
                return _instance;
            }
        }

        private StellarAccountManager accountManager;

        public event System.Action<PaymentQRData> OnQRCodeScanned;
        public event System.Action<string> OnPaymentSuccess;
        public event System.Action<string> OnPaymentError;
        public event System.Action<bool> OnScanningStateChanged;

        private bool isScanning;

        private void Start()
        {
            accountManager = StellarAccountManager.Instance;
        }

        public Texture2D GeneratePaymentQR(string publicKey, string label, decimal amount = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(publicKey))
                {
                    throw new ArgumentException("Public key cannot be empty");
                }

                var paymentData = new PaymentQRData
                {
                    address = publicKey,
                    amount = amount,
                    memo = $"Payment to {label}",
                    playerName = label,
                    blockchain = publicKey.StartsWith("0x") ? "Circle" : "Stellar",
                    label = label
                };

                string jsonData = JsonConvert.SerializeObject(paymentData);
                return QRCodeWrapper.GenerateQRCode(jsonData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to generate QR code: {e.Message}");
                OnPaymentError?.Invoke("Failed to generate QR code");
                return null;
            }
        }

        public async Task ProcessScannedQRCode(string qrData)
        {
            try
            {
                PaymentQRData paymentData = JsonConvert.DeserializeObject<PaymentQRData>(qrData);
                
                // Validate payment data
                if (paymentData == null || string.IsNullOrEmpty(paymentData.address))
                {
                    throw new Exception("Invalid QR code data");
                }

                // Infer blockchain if not specified
                if (string.IsNullOrEmpty(paymentData.blockchain))
                {
                    paymentData.blockchain = paymentData.address.StartsWith("0x") ? "Circle" : "Stellar";
                }

                OnQRCodeScanned?.Invoke(paymentData);

                // Process payment based on blockchain type
                if (paymentData.blockchain.ToLower() == "circle")
                {
                    throw new NotImplementedException("Circle payments not yet implemented");
                }
                else
                {
                    // Verify wallet connection
                    if (!accountManager.IsWalletConnected())
                    {
                        throw new Exception("Wallet not connected");
                    }

                    // Check balance
                    decimal balance = await accountManager.GetAccountBalance();
                    if (balance < paymentData.amount)
                    {
                        throw new Exception($"Insufficient balance. Required: {paymentData.amount} XLM, Available: {balance} XLM");
                    }

                    // Send payment
                    string txHash = await accountManager.SendPayment(
                        paymentData.address,
                        paymentData.amount,
                        paymentData.memo
                    );

                    OnPaymentSuccess?.Invoke(txHash);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to process payment: {e.Message}");
                OnPaymentError?.Invoke(e.Message);
                throw;
            }
        }

        public void ToggleScanning(bool shouldScan)
        {
            isScanning = shouldScan;
            Debug.Log($"QR scanning {(shouldScan ? "enabled" : "disabled")}");
            OnScanningStateChanged?.Invoke(shouldScan);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
} 