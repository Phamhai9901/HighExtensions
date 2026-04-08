#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor cho MusicPlaylist SO.
/// 
/// Features:
/// • Preview ▶ nghe thử từng track
/// • Hiển thị duration từng track
/// • Thanh Weight visual
/// • Tổng thời gian playlist
/// • Drag reorder tracks
/// </summary>
[CustomEditor(typeof(MusicPlaylist))]
public class MusicPlaylistEditor : Editor
{
    MusicPlaylist playlist;
    AudioSource previewSource;
    int previewingIndex = -1;

    SerializedProperty tracksProp;
    SerializedProperty modeProp;
    SerializedProperty crossfadeProp;
    SerializedProperty gapProp;
    SerializedProperty loopProp;
    SerializedProperty volumeProp;

    void OnEnable()
    {
        playlist = (MusicPlaylist)target;
        tracksProp = serializedObject.FindProperty("Tracks");
        modeProp = serializedObject.FindProperty("Mode");
        crossfadeProp = serializedObject.FindProperty("CrossfadeDuration");
        gapProp = serializedObject.FindProperty("GapBetweenTracks");
        loopProp = serializedObject.FindProperty("Loop");
        volumeProp = serializedObject.FindProperty("PlaylistVolume");
    }

    void OnDisable()
    {
        StopPreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSummary();

        EditorGUILayout.Space(8);
        DrawPlaybackSettings();

        EditorGUILayout.Space(8);
        DrawTrackList();

        EditorGUILayout.Space(4);
        DrawAddButton();

        serializedObject.ApplyModifiedProperties();
    }

    // ═══════════════ SUMMARY ═══════════════

    void DrawSummary()
    {
        int count = playlist.Tracks.Count;
        float totalDuration = 0f;
        int totalWeight = 0;

        foreach (var track in playlist.Tracks)
        {
            if (track.Clip != null)
                totalDuration += track.Clip.length;
            totalWeight += track.Weight;
        }

        float totalWithGaps = totalDuration + (count - 1) * playlist.GapBetweenTracks;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Playlist Summary", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"  Tracks: {count}    Total: {FormatTime(totalWithGaps)}    Mode: {playlist.Mode}");
        EditorGUILayout.EndVertical();
    }

    // ═══════════════ PLAYBACK SETTINGS ═══════════════

    void DrawPlaybackSettings()
    {
        EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(modeProp);
        EditorGUILayout.PropertyField(crossfadeProp);
        EditorGUILayout.PropertyField(gapProp);
        EditorGUILayout.PropertyField(loopProp);
        EditorGUILayout.PropertyField(volumeProp);
    }

    // ═══════════════ TRACK LIST ═══════════════

    void DrawTrackList()
    {
        EditorGUILayout.LabelField("Tracks", EditorStyles.boldLabel);

        for (int i = 0; i < tracksProp.arraySize; i++)
        {
            DrawTrackEntry(i);
        }
    }

    void DrawTrackEntry(int index)
    {
        var trackProp = tracksProp.GetArrayElementAtIndex(index);
        var clipProp = trackProp.FindPropertyRelative("Clip");
        var nameProp = trackProp.FindPropertyRelative("DisplayName");
        var volProp = trackProp.FindPropertyRelative("Volume");
        var weightProp = trackProp.FindPropertyRelative("Weight");

        var track = playlist.Tracks[index];
        bool isPreviewingThis = previewingIndex == index;

        // Color tint
        GUI.backgroundColor = isPreviewingThis
            ? new Color(0.7f, 1f, 0.7f)
            : Color.white;

        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = Color.white;

        // Row 1: Index + Clip + Duration + Preview + Delete
        EditorGUILayout.BeginHorizontal();

        // Move buttons
        GUI.enabled = index > 0;
        if (GUILayout.Button("▲", GUILayout.Width(22)))
        {
            tracksProp.MoveArrayElement(index, index - 1);
            serializedObject.ApplyModifiedProperties();
            return;
        }
        GUI.enabled = index < tracksProp.arraySize - 1;
        if (GUILayout.Button("▼", GUILayout.Width(22)))
        {
            tracksProp.MoveArrayElement(index, index + 1);
            serializedObject.ApplyModifiedProperties();
            return;
        }
        GUI.enabled = true;

        // Index label
        EditorGUILayout.LabelField($"#{index + 1}", EditorStyles.miniLabel, GUILayout.Width(24));

        // Clip field
        EditorGUILayout.PropertyField(clipProp, GUIContent.none, GUILayout.MinWidth(120));

        // Duration
        if (track.Clip != null)
        {
            string dur = FormatTime(track.Clip.length);
            EditorGUILayout.LabelField(dur, EditorStyles.miniLabel, GUILayout.Width(45));
        }

        // Preview
        if (track.Clip != null)
        {
            string btnText = isPreviewingThis ? "■" : "▶";
            if (GUILayout.Button(btnText, GUILayout.Width(24)))
            {
                if (isPreviewingThis)
                    StopPreview();
                else
                    PreviewTrack(index);
            }
        }

        // Delete
        if (GUILayout.Button("✕", GUILayout.Width(22)))
        {
            if (previewingIndex == index) StopPreview();
            tracksProp.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.EndHorizontal();

        // Row 2: Name + Volume + Weight bar
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Name", GUILayout.Width(36));
        EditorGUILayout.PropertyField(nameProp, GUIContent.none, GUILayout.MinWidth(80));

        EditorGUILayout.LabelField("Vol", GUILayout.Width(24));
        EditorGUILayout.PropertyField(volProp, GUIContent.none, GUILayout.Width(50));

        EditorGUILayout.LabelField("W", GUILayout.Width(14));
        EditorGUILayout.PropertyField(weightProp, GUIContent.none, GUILayout.Width(40));

        // Weight visual bar
        if (playlist.Tracks.Count > 0)
        {
            int maxWeight = 1;
            foreach (var t in playlist.Tracks)
                if (t.Weight > maxWeight) maxWeight = t.Weight;

            float ratio = (float)track.Weight / maxWeight;
            Rect barRect = GUILayoutUtility.GetRect(60, 14);
            EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f));
            barRect.width *= ratio;
            EditorGUI.DrawRect(barRect, new Color(0.3f, 0.7f, 1f));
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    void DrawAddButton()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+  Add Track", GUILayout.Width(100)))
        {
            tracksProp.InsertArrayElementAtIndex(tracksProp.arraySize);
        }

        // Bulk add from drag
        var dropArea = GUILayoutUtility.GetRect(200, 30);
        GUI.Box(dropArea, "Drop AudioClips Here", EditorStyles.helpBox);
        HandleDrop(dropArea);

        EditorGUILayout.EndHorizontal();
    }

    void HandleDrop(Rect area)
    {
        Event evt = Event.current;
        if (!area.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            Undo.RecordObject(playlist, "Add Tracks");

            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is AudioClip clip)
                {
                    playlist.Tracks.Add(new MusicTrack
                    {
                        Clip = clip,
                        Volume = 1f,
                        Weight = 1
                    });
                }
            }

            EditorUtility.SetDirty(playlist);
            evt.Use();
        }
    }

    // ═══════════════ PREVIEW ═══════════════

    void PreviewTrack(int index)
    {
        StopPreview();

        var track = playlist.Tracks[index];
        if (track.Clip == null) return;

        var go = EditorUtility.CreateGameObjectWithHideFlags(
            "PlaylistPreview", HideFlags.HideAndDontSave);
        previewSource = go.AddComponent<AudioSource>();
        previewSource.clip = track.Clip;
        previewSource.volume = track.Volume * playlist.PlaylistVolume;
        previewSource.Play();
        previewingIndex = index;

        EditorApplication.update += CheckPreviewDone;
    }

    void StopPreview()
    {
        EditorApplication.update -= CheckPreviewDone;
        previewingIndex = -1;

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
        else
            Repaint(); // Keep highlight updated
    }

    // ═══════════════ HELPERS ═══════════════

    string FormatTime(float seconds)
    {
        int min = (int)(seconds / 60);
        int sec = (int)(seconds % 60);
        return $"{min}:{sec:D2}";
    }
}
#endif
