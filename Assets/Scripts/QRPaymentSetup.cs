using UnityEngine;

namespace BlockchainMaps
{
    public class QRPaymentSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool setupOnStart = true;
        
        void Start()
        {
            if (setupOnStart)
            {
                SetupQRPayment();
            }
        }
        
        [ContextMenu("Setup QR Payment")]
        public void SetupQRPayment()
        {
            Debug.Log("=== Setting up QR Payment System ===");
            
            // Check if QRPaymentProcessor already exists
            var existingProcessor = FindFirstObjectByType<MonoBehaviour>();
            bool found = false;
            
            if (existingProcessor != null && existingProcessor.GetType().Name == "QRPaymentProcessor")
            {
                Debug.Log("QRPaymentProcessor already exists");
                found = true;
            }
            
            if (!found)
            {
                Debug.Log("Creating QRPaymentProcessor...");
                var processorObj = new GameObject("QRPaymentProcessor");
                processorObj.AddComponent<QRPaymentProcessor>();
                DontDestroyOnLoad(processorObj);
                Debug.Log("QRPaymentProcessor created successfully");
            }
            
            Debug.Log("=== QR Payment Setup Complete ===");
        }
        
        [ContextMenu("Clean Up QR Payment")]
        public void CleanUpQRPayment()
        {
            Debug.Log("=== Cleaning up QR Payment System ===");
            
            var processor = FindFirstObjectByType<MonoBehaviour>();
            if (processor != null && processor.GetType().Name == "QRPaymentProcessor")
            {
                Debug.Log("Destroying QRPaymentProcessor...");
                DestroyImmediate(processor.gameObject);
            }
            
            Debug.Log("=== QR Payment Cleanup Complete ===");
        }
    }
} 