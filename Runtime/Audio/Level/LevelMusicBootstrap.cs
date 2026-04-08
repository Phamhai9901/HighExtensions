using System;
using UnityEngine;

/// <summary>
/// Gắn vào scene gameplay — resolve config nhạc cho level hiện tại,
/// quản lý chuyển phase: Gameplay → Intense → Boss → Victory/Defeat.
///
/// AudioManager gọi vào đây qua static shortcuts:
///   AudioManager.PlayBossMusic()    → LevelMusicBootstrap.Instance.PlayBoss()
///   AudioManager.PlayVictoryMusic() → LevelMusicBootstrap.Instance.PlayVictory()
///
/// ╔══════════════════════════════════════════════════════════════╗
/// ║  SETUP                                                       ║
/// ║                                                              ║
/// ║  Scene "Level_05":                                           ║
/// ║    GameObject "LevelMusic"                                   ║
/// ║      └─ LevelMusicBootstrap                                  ║
/// ║           config = LevelMusicConfig (SO)                     ║
/// ║           levelId = "5"                                      ║
/// ║           autoPlay = true                                    ║
/// ╚══════════════════════════════════════════════════════════════╝
/// </summary>
public class LevelMusicBootstrap : MonoBehaviour
{
    public static LevelMusicBootstrap Instance { get; private set; }

    [Header("Config")]
    [SerializeField] LevelMusicConfig config;
    [SerializeField] string levelId;
    [SerializeField] bool autoPlay = true;
    [SerializeField] float phaseCrossfade = 2f;

    // ═══════════════ STATE ═══════════════

    LevelMusicResult currentMusic;
    MusicPhase currentPhase = MusicPhase.None;

    public LevelMusicConfig Config => config;
    public MusicPhase CurrentPhase => currentPhase;
    public LevelMusicResult CurrentMusic => currentMusic;
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
            Debug.LogWarning("[LevelMusicBootstrap] No LevelMusicConfig assigned!");
            return;
        }

        currentMusic = config.GetMusicForLevel(levelId);

        if (autoPlay)
            PlayGameplay();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ═══════════════ PHASE METHODS ═══════════════
    // AudioManager gọi trực tiếp vào đây

    public void PlayGameplay(float fadeDuration = -1f)
        => SetPhase(MusicPhase.Gameplay, fadeDuration);

    public void PlayIntense(float fadeDuration = -1f)
        => SetPhase(MusicPhase.Intense, fadeDuration);

    public void PlayBoss(float fadeDuration = -1f)
        => SetPhase(MusicPhase.Boss, fadeDuration);

    public void PlayVictory(float fadeDuration = -1f)
        => SetPhase(MusicPhase.Victory, fadeDuration);

    public void PlayDefeat(float fadeDuration = -1f)
        => SetPhase(MusicPhase.Defeat, fadeDuration);

    // ═══════════════ CORE ═══════════════

    public void SetPhase(MusicPhase phase, float fadeDuration = -1f)
    {
        if (currentMusic == null) return;
        if (phase == currentPhase) return;

        float fade = fadeDuration >= 0f ? fadeDuration : phaseCrossfade;

        MusicPlaylist target = phase switch
        {
            MusicPhase.Gameplay => currentMusic.Gameplay,
            MusicPhase.Intense  => currentMusic.Intense,
            MusicPhase.Boss     => currentMusic.Boss,
            MusicPhase.Victory  => currentMusic.Victory,
            MusicPhase.Defeat   => currentMusic.Defeat,
            _ => null
        };

        if (target == null)
        {
            Debug.LogWarning($"[LevelMusicBootstrap] No playlist for phase {phase} (level: {levelId})");
            return;
        }

        currentPhase = phase;
        AudioManager.SwitchPlaylist(target, fade);
        OnPhaseChanged?.Invoke(phase);
    }

    /// <summary>Đổi level runtime (endless mode, v.v.).</summary>
    public void ChangeLevel(string newLevelId, MusicPhase startPhase = MusicPhase.Gameplay)
    {
        levelId = newLevelId;
        currentPhase = MusicPhase.None;
        currentMusic = config.GetMusicForLevel(levelId);
        SetPhase(startPhase);
    }

    public void ChangeLevel(int levelIndex, MusicPhase startPhase = MusicPhase.Gameplay)
        => ChangeLevel(levelIndex.ToString(), startPhase);

    public void StopAll(float fadeDuration = 1.5f)
    {
        AudioManager.StopPlaylist(fadeDuration);
        currentPhase = MusicPhase.None;
    }
}

public enum MusicPhase
{
    None,
    Gameplay,
    Intense,
    Boss,
    Victory,
    Defeat
}
