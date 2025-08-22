using UnityEngine;

namespace BlockchainMaps
{
    /// <summary>
    /// WebGL-specific configuration and safety checks
    /// </summary>
    public class WebGLConfig : MonoBehaviour
    {
        [Header("WebGL Settings")]
        [SerializeField] private bool enableWebGLOptimizations = true;
        [SerializeField] private bool disableAudioOnStart = true;
        
        // Properties to use the fields
        public bool EnableWebGLOptimizations => enableWebGLOptimizations;
        public bool DisableAudioOnStart => disableAudioOnStart;
        
        void Awake()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            ConfigureWebGL();
            #endif
        }
        
        void Start()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            if (DisableAudioOnStart)
            {
                // Disable audio context to prevent browser warnings
                AudioListener.volume = 0f;
                Debug.Log("[WebGLConfig] Audio disabled for WebGL build");
            }
            #endif
        }
        
        private void ConfigureWebGL()
        {
            Debug.Log("[WebGLConfig] Configuring WebGL-specific settings");
            
            // Set target frame rate for WebGL
            Application.targetFrameRate = 60;
            
            // Disable vsync for WebGL
            QualitySettings.vSyncCount = 0;
            
            // Set WebGL memory settings
            #if UNITY_WEBGL && !UNITY_EDITOR
            if (EnableWebGLOptimizations)
            {
                // These settings help with WebGL memory management
                QualitySettings.globalTextureMipmapLimit = 1; // Reduce texture quality for WebGL
                QualitySettings.shadowDistance = 50f; // Reduce shadow distance for WebGL
            }
            #endif
            
            Debug.Log("[WebGLConfig] WebGL configuration complete");
        }
        
        /// <summary>
        /// Safe method to call WebGL-specific functions
        /// </summary>
        public static void SafeWebGLCall(System.Action action)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                action?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WebGLConfig] WebGL call failed: {e.Message}");
            }
            #endif
        }
    }
} 