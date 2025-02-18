using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
                }
                return instance;
            }
        }

        private bool isInitialized = false;

        // Import JavaScript functions
        #if !UNITY_EDITOR && UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void InitializeSoroban();

        [DllImport("__Internal")]
        private static extern void ExecuteContractMethod(string paramsJson);

        [DllImport("__Internal")]
        private static extern void GetContractState(string contractId);
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
            InitializeSorobanSDK();
        }

        public void InitializeSorobanSDK()
        {
            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                Debug.Log("[SorobanManager] Initializing Soroban SDK...");
                isInitialized = false;
                InitializeSoroban();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SorobanManager] Error initializing Soroban SDK: {e.Message}");
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
            if (!isInitialized)
            {
                throw new InvalidOperationException("Soroban SDK not initialized");
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
                    tcs.SetException(new Exception(error));
                };
                OnSorobanError += errorHandler;

                ExecuteContractMethod(paramsJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SorobanManager] Error executing contract: {e.Message}");
                tcs.SetException(e);
            }
            #else
            // Simulate contract execution in editor
            await Task.Delay(100);
            tcs.SetResult("{ \"success\": true, \"result\": \"simulated_response\" }");
            #endif

            return await tcs.Task;
        }

        public async Task<string> GetContractStateAsync(string contractId)
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("Soroban SDK not initialized");
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
                    tcs.SetException(new Exception(error));
                };
                OnSorobanError += errorHandler;

                GetContractState(contractId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SorobanManager] Error getting contract state: {e.Message}");
                tcs.SetException(e);
            }
            #else
            // Simulate contract state in editor
            await Task.Delay(100);
            tcs.SetResult("{ \"state\": \"simulated_state\" }");
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