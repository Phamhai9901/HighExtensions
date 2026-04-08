using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Config nhạc cho toàn bộ game.
/// Chứa: playlist ngoài gameplay (Menu) + generic fallback + per-level overrides.
///
/// Tạo: Create → Audio → Level Music Config
///
/// ╔══════════════════════════════════════════════════════════════════╗
/// ║                                                                  ║
/// ║  LevelMusicConfig                                                ║
/// ║  │                                                               ║
/// ║  ├── Standalone Playlists (không thuộc level nào)                ║
/// ║  │   └── MenuPlaylist                                            ║
/// ║  │                                                               ║
/// ║  ├── Generic Playlists (fallback cho level không override)       ║
/// ║  │   ├── Gameplay  → "Gameplay_Chill"                            ║
/// ║  │   ├── Intense   → "Gameplay_Intense"                          ║
/// ║  │   ├── Boss      → "Boss_Generic"                              ║
/// ║  │   ├── Victory   → "Victory_Fanfare"                           ║
/// ║  │   └── Defeat    → "Defeat_Theme"                              ║
/// ║  │                                                               ║
/// ║  └── Level Overrides                                             ║
/// ║      ├── Lv "1"  → (trống) → all generic                        ║
/// ║      ├── Lv "5"  → Gameplay = "Forest_Mix"                       ║
/// ║      └── Lv "10" → Gameplay + Boss override                      ║
/// ║                                                                  ║
/// ╚══════════════════════════════════════════════════════════════════╝
/// </summary>
[CreateAssetMenu(menuName = "Audio/Level Music Config", fileName = "LevelMusicConfig")]
public class LevelMusicConfig : ScriptableObject
{
    [Header("Standalone Playlists")]
    [Tooltip("Nhạc main menu / lobby — không thuộc level nào")]
    public MusicPlaylist MenuPlaylist;

    [Header("Generic Playlists (fallback cho level)")]
    public MusicPlaylist GenericGameplay;
    public MusicPlaylist GenericIntense;
    public MusicPlaylist GenericBoss;
    public MusicPlaylist GenericVictory;
    public MusicPlaylist GenericDefeat;

    [Header("Per-Level Overrides")]
    [Tooltip("Level cần nhạc riêng. Slot để trống = fallback generic.")]
    public List<LevelMusicEntry> LevelOverrides = new();

    // Runtime lookup
    Dictionary<string, LevelMusicEntry> lookup;

    // ═══════════════ PUBLIC API ═══════════════

    /// <summary>
    /// Resolve config nhạc cho level. Override > generic.
    /// </summary>
    public LevelMusicResult GetMusicForLevel(string levelId)
    {
        BuildLookup();

        LevelMusicEntry entry = null;
        lookup?.TryGetValue(levelId, out entry);

        return new LevelMusicResult
        {
            Gameplay = entry?.Gameplay ?? GenericGameplay,
            Intense  = entry?.Intense  ?? GenericIntense,
            Boss     = entry?.Boss     ?? GenericBoss,
            Victory  = entry?.Victory  ?? GenericVictory,
            Defeat   = entry?.Defeat   ?? GenericDefeat,
        };
    }

    public LevelMusicResult GetMusicForLevel(int levelIndex)
        => GetMusicForLevel(levelIndex.ToString());

    public bool HasOverride(string levelId)
    {
        BuildLookup();
        return lookup != null && lookup.ContainsKey(levelId);
    }

    // ═══════════════ INTERNAL ═══════════════

    void BuildLookup()
    {
        if (lookup != null) return;

        lookup = new Dictionary<string, LevelMusicEntry>();
        foreach (var entry in LevelOverrides)
        {
            if (string.IsNullOrEmpty(entry.LevelId)) continue;
            if (!lookup.TryAdd(entry.LevelId, entry))
                Debug.LogWarning($"[LevelMusicConfig] Duplicate levelId: \"{entry.LevelId}\"");
        }
    }

#if UNITY_EDITOR
    void OnValidate() => lookup = null;
#endif
}

[Serializable]
public class LevelMusicEntry
{
    public string LevelId;
    public string DisplayName;

    [Header("Override (trống = dùng generic)")]
    public MusicPlaylist Gameplay;
    public MusicPlaylist Intense;
    public MusicPlaylist Boss;
    public MusicPlaylist Victory;
    public MusicPlaylist Defeat;

    public bool HasAnyOverride =>
        Gameplay != null || Intense != null || Boss != null
        || Victory != null || Defeat != null;
}

public class LevelMusicResult
{
    public MusicPlaylist Gameplay;
    public MusicPlaylist Intense;
    public MusicPlaylist Boss;
    public MusicPlaylist Victory;
    public MusicPlaylist Defeat;
}
