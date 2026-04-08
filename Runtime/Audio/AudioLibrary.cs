using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject chứa toàn bộ audio entries của game.
/// Tạo: Create → Audio → Audio Library
/// 
/// ╔══════════════════════════════════════════════════════════╗
/// ║  WORKFLOW                                                ║
/// ║                                                          ║
/// ║  1. Create AudioLibrary asset                            ║
/// ║  2. Add entries: key + clip(s) + config                  ║
/// ║  3. Gắn vào AudioManager                                ║
/// ║  4. Code: AudioManager.Play("sword_hit")                 ║
/// ║                                                          ║
/// ║  Đổi clip? → Swap trong library, code không đổi          ║
/// ║  Thêm variation? → Thêm clip vào array, tự random        ║
/// ╚══════════════════════════════════════════════════════════╝
/// </summary>
[CreateAssetMenu(menuName = "Audio/Audio Library", fileName = "AudioLibrary")]
public class AudioLibrary : ScriptableObject
{
    [SerializeField] List<AudioEntry> entries = new();

    // Runtime lookup — build on first access
    Dictionary<string, AudioEntry> lookup;

    /// <summary>Lấy entry theo key. O(1) sau lần đầu.</summary>
    public AudioEntry Get(string key)
    {
        BuildLookup();
        lookup.TryGetValue(key, out var entry);

#if UNITY_EDITOR
        if (entry == null)
            Debug.LogWarning($"[AudioLibrary] Key not found: \"{key}\"");
#endif

        return entry;
    }

    /// <summary>Check key tồn tại.</summary>
    public bool Has(string key)
    {
        BuildLookup();
        return lookup.ContainsKey(key);
    }

    /// <summary>Lấy tất cả entries (dùng cho Editor).</summary>
    public List<AudioEntry> GetAllEntries() => entries;

    /// <summary>Lấy entries theo category.</summary>
    public List<AudioEntry> GetByCategory(AudioCategory category)
    {
        return entries.FindAll(e => e.Category == category);
    }

    /// <summary>Thêm entry mới (dùng cho Editor tool).</summary>
    public void AddEntry(AudioEntry entry)
    {
        entries.Add(entry);
        lookup = null; // Force rebuild
    }

    /// <summary>Xóa entry (dùng cho Editor tool).</summary>
    public bool RemoveEntry(string key)
    {
        int removed = entries.RemoveAll(e => e.Key == key);
        if (removed > 0) lookup = null;
        return removed > 0;
    }

    void BuildLookup()
    {
        if (lookup != null) return;

        lookup = new Dictionary<string, AudioEntry>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.Key)) continue;

            if (!lookup.TryAdd(entry.Key, entry))
                Debug.LogWarning($"[AudioLibrary] Duplicate key: \"{entry.Key}\" in {name}");
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        lookup = null; // Rebuild khi thay đổi trong Inspector
    }
#endif
}
