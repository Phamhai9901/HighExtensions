using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
[InitializeOnLoad]
public static class ScriptIconAssigner
{
    private const string ICON_FOLDER = "Assets/Editor/IconScripts";

    static ScriptIconAssigner()
    {
        EditorApplication.delayCall += AssignIconsByName;
    }

    private static void AssignIconsByName()
    {
        if (!AssetDatabase.IsValidFolder(ICON_FOLDER))
        {
            Debug.LogWarning($"Icon folder not found: {ICON_FOLDER}");
            return;
        }

        // 1) Quét tất cả Texture2D trong folder icon
        // (bao gồm subfolder)
        string[] iconGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ICON_FOLDER });

        // Map: ScriptName -> iconPath
        var iconPathByName = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var guid in iconGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(path);

            // Nếu trùng tên, ưu tiên cái đầu tiên (hoặc bạn có thể đổi rule)
            if (!iconPathByName.ContainsKey(fileNameNoExt))
                iconPathByName.Add(fileNameNoExt, path);
        }

        // 2) Quét toàn bộ MonoScript trong project
        MonoScript[] scripts = Resources.FindObjectsOfTypeAll<MonoScript>();

        foreach (var script in scripts)
        {
            Type type = script.GetClass();
            if (type == null) continue; // có thể là file không phải MonoBehaviour/ScriptableObject

            // Lấy tên class (thường trùng file .cs nếu chuẩn)
            string className = type.Name;

            if (!iconPathByName.TryGetValue(className, out string iconPath))
                continue;

            AssignIcon(script, iconPath);
        }
    }

    private static void AssignIcon(MonoScript script, string iconPath)
    {
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (icon == null)
        {
            Debug.LogWarning($"Icon not found: {iconPath}");
            return;
        }

        EditorGUIUtility.SetIconForObject(script, icon);
    }
}
#endif
