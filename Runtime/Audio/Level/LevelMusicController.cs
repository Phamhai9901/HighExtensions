using System;
using UnityEngine;

/// <summary>
/// Gắn vào mỗi scene gameplay — tự phát nhạc đúng level khi load.
/// Quản lý chuyển phase: Gameplay → Intense → Boss → Victory/Defeat.
///
/// ╔══════════════════════════════════════════════════════════════╗
/// ║  SETUP                                                       ║
/// ║                                                              ║
/// ║  Scene "Level_05":                                           ║
/// ║    GameObject "LevelMusic"                                   ║
/// ║      └─ LevelMusicController                                ║
/// ║           config = LevelMusicConfig (SO)                     ║
/// ║           levelId = "5"                                      ║
/// ║           autoPlay = true                                    ║
/// ║                                                              ║
/// ║  → Load scene → tự resolve config cho level "5"              ║
/// ║  → Nếu level 5 có override Gameplay → phát playlist riêng   ║
/// ║  → Nếu không → phát GenericGameplay                          ║
/// ╚══════════════════════════════════════════════════════════════╝
///
/// PHASE TRANSITIONS (gọi từ game logic):
///
///   LevelMusicController.Instance.SetPhase(MusicPhase.Intense);
///   LevelMusicController.Instance.SetPhase(MusicPhase.Boss);
///   LevelMusicController.Instance.SetPhase(MusicPhase.Victory);
///
/// Hoặc dùng static shortcut:
///
///   LevelMusicController.GoIntense();
///   LevelMusicController.GoBoss();
///   LevelMusicController.GoVictory();
/// </summary>
public class LevelMusicController : MonoBehaviour
{
    public static LevelMusicController Instance { get; private set; }

    [Header("Config")]
    [Tooltip("LevelMusicConfig asset chứa mapping level → playlist")]
    [SerializeField] LevelMusicConfig config;

    [Tooltip("ID level hiện tại — match với LevelMusicEntry.LevelId")]
    [SerializeField] string levelId;

    [Tooltip("Tự phát music khi scene load")]
    [SerializeField] bool autoPlay = true;

    [Tooltip("Crossfade khi chuyển phase")]
    [SerializeField] float phaseCrossfade = 2f;

    // ═══════════════ STATE ═══════════════

    LevelMusicResult currentMusic;
    MusicPhase currentPhase = MusicPhase.None;

    /// <summary>Phase nhạc đang phát.</summary>
    public MusicPhase CurrentPhase => currentPhase;

    /// <summary>Config nhạc đã resolve cho level hiện tại.</summary>
    public LevelMusicResult CurrentMusic => currentMusic;

    /// <summary>Khi phase thay đổi.</summary>
    public event Action<MusicPhase> OnPhaseChanged;

    // ═══════════════ LIFECYCLE ═══════════════

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (config == null)
        {
            Debug.LogWarning("[LevelMusicController] No LevelMusicConfig assigned!");
            return;
        }

        // Resolve music cho level này
        currentMusic = config.GetMusicForLevel(levelId);

        if (autoPlay)
            SetPhase(MusicPhase.Gameplay);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ═══════════════ PHASE CONTROL ═══════════════

    /// <summary>
    /// Chuyển phase nhạc. Crossfade tự động sang playlist tương ứng.
    /// </summary>
    public void SetPhase(MusicPhase phase, float fadeDuration = -1f)
    {
        if (currentMusic == null) return;
        if (phase == currentPhase) return;

        float fade = fadeDuration >= 0f ? fadeDuration : phaseCrossfade;

        MusicPlaylist targetPlaylist = phase switch
        {
            MusicPhase.Gameplay => currentMusic.Gameplay,
            MusicPhase.Intense => currentMusic.Intense,
            MusicPhase.Boss => currentMusic.Boss,
            MusicPhase.Victory => currentMusic.Victory,
            MusicPhase.Defeat => currentMusic.Defeat,
            _ => null
        };

        if (targetPlaylist == null)
        {
            Debug.LogWarning($"[LevelMusicController] No playlist for phase {phase} (level: {levelId})");
            return;
        }

        MusicPhase previousPhase = currentPhase;
        currentPhase = phase;

        AudioManager.SwitchPlaylist(targetPlaylist, fade);
        OnPhaseChanged?.Invoke(phase);
    }

    /// <summary>Chuyển level ID runtime (ví dụ: endless mode đổi level liên tục).</summary>
    public void ChangeLevel(string newLevelId, MusicPhase startPhase = MusicPhase.Gameplay)
    {
        levelId = newLevelId;
        currentPhase = MusicPhase.None;
        currentMusic = config.GetMusicForLevel(levelId);
        SetPhase(startPhase);
    }

    /// <summary>Overload int.</summary>
    public void ChangeLevel(int levelIndex, MusicPhase startPhase = MusicPhase.Gameplay)
        => ChangeLevel(levelIndex.ToString(), startPhase);

    /// <summary>Dừng music hoàn toàn.</summary>
    public void StopAll(float fadeDuration = 1.5f)
    {
        AudioManager.StopPlaylist(fadeDuration);
        currentPhase = MusicPhase.None;
    }

    // ═══════════════ STATIC SHORTCUTS ═══════════════

    public static void GoGameplay() => Instance?.SetPhase(MusicPhase.Gameplay);
    public static void GoIntense() => Instance?.SetPhase(MusicPhase.Intense);
    public static void GoBoss() => Instance?.SetPhase(MusicPhase.Boss);
    public static void GoVictory() => Instance?.SetPhase(MusicPhase.Victory);
    public static void GoDefeat() => Instance?.SetPhase(MusicPhase.Defeat);
}


