using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BlockchainMaps.Authentication
{
    public class PasskeyManager : MonoBehaviour
    {
        private static PasskeyManager instance;
        public static PasskeyManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject existingManager = GameObject.Find("PasskeyManager");
                    if (existingManager != null)
                    {
                        instance = existingManager.GetComponent<PasskeyManager>();
                    }
                    
                    if (instance == null)
                    {
                        GameObject go = new GameObject("PasskeyManager");
                        instance = go.AddComponent<PasskeyManager>();
                        Debug.Log("[PasskeyManager] Created new instance");
                    }
                }
                return instance;
            }
        }

        private bool isInitialized = false;
        private bool isAuthenticated = false;

        // Import JavaScript functions
        #if !UNITY_EDITOR && UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void InitializePasskey();

        [DllImport("__Internal")]
        private static extern void AuthenticateUser(string username);

        [DllImport("__Internal")]
        private static extern void SignStellarTransaction(string txData);

        [DllImport("__Internal")]
        private static extern void LogOffPasskey();
        #endif

        // Events
        private event Action onPasskeyCreated;
        private event Action<string> onAuthenticationError;
        private event Action<string> onTransactionSigned;

        public event Action OnPasskeyCreated
        {
            add { onPasskeyCreated += value; }
            remove { onPasskeyCreated -= value; }
        }

        public event Action<string> OnAuthenticationError
        {
            add { onAuthenticationError += value; }
            remove { onAuthenticationError -= value; }
        }

        public event Action<string> OnTransactionSigned
        {
            add { onTransactionSigned += value; }
            remove { onTransactionSigned -= value; }
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                gameObject.name = "PasskeyManager"; // Ensure the GameObject has the exact name
                DontDestroyOnLoad(gameObject);
                Debug.Log("[PasskeyManager] Instance created and set to DontDestroyOnLoad");
            }
            else if (instance != this)
            {
                Debug.Log("[PasskeyManager] Duplicate instance found, destroying this instance");
                Destroy(gameObject);
            }
        }

        void Start()
        {
            Debug.Log($"[PasskeyManager] Starting initialization... GameObject name: {gameObject.name}");
            InitializePasskeyKit();
        }

        public void InitializePasskeyKit()
        {
            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                if (gameObject.name != "PasskeyManager")
                {
                    Debug.LogWarning("[PasskeyManager] GameObject name is not 'PasskeyManager', fixing...");
                    gameObject.name = "PasskeyManager";
                }
                Debug.Log("[PasskeyManager] Calling InitializePasskey...");
                isInitialized = false; // Reset initialization flag
                InitializePasskey();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyManager] Error initializing PasskeyKit: {e.Message}");
                onAuthenticationError?.Invoke(e.Message);
            }
            #else
            Debug.Log("[PasskeyManager] PasskeyKit is only available in WebGL builds");
            // For testing in editor
            isInitialized = true;
            HandlePasskeyCreated("Editor Mode");
            #endif
        }

        public async Task<bool> Authenticate(string username)
        {
            if (!isInitialized)
            {
                Debug.LogError("[PasskeyManager] PasskeyKit not initialized. Attempting to initialize...");
                InitializePasskeyKit();
                await Task.Delay(1000); // Give it a moment to initialize
                if (!isInitialized)
                {
                    Debug.LogError("[PasskeyManager] Failed to initialize PasskeyKit");
                    return false;
                }
            }

            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                Debug.Log($"[PasskeyManager] Authenticating user: {username}");
                AuthenticateUser(username);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyManager] Authentication error: {e.Message}");
                onAuthenticationError?.Invoke(e.Message);
                return false;
            }
            #else
            Debug.Log($"[PasskeyManager] Authentication simulated for user: {username}");
            isAuthenticated = true;
            return true;
            #endif
        }

        public async Task<string> SignTransaction(string transactionXdr)
        {
            if (!isInitialized || !isAuthenticated)
            {
                throw new InvalidOperationException("Must be initialized and authenticated to sign transactions");
            }

            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                SignStellarTransaction(transactionXdr);
                return "transaction_signed"; // The actual signature will come through OnTransactionSigned event
            }
            catch (Exception e)
            {
                Debug.LogError($"Transaction signing error: {e.Message}");
                onAuthenticationError?.Invoke(e.Message);
                return null;
            }
            #else
            Debug.Log($"Transaction signing simulated for XDR: {transactionXdr}");
            return "simulated_signature";
            #endif
        }

        // Called from JavaScript
        public void HandlePasskeyCreated(string message)
        {
            Debug.Log($"[PasskeyManager] PasskeyKit initialized: {message}");
            isInitialized = true;
            onPasskeyCreated?.Invoke();
        }

        // Called from JavaScript
        public void HandleAuthenticationError(string error)
        {
            Debug.LogError($"[PasskeyManager] PasskeyKit error: {error}");
            onAuthenticationError?.Invoke(error);
        }

        // Called from JavaScript
        public void HandleAuthenticationSuccess()
        {
            Debug.Log("[PasskeyManager] Authentication successful");
            isAuthenticated = true;
        }

        public void LogOff()
        {
            Debug.Log("[PasskeyManager] Logging off...");
            isAuthenticated = false;
            isInitialized = false; // Reset initialization to ensure fresh start on next login
            
            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                LogOffPasskey();
                Debug.Log("[PasskeyManager] Successfully logged off");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyManager] Error during log off: {e.Message}");
            }
            #endif
        }

        public bool IsAuthenticated()
        {
            return isAuthenticated;
        }

        public bool IsInitialized()
        {
            return isInitialized;
        }
    }
} 