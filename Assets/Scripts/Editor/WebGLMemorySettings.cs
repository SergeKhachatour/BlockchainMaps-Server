using UnityEditor;
using UnityEngine;

namespace BlockchainMaps.Editor
{
    public static class WebGLMemorySettings
    {
        private const string MENU_PATH = "BlockchainMaps/Setup/Configure WebGL Memory";
        private const string PASSKEY_TEMPLATE = "PROJECT:Passkey";

        [MenuItem(MENU_PATH, false, 20)]
        public static void ConfigureWebGLMemory()
        {
            try
            {
                // Verify we're in the editor
                if (!Application.isEditor)
                {
                    Debug.LogError("WebGL memory configuration can only be done in the Unity Editor.");
                    return;
                }

                // Configure memory settings with streaming support
                PlayerSettings.WebGL.memorySize = 2048; // Set to 2GB initial size
                PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
                PlayerSettings.WebGL.threadsSupport = false;
                
                // Configure compression and caching
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
                PlayerSettings.WebGL.dataCaching = true;
                PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.External;
                PlayerSettings.WebGL.nameFilesAsHashes = true;
                
                // Set template and decompression
                if (string.IsNullOrEmpty(PlayerSettings.WebGL.template))
                {
                    PlayerSettings.WebGL.template = PASSKEY_TEMPLATE;
                }
                PlayerSettings.WebGL.decompressionFallback = true;
                
                // Save settings
                AssetDatabase.SaveAssets();
                
                // Log success and configuration summary
                Debug.Log("<color=green>WebGL memory settings configured successfully.</color>");
                Debug.Log("WebGL Configuration Summary:");
                Debug.Log($"- Initial Memory: {PlayerSettings.WebGL.memorySize}MB");
                Debug.Log($"- Compression: {PlayerSettings.WebGL.compressionFormat}");
                Debug.Log($"- Template: {PlayerSettings.WebGL.template}");
                Debug.Log($"- Data Caching: {PlayerSettings.WebGL.dataCaching}");
                
                // Open Project Settings window and show WebGL settings
                EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
                EditorPrefs.SetString("SelectedPreferencesTab", "WebGL");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to configure WebGL memory settings: {e.Message}\n{e.StackTrace}");
            }
        }

        // Validate the menu item
        [MenuItem(MENU_PATH, true)]
        private static bool ValidateConfigureWebGLMemory()
        {
            return EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
        }
    }
} 