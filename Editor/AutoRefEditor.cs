using UnityEditor;
using UnityEngine;

public class AutoRefEditor : Editor
{
    public static System.Type registryType = null;

    [MenuItem("Tools/UAR/Find All References")]
    public static void Click()
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
            return;
        }

        var field = registryType.GetField("TargetTypes", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        System.Type[] types = field?.GetValue(null) as System.Type[];

        if (types == null || types.Length == 0) return;

        foreach (var type in types)
        {
            var objects = FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in objects)
            {
                if (obj is UniAutoRef.IAutoReference findable)
                {
                    Undo.RecordObject(obj, "AutoRef");
                    findable.AutoFind_Execute();
                    EditorUtility.SetDirty(obj);
                }
            }
        }

        Debug.Log("<b><color=#8EF1E4>[AutoRef] References successfully found. Any missing elements will be listed in the log above.</color></b>");
    }
}
