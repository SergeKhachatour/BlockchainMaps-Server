using UnityEngine;
using BlockchainMaps.Soroban;

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

        [Header("Soroban Integration")]
        [SerializeField] private SorobanConfig sorobanConfig;

        public string GetConfigJson()
        {
            var config = new
            {
                rpcUrl = sorobanConfig != null ? sorobanConfig.rpcUrl : this.rpcUrl,
                networkPassphrase = sorobanConfig != null ? sorobanConfig.networkPassphrase : this.networkPassphrase,
                factoryContractId = sorobanConfig != null ? sorobanConfig.markerFactoryContractId : this.factoryContractId,
                requireBiometrics = this.requireBiometrics,
                allowSecurityKeys = this.allowSecurityKeys,
                sessionTimeout = this.sessionTimeout
            };

            return JsonUtility.ToJson(config);
        }

        private void OnValidate()
        {
            if (sorobanConfig != null)
            {
                // Sync values with SorobanConfig
                rpcUrl = sorobanConfig.rpcUrl;
                networkPassphrase = sorobanConfig.networkPassphrase;
                factoryContractId = sorobanConfig.markerFactoryContractId;
            }
        }
    }
} 