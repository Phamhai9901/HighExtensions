#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor cho AudioTrigger.
/// Thay vì gõ key bằng tay → dropdown chọn từ AudioLibrary.
/// Preview nghe thử ngay trong Inspector.
/// </summary>
[CustomEditor(typeof(AudioTrigger))]
public class AudioTriggerEditor : Editor
{
    AudioLibrary cachedLibrary;
    string[] keyOptions;
    AudioSource previewSource;

    SerializedProperty audioKeyProp;
    SerializedProperty playModeProp;
    SerializedProperty playOnEnableProp;
    SerializedProperty playOnDisableProp;
    SerializedProperty stopOnDisableProp;
    SerializedProperty use3DProp;
    SerializedProperty playOnCollisionProp;
    SerializedProperty playOnTriggerProp;
    SerializedProperty collisionLayersProp;

    void OnEnable()
    {
        audioKeyProp = serializedObject.FindProperty("audioKey");
        playModeProp = serializedObject.FindProperty("playMode");
        playOnEnableProp = serializedObject.FindProperty("playOnEnable");
        playOnDisableProp = serializedObject.FindProperty("playOnDisable");
        stopOnDisableProp = serializedObject.FindProperty("stopOnDisable");
        use3DProp = serializedObject.FindProperty("use3DPosition");
        playOnCollisionProp = serializedObject.FindProperty("playOnCollision");
        playOnTriggerProp = serializedObject.FindProperty("playOnTrigger");
        collisionLayersProp = serializedObject.FindProperty("collisionLayers");

        RefreshLibrary();
    }

    void OnDisable()
    {
        StopPreview();
    }

    void RefreshLibrary()
    {
        // Tìm AudioLibrary trong project
        var guids = AssetDatabase.FindAssets("t:AudioLibrary");
        if (guids.Length == 0)
        {
            keyOptions = new[] { "(No AudioLibrary found)" };
            return;
        }

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        cachedLibrary = AssetDatabase.LoadAssetAtPath<AudioLibrary>(path);

        if (cachedLibrary != null)
        {
            var entries = cachedLibrary.GetAllEntries();
            keyOptions = new[] { "(None)" }
                .Concat(entries.Select(e => e.Key))
                .ToArray();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Audio Key — dropdown thay vì text field
        EditorGUILayout.LabelField("Audio Key", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (keyOptions != null && keyOptions.Length > 0)
        {
            int currentIndex = System.Array.IndexOf(keyOptions, audioKeyProp.stringValue);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup(currentIndex, keyOptions);
            if (newIndex > 0 && newIndex < keyOptions.Length)
                audioKeyProp.stringValue = keyOptions[newIndex];
            else if (newIndex == 0)
                audioKeyProp.stringValue = "";
        }
        else
        {
            EditorGUILayout.PropertyField(audioKeyProp, GUIContent.none);
        }

        // Preview buttons
        if (GUILayout.Button("▶", GUILayout.Width(24)))
            PreviewCurrentKey();

        if (GUILayout.Button("■", GUILayout.Width(24)))
            StopPreview();

        if (GUILayout.Button("↻", GUILayout.Width(24)))
            RefreshLibrary();

        EditorGUILayout.EndHorizontal();

        // Show current key as text (for manual edit if needed)
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(audioKeyProp, new GUIContent("Manual Key"));
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(8);

        // Play Mode
        EditorGUILayout.LabelField("Play Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(playModeProp);
        EditorGUILayout.PropertyField(playOnEnableProp);
        EditorGUILayout.PropertyField(playOnDisableProp);
        EditorGUILayout.PropertyField(stopOnDisableProp);

        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(use3DProp);

        // Collision
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Collision Triggers", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(playOnCollisionProp);
        EditorGUILayout.PropertyField(playOnTriggerProp);

        if (playOnCollisionProp.boolValue || playOnTriggerProp.boolValue)
            EditorGUILayout.PropertyField(collisionLayersProp);

        serializedObject.ApplyModifiedProperties();
    }

    void PreviewCurrentKey()
    {
        if (cachedLibrary == null || string.IsNullOrEmpty(audioKeyProp.stringValue))
            return;

        var entry = cachedLibrary.Get(audioKeyProp.stringValue);
        if (entry == null) return;

        var clip = entry.GetClip();
        if (clip == null) return;

        StopPreview();

        var go = EditorUtility.CreateGameObjectWithHideFlags(
            "AudioTriggerPreview", HideFlags.HideAndDontSave);
        previewSource = go.AddComponent<AudioSource>();
        previewSource.clip = clip;
        previewSource.volume = entry.Volume;
        previewSource.pitch = entry.GetPitch();
        previewSource.Play();

        EditorApplication.update += CheckDone;
    }

    void StopPreview()
    {
        EditorApplication.update -= CheckDone;
        if (previewSource != null)
        {
            previewSource.Stop();
            DestroyImmediate(previewSource.gameObject);
            previewSource = null;
        }
    }

    void CheckDone()
    {
        if (previewSource == null || !previewSource.isPlaying)
            StopPreview();
    }
}
#endif
