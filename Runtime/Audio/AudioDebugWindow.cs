#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Window hiện trạng thái audio runtime.
/// Menu: Window → Audio → Audio Debug
/// 
/// Hiển thị:
/// • Music đang phát (key, time, volume)
/// • SFX pool (active/total)
/// • Volume per category (editable realtime)
/// • Quick play — test key trực tiếp
/// </summary>
public class AudioDebugWindow : EditorWindow
{
    string quickPlayKey = "";
    Vector2 scroll;

    [MenuItem("Window/Audio/Audio Debug")]
    static void Open()
    {
        GetWindow<AudioDebugWindow>("Audio Debug").Show();
    }

    void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see audio debug info.", MessageType.Info);
            return;
        }

        if (AudioManager.Instance == null)
        {
            EditorGUILayout.HelpBox("AudioManager not found in scene.", MessageType.Warning);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawMusicSection();
        EditorGUILayout.Space(8);
        DrawVolumeSection();
        EditorGUILayout.Space(8);
        DrawQuickPlay();

        EditorGUILayout.EndScrollView();

        // Auto-refresh
        if (Application.isPlaying)
            Repaint();
    }

    void DrawMusicSection()
    {
        EditorGUILayout.LabelField("Music", EditorStyles.boldLabel);

        if (AudioManager.IsMusicPlaying)
        {
            EditorGUILayout.LabelField("  Now Playing:", AudioManager.CurrentMusicKey ?? "(unknown)");

            if (GUILayout.Button("Stop Music (2s fade)"))
                AudioManager.StopMusic(2f);
        }
        else
        {
            EditorGUILayout.LabelField("  Not playing");
        }
    }

    void DrawVolumeSection()
    {
        EditorGUILayout.LabelField("Volume Controls", EditorStyles.boldLabel);

        foreach (AudioCategory cat in System.Enum.GetValues(typeof(AudioCategory)))
        {
            float current = AudioManager.GetVolume(cat);
            float newVal = EditorGUILayout.Slider($"  {cat}", current, 0f, 1f);
            if (!Mathf.Approximately(current, newVal))
                AudioManager.SetVolume(cat, newVal);
        }
    }

    void DrawQuickPlay()
    {
        EditorGUILayout.LabelField("Quick Play", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        quickPlayKey = EditorGUILayout.TextField(quickPlayKey);

        if (GUILayout.Button("Play SFX", GUILayout.Width(70)))
        {
            if (!string.IsNullOrEmpty(quickPlayKey))
                AudioManager.Play(quickPlayKey);
        }

        if (GUILayout.Button("Play Music", GUILayout.Width(80)))
        {
            if (!string.IsNullOrEmpty(quickPlayKey))
                AudioManager.PlayMusic(quickPlayKey);
        }

        EditorGUILayout.EndHorizontal();
    }
}
#endif
