using UnityEngine;

namespace BlockchainMaps.Authentication
{
    [CreateAssetMenu(fileName = "New Auth Config", menuName = "Game/Authentication Config")]
    public class AuthenticationConfig : ScriptableObject
    {
        [Header("Stellar Configuration")]
        public string rpcUrl = "https://soroban-testnet.stellar.org";
        public string networkPassphrase = "Test SDF Network ; September 2015";
        public string factoryContractId;

        [Header("Security Settings")]
        public bool requireBiometrics = true;
        public bool allowSecurityKeys = true;
        public float sessionTimeout = 3600f; // 1 hour in seconds

        public string GetConfigJson()
        {
            return JsonUtility.ToJson(this);
        }
    }
} 