using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace BlockchainMaps
{
    [System.Serializable]
    public class PaymentRecord
    {
        public string TransactionHash { get; set; }
        public string Recipient { get; set; }
        public decimal Amount { get; set; }
        public string AssetCode { get; set; }
        public string Memo { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } // "pending", "completed", "failed"
        public string Network { get; set; } // "testnet", "mainnet"
        
        public PaymentRecord()
        {
            Timestamp = DateTime.Now;
            Status = "pending";
        }
        
        public PaymentRecord(string transactionHash, string recipient, decimal amount, string assetCode = "XLM", string memo = "", string network = "testnet")
        {
            TransactionHash = transactionHash;
            Recipient = recipient;
            Amount = amount;
            AssetCode = assetCode;
            Memo = memo;
            Network = network;
            Timestamp = DateTime.Now;
            Status = "completed";
        }
    }

    public class PaymentHistory : MonoBehaviour
    {
        private static PaymentHistory instance;
        public static PaymentHistory Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("PaymentHistory");
                    instance = go.AddComponent<PaymentHistory>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private static List<PaymentRecord> payments = new List<PaymentRecord>();
        private const string PAYMENT_HISTORY_KEY = "PaymentHistory";

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadFromPlayerPrefs();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        public static void AddPayment(PaymentRecord payment)
        {
            payments.Add(payment);
            SaveToPlayerPrefs();
            Debug.Log($"[PaymentHistory] Payment recorded: {payment.Amount} to {payment.Recipient}");
        }

        public static List<PaymentRecord> GetRecentPayments(int count = 10)
        {
            return payments.OrderByDescending(p => p.Timestamp).Take(count).ToList();
        }

        public static List<PaymentRecord> GetAllPayments()
        {
            return payments.OrderByDescending(p => p.Timestamp).ToList();
        }

        public static void ClearHistory()
        {
            payments.Clear();
            PlayerPrefs.DeleteKey(PAYMENT_HISTORY_KEY);
            PlayerPrefs.Save();
            Debug.Log("[PaymentHistory] Payment history cleared");
        }

        private static void SaveToPlayerPrefs()
        {
            try
            {
                var jsonData = JsonConvert.SerializeObject(payments);
                PlayerPrefs.SetString(PAYMENT_HISTORY_KEY, jsonData);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PaymentHistory] Failed to save payment history: {e.Message}");
            }
        }

        private void LoadFromPlayerPrefs()
        {
            try
            {
                if (PlayerPrefs.HasKey(PAYMENT_HISTORY_KEY))
                {
                    var jsonData = PlayerPrefs.GetString(PAYMENT_HISTORY_KEY);
                    payments = JsonConvert.DeserializeObject<List<PaymentRecord>>(jsonData) ?? new List<PaymentRecord>();
                    Debug.Log($"[PaymentHistory] Loaded {payments.Count} payment records");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PaymentHistory] Failed to load payment history: {e.Message}");
                payments = new List<PaymentRecord>();
            }
        }
    }
} 