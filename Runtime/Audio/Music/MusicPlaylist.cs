using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject chứa danh sách nhạc cho một context (level, menu, boss, v.v.).
/// Tạo: Create → Audio → Music Playlist
///
/// ╔═══════════════════════════════════════════════════════════════╗
/// ║  Một game có thể có nhiều playlist:                          ║
/// ║                                                               ║
/// ║  • "Gameplay_Chill"   — 5 bài nhẹ, shuffle, gap 2s           ║
/// ║  • "Gameplay_Intense" — 3 bài nhanh, shuffle, no gap          ║
/// ║  • "Boss_Phase1"      — 1 bài loop                            ║
/// ║  • "MainMenu"         — 2 bài, sequence order                 ║
/// ║                                                               ║
/// ║  MusicPlaylistPlayer sẽ nhận playlist và tự chạy.             ║
/// ╚═══════════════════════════════════════════════════════════════╝
/// </summary>
[CreateAssetMenu(menuName = "Audio/Music Playlist", fileName = "MusicPlaylist")]
public class MusicPlaylist : ScriptableObject
{
    [Header("Tracks")]
    [Tooltip("Danh sách bài nhạc. Mỗi track là 1 AudioClip + config riêng.")]
    public List<MusicTrack> Tracks = new();

    [Header("Playback")]
    public PlaylistMode Mode = PlaylistMode.Shuffle;

    [Tooltip("Thời gian crossfade giữa 2 bài (giây)")]
    [Range(0f, 5f)]
    public float CrossfadeDuration = 2f;

    [Tooltip("Khoảng lặng giữa 2 bài (giây). 0 = chuyển liền.")]
    [Range(0f, 10f)]
    public float GapBetweenTracks = 0f;

    [Tooltip("Lặp lại playlist khi hết? Nếu false → dừng sau lượt cuối.")]
    public bool Loop = true;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float PlaylistVolume = 1f;
}

[Serializable]
public class MusicTrack
{
    public AudioClip Clip;

    [Tooltip("Tên hiển thị (debug / UI). Để trống = dùng tên file.")]
    public string DisplayName;

    [Tooltip("Volume riêng cho track này (nhân với playlist volume)")]
    [Range(0f, 1f)]
    public float Volume = 1f;

    [Tooltip("Trọng số khi shuffle — cao hơn = hay được chọn hơn")]
    [Range(1, 10)]
    public int Weight = 1;

    /// <summary>Tên hiển thị, fallback về clip name.</summary>
    public string Name => !string.IsNullOrEmpty(DisplayName)
        ? DisplayName
        : (Clip != null ? Clip.name : "(empty)");
}

public enum PlaylistMode
{
    /// <summary>Phát ngẫu nhiên, không lặp bài liền kề.</summary>
    Shuffle,

    /// <summary>Phát theo thứ tự trong list.</summary>
    Sequential,

    /// <summary>Random hoàn toàn (có thể lặp bài).</summary>
    Random,

    /// <summary>Chỉ phát 1 bài duy nhất, loop.</summary>
    SingleLoop
}
