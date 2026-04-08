using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using High;

/// <summary>
/// Audio Manager — singleton quản lý toàn bộ audio trong game.
///
/// ╔══════════════════════════════════════════════════════════════╗
/// ║  API CHÍNH                                                   ║
/// ║                                                              ║
/// ║  AudioManager.Play("sword_hit");           // SFX oneshot    ║
/// ║  AudioManager.Play("sword_hit", pos);      // SFX 3D         ║
/// ║  AudioManager.PlayMusic("boss_theme");     // Crossfade music ║
/// ║  AudioManager.StopMusic(2f);               // Fade out        ║
/// ║  AudioManager.SetVolume(AudioCategory.SFX, 0.5f);            ║
/// ║                                                              ║
/// ║  // Advanced                                                 ║
/// ║  var h = AudioManager.Play("alarm", SFXPlayMode.Loop);       ║
/// ║  h.FadeOut(1f);                                              ║
/// ╚══════════════════════════════════════════════════════════════╝
///
/// SETUP:
/// 1. Tạo AudioLibrary (Create → Audio → Audio Library)
/// 2. Thêm entries vào library
/// 3. Gắn AudioManager vào scene, assign library
/// 4. (Optional) Assign AudioMixer + groups
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Library")]
    [SerializeField] AudioLibrary library;

    [Header("Mixer (Optional)")]
    [SerializeField] AudioMixerGroup sfxMixerGroup;
    [SerializeField] AudioMixerGroup musicMixerGroup;
    [SerializeField] AudioMixerGroup uiMixerGroup;
    [SerializeField] AudioMixerGroup ambientMixerGroup;
    [SerializeField] AudioMixerGroup voiceMixerGroup;

    [Header("Pool Settings")]
    [SerializeField] int initialPoolSize = 10;
    [SerializeField] int maxPoolSize = 30;

    [Header("Music")]
    [SerializeField] float defaultCrossfadeDuration = 1.5f;

    [Header("Playlist")]
    [SerializeField] MusicPlaylistPlayer playlistPlayer;

    [Header("Level Music")]
    [SerializeField] LevelMusicConfig levelMusicConfig;

    /// <summary>LevelMusicConfig reference (dùng bởi LevelMusicBootstrap).</summary>
    public LevelMusicConfig LevelConfig => levelMusicConfig;

    // ═══════════════ STATE ═══════════════

    // Source pool cho SFX
    readonly Queue<AudioSource> sourcePool = new();
    readonly HashSet<AudioSource> activeSources = new();

    // Music — 2 sources cho crossfade
    AudioSource musicSourceA;
    AudioSource musicSourceB;
    AudioSource currentMusicSource;
    string currentMusicKey;
    Coroutine crossfadeRoutine;

    // Volume per category
    readonly Dictionary<AudioCategory, float> categoryVolumes = new();

    // PlayerPrefs keys
    const string PREF_MASTER = "Audio_Master";
    const string PREF_SFX = "Audio_SFX";
    const string PREF_MUSIC = "Audio_Music";
    const string PREF_UI = "Audio_UI";
    const string PREF_AMBIENT = "Audio_Ambient";
    const string PREF_VOICE = "Audio_Voice";

    // ═══════════════ INIT ═══════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitPool();
        InitMusicSources();
        LoadVolumeSettings();
        InitPlaylistPlayer();
    }

    void InitPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
            sourcePool.Enqueue(CreateSource($"SFX_Pool_{i}"));
    }

    void InitMusicSources()
    {
        musicSourceA = CreateSource("Music_A");
        musicSourceA.loop = true;
        musicSourceA.outputAudioMixerGroup = musicMixerGroup;

        musicSourceB = CreateSource("Music_B");
        musicSourceB.loop = true;
        musicSourceB.outputAudioMixerGroup = musicMixerGroup;

        currentMusicSource = musicSourceA;
    }

    AudioSource CreateSource(string label)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform);
        return go.AddComponent<AudioSource>();
    }

    void LoadVolumeSettings()
    {
        categoryVolumes[AudioCategory.SFX] = PlayerPrefs.GetFloat(PREF_SFX, 1f);
        categoryVolumes[AudioCategory.Music] = PlayerPrefs.GetFloat(PREF_MUSIC, 1f);
        categoryVolumes[AudioCategory.UI] = PlayerPrefs.GetFloat(PREF_UI, 1f);
        categoryVolumes[AudioCategory.Ambient] = PlayerPrefs.GetFloat(PREF_AMBIENT, 1f);
        categoryVolumes[AudioCategory.Voice] = PlayerPrefs.GetFloat(PREF_VOICE, 1f);
    }

    void InitPlaylistPlayer()
    {
        if (playlistPlayer != null) return;

        playlistPlayer = GetComponentInChildren<MusicPlaylistPlayer>();
        if (playlistPlayer == null)
        {
            var go = new GameObject("PlaylistPlayer");
            go.transform.SetParent(transform);
            playlistPlayer = go.AddComponent<MusicPlaylistPlayer>();
        }
    }

    // ═══════════════ STATIC API ═══════════════

    /// <summary>Phát SFX theo key. Trả về handle để control.</summary>
    public static AudioHandle Play(string key, SFXPlayMode mode = SFXPlayMode.OneShot)
    {
        return Instance != null ? Instance.PlayInternal(key, Vector3.zero, mode) : null;
    }

    /// <summary>Phát SFX 3D tại vị trí.</summary>
    public static AudioHandle Play(string key, Vector3 position, SFXPlayMode mode = SFXPlayMode.OneShot)
    {
        return Instance != null ? Instance.PlayInternal(key, position, mode) : null;
    }

    /// <summary>Phát music với crossfade.</summary>
    public static AudioHandle PlayMusic(string key, float fadeDuration = -1f)
    {
        return Instance != null ? Instance.PlayMusicInternal(key, fadeDuration) : null;
    }

    /// <summary>Dừng music với fade out.</summary>
    public static void StopMusic(float fadeDuration = 1f)
    {
        Instance?.StopMusicInternal(fadeDuration);
    }

    // ═══════════════ STATIC API — PLAYLIST ═══════════════

    /// <summary>Phát playlist (shuffle/sequence tùy config).</summary>
    public static void PlayPlaylist(MusicPlaylist playlist)
    {
        if (Instance == null || Instance.playlistPlayer == null) return;

        // Dừng single-track music nếu đang phát
        Instance.StopMusicInternal(0.5f);

        Instance.playlistPlayer.Play(playlist);
    }

    /// <summary>Chuyển sang playlist khác với crossfade.</summary>
    public static void SwitchPlaylist(MusicPlaylist playlist, float fadeDuration = -1f)
    {
        if (Instance == null || Instance.playlistPlayer == null) return;

        Instance.StopMusicInternal(0f);

        Instance.playlistPlayer.SwitchTo(playlist, fadeDuration);
    }

    /// <summary>Dừng playlist với fade out.</summary>
    public static void StopPlaylist(float fadeDuration = 1.5f)
    {
        Instance?.playlistPlayer?.Stop(fadeDuration);
    }

    /// <summary>Skip sang bài tiếp trong playlist.</summary>
    public static void SkipTrack()
    {
        Instance?.playlistPlayer?.SkipTrack();
    }

    /// <summary>Pause playlist.</summary>
    public static void PausePlaylist()
    {
        Instance?.playlistPlayer?.Pause();
    }

    /// <summary>Resume playlist.</summary>
    public static void ResumePlaylist()
    {
        Instance?.playlistPlayer?.Resume();
    }

    /// <summary>Playlist player reference (để subscribe event, v.v.).</summary>
    public static MusicPlaylistPlayer Playlist =>
        Instance?.playlistPlayer;

    // ═══════════════ STATIC API — LEVEL MUSIC SHORTCUTS ═══════════════
    // Gọi từ game logic — delegate xuống LevelMusicBootstrap

    /// <summary>Chuyển sang gameplay music.</summary>
    public static void PlayGameplayMusic(float fadeDuration = -1f)
    {
        LevelMusicBootstrap.Instance?.PlayGameplay(fadeDuration);
    }

    /// <summary>Chuyển sang intense music (wave cuối, time pressure).</summary>
    public static void PlayIntenseMusic(float fadeDuration = -1f)
    {
        LevelMusicBootstrap.Instance?.PlayIntense(fadeDuration);
    }

    /// <summary>Chuyển sang boss music.</summary>
    public static void PlayBossMusic(float fadeDuration = -1f)
    {
        LevelMusicBootstrap.Instance?.PlayBoss(fadeDuration);
    }

    /// <summary>Chuyển sang victory music.</summary>
    public static void PlayVictoryMusic(float fadeDuration = -1f)
    {
        LevelMusicBootstrap.Instance?.PlayVictory(fadeDuration);
    }

    /// <summary>Chuyển sang defeat music.</summary>
    public static void PlayDefeatMusic(float fadeDuration = -1f)
    {
        LevelMusicBootstrap.Instance?.PlayDefeat(fadeDuration);
    }

    /// <summary>Phát menu music.</summary>
    public static void PlayMenuMusic(float fadeDuration = -1f)
    {
        if (Instance?.levelMusicConfig?.MenuPlaylist != null)
            SwitchPlaylist(Instance.levelMusicConfig.MenuPlaylist, fadeDuration);
    }

    /// <summary>Đổi level runtime (endless mode).</summary>
    public static void ChangeLevel(string levelId)
    {
        LevelMusicBootstrap.Instance?.ChangeLevel(levelId);
    }

    /// <summary>Dừng mọi music.</summary>
    public static void StopAllMusic(float fadeDuration = 1.5f)
    {
        LevelMusicBootstrap.Instance?.StopAll(fadeDuration);
    }

    /// <summary>LevelMusicConfig reference.</summary>
    public static LevelMusicConfig GetLevelConfig => Instance.levelMusicConfig;

    /// <summary>Set volume theo category (0-1). Tự save PlayerPrefs.</summary>
    public static void SetVolume(AudioCategory category, float volume)
    {
        Instance?.SetVolumeInternal(category, volume);
    }

    /// <summary>Lấy volume hiện tại.</summary>
    public static float GetVolume(AudioCategory category)
    {
        if (Instance == null) return 1f;
        return Instance.categoryVolumes.GetValueOrDefault(category, 1f);
    }

    /// <summary>Phát clip trực tiếp (không cần key trong library).</summary>
    public static AudioHandle PlayDirect(AudioClip clip, AudioCategory category = AudioCategory.SFX,
        float volume = 1f, float pitch = 1f)
    {
        return Instance != null ? Instance.PlayDirectInternal(clip, category, volume, pitch) : null;
    }

    /// <summary>Music đang phát?</summary>
    public static bool IsMusicPlaying => Instance != null
        && Instance.currentMusicSource != null
        && Instance.currentMusicSource.isPlaying;

    /// <summary>Key music đang phát.</summary>
    public static string CurrentMusicKey => Instance?.currentMusicKey;

    // ═══════════════ INTERNAL — SFX ═══════════════

    AudioHandle PlayInternal(string key, Vector3 position, SFXPlayMode mode)
    {
        var entry = library.Get(key);
        if (entry == null) return null;

        if (!entry.IsReady()) return null;

        var clip = entry.GetClip();
        if (clip == null) return null;

        // IfNotPlaying check
        if (mode == SFXPlayMode.IfNotPlaying && IsClipPlaying(clip))
            return null;

        var source = GetSource();
        if (source == null) return null;

        // Config source
        float catVolume = categoryVolumes.GetValueOrDefault(entry.Category, 1f);
        source.clip = clip;
        source.volume = entry.Volume * catVolume;
        source.pitch = entry.GetPitch();
        source.spatialBlend = entry.SpatialBlend;
        source.loop = mode == SFXPlayMode.Loop;
        source.outputAudioMixerGroup = GetMixerGroup(entry.Category);
        source.transform.position = position;

        source.Play();
        entry.LastPlayedTime = Time.unscaledTime;
        activeSources.Add(source);

        // Auto return nếu không loop
        if (!source.loop)
            StartCoroutine(ReturnAfterPlay(source, clip.length / Mathf.Abs(source.pitch)));

        return new AudioHandle(source, key, this);
    }

    AudioHandle PlayDirectInternal(AudioClip clip, AudioCategory category, float volume, float pitch)
    {
        if (clip == null) return null;

        var source = GetSource();
        if (source == null) return null;

        float catVolume = categoryVolumes.GetValueOrDefault(category, 1f);
        source.clip = clip;
        source.volume = volume * catVolume;
        source.pitch = pitch;
        source.loop = false;
        source.outputAudioMixerGroup = GetMixerGroup(category);

        source.Play();
        activeSources.Add(source);
        StartCoroutine(ReturnAfterPlay(source, clip.length / Mathf.Abs(pitch)));

        return new AudioHandle(source, clip.name, this);
    }

    bool IsClipPlaying(AudioClip clip)
    {
        foreach (var source in activeSources)
        {
            if (source != null && source.isPlaying && source.clip == clip)
                return true;
        }
        return false;
    }

    // ═══════════════ INTERNAL — MUSIC ═══════════════

    AudioHandle PlayMusicInternal(string key, float fadeDuration)
    {
        // Nếu music đang phát cùng key → skip
        if (currentMusicKey == key && currentMusicSource.isPlaying)
            return new AudioHandle(currentMusicSource, key, this);

        var entry = library.Get(key);
        if (entry == null) return null;

        var clip = entry.GetClip();
        if (clip == null) return null;

        float duration = fadeDuration >= 0f ? fadeDuration : defaultCrossfadeDuration;
        float catVolume = categoryVolumes.GetValueOrDefault(AudioCategory.Music, 1f);
        float targetVolume = entry.Volume * catVolume;

        // Crossfade giữa 2 sources
        var nextSource = currentMusicSource == musicSourceA ? musicSourceB : musicSourceA;
        nextSource.clip = clip;
        nextSource.volume = 0f;
        nextSource.pitch = entry.GetPitch();
        nextSource.Play();

        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);

        crossfadeRoutine = StartCoroutine(CoCrossfade(
            currentMusicSource, nextSource, targetVolume, duration));

        currentMusicSource = nextSource;
        currentMusicKey = key;

        return new AudioHandle(nextSource, key, this);
    }

    void StopMusicInternal(float fadeDuration)
    {
        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);

        if (currentMusicSource != null && currentMusicSource.isPlaying)
        {
            StartCoroutine(CoFadeAndStop(currentMusicSource, fadeDuration));
        }

        currentMusicKey = null;
    }

    IEnumerator CoCrossfade(AudioSource fadeOut, AudioSource fadeIn,
        float targetVolume, float duration)
    {
        float elapsed = 0f;
        float startVolume = fadeOut.volume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            fadeOut.volume = Mathf.Lerp(startVolume, 0f, t);
            fadeIn.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        fadeOut.Stop();
        fadeOut.volume = 0f;
        fadeIn.volume = targetVolume;
    }

    IEnumerator CoFadeAndStop(AudioSource source, float duration)
    {
        float start = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        source.Stop();
        source.volume = 0f;
    }

    // ═══════════════ INTERNAL — VOLUME ═══════════════

    void SetVolumeInternal(AudioCategory category, float volume)
    {
        volume = Mathf.Clamp01(volume);
        categoryVolumes[category] = volume;

        // Save
        string key = category switch
        {
            AudioCategory.SFX => PREF_SFX,
            AudioCategory.Music => PREF_MUSIC,
            AudioCategory.UI => PREF_UI,
            AudioCategory.Ambient => PREF_AMBIENT,
            AudioCategory.Voice => PREF_VOICE,
            _ => PREF_SFX
        };
        PlayerPrefs.SetFloat(key, volume);

        // Cập nhật music source đang phát
        if (category == AudioCategory.Music && currentMusicSource != null
            && currentMusicSource.isPlaying)
        {
            var entry = library.Get(currentMusicKey);
            if (entry != null)
                currentMusicSource.volume = entry.Volume * volume;
        }
    }

    // ═══════════════ POOL ═══════════════

    AudioSource GetSource()
    {
        // Dọn sources đã phát xong
        CleanupFinished();

        if (sourcePool.Count > 0)
            return sourcePool.Dequeue();

        // Pool hết → tạo thêm nếu chưa max
        if (activeSources.Count < maxPoolSize)
            return CreateSource($"SFX_Pool_{activeSources.Count}");

        Debug.LogWarning("[AudioManager] Pool exhausted! Consider increasing maxPoolSize.");
        return null;
    }

    void ReturnSource(AudioSource source)
    {
        if (source == null) return;

        source.Stop();
        source.clip = null;
        source.loop = false;
        source.spatialBlend = 0f;
        source.transform.localPosition = Vector3.zero;

        activeSources.Remove(source);
        sourcePool.Enqueue(source);
    }

    IEnumerator ReturnAfterPlay(AudioSource source, float delay)
    {
        yield return new WaitForSecondsRealtime(delay + 0.1f);
        ReturnSource(source);
    }

    void CleanupFinished()
    {
        var toReturn = new List<AudioSource>();
        foreach (var source in activeSources)
        {
            if (source != null && !source.isPlaying && !source.loop)
                toReturn.Add(source);
        }
        foreach (var source in toReturn)
            ReturnSource(source);
    }

    // ═══════════════ MIXER ═══════════════

    AudioMixerGroup GetMixerGroup(AudioCategory category)
    {
        return category switch
        {
            AudioCategory.SFX => sfxMixerGroup,
            AudioCategory.Music => musicMixerGroup,
            AudioCategory.UI => uiMixerGroup,
            AudioCategory.Ambient => ambientMixerGroup,
            AudioCategory.Voice => voiceMixerGroup,
            _ => sfxMixerGroup
        };
    }
}
