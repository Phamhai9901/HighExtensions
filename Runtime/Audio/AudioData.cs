using System;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════
//  AUDIO SYSTEM — Enums & Data
// ═══════════════════════════════════════════════════════════════

public enum AudioCategory
{
    SFX,
    Music,
    UI,
    Ambient,
    Voice
}

public enum SFXPlayMode
{
    /// <summary>Phát 1 lần</summary>
    OneShot,

    /// <summary>Loop cho đến khi Stop</summary>
    Loop,

    /// <summary>Chỉ phát nếu clip này chưa đang phát</summary>
    IfNotPlaying
}

/// <summary>
/// Một entry audio trong AudioLibrary.
/// Chứa clip + config cho volume, pitch, randomization.
/// </summary>
[Serializable]
public class AudioEntry
{
    [Tooltip("Key dùng trong code: AudioManager.Play(\"sword_hit\")")]
    public string Key;

    public AudioCategory Category = AudioCategory.SFX;

    [Tooltip("Danh sách clip — random pick 1 khi phát (variation)")]
    public AudioClip[] Clips;

    [Range(0f, 1f)]
    public float Volume = 1f;

    [Tooltip("Pitch ngẫu nhiên trong khoảng [min, max]")]
    public Vector2 PitchRange = new(1f, 1f);

    [Tooltip("Spatial blend: 0 = 2D, 1 = 3D")]
    [Range(0f, 1f)]
    public float SpatialBlend;

    [Tooltip("Cooldown giữa 2 lần phát (tránh spam)")]
    public float Cooldown;

    [HideInInspector]
    public float LastPlayedTime = -999f;

    /// <summary>Random pick 1 clip từ danh sách</summary>
    public AudioClip GetClip()
    {
        if (Clips == null || Clips.Length == 0) return null;
        return Clips[UnityEngine.Random.Range(0, Clips.Length)];
    }

    /// <summary>Random pitch trong range</summary>
    public float GetPitch()
    {
        return UnityEngine.Random.Range(PitchRange.x, PitchRange.y);
    }

    /// <summary>Check cooldown</summary>
    public bool IsReady()
    {
        if (Cooldown <= 0f) return true;
        return Time.unscaledTime - LastPlayedTime >= Cooldown;
    }
}
