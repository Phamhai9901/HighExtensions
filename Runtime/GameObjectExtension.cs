using System.Collections.Concurrent;
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Linq;

public static class GameObjectExtension
{
    public static GameObject Spawn(this GameObject prefab, Vector3 worldPosition, Transform parent)
    {
        //Debug.LogWarning("Spawn: " + prefab.name + " / " + parent.name);
        return PoolManager.Instance.Spawn(prefab, worldPosition, parent);
    }
    public static GameObject Spawn(this GameObject prefab, Vector3 worldPosition, Quaternion quaternion, Transform parent)
    {
        //Debug.LogWarning("Spawn: " + prefab.name + " / " + parent.name);
        var obj = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        obj.transform.rotation = quaternion;
        return obj;
    }
    public static GameObject Spawn(this GameObject prefab, Vector3 worldPosition, Vector3 size, Transform parent)
    {
        //Debug.LogWarning("Spawn: " + prefab.name + " / " + parent.name);
        var obj = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        obj.transform.localScale = size;
        return obj;
    }
    public static GameObject SpawnLocal(this GameObject prefab, Vector3 LocalPosition, Transform parent)
    {
        //Debug.LogWarning("Spawn: " + prefab.name + " / " + parent.name);
        var obj = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        obj.transform.localPosition = LocalPosition;
        return obj;
    }
    public static GameObject SpawnDelay(this GameObject prefab, Vector3 worldPosition, float duration, Transform parent)
    {
        return PoolManager.Instance.SpawnDelay(prefab, worldPosition,duration, parent);
    }
    public static RectTransform UISpawn(this GameObject prefab, Vector2 uiPosition, Transform parent)
    {
        var obj = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        var item = obj.GetComponent<RectTransform>();
        if (item)
        {
            item.anchoredPosition = uiPosition;
        }
        return item;
    }
    public static RectTransform UISpawn(this GameObject prefab, Vector3 uiPosition, Transform parent)
    {
        var obj = PoolManager.Instance.Spawn(prefab, Vector3.zero, parent);
        var item = obj.GetComponent<RectTransform>();
        if (item)
        {
            item.anchoredPosition3D = uiPosition;
        }
        return item;
    }
    public static void Despawn(this GameObject obj, bool surpassWarning = false)
    {
        PoolManager.Instance.Kill(obj, surpassWarning);
    }
    public static void DespawnDelay(this GameObject obj, float duration, bool surpassWarning = false)
    {
        PoolManager.Instance.KillDelay(obj, duration, surpassWarning);
    }
    #region Special Spawn
    private static readonly Dictionary<int, object> breakerBlock_cache = new ();
    public static GameObject SpawnBreakerBlock(GameObject prefab, Vector3 worldPosition, Transform parent, out object sctipt)
    {
        //Debug.LogWarning("Spawn: " + prefab.name + " / " + parent.name);
        var obj = PoolManager.Instance.Spawn(prefab, worldPosition, parent);
        sctipt = getBreakerBlock(obj);
        return obj;
    }
    public static object getBreakerBlock(GameObject clone)
    {
        if (breakerBlock_cache.TryGetValue(clone.GetInstanceID(), out var vals))
        {
            return vals;
        }
        else
        {
            var script = clone.GetComponent<object>();
            breakerBlock_cache.Add(clone.GetInstanceID(), script);
            return script;
        }
    }
    #endregion
}