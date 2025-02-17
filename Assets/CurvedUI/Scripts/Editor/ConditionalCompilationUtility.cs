using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using Debug = UnityEngine.Debug;

namespace CurvedUI.ConditionalCompilation
{
    /// <summary>
    /// Conditional Compilation Utility (CCU) by Unity
    /// https://github.com/Unity-Technologies/EditorXR/blob/development/Scripts/Utilities/Editor/ConditionalCompilationUtility.cs
    /// 
    /// The Conditional Compilation Utility (CCU) will add defines to the build settings once dependendent classes have been detected. 
    /// In order for this to be specified in any project without the project needing to include the CCU, at least one custom attribute 
    /// must be created in the following form:
    ///
    /// [Conditional(UNITY_CCU)]                                    // | This is necessary for CCU to pick up the right attributes
    /// public class OptionalDependencyAttribute : Attribute        // | Must derive from System.Attribute
    /// {
    ///     public string dependentClass;                           // | Required field specifying the fully qualified dependent class
    ///     public string define;                                   // | Required field specifying the define to add
    /// }
    ///
    /// Then, simply specify the assembly attribute(s) you created:
    /// [assembly: OptionalDependency("UnityEngine.InputNew.InputSystem", "USE_NEW_INPUT")]
    /// [assembly: OptionalDependency("Valve.VR.IVRSystem", "ENABLE_STEAMVR_INPUT")]
    /// </summary>
    [InitializeOnLoad]
    public static class ConditionalCompilationUtility
    {
        const string k_EnableCCU = "UNITY_CCU";

        static BuildTargetGroup GetCurrentBuildTargetGroup()
        {
            // Default to Standalone if we can't determine the platform
            if (!Enum.IsDefined(typeof(BuildTargetGroup), EditorUserBuildSettings.selectedBuildTargetGroup))
                return BuildTargetGroup.Standalone;

            return EditorUserBuildSettings.selectedBuildTargetGroup;
        }

        public static bool enabled
        {
            get
            {
                try
                {
                    var buildTargetGroup = GetCurrentBuildTargetGroup();
                    #if UNITY_2021_2_OR_NEWER
                    var defines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));
                    #else
                    var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                    #endif
                    return defines != null && defines.Contains(k_EnableCCU);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CCU] Error checking enabled status: {e.Message}");
                    return false;
                }
            }
        }

        public static string[] defines { private set; get; }

        static ConditionalCompilationUtility()
        {
            try
            {
                var buildTargetGroup = GetCurrentBuildTargetGroup();
                string currentDefines;
                
                #if UNITY_2021_2_OR_NEWER
                currentDefines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));
                #else
                currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                #endif

                // Ensure we have a valid string to work with
                if (string.IsNullOrEmpty(currentDefines))
                    currentDefines = k_EnableCCU;
                
                var definesList = new List<string>(currentDefines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                
                if (!definesList.Contains(k_EnableCCU, StringComparer.OrdinalIgnoreCase))
                {
                    definesList.Add(k_EnableCCU);
                    try
                    {
                        #if UNITY_2021_2_OR_NEWER
                        PlayerSettings.SetScriptingDefineSymbols(
                            UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup),
                            string.Join(";", definesList.ToArray()));
                        #else
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", definesList.ToArray()));
                        #endif
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[CCU] Failed to set initial define symbols: {e.Message}");
                    }
                    return;
                }

                var ccuDefines = new List<string> { k_EnableCCU };
                ConditionalCompilationUtility.defines = ccuDefines.ToArray();

                try
                {
                    #if UNITY_2021_2_OR_NEWER
                    PlayerSettings.SetScriptingDefineSymbols(
                        UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup),
                        string.Join(";", definesList.ToArray()));
                    #else
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", definesList.ToArray()));
                    #endif
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CCU] Failed to set final define symbols: {e.Message}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CCU] Error in static constructor: {e.Message}");
            }
        }

        static void ForEachAssembly(Action<Assembly> callback)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    callback(assembly);
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip any assemblies that don't load properly
                    continue;
                }
            }
        }

        static void ForEachType(Action<Type> callback)
        {
            ForEachAssembly(assembly =>
            {
                var types = assembly.GetTypes();
                foreach (var t in types)
                    callback(t);
            });
        }

        static IEnumerable<Type> GetAssignableTypes(Type type, Func<Type, bool> predicate = null)
        {
            var list = new List<Type>();
            ForEachType(t =>
            {
                if (type.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && (predicate == null || predicate(t)))
                    list.Add(t);
            });

            return list;
        }
    }
}