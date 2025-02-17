using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System;

public class WebGLBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.WebGL)
        {
            try
            {
                // 1. Copy template images if missing
                CopyTemplateImages();

                // 2. Verify WebGL template setup
                VerifyWebGLTemplate();
            }
            catch (Exception e)
            {
                Debug.LogError($"WebGL build preprocessing failed: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }
    }

    private void CopyTemplateImages()
    {
        string unityEditorPath = EditorApplication.applicationPath;
        string defaultTemplatePath = Path.Combine(
            Path.GetDirectoryName(unityEditorPath),
            "Data/PlaybackEngines/WebGLSupport/BuildTools/WebGLTemplates/Default/TemplateData"
        );

        string targetPath = Path.Combine(
            Application.dataPath,
            "WebGLTemplates/Passkey/TemplateData"
        );

        // Create target directory if it doesn't exist
        Directory.CreateDirectory(targetPath);

        string[] requiredImages = new[]
        {
            "unity-logo-dark.png",
            "progress-bar-empty-dark.png",
            "progress-bar-full-dark.png",
            "webgl-logo.png",
            "fullscreen-button.png",
            "favicon.ico"
        };

        foreach (string image in requiredImages)
        {
            string sourcePath = Path.Combine(defaultTemplatePath, image);
            string destPath = Path.Combine(targetPath, image);

            if (File.Exists(sourcePath) && !File.Exists(destPath))
            {
                File.Copy(sourcePath, destPath);
                Debug.Log($"Copied template image: {image}");
            }
        }
    }

    private void VerifyWebGLTemplate()
    {
        string templatePath = Path.Combine(Application.dataPath, "WebGLTemplates/Passkey");
        
        // Check required files
        string[] requiredFiles = new[]
        {
            "index.html",
            "TemplateData/style.css",
            "TemplateData/unity-logo-dark.png",
            "TemplateData/favicon.ico",
            "passkey-kit-bundle.js"
        };

        foreach (string file in requiredFiles)
        {
            string fullPath = Path.Combine(templatePath, file);
            if (!File.Exists(fullPath))
            {
                throw new BuildFailedException($"Required WebGL template file missing: {file}");
            }
        }
    }
} 