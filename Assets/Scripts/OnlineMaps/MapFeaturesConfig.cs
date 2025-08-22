using UnityEngine;

namespace BlockchainMaps.OnlineMaps
{
    [CreateAssetMenu(fileName = "MapFeaturesConfig", menuName = "BlockchainMaps/Map Features Config")]
    public class MapFeaturesConfig : ScriptableObject
    {
        private static MapFeaturesConfig defaultInstance;
        public static MapFeaturesConfig Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = Resources.Load<MapFeaturesConfig>("DefaultMapFeaturesConfig");
                    if (defaultInstance == null)
                    {
                        Debug.LogWarning("Default MapFeaturesConfig not found in Resources. Using fallback values.");
                        defaultInstance = CreateInstance<MapFeaturesConfig>();
                        defaultInstance.SetDefaultValues();
                    }
                }
                return defaultInstance;
            }
        }

        [Header("Elevation Settings")]
        public float elevationScale = 1f;
        public bool autoUpdateElevation = true;
        public float elevationUpdateInterval = 1f;
        public float maxElevation = 8848f;

        [Header("Building Settings")]
        public float buildingScale = 1f;
        public Material defaultBuildingMaterial;
        public bool autoUpdateBuildings = true;
        public float buildingUpdateInterval = 1f;
        public float maxBuildingHeight = 828f;

        [Header("Level of Detail")]
        public float buildingDetailDistance = 1000f;
        public int maxBuildingsPerUpdate = 50;

        [Header("Memory Management")]
        public int maxTileDownloads = 2;
        public bool useMemoryCache = true;
        public int maxMemoryCacheSize = 512;
        public float memoryCacheUnloadRate = 0.5f;
        public int maxConcurrentBuildings = 500;
        public float buildingCullingDistance = 2000f;

        private void SetDefaultValues()
        {
            elevationScale = 1f;
            autoUpdateElevation = true;
            elevationUpdateInterval = 1f;
            maxElevation = 8848f;

            buildingScale = 1f;
            autoUpdateBuildings = true;
            buildingUpdateInterval = 1f;
            maxBuildingHeight = 828f;
            buildingDetailDistance = 1000f;
            maxBuildingsPerUpdate = 50;

            maxTileDownloads = 2;
            useMemoryCache = true;
            maxMemoryCacheSize = 512;
            memoryCacheUnloadRate = 0.5f;
            maxConcurrentBuildings = 500;
            buildingCullingDistance = 2000f;
        }

        public void ApplyToManagers(GameObject mapObject)
        {
            if (mapObject == null)
            {
                Debug.LogError("Cannot apply settings: Map object is null");
                return;
            }

            try
            {
                // Apply elevation settings
                var elevationManager = mapObject.GetComponent<ElevationManager>();
                if (elevationManager != null)
                {
                    elevationManager.SetElevationScale(elevationScale);
                    elevationManager.ToggleAutoUpdate(autoUpdateElevation);
                }

                // Apply building settings
                var buildingsManager = mapObject.GetComponent<BuildingsManager>();
                if (buildingsManager != null)
                {
                    buildingsManager.SetBuildingScale(buildingScale);
                    if (defaultBuildingMaterial != null)
                    {
                        buildingsManager.SetBuildingMaterial(defaultBuildingMaterial);
                    }
                    buildingsManager.ToggleAutoUpdate(autoUpdateBuildings);
                }

                // Apply tile management settings
                var onlineMaps = mapObject.GetComponent<global::OnlineMaps>();
                if (onlineMaps != null)
                {
                    try
                    {
                        // Configure tile management
                        global::OnlineMapsTileManager.maxTileDownloads = maxTileDownloads;
                        
                        // Configure memory cache
                        var cache = mapObject.GetComponent<OnlineMapsCache>();
                        if (cache != null)
                        {
                            cache.useMemoryCache = useMemoryCache;
                            cache.maxMemoryCacheSize = maxMemoryCacheSize;
                            cache.memoryCacheUnloadRate = memoryCacheUnloadRate;
                        }

                        // Configure building limits
                        var buildingsComponent = mapObject.GetComponent<OnlineMapsBuildings>();
                        if (buildingsComponent != null)
                        {
                            buildingsComponent.maxActiveBuildings = maxConcurrentBuildings;
                            buildingsComponent.maxBuilding = (int)buildingCullingDistance;
                        }

                        // Enable texture streaming with safe defaults
                        QualitySettings.streamingMipmapsActive = true;
                        QualitySettings.streamingMipmapsMemoryBudget = Mathf.Max(512, maxMemoryCacheSize);
                        QualitySettings.streamingMipmapsMaxLevelReduction = 2;
                        QualitySettings.streamingMipmapsAddAllCameras = false;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error applying tile management settings: {e.Message}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error applying map features configuration: {e.Message}\n{e.StackTrace}");
            }
        }

        private void OnValidate()
        {
            // Ensure values are within reasonable ranges
            maxBuildingsPerUpdate = Mathf.Clamp(maxBuildingsPerUpdate, 1, 1000);
            maxTileDownloads = Mathf.Clamp(maxTileDownloads, 1, 10);
            maxMemoryCacheSize = Mathf.Clamp(maxMemoryCacheSize, 128, 2048);
            memoryCacheUnloadRate = Mathf.Clamp01(memoryCacheUnloadRate);
            maxConcurrentBuildings = Mathf.Clamp(maxConcurrentBuildings, 100, 5000);
            buildingCullingDistance = Mathf.Clamp(buildingCullingDistance, 500f, 5000f);
        }
    }
} 