using UnityEngine;
using System.Threading.Tasks;
using BlockchainMaps.Authentication;

namespace BlockchainMaps.Tests
{
    public class PasskeyKitTest : MonoBehaviour
    {
        [Header("Test Settings")]
        public bool runTestOnStart = false;
        public string testUsername = "testuser";

        private PasskeyManager passkeyManager;

        void Start()
        {
            if (runTestOnStart)
            {
                StartCoroutine(RunTest());
            }
        }

        private System.Collections.IEnumerator RunTest()
        {
            Debug.Log("=== Starting Official PasskeyKit Integration Test ===");
            
            // Wait a moment for everything to initialize
            yield return new WaitForSeconds(2f);
            
            // Get PasskeyManager instance
            passkeyManager = PasskeyManager.Instance;
            if (passkeyManager == null)
            {
                Debug.LogError("PasskeyManager instance not found!");
                yield break;
            }
            
            Debug.Log("PasskeyManager instance found");
            
            // Test bridge readiness
            bool bridgeReady = passkeyManager.IsBridgeReady();
            Debug.Log($"JavaScript bridge ready: {bridgeReady}");
            
            if (!bridgeReady)
            {
                Debug.LogWarning("JavaScript bridge not ready, waiting...");
                yield return new WaitForSeconds(3f);
                bridgeReady = passkeyManager.IsBridgeReady();
                Debug.Log($"JavaScript bridge ready after wait: {bridgeReady}");
            }
            
            // Test initialization
            if (!passkeyManager.IsInitialized())
            {
                Debug.Log("Initializing Official PasskeyKit...");
                passkeyManager.InitializePasskeyKit();
                
                // Wait for initialization
                yield return new WaitForSeconds(3f);
                
                bool initialized = passkeyManager.IsInitialized();
                Debug.Log($"Official PasskeyKit initialized: {initialized}");
                
                if (!initialized)
                {
                    Debug.LogError("Failed to initialize Official PasskeyKit!");
                    yield break;
                }
            }
            
            // Test status
            string status = passkeyManager.GetStatus();
            Debug.Log($"PasskeyKit status: {status}");
            
            // Test authentication (this will trigger the actual passkey flow)
            Debug.Log($"Testing authentication for user: {testUsername}");
            
            // Use Task.Run to handle async operations in coroutine
            var authTask = Task.Run(async () => await passkeyManager.Authenticate(testUsername));
            
            // Wait for authentication to complete
            while (!authTask.IsCompleted)
            {
                yield return null;
            }
            
            bool authenticated = false;
            try
            {
                authenticated = authTask.Result;
                Debug.Log($"Authentication result: {authenticated}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Authentication failed: {e.Message}");
                yield break;
            }
            
            if (authenticated)
            {
                Debug.Log("Authentication successful! Testing transaction signing...");
                
                // Test transaction signing
                string testTransaction = "AAAAAA=="; // Base64 encoded test transaction
                var signTask = Task.Run(async () => await passkeyManager.SignTransaction(testTransaction));
                
                // Wait for signing to complete
                while (!signTask.IsCompleted)
                {
                    yield return null;
                }
                
                try
                {
                    string signature = signTask.Result;
                    Debug.Log($"Transaction signing result: {signature}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Transaction signing failed: {e.Message}");
                }
            }
            
            Debug.Log("=== Official PasskeyKit Integration Test Complete ===");
        }

        [ContextMenu("Run PasskeyKit Test")]
        public void RunTestManually()
        {
            StartCoroutine(RunTest());
        }

        [ContextMenu("Check Bridge Status")]
        public void CheckBridgeStatus()
        {
            if (passkeyManager == null)
                passkeyManager = PasskeyManager.Instance;
                
            if (passkeyManager != null)
            {
                bool bridgeReady = passkeyManager.IsBridgeReady();
                bool initialized = passkeyManager.IsInitialized();
                string status = passkeyManager.GetStatus();
                
                Debug.Log($"Bridge Ready: {bridgeReady}");
                Debug.Log($"Initialized: {initialized}");
                Debug.Log($"Status: {status}");
            }
            else
            {
                Debug.LogError("PasskeyManager not found!");
            }
        }
    }
} 