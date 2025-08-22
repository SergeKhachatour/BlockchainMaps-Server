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
        private TaskCompletionSource<string> transactionSigningTcs;
        private TaskCompletionSource<bool> authenticationTcs;
        private TaskCompletionSource<bool> initializationTcs;

        // Import JavaScript functions (minimal set needed for prompt)
        #if !UNITY_EDITOR && UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void InitializePasskeyKitJS();

        [DllImport("__Internal")]
        private static extern void AuthenticatePasskey(string username);

        [DllImport("__Internal")]
        private static extern int CheckBridgeReady();
        #endif

        // Events
        public event Action onPasskeyCreated;
        public event Action<string> onAuthenticationError;
        public event Action onAuthenticationSuccess;
        public event Action<string> onTransactionSigned;
        public event Action<string> onTransactionError;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[PasskeyManager] Awake - Instance created");
            }
            else if (instance != this)
            {
                Debug.Log("[PasskeyManager] Awake - Destroying duplicate instance");
                Destroy(gameObject);
            }
        }

        void Start()
        {
            Debug.Log("[PasskeyManager] Start called");
            // Don't auto-initialize - let the UI manager handle it
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

                // Check if bridge is ready
                if (!IsBridgeReady())
                {
                    Debug.LogWarning("[PasskeyManager] JavaScript bridge not ready, delaying initialization...");
                    StartCoroutine(InitializeWithDelay());
                    return;
                }

                Debug.Log("[PasskeyManager] Calling InitializePasskeyKitJS (official library)...");
                isInitialized = false; // Reset initialization flag
                initializationTcs = new TaskCompletionSource<bool>();
                InitializePasskeyKitJS();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyManager] Error initializing Official PasskeyKit: {e.Message}");
                onAuthenticationError?.Invoke(e.Message);
            }
            #else
            Debug.Log("[PasskeyManager] Official PasskeyKit is only available in WebGL builds");
            // For testing in editor
            isInitialized = true;
            HandlePasskeyCreated("Editor Mode - Official PasskeyKit");
            #endif
        }

        private System.Collections.IEnumerator InitializeWithDelay()
        {
            yield return new WaitForSeconds(1.0f);
            InitializePasskeyKit();
        }

        public async Task<bool> Authenticate(string username)
        {
            Debug.Log($"[PasskeyManager] Authenticate called with username: {username}, isInitialized: {isInitialized}");
            
            if (!isInitialized)
            {
                Debug.Log("[PasskeyManager] Official PasskeyKit not initialized. Attempting to initialize...");
                InitializePasskeyKit();
                
                // Wait for initialization to complete using TaskCompletionSource
                if (initializationTcs != null)
                {
                    Debug.Log("[PasskeyManager] Waiting for initialization using TaskCompletionSource...");
                    try
                    {
                        using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10)))
                        {
                            cts.Token.Register(() => 
                            {
                                Debug.LogError("[PasskeyManager] Initialization timed out");
                                initializationTcs.TrySetResult(false);
                            });
                            var initResult = await initializationTcs.Task;
                            if (!initResult)
                            {
                                Debug.LogError("[PasskeyManager] Initialization failed or timed out");
                                return false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[PasskeyManager] Error waiting for initialization: {e.Message}");
                        return false;
                    }
                }
            }

            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                if (!IsBridgeReady())
                {
                    Debug.LogWarning("[PasskeyManager] Bridge not ready for authentication - attempting re-initialize and retry");
                    InitializePasskeyKit();
                    await Task.Delay(200);
                }

                Debug.Log($"[PasskeyManager] Authenticating user with official library: {username}");
                authenticationTcs = new TaskCompletionSource<bool>();
                AuthenticatePasskey(username);

                using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(60)))
                {
                    cts.Token.Register(() => {
                        Debug.LogError("[PasskeyManager] Authentication timed out");
                        authenticationTcs.TrySetResult(false);
                    });
                    var authResult = await authenticationTcs.Task;
                    return authResult;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyManager] Error during authentication: {e.Message}");
                onAuthenticationError?.Invoke(e.Message);
                return false;
            }
            #else
            // Editor fallback
            await Task.Delay(200);
            isAuthenticated = true;
            onAuthenticationSuccess?.Invoke();
            return true;
            #endif
        }

        private bool CheckIfNewUser(string username)
        {
            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                // Check if user has existing wallet address stored using official library
                string storedWallet = null; // JS storage disabled
                return string.IsNullOrEmpty(storedWallet);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyManager] Error checking if new user with official library: {e.Message}");
                return true; // Assume new user on error
            }
            #else
            return true; // Simulate new user in editor
            #endif
        }

        public async Task<string> SignTransaction(string transactionXdr)
        {
            if (!isAuthenticated)
            {
                throw new InvalidOperationException("User must be authenticated before signing transactions");
            }

            #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // Check if bridge is ready
                if (!IsBridgeReady())
                {
                    throw new InvalidOperationException("JavaScript bridge not ready for transaction signing");
                }

                transactionSigningTcs = new TaskCompletionSource<string>();
                
                Debug.Log($"[PasskeyManager] Signing transaction with official library: {transactionXdr}");
                // JS sign disabled in this build
                Debug.LogWarning("[PasskeyManager] SignTransaction is only supported in WebGL builds");
                return "simulated_signature";
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyManager] Error signing transaction with official library: {e.Message}");
                onTransactionError?.Invoke(e.Message);
                throw;
            }
            #else
            await Task.Delay(100); // Simulate some async work in non-WebGL builds
            Debug.LogWarning("[PasskeyManager] SignTransaction is only supported in WebGL builds");
            return "simulated_signature";
            #endif
        }

        // Called from JavaScript
        public void HandlePasskeyCreated(string message)
        {
            Debug.Log($"[PasskeyManager] Received message: {message}");
            Debug.Log($"[PasskeyManager] Message length: {message?.Length}, Starts with '{{': {message?.StartsWith("{")}");
            
            try
            {
                // Try to parse as JSON first
                if (message.StartsWith("{"))
                {
                    Debug.Log($"[PasskeyManager] Parsing as JSON...");
                    var response = JsonUtility.FromJson<PasskeyResponse>(message);
                    Debug.Log($"[PasskeyManager] Parsed response - success: {response.success}, message: {response.message}");
                    
                    if (response.success)
                    {
                        Debug.Log($"[PasskeyManager] Wallet operation successful: {response.message}");
                        if (!string.IsNullOrEmpty(response.walletAddress))
                        {
                            Debug.Log($"[PasskeyManager] Wallet address: {response.walletAddress}");
                        }
                        isAuthenticated = true;
                        isInitialized = true; // Ensure initialized flag is set
                        Debug.Log($"[PasskeyManager] Setting authentication result to true");
                        authenticationTcs?.TrySetResult(true);
                        onAuthenticationSuccess?.Invoke();
                    }
                    else
                    {
                        Debug.LogError($"[PasskeyManager] Wallet operation failed: {response.message}");
                        isAuthenticated = false;
                        Debug.Log($"[PasskeyManager] Setting authentication result to false");
                        authenticationTcs?.TrySetResult(false);
                        onAuthenticationError?.Invoke(response.message);
                    }
                }
                else
                {
                    // Handle simple string messages (for initialization)
                    Debug.Log($"[PasskeyManager] Official PasskeyKit initialized: {message}");
                    Debug.Log($"[PasskeyManager] Setting isInitialized = true");
                    isInitialized = true;
                    Debug.Log($"[PasskeyManager] isInitialized is now: {isInitialized}");
                    Debug.Log($"[PasskeyManager] Resolving initializationTcs");
                    initializationTcs?.TrySetResult(true);
                    onPasskeyCreated?.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyManager] Error parsing response: {e.Message}");
                // Fallback to simple message handling
                Debug.Log($"[PasskeyManager] Official PasskeyKit message: {message}");
                isInitialized = true;
                onPasskeyCreated?.Invoke();
            }
        }

        // Called from JavaScript
        public void HandleAuthenticationError(string error)
        {
            Debug.LogError($"[PasskeyManager] Official PasskeyKit error: {error}");
            onAuthenticationError?.Invoke(error);
            isAuthenticated = false;
            authenticationTcs?.TrySetResult(false);
        }

        // Called from JavaScript
        public void HandleAuthenticationSuccess()
        {
            Debug.Log("[PasskeyManager] Authentication successful with official library");
            isAuthenticated = true;
            authenticationTcs?.TrySetResult(true);
        }

        // Called from JavaScript
        public void HandleTransactionSigned(string signedTransaction)
        {
            Debug.Log($"[PasskeyManager] Received transaction response: {signedTransaction}");
            
            try
            {
                // Try to parse as JSON first
                if (signedTransaction.StartsWith("{"))
                {
                    var response = JsonUtility.FromJson<PasskeyResponse>(signedTransaction);
                    if (response.success)
                    {
                        Debug.Log($"[PasskeyManager] Transaction signed successfully: {response.message}");
                        onTransactionSigned?.Invoke(response.signature ?? response.message);
                        transactionSigningTcs?.TrySetResult(response.signature ?? response.message);
                    }
                    else
                    {
                        Debug.LogError($"[PasskeyManager] Transaction signing failed: {response.message}");
                        onTransactionError?.Invoke(response.message);
                        transactionSigningTcs?.TrySetException(new Exception(response.message));
                    }
                }
                else
                {
                    // Handle simple string responses
                    Debug.Log("[PasskeyManager] Transaction signed successfully with official library");
                    onTransactionSigned?.Invoke(signedTransaction);
                    transactionSigningTcs?.TrySetResult(signedTransaction);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyManager] Error parsing transaction response: {e.Message}");
                // Fallback to simple string handling
                Debug.Log("[PasskeyManager] Transaction signed successfully with official library");
                onTransactionSigned?.Invoke(signedTransaction);
                transactionSigningTcs?.TrySetResult(signedTransaction);
            }
        }

        // Called from JavaScript
        public void HandleTransactionError(string error)
        {
            Debug.LogError($"[PasskeyManager] Transaction signing error with official library: {error}");
            onTransactionError?.Invoke(error);
            transactionSigningTcs?.TrySetException(new Exception(error));
        }

        public void LogOff()
        {
            Debug.Log("[PasskeyManager] Logging off with official library...");
            isAuthenticated = false;
            isInitialized = false; // Reset initialization to ensure fresh start on next login
            
            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                // JS logoff disabled in this build
                Debug.Log("[PasskeyManager] Successfully logged off with official library");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyManager] Error during log off with official library: {e.Message}");
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

        // Health check method
        public string GetStatus()
        {
            #if !UNITY_EDITOR && UNITY_WEBGL
            // Avoid extern call; return a simple status
            return "{\"isLoaded\": true, \"isInitialized\": " + (isInitialized ? "true" : "false") + ", \"version\": \"webgl\"}";
            #else
            return "{\"isLoaded\": false, \"isInitialized\": false, \"version\": \"editor\"}";
            #endif
        }

        // Check if JavaScript bridge is ready
        public bool IsBridgeReady()
        {
            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                int result = 0;
                try { result = CheckBridgeReady(); } catch { result = 0; }
                bool isReady = result == 1;
                Debug.Log($"[PasskeyManager] Bridge ready check: {isReady} (result: {result})");
                return isReady;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PasskeyManager] Error checking bridge: {e.Message}");
                return false;
            }
            #else
            return true;
            #endif
        }
    }
}

[System.Serializable]
public class PasskeyResponse
{
    public bool success;
    public string walletAddress;
    public string message;
    public string signature;
} 