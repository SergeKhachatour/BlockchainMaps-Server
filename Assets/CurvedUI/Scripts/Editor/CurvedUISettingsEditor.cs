using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

#if CURVEDUI_TMP || TMP_PRESENT
using TMPro;
#endif

namespace CurvedUI
{
    [CustomEditor(typeof(CurvedUISettings))]
    public class CurvedUISettingsEditor : Editor
    {
        void Awake()
        {
            CurvedUISettings myTarget = (CurvedUISettings)target;
        }

        public override void OnInspectorGUI()
        {
            CurvedUISettings myTarget = (CurvedUISettings)target;

            //initial settings
            bool preserveAspect = myTarget.PreserveAspect;
            bool disabled = !myTarget.enabled;

            //drawing the inspector
            DrawDefaultInspector();

            //custom buttons
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Utils", EditorStyles.boldLabel);

            if (GUILayout.Button("Add CurvedUIVertexEffect to all children"))
            {
                Text[] texts = Resources.FindObjectsOfTypeAll<Text>();
                foreach (Text tex in texts)
                {
                    if (tex.GetComponentInParent<CurvedUISettings>() == myTarget)
                        if (tex.GetComponent<CurvedUIVertexEffect>() == null)
                            tex.gameObject.AddComponent<CurvedUIVertexEffect>();
                }

#if CURVEDUI_TMP || TMP_PRESENT
                TextMeshProUGUI[] tmpTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
                foreach (TextMeshProUGUI tex in tmpTexts)
                {
                    if (tex.GetComponentInParent<CurvedUISettings>() == myTarget)
                        if (tex.GetComponent<CurvedUIVertexEffect>() == null)
                            tex.gameObject.AddComponent<CurvedUIVertexEffect>();
                }
#endif
            }

            //TMP installation buttons and labels
            bool tmpPresent = false;
            bool tmpInstalled = false;
            try
            {
                tmpPresent = System.IO.File.Exists("Assets/TextMesh Pro");
#pragma warning disable 618
                tmpInstalled = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Contains("CURVEDUI_TMP");
            }
            catch (System.Exception)
            {
                Debug.LogError("Couldn't check for TMP define symbols!");
            }

            if (tmpPresent && !tmpInstalled)
            {
                if (GUILayout.Button("Enable TextMeshPro Support"))
                {
                    string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
                    if (!defines.Contains("CURVEDUI_TMP"))
                    {
                        defines += ";CURVEDUI_TMP";
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defines);
                    }
                }
#pragma warning restore 618
            }

            //warning
            if (preserveAspect != myTarget.PreserveAspect)
            {
                if (EditorUtility.DisplayDialog("CurvedUI", "Changing Preserve Aspect on Canvas using CurvedUI can lead to unexpected behavior. It is suggested to keep it enabled. Are you sure you want to continue?", "Yes, change it", "Keep it enabled"))
                    Debug.LogWarning("CurvedUI: Changing Preserve Aspect on Canvas using CurvedUI can lead to unexpected behavior.");
                else
                    myTarget.PreserveAspect = true;
            }

            //save changes
            if (GUI.changed && !Application.isPlaying)
                EditorUtility.SetDirty(target);

            //final settings
            if (disabled != !myTarget.enabled)
                myTarget.gameObject.GetComponent<Canvas>().GetComponent<CurvedUIRaycaster>().enabled = myTarget.enabled;
        }
    }
}

