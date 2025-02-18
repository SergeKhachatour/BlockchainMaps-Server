using UnityEngine;

namespace BlockchainMaps.Soroban
{
    [CreateAssetMenu(fileName = "SorobanConfig", menuName = "BlockchainMaps/Soroban Config")]
    public class SorobanConfig : ScriptableObject
    {
        [Header("Network Settings")]
        public string rpcUrl = "https://soroban-testnet.stellar.org";
        public string networkPassphrase = "Test SDF Network ; September 2015";

        [Header("Contract Settings")]
        public string markerFactoryContractId;
        public string markerRegistryContractId;
        public string tokenContractId;

        [Header("Gas Settings")]
        public long defaultGasLimit = 100000;
        public long defaultBaseFee = 100;
    }
} 