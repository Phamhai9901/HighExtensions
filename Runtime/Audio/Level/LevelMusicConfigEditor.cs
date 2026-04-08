#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelMusicConfig))]
public class LevelMusicConfigEditor : Editor
{
    LevelMusicConfig config;
    bool showMatrix = true;
    string newLevelId = "";
    Vector2 scrollPos;

    void OnEnable()
    {
        config = (LevelMusicConfig)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSummary();
        EditorGUILayout.Space(6);
        DrawStandalone();
        EditorGUILayout.Space(6);
        DrawGeneric();
        EditorGUILayout.Space(8);
        DrawOverrideMatrix();
        EditorGUILayout.Space(4);
        DrawAddOverride();
        EditorGUILayout.Space(4);

        if (GUILayout.Button("Validate"))
            Validate();

        serializedObject.ApplyModifiedProperties();
    }

    // ═══════════════ SUMMARY ═══════════════

    void DrawSummary()
    {
        int total = config.LevelOverrides.Count;
        int withOverride = config.LevelOverrides.Count(e => e.HasAnyOverride);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Level Music Config", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            $"  {total} levels  |  {withOverride} with overrides  |  {total - withOverride} all-generic");
        EditorGUILayout.EndVertical();
    }

    // ═══════════════ STANDALONE ═══════════════

    void DrawStandalone()
    {
        EditorGUILayout.LabelField("Standalone Playlists", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MenuPlaylist"));
    }

    // ═══════════════ GENERIC ═══════════════

    void DrawGeneric()
    {
        EditorGUILayout.LabelField("Generic Playlists (fallback)", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("GenericGameplay"), new GUIContent("Gameplay"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GenericIntense"), new GUIContent("Intense"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GenericBoss"), new GUIContent("Boss"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GenericVictory"), new GUIContent("Victory"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GenericDefeat"), new GUIContent("Defeat"));
    }

    // ═══════════════ OVERRIDE MATRIX ═══════════════

    void DrawOverrideMatrix()
    {
        showMatrix = EditorGUILayout.Foldout(showMatrix, "Per-Level Overrides", true, EditorStyles.foldoutHeader);
        if (!showMatrix) return;

        var overridesProp = serializedObject.FindProperty("LevelOverrides");

        if (overridesProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Chưa có override. Tất cả level dùng generic.", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(400));

        // Header
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Level", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(75));
        GUILayout.Label("Gameplay", EditorStyles.miniLabel, GUILayout.Width(85));
        GUILayout.Label("Intense", EditorStyles.miniLabel, GUILayout.Width(85));
        GUILayout.Label("Boss", EditorStyles.miniLabel, GUILayout.Width(85));
        GUILayout.Label("Victory", EditorStyles.miniLabel, GUILayout.Width(85));
        GUILayout.Label("Defeat", EditorStyles.miniLabel, GUILayout.Width(85));
        GUILayout.Label("", GUILayout.Width(22));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < overridesProp.arraySize; i++)
        {
            var prop = overridesProp.GetArrayElementAtIndex(i);
            var entry = config.LevelOverrides[i];

            EditorGUILayout.BeginHorizontal("box");

            EditorGUILayout.PropertyField(prop.FindPropertyRelative("LevelId"), GUIContent.none, GUILayout.Width(70));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("DisplayName"), GUIContent.none, GUILayout.Width(75));

            SlotField(prop, "Gameplay", entry.Gameplay, config.GenericGameplay, 85);
            SlotField(prop, "Intense", entry.Intense, config.GenericIntense, 85);
            SlotField(prop, "Boss", entry.Boss, config.GenericBoss, 85);
            SlotField(prop, "Victory", entry.Victory, config.GenericVictory, 85);
            SlotField(prop, "Defeat", entry.Defeat, config.GenericDefeat, 85);

            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                overridesProp.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    void SlotField(SerializedProperty parent, string field,
        MusicPlaylist overrideVal, MusicPlaylist genericVal, float w)
    {
        // Green = override, Gray = generic fallback, Red = nothing
        GUI.backgroundColor = overrideVal != null
            ? new Color(0.6f, 0.9f, 0.6f)
            : genericVal != null
                ? new Color(0.75f, 0.75f, 0.75f)
                : new Color(1f, 0.6f, 0.6f);

        EditorGUILayout.PropertyField(parent.FindPropertyRelative(field), GUIContent.none, GUILayout.Width(w));
        GUI.backgroundColor = Color.white;
    }

    // ═══════════════ ADD ═══════════════

    void DrawAddOverride()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        newLevelId = EditorGUILayout.TextField(newLevelId, GUILayout.Width(100));

        if (GUILayout.Button("+ Add Level", GUILayout.Width(100)))
        {
            if (string.IsNullOrEmpty(newLevelId))
                newLevelId = (config.LevelOverrides.Count + 1).ToString();

            if (config.LevelOverrides.Any(e => e.LevelId == newLevelId))
            {
                EditorUtility.DisplayDialog("Duplicate", $"Level \"{newLevelId}\" already exists.", "OK");
            }
            else
            {
                Undo.RecordObject(config, "Add Level Override");
                config.LevelOverrides.Add(new LevelMusicEntry
                {
                    LevelId = newLevelId,
                    DisplayName = $"Level {newLevelId}"
                });
                EditorUtility.SetDirty(config);
                newLevelId = "";
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════ VALIDATE ═══════════════

    void Validate()
    {
        int issues = 0;

        if (config.GenericGameplay == null) { Warn("GenericGameplay is empty"); issues++; }
        if (config.MenuPlaylist == null) { Warn("MenuPlaylist is empty"); issues++; }

        foreach (var e in config.LevelOverrides)
        {
            if (string.IsNullOrEmpty(e.LevelId)) { Warn("Entry with empty LevelId"); issues++; }
            if (!e.HasAnyOverride) Warn($"Level \"{e.LevelId}\" has no overrides — consider removing");
        }

        var dups = config.LevelOverrides
            .Where(e => !string.IsNullOrEmpty(e.LevelId))
            .GroupBy(e => e.LevelId)
            .Where(g => g.Count() > 1);
        foreach (var d in dups) { Warn($"Duplicate LevelId: \"{d.Key}\""); issues++; }

        Debug.Log(issues == 0
            ? $"[LevelMusicConfig] All good! ({config.LevelOverrides.Count} levels)"
            : $"[LevelMusicConfig] {issues} issue(s)");
    }

    void Warn(string msg) => Debug.LogWarning($"[LevelMusicConfig] {msg}", config);
}
#endif
