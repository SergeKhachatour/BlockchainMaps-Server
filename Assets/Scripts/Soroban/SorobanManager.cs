using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace BlockchainMaps.Soroban
{
    public class SorobanManager : MonoBehaviour
    {
        private static SorobanManager instance;
        public static SorobanManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("SorobanManager");
                    instance = go.AddComponent<SorobanManager>();
                    // Ensure initialization starts immediately
                    instance.InitializeSorobanSDK();
                }
                return instance;
            }
        }

        private bool isInitialized = false;

        // WebGL JavaScript functions are no longer available - using fallback approach
        #if !UNITY_EDITOR && UNITY_WEBGL
        // Placeholder for future WebGL implementation
        #endif

        // Events
        public event Action<string> OnContractResponse;
        public event Action<string> OnContractState;
        public event Action<string> OnSorobanError;
        public event Action OnSorobanInitialized;

        void Awake()
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

        void Start()
        {
            // Only initialize if not already initialized (to avoid double initialization)
            if (!isInitialized)
            {
                InitializeSorobanSDK();
            }
        }

        public void InitializeSorobanSDK()
        {
            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                Debug.Log("[SorobanManager] WebGL Soroban SDK initialization - using fallback mode");
                
                // For WebGL, we'll use a fallback approach since the JavaScript functions are not available
                isInitialized = true;
                Debug.Log("[SorobanManager] Soroban SDK initialized in fallback mode for WebGL");
                OnSorobanInitialized?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SorobanManager] Critical error initializing Soroban SDK: {e.Message}");
                isInitialized = false;
                OnSorobanError?.Invoke(e.Message);
            }
            #else
            Debug.Log("[SorobanManager] Soroban SDK is only available in WebGL builds");
            isInitialized = true;
            OnSorobanInitialized?.Invoke();
            #endif
        }

        public async Task<string> ExecuteContract(string contractId, string method, object[] args)
        {
            // Wait for initialization if not already initialized
            if (!isInitialized)
            {
                Debug.Log("[SorobanManager] Waiting for Soroban SDK to initialize...");
                int maxWaitTime = 10000; // 10 seconds
                int waitTime = 0;
                while (!isInitialized && waitTime < maxWaitTime)
                {
                    await Task.Delay(100);
                    waitTime += 100;
                }
                
                if (!isInitialized)
                {
                    Debug.LogWarning("[SorobanManager] Soroban SDK failed to initialize within timeout period, using fallback");
                    // Return a fallback response instead of throwing an exception
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = "Soroban SDK not available", 
                        result = "fallback_response" 
                    });
                }
            }

            var tcs = new TaskCompletionSource<string>();

            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                var parameters = new
                {
                    contractId = contractId,
                    method = method,
                    args = args
                };

                string paramsJson = JsonConvert.SerializeObject(parameters);
                Debug.Log($"[SorobanManager] Executing contract method: {paramsJson}");
                
                // Set up one-time event handler for the response
                Action<string> responseHandler = null;
                responseHandler = (response) =>
                {
                    OnContractResponse -= responseHandler;
                    tcs.SetResult(response);
                };
                OnContractResponse += responseHandler;

                // Set up one-time error handler
                Action<string> errorHandler = null;
                errorHandler = (error) =>
                {
                    OnSorobanError -= errorHandler;
                    Debug.LogWarning($"[SorobanManager] Soroban error, using fallback: {error}");
                    // Return fallback response instead of throwing exception
                    tcs.SetResult(JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = error, 
                        result = "fallback_response" 
                    }));
                };
                OnSorobanError += errorHandler;

                // WebGL JavaScript functions are not available - using fallback
                Debug.LogWarning("[SorobanManager] WebGL JavaScript functions not available, using fallback response");
                tcs.SetResult(JsonConvert.SerializeObject(new { 
                    success = false, 
                    error = "WebGL JavaScript functions not available", 
                    result = "fallback_response" 
                }));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SorobanManager] Error executing contract: {e.Message}");
                tcs.SetResult(JsonConvert.SerializeObject(new { 
                    success = false, 
                    error = e.Message, 
                    result = "fallback_response" 
                }));
            }
            #else
            // Simulate contract execution in editor
            await Task.Delay(100);
            tcs.SetResult(JsonConvert.SerializeObject(new { 
                success = true, 
                result = "simulated_response" 
            }));
            #endif

            return await tcs.Task;
        }

        public async Task<string> GetContractStateAsync(string contractId)
        {
            // Wait for initialization if not already initialized
            if (!isInitialized)
            {
                Debug.Log("[SorobanManager] Waiting for Soroban SDK to initialize...");
                int maxWaitTime = 10000; // 10 seconds
                int waitTime = 0;
                while (!isInitialized && waitTime < maxWaitTime)
                {
                    await Task.Delay(100);
                    waitTime += 100;
                }
                
                if (!isInitialized)
                {
                    Debug.LogWarning("[SorobanManager] Soroban SDK failed to initialize within timeout period, using fallback");
                    // Return a fallback response instead of throwing an exception
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = "Soroban SDK not available", 
                        state = "fallback_state" 
                    });
                }
            }

            var tcs = new TaskCompletionSource<string>();

            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                Debug.Log($"[SorobanManager] Getting contract state for: {contractId}");
                
                // Set up one-time event handler for the state
                Action<string> stateHandler = null;
                stateHandler = (state) =>
                {
                    OnContractState -= stateHandler;
                    tcs.SetResult(state);
                };
                OnContractState += stateHandler;

                // Set up one-time error handler
                Action<string> errorHandler = null;
                errorHandler = (error) =>
                {
                    OnSorobanError -= errorHandler;
                    Debug.LogWarning($"[SorobanManager] Soroban error, using fallback: {error}");
                    // Return fallback response instead of throwing exception
                    tcs.SetResult(JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = error, 
                        state = "fallback_state" 
                    }));
                };
                OnSorobanError += errorHandler;

                // WebGL JavaScript functions are not available - using fallback
                Debug.LogWarning("[SorobanManager] WebGL JavaScript functions not available, using fallback state");
                tcs.SetResult(JsonConvert.SerializeObject(new { 
                    success = false, 
                    error = "WebGL JavaScript functions not available", 
                    state = "fallback_state" 
                }));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SorobanManager] Error getting contract state: {e.Message}");
                tcs.SetResult(JsonConvert.SerializeObject(new { 
                    success = false, 
                    error = e.Message, 
                    state = "fallback_state" 
                }));
            }
            #else
            // Simulate contract state in editor
            await Task.Delay(100);
            tcs.SetResult(JsonConvert.SerializeObject(new { 
                success = true, 
                state = "simulated_state" 
            }));
            #endif

            return await tcs.Task;
        }

        // Called from JavaScript
        public void HandleSorobanInitialized(string message)
        {
            Debug.Log($"[SorobanManager] Soroban SDK initialized: {message}");
            isInitialized = true;
            OnSorobanInitialized?.Invoke();
        }

        // Called from JavaScript
        public void HandleContractResponse(string response)
        {
            Debug.Log($"[SorobanManager] Contract response: {response}");
            OnContractResponse?.Invoke(response);
        }

        // Called from JavaScript
        public void HandleContractState(string state)
        {
            Debug.Log($"[SorobanManager] Contract state: {state}");
            OnContractState?.Invoke(state);
        }

        // Called from JavaScript
        public void HandleSorobanError(string error)
        {
            Debug.LogError($"[SorobanManager] Soroban error: {error}");
            OnSorobanError?.Invoke(error);
        }

        public bool IsInitialized()
        {
            return isInitialized;
        }
    }
} 