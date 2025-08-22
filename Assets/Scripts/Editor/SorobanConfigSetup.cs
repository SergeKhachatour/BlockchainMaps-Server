using UnityEngine;
using UnityEditor;
using BlockchainMaps.Soroban;

namespace BlockchainMaps.Editor
{
    public class SorobanConfigSetup
    {
        private const string CONFIG_PATH = "Assets/Resources/SorobanConfig.asset";

        [MenuItem("BlockchainMaps/Setup/Create Soroban Config")]
        public static void CreateSorobanConfig()
        {
            // Check if config already exists
            var config = AssetDatabase.LoadAssetAtPath<SorobanConfig>(CONFIG_PATH);
            if (config != null)
            {
                Debug.Log("SorobanConfig already exists at " + CONFIG_PATH);
                Selection.activeObject = config;
                return;
            }

            // Create config
            config = ScriptableObject.CreateInstance<SorobanConfig>();
            
            // Set default values
            config.rpcUrl = "https://soroban-testnet.stellar.org";
            config.networkPassphrase = "Test SDF Network ; September 2015";
            config.markerFactoryContractId = "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";
            config.defaultGasLimit = 100000;
            config.defaultBaseFee = 100;

            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(CONFIG_PATH);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Create asset
            AssetDatabase.CreateAsset(config, CONFIG_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log("Created SorobanConfig at " + CONFIG_PATH);
            Selection.activeObject = config;
        }
    }
} 