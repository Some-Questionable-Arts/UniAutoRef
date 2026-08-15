using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AutoRefEditor : EditorWindow
{
    private static System.Type registryType = null;

    public static bool IsMassScanRequired(int Lenght)
    {
        if (Lenght >= 15) return true;
        return false;
    }

    [MenuItem("Tools/UAR/Window")]
    public static void UarPanelClick()
    {
        AutoRefEditor window = GetWindow<AutoRefEditor>();
        window.titleContent = new GUIContent("UAR Panel");

        window.minSize = new Vector2(400, 300);
        window.maxSize = new Vector2(800, 600); 
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        Button findAllRefsButton = new() { text = "<b><color=#6CFFBD>Find All References</color></b>" };
        Button findAllRefsInAllScenesButton = new() { text = "<b><color=#6CFFBD>Find All References In All Scenes</color></b>" };
        Button smallButton = new() { text = "<b><color=#6E8A86>Show Debug</color></b>" };

        findAllRefsButton.style.height = 35;
        findAllRefsButton.style.marginTop = 10;
        findAllRefsButton.style.marginLeft = 10;
        findAllRefsButton.style.marginRight = 10;
        findAllRefsButton.style.fontSize = 15;

        findAllRefsInAllScenesButton.style.height = 35;
        findAllRefsInAllScenesButton.style.marginTop = 10;
        findAllRefsInAllScenesButton.style.marginLeft = 10;
        findAllRefsInAllScenesButton.style.marginRight = 10;
        findAllRefsInAllScenesButton.style.fontSize = 15;

        smallButton.style.position = Position.Absolute;
        smallButton.style.bottom = 10;
        smallButton.style.left = 10;
        smallButton.style.width = 100;
        smallButton.style.height = 30;

        findAllRefsButton.clicked += () =>
        {
            FindAllRefsClick();
        };

        findAllRefsInAllScenesButton.clicked += () =>
        {
            FindAllRefsInAllScenesClick();
        };

        root.Add(findAllRefsButton);
        root.Add(findAllRefsInAllScenesButton);
        root.Add(smallButton);
    }

    [MenuItem("Tools/UAR/Actions (HotKeys)/FindAllRefs &#g")]
    private static void FindAllRefsClick()
    {
        ExecuteAutoRefOnTypes(GetRegisteredTypes());
        Debug.Log("<b><color=#8EF1E4>[AutoRef] References successfully found. Any missing elements will be listed in the log above.</color></b>");
    }

    [MenuItem("Tools/UAR/Actions (HotKeys)/FindAllRefsInAllScenes &#t")]
    private static void FindAllRefsInAllScenesClick()
    {
        var types = GetRegisteredTypes();
        if (types == null) return;

        var scenesBuilds = EditorBuildSettings.scenes;

        if (scenesBuilds.Length == 0)
        {
            Debug.LogError("<b><color=#EA4F4F>[AutoRef] No scenes in build settings were added.</color></b> ");
            return;
        }

        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string originalScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;

        foreach (var sceneBuild in scenesBuilds)
        {
            if (!sceneBuild.enabled) continue;

            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sceneBuild.path, UnityEditor.SceneManagement.OpenSceneMode.Single);

            ExecuteAutoRefOnTypes(types);

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }

        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(originalScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        Debug.Log("<b><color=#8EF1E4>[AutoRef] References in all scenes successfully found. Any missing elements will be listed in the log above.</color></b>");
    }

    private static System.Type[] GetRegisteredTypes()
    {
        if (registryType == null)
        {
            try
            {
                var gameAssembly = System.Reflection.Assembly.Load("Assembly-CSharp");
                registryType = gameAssembly.GetType("AutoRefRegistry");
            }
            catch
            {
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.StartsWith("Assembly-CSharp"))
                    {
                        registryType = assembly.GetType("AutoRefRegistry");
                        if (registryType != null) break;
                    }
                }
            }
        }

        if (registryType == null)
        {
            Debug.LogError("<b><color=#EA4F4F>[AutoRef] No classes with the [AutoRef] attribute were compiled.</color></b>");
            return null;
        }

        var field = registryType.GetField("TargetTypes", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (field?.GetValue(null) is System.Type[] types && types.Length > 0)
        {
            return types;
        }

        return null;
    }

    private static void ExecuteAutoRefOnTypes(System.Type[] types)
    {
        if (IsMassScanRequired(types.Length))
        {
            var allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            var registeredTypesSet = new System.Collections.Generic.HashSet<System.Type>(types);

            for (int i = 0; i < allScripts.Length; i++)
            {
                var script = allScripts[i];
                if (script == null) continue;

                System.Type scriptType = script.GetType();

                if (registeredTypesSet.Contains(scriptType))
                {
                    BakeReferences(script);
                }
            }
        }
        else
        {
            foreach (var type in types)
            {
                var objects = FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var obj in objects)
                {
                    BakeReferences(obj);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void BakeReferences(Object script)
    {
        if (script is UniAutoRef.IAutoReference findable)
        {
            Undo.RecordObject(script, "AutoRef");
            findable.AutoFind_Execute();
            EditorUtility.SetDirty(script);
        }
    }

}
