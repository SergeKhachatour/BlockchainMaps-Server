using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using BlockchainMaps;

namespace BlockchainMaps.OnlineMaps
{
    [RequireComponent(typeof(global::OnlineMaps))]
    [RequireComponent(typeof(global::OnlineMapsBuildings))]
    public class BuildingsManager : MonoBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] private float buildingScale = 1f;
        [SerializeField] private bool autoUpdateBuildings = true;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private MapFeaturesConfig config;

        private global::OnlineMaps map;
        private global::OnlineMapsBuildings buildingsManager;
        private float lastUpdateTime;

        private void Awake()
        {
            map = GetComponent<global::OnlineMaps>();
            buildingsManager = GetComponent<global::OnlineMapsBuildings>();

            if (buildingsManager == null)
            {
                buildingsManager = gameObject.AddComponent<global::OnlineMapsBuildings>();
                Debug.Log("Added OnlineMapsBuildings component");
            }

            // Configure buildings manager
            ConfigureBuildingsManager();
        }

        private void ConfigureBuildingsManager()
        {
            try
            {
                if (config == null)
                {
                    config = MapFeaturesConfig.Default;
                }

                buildingScale = config.buildingScale;
                autoUpdateBuildings = config.autoUpdateBuildings;
                updateInterval = config.buildingUpdateInterval;

                if (buildingsManager != null)
                {
                    buildingsManager.enabled = autoUpdateBuildings;
                    buildingsManager.zoomRange = new global::OnlineMapsRange(17, 20);
                    buildingsManager.materials = new[] { new global::OnlineMapsBuildingMaterial { wall = config.defaultBuildingMaterial, roof = config.defaultBuildingMaterial } };
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error configuring BuildingsManager: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void Update()
        {
            if (!autoUpdateBuildings) return;

            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateBuildings();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdateBuildings()
        {
            if (buildingsManager == null || !buildingsManager.enabled) return;

            try
            {
                buildingsManager.heightScale = buildingScale;
                // Additional building update logic can be added here
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating buildings: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void SetBuildingScale(float scale)
        {
            buildingScale = Mathf.Max(scale, 0f);
            if (buildingsManager != null)
            {
                buildingsManager.heightScale = buildingScale;
            }
        }

        public void SetBuildingMaterial(Material material)
        {
            if (buildingsManager != null && material != null)
            {
                buildingsManager.materials = new[] { new global::OnlineMapsBuildingMaterial { wall = material, roof = material } };
            }
        }

        public void ToggleAutoUpdate(bool enable)
        {
            autoUpdateBuildings = enable;
            if (buildingsManager != null)
            {
                buildingsManager.enabled = enable;
            }
        }

        private void OnEnable()
        {
            if (map != null)
            {
                map.OnChangePosition += OnMapPositionChanged;
                map.OnChangeZoom += OnMapZoomChanged;
            }
        }

        private void OnDisable()
        {
            if (map != null)
            {
                map.OnChangePosition -= OnMapPositionChanged;
                map.OnChangeZoom -= OnMapZoomChanged;
            }
        }

        private void OnMapPositionChanged()
        {
            if (autoUpdateBuildings)
            {
                UpdateBuildings();
            }
        }

        private void OnMapZoomChanged()
        {
            if (autoUpdateBuildings)
            {
                UpdateBuildings();
            }
        }
    }
}