#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor cho AudioLibrary.
/// 
/// Features:
/// • Preview button — nghe thử clip ngay trong Inspector
/// • Search / filter theo key hoặc category
/// • Bulk Import — kéo thả folder AudioClip, tự tạo entries
/// • Validate — tìm entry thiếu clip, duplicate key
/// • Generate AudioKeys.cs — tạo const string tự động
/// </summary>
[CustomEditor(typeof(AudioLibrary))]
public class AudioLibraryEditor : Editor
{
    AudioLibrary lib;
    AudioSource previewSource;

    // State
    string searchFilter = "";
    AudioCategory? categoryFilter = null;
    bool showImportSection;
    Vector2 scrollPos;

    // Foldouts
    readonly Dictionary<int, bool> foldouts = new();

    void OnEnable()
    {
        lib = (AudioLibrary)target;
    }

    void OnDisable()
    {
        StopPreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawToolbar();
        DrawSearchBar();

        EditorGUILayout.Space(4);

        DrawEntryList();

        EditorGUILayout.Space(8);

        DrawImportSection();
        DrawValidateSection();
        DrawGenerateKeysButton();

        serializedObject.ApplyModifiedProperties();
    }

    // ═══════════════ TOOLBAR ═══════════════

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        var entries = lib.GetAllEntries();
        int sfxCount = entries.Count(e => e.Category == AudioCategory.SFX);
        int musicCount = entries.Count(e => e.Category == AudioCategory.Music);
        int uiCount = entries.Count(e => e.Category == AudioCategory.UI);

        EditorGUILayout.LabelField(
            $"Total: {entries.Count}  |  SFX: {sfxCount}  Music: {musicCount}  UI: {uiCount}",
            EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+  Add Entry", EditorStyles.toolbarButton, GUILayout.Width(90)))
        {
            Undo.RecordObject(lib, "Add Audio Entry");
            lib.AddEntry(new AudioEntry { Key = "new_entry" });
            EditorUtility.SetDirty(lib);
        }

        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════ SEARCH ═══════════════

    void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Search", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);

        // Category filter dropdown
        string[] catNames = { "All", "SFX", "Music", "UI", "Ambient", "Voice" };
        int catIndex = categoryFilter.HasValue ? (int)categoryFilter.Value + 1 : 0;
        int newCatIndex = EditorGUILayout.Popup(catIndex, catNames, GUILayout.Width(80));
        categoryFilter = newCatIndex == 0 ? null : (AudioCategory)(newCatIndex - 1);

        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════ ENTRY LIST ═══════════════

    void DrawEntryList()
    {
        var entries = lib.GetAllEntries();
        var entriesProp = serializedObject.FindProperty("entries");

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(600));

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            // Filter
            if (!string.IsNullOrEmpty(searchFilter) &&
                !entry.Key.ToLower().Contains(searchFilter.ToLower()))
                continue;

            if (categoryFilter.HasValue && entry.Category != categoryFilter.Value)
                continue;

            DrawEntry(i, entry, entriesProp);
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawEntry(int index, AudioEntry entry, SerializedProperty entriesProp)
    {
        // Color tint based on category
        Color bgColor = entry.Category switch
        {
            AudioCategory.SFX => new Color(0.8f, 0.9f, 1f),
            AudioCategory.Music => new Color(0.8f, 1f, 0.85f),
            AudioCategory.UI => new Color(1f, 0.95f, 0.8f),
            AudioCategory.Ambient => new Color(0.9f, 0.85f, 1f),
            AudioCategory.Voice => new Color(1f, 0.85f, 0.85f),
            _ => Color.white
        };

        GUI.backgroundColor = bgColor;
        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = Color.white;

        // Header row: foldout + key + category + preview + delete
        EditorGUILayout.BeginHorizontal();

        if (!foldouts.ContainsKey(index)) foldouts[index] = false;
        foldouts[index] = EditorGUILayout.Foldout(foldouts[index], "", true);

        // Key
        var prop = entriesProp.GetArrayElementAtIndex(index);
        var keyProp = prop.FindPropertyRelative("Key");
        EditorGUILayout.PropertyField(keyProp, GUIContent.none, GUILayout.MinWidth(120));

        // Category
        var catProp = prop.FindPropertyRelative("Category");
        EditorGUILayout.PropertyField(catProp, GUIContent.none, GUILayout.Width(70));

        // Clip count badge
        int clipCount = entry.Clips?.Length ?? 0;
        EditorGUILayout.LabelField($"[{clipCount} clip{(clipCount != 1 ? "s" : "")}]",
            EditorStyles.miniLabel, GUILayout.Width(60));

        // Preview button
        if (clipCount > 0)
        {
            if (GUILayout.Button("▶", GUILayout.Width(24)))
                PreviewClip(entry.GetClip(), entry.Volume, entry.GetPitch());

            if (GUILayout.Button("■", GUILayout.Width(24)))
                StopPreview();
        }

        // Delete
        if (GUILayout.Button("✕", GUILayout.Width(22)))
        {
            if (EditorUtility.DisplayDialog("Delete Entry",
                $"Delete \"{entry.Key}\"?", "Delete", "Cancel"))
            {
                Undo.RecordObject(lib, "Remove Audio Entry");
                lib.RemoveEntry(entry.Key);
                EditorUtility.SetDirty(lib);
                return;
            }
        }

        EditorGUILayout.EndHorizontal();

        // Expanded details
        if (foldouts[index])
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(prop.FindPropertyRelative("Clips"), true);
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("Volume"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("PitchRange"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("SpatialBlend"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("Cooldown"));

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    // ═══════════════ PREVIEW ═══════════════

    void PreviewClip(AudioClip clip, float volume, float pitch)
    {
        if (clip == null) return;

        StopPreview();

        var go = EditorUtility.CreateGameObjectWithHideFlags(
            "AudioPreview", HideFlags.HideAndDontSave);
        previewSource = go.AddComponent<AudioSource>();
        previewSource.clip = clip;
        previewSource.volume = volume;
        previewSource.pitch = pitch;
        previewSource.Play();

        EditorApplication.update += CheckPreviewDone;
    }

    void StopPreview()
    {
        EditorApplication.update -= CheckPreviewDone;
        if (previewSource != null)
        {
            previewSource.Stop();
            DestroyImmediate(previewSource.gameObject);
            previewSource = null;
        }
    }

    void CheckPreviewDone()
    {
        if (previewSource == null || !previewSource.isPlaying)
            StopPreview();
    }

    // ═══════════════ BULK IMPORT ═══════════════

    void DrawImportSection()
    {
        showImportSection = EditorGUILayout.Foldout(showImportSection,
            "Bulk Import", true, EditorStyles.foldoutHeader);

        if (!showImportSection) return;

        EditorGUILayout.HelpBox(
            "Kéo thả folder chứa AudioClip vào đây.\n" +
            "Key sẽ tự tạo từ tên file (spaces → underscores, lowercase).\n" +
            "Category tự detect: tên file chứa \"music\" → Music, \"ui\" → UI, v.v.",
            MessageType.Info);

        var dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop Folder / AudioClips Here", EditorStyles.helpBox);

        HandleDragAndDrop(dropArea);
    }

    void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition)) return;

        switch (evt.type)
        {
            case EventType.DragUpdated:
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
                break;

            case EventType.DragPerform:
                DragAndDrop.AcceptDrag();
                ImportDraggedAssets(DragAndDrop.objectReferences);
                evt.Use();
                break;
        }
    }

    void ImportDraggedAssets(Object[] objects)
    {
        Undo.RecordObject(lib, "Bulk Import Audio");

        int imported = 0;
        var existing = lib.GetAllEntries().Select(e => e.Key).ToHashSet();

        foreach (var obj in objects)
        {
            // Nếu là folder → import tất cả clip trong folder
            string path = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(path))
            {
                var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { path });
                foreach (var guid in guids)
                {
                    var clipPath = AssetDatabase.GUIDToAssetPath(guid);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                    if (clip != null)
                        imported += TryImportClip(clip, existing);
                }
            }
            // Nếu là AudioClip trực tiếp
            else if (obj is AudioClip audioClip)
            {
                imported += TryImportClip(audioClip, existing);
            }
        }

        EditorUtility.SetDirty(lib);
        Debug.Log($"[AudioLibrary] Imported {imported} new entries.");
    }

    int TryImportClip(AudioClip clip, HashSet<string> existing)
    {
        string key = GenerateKey(clip.name);
        if (existing.Contains(key)) return 0;

        var entry = new AudioEntry
        {
            Key = key,
            Category = DetectCategory(clip.name),
            Clips = new[] { clip },
            Volume = 1f,
            PitchRange = new Vector2(1f, 1f),
        };

        lib.AddEntry(entry);
        existing.Add(key);
        return 1;
    }

    string GenerateKey(string fileName)
    {
        // Remove extension, lowercase, spaces → underscores
        string key = Path.GetFileNameWithoutExtension(fileName);
        key = key.ToLower().Trim();
        key = Regex.Replace(key, @"[^a-z0-9_]", "_");
        key = Regex.Replace(key, @"_+", "_");
        return key.Trim('_');
    }

    AudioCategory DetectCategory(string fileName)
    {
        string lower = fileName.ToLower();
        if (lower.Contains("music") || lower.Contains("bgm") || lower.Contains("ost"))
            return AudioCategory.Music;
        if (lower.Contains("ui") || lower.Contains("button") || lower.Contains("click"))
            return AudioCategory.UI;
        if (lower.Contains("ambient") || lower.Contains("env") || lower.Contains("atmos"))
            return AudioCategory.Ambient;
        if (lower.Contains("voice") || lower.Contains("vo_") || lower.Contains("dialog"))
            return AudioCategory.Voice;
        return AudioCategory.SFX;
    }

    // ═══════════════ VALIDATE ═══════════════

    void DrawValidateSection()
    {
        if (GUILayout.Button("Validate Library"))
        {
            ValidateLibrary();
        }
    }

    void ValidateLibrary()
    {
        var entries = lib.GetAllEntries();
        int issues = 0;

        // Duplicate keys
        var keyCount = new Dictionary<string, int>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.Key))
            {
                Debug.LogWarning("[Validate] Empty key found!", lib);
                issues++;
                continue;
            }

            if (!keyCount.TryAdd(entry.Key, 1))
            {
                keyCount[entry.Key]++;
                Debug.LogWarning($"[Validate] Duplicate key: \"{entry.Key}\"", lib);
                issues++;
            }
        }

        // Missing clips
        foreach (var entry in entries)
        {
            if (entry.Clips == null || entry.Clips.Length == 0)
            {
                Debug.LogWarning($"[Validate] \"{entry.Key}\" has no clips!", lib);
                issues++;
            }
            else
            {
                for (int i = 0; i < entry.Clips.Length; i++)
                {
                    if (entry.Clips[i] == null)
                    {
                        Debug.LogWarning($"[Validate] \"{entry.Key}\" clip[{i}] is null!", lib);
                        issues++;
                    }
                }
            }

            // Invalid pitch range
            if (entry.PitchRange.x > entry.PitchRange.y)
            {
                Debug.LogWarning($"[Validate] \"{entry.Key}\" pitch min > max!", lib);
                issues++;
            }
        }

        if (issues == 0)
            Debug.Log($"[Validate] AudioLibrary \"{lib.name}\" — All good! ({entries.Count} entries)");
        else
            Debug.LogWarning($"[Validate] AudioLibrary \"{lib.name}\" — {issues} issue(s) found.");
    }

    // ═══════════════ GENERATE KEYS ═══════════════

    void DrawGenerateKeysButton()
    {
        EditorGUILayout.Space(4);

        if (GUILayout.Button("Generate AudioKeys.cs"))
        {
            GenerateAudioKeys();
        }
    }

    void GenerateAudioKeys()
    {
        var entries = lib.GetAllEntries();

        var sb = new StringBuilder();
        sb.AppendLine("// AUTO-GENERATED by AudioLibrary Editor — DO NOT EDIT MANUALLY");
        sb.AppendLine("// Regenerate: AudioLibrary Inspector → \"Generate AudioKeys.cs\"");
        sb.AppendLine();
        sb.AppendLine("public static class AudioKeys");
        sb.AppendLine("{");

        // Group by category
        foreach (AudioCategory cat in System.Enum.GetValues(typeof(AudioCategory)))
        {
            var catEntries = entries.Where(e => e.Category == cat).ToList();

            sb.AppendLine($"    public static class {cat}");
            sb.AppendLine("    {");

            foreach (var entry in catEntries)
            {
                string constName = entry.Key.ToUpper();
                sb.AppendLine($"        public const string {constName} = \"{entry.Key}\";");
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        // Find existing AudioKeys.cs or create new
        string[] guids = AssetDatabase.FindAssets("AudioKeys t:TextAsset");
        string path = guids.Length > 0
            ? AssetDatabase.GUIDToAssetPath(guids[0])
            : "Assets/Scripts/Audio/AudioKeys.cs";

        // Ensure directory exists
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, sb.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"[AudioLibrary] Generated AudioKeys.cs at {path} ({entries.Count} entries)");
    }
}
#endif
