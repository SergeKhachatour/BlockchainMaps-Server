using UnityEngine;
using UnityEditor;
using BlockchainMaps.OnlineMaps;

namespace BlockchainMaps.Editor
{
    public class MapFeaturesConfigSetup : UnityEditor.Editor
    {
        private const string CONFIG_PATH = "Assets/Resources/DefaultMapFeaturesConfig.asset";
        private const string MATERIAL_PATH = "Assets/Resources/DefaultBuildingMaterial.mat";

        [MenuItem("BlockchainMaps/Setup/Create Map Features Config", false, 11)]
        public static void CreateMapFeaturesConfig()
        {
            // Check if config already exists
            var config = AssetDatabase.LoadAssetAtPath<MapFeaturesConfig>(CONFIG_PATH);
            if (config != null)
            {
                Debug.Log("DefaultMapFeaturesConfig already exists at " + CONFIG_PATH);
                Selection.activeObject = config;
                return;
            }

            // Create config
            config = ScriptableObject.CreateInstance<MapFeaturesConfig>();
            
            // Set default values
            config.elevationScale = 1f;
            config.autoUpdateElevation = true;
            config.elevationUpdateInterval = 1f;
            config.maxElevation = 8848f;

            config.buildingScale = 1f;
            config.autoUpdateBuildings = true;
            config.buildingUpdateInterval = 1f;
            config.maxBuildingHeight = 828f;
            config.buildingDetailDistance = 1000f;
            config.maxBuildingsPerUpdate = 50;

            // Create the default building material
            var material = new Material(Shader.Find("Standard"));
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            material.color = new Color(0.3f, 0.5f, 0.85f, 0.5f);

            // Ensure directory exists
            string materialDirectory = System.IO.Path.GetDirectoryName(MATERIAL_PATH);
            if (!System.IO.Directory.Exists(materialDirectory))
            {
                System.IO.Directory.CreateDirectory(materialDirectory);
            }

            // Delete existing material if it exists
            if (System.IO.File.Exists(MATERIAL_PATH))
            {
                AssetDatabase.DeleteAsset(MATERIAL_PATH);
            }

            // Create new material asset
            AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Assign the material to config
            config.defaultBuildingMaterial = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);

            // Ensure directory exists for config
            string configDirectory = System.IO.Path.GetDirectoryName(CONFIG_PATH);
            if (!System.IO.Directory.Exists(configDirectory))
            {
                System.IO.Directory.CreateDirectory(configDirectory);
            }

            // Create config asset
            AssetDatabase.CreateAsset(config, CONFIG_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Created DefaultMapFeaturesConfig at " + CONFIG_PATH);
            Selection.activeObject = config;
        }
    }
} 