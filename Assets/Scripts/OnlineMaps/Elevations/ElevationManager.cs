using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using BlockchainMaps;

namespace BlockchainMaps.OnlineMaps
{
    [RequireComponent(typeof(global::OnlineMaps))]
    public class ElevationManager : MonoBehaviour
    {
        [Header("Elevation Settings")]
        [SerializeField] private float elevationScale = 1f;
        [SerializeField] private bool autoUpdateElevation = true;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private float maxElevation = 8848f; // Height of Mount Everest
        [SerializeField] private MapFeaturesConfig config;
        
        private global::OnlineMaps map;
        private global::OnlineMapsArcGISElevationManager elevationManager;
        private float lastUpdateTime;

        private void Awake()
        {
            map = GetComponent<global::OnlineMaps>();
            elevationManager = GetComponent<global::OnlineMapsArcGISElevationManager>();
            
            if (elevationManager == null)
            {
                elevationManager = gameObject.AddComponent<global::OnlineMapsArcGISElevationManager>();
                Debug.Log("Added OnlineMapsArcGISElevationManager component");
            }

            // Configure elevation manager
            ConfigureElevationManager();
        }

        private void ConfigureElevationManager()
        {
            try
            {
                if (config == null)
                {
                    config = MapFeaturesConfig.Default;
                }

                elevationScale = config.elevationScale;
                autoUpdateElevation = config.autoUpdateElevation;
                updateInterval = config.elevationUpdateInterval;
                maxElevation = config.maxElevation;

                if (elevationManager != null)
                {
                    elevationManager.enabled = autoUpdateElevation;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error configuring ElevationManager: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void Update()
        {
            if (!autoUpdateElevation) return;

            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateElevation();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdateElevation()
        {
            if (elevationManager == null || !elevationManager.enabled) return;

            try
            {
                elevationManager.scale = elevationScale;
                // Additional elevation update logic can be added here
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating elevation: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void SetElevationScale(float scale)
        {
            elevationScale = Mathf.Clamp(scale, 0f, maxElevation);
            if (elevationManager != null)
            {
                elevationManager.scale = elevationScale;
            }
        }

        public void ToggleAutoUpdate(bool enable)
        {
            autoUpdateElevation = enable;
            if (elevationManager != null)
            {
                elevationManager.enabled = enable;
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
            if (autoUpdateElevation)
            {
                UpdateElevation();
            }
        }

        private void OnMapZoomChanged()
        {
            if (autoUpdateElevation)
            {
                UpdateElevation();
            }
        }

        void Start()
        {
            // Get references
            map = GetComponent<global::OnlineMaps>();
            elevationManager = GetComponent<global::OnlineMapsArcGISElevationManager>();

            if (elevationManager == null)
            {
                elevationManager = gameObject.AddComponent<global::OnlineMapsArcGISElevationManager>();
            }

            // Configure elevation settings
            if (config != null)
            {
                elevationManager.zoomRange = new global::OnlineMapsRange(5, 20); // Use fixed zoom range for now
                elevationManager.scale = elevationScale;
            }
            else
            {
                Debug.LogWarning("MapFeaturesConfig not assigned to ElevationManager");
                elevationManager.zoomRange = new global::OnlineMapsRange(12, 20);
                elevationManager.scale = 1f;
            }

            elevationManager.enabled = true;
        }
    }
} 