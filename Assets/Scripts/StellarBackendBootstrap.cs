using UnityEngine;
using System.Collections;

namespace BlockchainMaps
{
    [DefaultExecutionOrder(-300)] // Execute very early
    public class StellarBackendBootstrap : MonoBehaviour
    {
        private static StellarBackendBootstrap instance;
        private static bool isBootstrapped = false;
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                gameObject.name = "StellarBackendBootstrap";
                DontDestroyOnLoad(gameObject);
                
                Debug.Log("=== StellarBackendBootstrap Awake ===");
                
                if (!isBootstrapped)
                {
                    StartCoroutine(BootstrapBackend());
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            Debug.Log("=== StellarBackendBootstrap Start ===");
        }
        
        private IEnumerator BootstrapBackend()
        {
            Debug.Log("=== Starting Stellar Backend Bootstrap ===");
            isBootstrapped = true;
            
            // Wait a moment for Unity to fully initialize
            yield return new WaitForSeconds(0.5f);
            
            CreateBackendComponents();
            
            Debug.Log("=== Stellar Backend Bootstrap Complete ===");
        }
        
        /// <summary>
        /// Method that can be called from JavaScript to ensure backend components are created
        /// </summary>
        public void EnsureBackendComponents()
        {
            Debug.Log("=== EnsureBackendComponents called from JavaScript ===");
            CreateBackendComponents();
        }
        
        private void CreateBackendComponents()
        {
            Debug.Log("=== Creating Stellar Backend Components ===");
            
            try
            {
                // Create StellarApiClient if it doesn't exist
                var apiClient = FindFirstObjectByType<StellarApiClient>();
                if (apiClient == null)
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
                var walletManager = FindFirstObjectByType<StellarWalletManager>();
                if (walletManager == null)
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
                var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                bool processorFound = false;
                
                foreach (var mb in allMonoBehaviours)
                {
                    if (mb.GetType().Name == "QRPaymentProcessor")
                    {
                        processorFound = true;
                        Debug.Log("QRPaymentProcessor already exists");
                        break;
                    }
                }
                
                if (!processorFound)
                {
                    Debug.Log("Creating QRPaymentProcessor...");
                    var processorObj = new GameObject("QRPaymentProcessor");
                    
                    var qrPaymentProcessorType = System.Type.GetType("BlockchainMaps.QRPaymentProcessor");
                    if (qrPaymentProcessorType != null)
                    {
                        processorObj.AddComponent(qrPaymentProcessorType);
                        DontDestroyOnLoad(processorObj);
                        Debug.Log("QRPaymentProcessor created successfully");
                    }
                    else
                    {
                        Debug.LogError("QRPaymentProcessor type not found");
                        Destroy(processorObj);
                    }
                }
                
                // Create StellarQRManager if it doesn't exist
                var qrManager = FindFirstObjectByType<StellarQRManager>();
                if (qrManager == null)
                {
                    Debug.Log("Creating StellarQRManager...");
                    var qrManagerObj = new GameObject("StellarQRManager");
                    qrManagerObj.AddComponent<StellarQRManager>();
                    DontDestroyOnLoad(qrManagerObj);
                    Debug.Log("StellarQRManager created successfully");
                }
                else
                {
                    Debug.Log("StellarQRManager already exists");
                }
                
                Debug.Log("=== Backend Components Creation Complete ===");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating backend components: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Create an instance of the bootstrap if it doesn't exist
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (instance == null && !isBootstrapped)
            {
                Debug.Log("=== Creating StellarBackendBootstrap via RuntimeInitializeOnLoadMethod ===");
                var bootstrapObj = new GameObject("StellarBackendBootstrap");
                bootstrapObj.AddComponent<StellarBackendBootstrap>();
            }
        }
    }
} 