using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
public static class ExtensionMethods
{
    private static readonly ConcurrentDictionary<Enum, string> s_cache = new ConcurrentDictionary<Enum, string>();
    public static string ToStringCached(this Enum value)
    {
        return s_cache.GetOrAdd(value, v => v.ToString());
    }
    private static readonly ConcurrentDictionary<string, DateTime> Date_cache = new ConcurrentDictionary<string, DateTime>();
    public static DateTime ToDateTimeCached(string value)
    {
        return Date_cache.GetOrAdd(value, v => DateTime.Parse(v));
    }
    public static T ToEnum<T>(this string value)
    {
        try
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
        catch (System.Exception)
        {
            return (T)Enum.Parse(typeof(T), "NONE", true);
        }

    }
    public static List<T> SwapInList<T>(this List<T> list, T obj1, T obj2)
    {
        if (!list.Contains(obj1) || !list.Contains(obj2))
            return list;
        var index1 = list.IndexOf(obj1);
        var index2 = list.IndexOf(obj2);
        var temp = list[index1];
        list[index1] = list[index2];
        list[index2] = temp;
        return list;
    }
    public static T PickRandom<T>(this IEnumerable<T> source)
    {
        return source.PickRandom(1).SingleOrDefault();
    }
    public static T SimplePickRandom<T>(this IEnumerable<T> source)
    {
        if (source.Count() == 0)
        {
            return default;
        }
        var random = UnityEngine.Random.Range(0, source.Count());
        return source.ElementAt(random);
    }
    public static T PickNewRandom<T>(this IEnumerable<T> source, IEnumerable<T> contains)
    {
        var cooked = source.Where(x => !contains.Contains(x));
        return cooked.PickRandom(1).SingleOrDefault();
    }
    public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
    {
        return source.Shuffle().Take(count);
    }
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        return source.OrderBy(x => Guid.NewGuid());
    }
    public static IList<T> Shuffle<T>(this IList<T> list, System.Random random)
    {
        if (random == null)
            random = new System.Random(System.DateTime.Now.Millisecond);
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
        return list;
    }
    public static List<Transform> GetChilden(this Transform transforms)
    {
        var raw = new List<Transform>();
        for (int i = 0; i < transforms.childCount; i++)
        {
            raw.Add(transforms.GetChild(i));
        }
        return raw;
    }
    public static IList<T> PickRandom<T>(this IList<T> source, int count, System.Random random = null)
    {
        return source.Shuffle(random).Take(count).ToList();
    }
    public static T PickRandom<T>(this IList<T> source, System.Random random)
    {
        return source.PickRandom(1, random).SingleOrDefault();
    }
    public static bool HasNullKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) where TKey : UnityEngine.Object
    {
        foreach (var key in dictionary.Keys)
        {
            if (key == null)
            {
                return true;
            }
        }

        return false;
    }


    public static void RemoveNullKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) where TKey : UnityEngine.Object
    {
        if (!dictionary.HasNullKeys())
        {
            return;
        }

        foreach (var key in dictionary.Keys.ToArray())
        {
            if (key == null)
            {
                dictionary.Remove(key);
            }
        }
    }
    public static Dictionary<TKey, TValue> InsertAtIndexOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value, int Index)
    {
        if (dict == null)
        {
            throw new ArgumentNullException(nameof(dict));
        }

        if (dict.Count < Index)
        {
            dict.Add(key, value);
            return dict;
        }

        // create a new dictionary to hold the new order of items
        var newDict = new Dictionary<TKey, TValue>();

        // add the first four items from the original dictionary
        int count = 0;
        foreach (var kvp in dict)
        {
            if (count < Index - 1)
            {
                newDict.Add(kvp.Key, kvp.Value);
            }
            else
            {
                break;
            }
            count++;
        }

        // add the new _item to the new dictionary
        newDict.Add(key, value);

        // add the remaining items from the original dictionary
        foreach (var kvp in dict)
        {
            if (count >= Index - 1)
            {
                newDict.Add(kvp.Key, kvp.Value);
            }
            count++;
        }

        // update the original dictionary with the new order of items
        dict.Clear();
        foreach (var kvp in newDict)
        {
            dict.Add(kvp.Key, kvp.Value);
        }
        return dict;
    }
    public static int SearchIndex<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
    {
        if (dict == null)
        {
            throw new ArgumentNullException(nameof(dict));
        }
        int index = 0;
        foreach (var item in dict)
        {
            if (item.Key.Equals(key))
            {
                return index;
            }
            index++;
        }
        return 0;
    }
    public static int GetVollum<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
    {
        if (dict == null)
        {
            throw new ArgumentNullException(nameof(dict));
        }
        int index = 1;
        foreach (var item in dict)
        {
            if (item.Key.Equals(key))
            {
                return index;
            }
            index++;
        }
        return 1;
    }
    public static Dictionary<TKey, TValue> GetEnemiesByIndexRange<TKey, TValue>(this Dictionary<TKey, TValue> dict, int startIndex, int endIndex)
    {
        if (dict == null || dict.Count == 0)
        {
            Debug.LogWarning("Dictionary is empty or null.");
            return new Dictionary<TKey, TValue>();
        }

        // Validate indices
        if (startIndex < 0 || endIndex >= dict.Count || startIndex > endIndex)
        {
            Debug.LogWarning("Invalid index range.");
            return new Dictionary<TKey, TValue>();
        }

        // Convert Dictionary to List and take the range
        var orderedEnemies = dict.ToList();
        var selectedRange = orderedEnemies.Skip(startIndex).Take(endIndex - startIndex + 1);

        // Convert back to Dictionary
        return selectedRange.ToDictionary(pair => pair.Key, pair => pair.Value);
    }
    public static int OrderInSprite(this Transform transform, float bonus = 0)
    {
        return (int)(-1 * (transform.position.y + bonus) * 1000f);
    }
    public static Dictionary<TKey, TValue> OverrideAtKeyOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value, TKey TargetKey)
    {
        if (dict == null)
        {
            throw new ArgumentNullException(nameof(dict));
        }

        if (!dict.ContainsKey(TargetKey))
        {
            dict.Add(key, value);
            return dict;
        }

        // create a new dictionary to hold the new order of items
        var newDict = new Dictionary<TKey, TValue>();

        // add the first four items from the original dictionary
        foreach (var kvp in dict)
        {
            if (!TargetKey.Equals(kvp.Key))
            {
                newDict.Add(kvp.Key, kvp.Value);
            }
            else
            {
                newDict.Add(key, value);
            }
        }

        // update the original dictionary with the new order of items
        dict.Clear();
        foreach (var kvp in newDict)
        {
            dict.Add(kvp.Key, kvp.Value);
        }
        return dict;
    }
    public static void DestroyAllChildWithPool(this Transform transform)
    {
        bool isPlaying = Application.isPlaying;

        while (transform.childCount != 0)
        {
            Transform child = transform.GetChild(0);

            child.SetParent(null);
            GameObject.Destroy(child.gameObject);
        }
    }
    public static void DestroyAllChildEditor(this Transform transform)
    {

        while (transform.childCount != 0)
        {
            Transform child = transform.GetChild(0);
            child.SetParent(null);
            GameObject.DestroyImmediate(child.gameObject);
        }
    }
    //public static T GetNextElement<T>(this IList<IList<T>> source)
    //{
    //    return source;
    //}
    public static bool Contains(this LayerMask mask, int layer)
    {
        return ((mask & (1 << layer)) != 0);
    }
    public static Color SetA(this Color color, float alpha)
    {
        return new Color()
        {
            a = alpha,
            r = color.r,
            g = color.g,
            b = color.b,
        };
    }
    public static string MainID(this GameObject game)
    {
        return game.GetInstanceID().ToString();
    }
    public static Transform HighParent(this Transform target)
    {
        Transform current = target;
        Transform result = current;
        while (current != null)
        {
            current = current.parent;
            if (current != null)
            {
                result = current;
            }
        }
        return result;
    }
    public static Transform HighParentTag(this Transform target, string tag, bool checkNull = false)
    {
        Transform current = target;
        Transform result = current;
        while (current != null && current.parent != null && !current.parent.CompareTag(tag))
        {
            current = current.parent;
            if (current != null)
            {
                result = current;
            }
        }
        if (checkNull && result == target) return null;
        return result;
    }
    public static Transform FindParentTag(this Transform target, string tag)
    {
        Transform current = target;
        Transform result = current;
        while (current != null && current.parent != null)
        {
            if (current.parent.CompareTag(tag))
            {
                return current;
            }
            else
            {
                current = current.parent;
                if (current != null)
                {
                    result = current;
                }
                else
                {
                    return null;
                }
            }
        }
        return result;
    }
    public static Transform FindParentLayer(this Transform target, int layer)
    {
        Transform current = target;
        Transform result = current;
        while (current != null && current.parent != null)
        {
            if (layer == current.gameObject.layer)
            {
                return current;
            }
            else
            {
                current = current.parent;
                if (current != null)
                {
                    result = current;
                }
                else
                {
                    return null;
                }
            }
        }
        return result;
    }
    public static Transform HighParentLayer(this Transform target, LayerMask layer, bool checkNull = false)
    {
        Transform current = target;
        Transform result = current;
        while (current != null && current.parent != null)
        {
            if (layer.Contains(current.gameObject.layer))
            {
                return current;
            }
            else
            {
                current = current.parent;
                if (current != null)
                {
                    result = current;
                }
            }
        }
        if (checkNull && result == target) return null;
        return result;
    }
    public static Transform GetChild(this Transform target, string codename)
    {
        foreach (Transform child in target)
        {
            if (child.name == codename)
            {
                return child;
            }
        }
        return null;
    }
    //public static IEnumerable<T> Where<T>(this IEnumerable<TSource> source, Func<TSource, T> predicate);  
    public static Sprite ToSprite(this Texture2D texture)
    {
        Rect rect = new Rect(0, 0, texture.width, texture.height);
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
    }
    public static bool IsPointerOverGameObject()
    {
        //check mouse
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        //check touch
        if (IsPointerOverUIObject())
        {
            //if (EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId))
            return true;
        }

        return false;
    }
    public static bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject.layer == 5) //5 = UI layer
            {
                return true;
            }
        }
        return false;
    }

    // Optional: Check if a target is within the elliptical range
    public static bool IsTargetInRangeElipse(Vector2 pivot,Vector2 targetPosition, float rangeX, float rangeY)
    {
        Vector2 relativePos = targetPosition - pivot;
        float distanceX = relativePos.x / rangeX;
        float distanceY = relativePos.y / rangeY;
        return (distanceX * distanceX + distanceY * distanceY) <= 1f;

    }

    public static Quaternion FromToRotation(Vector2 fromDirection, Vector2 toDirection)
    {
        var angle = Vector2.SignedAngle(fromDirection, toDirection);
        var rotation = Quaternion.Euler(0, 0, angle);
        return rotation;
    }
    public static (int, int, int) UpLevel(this (int, int, int) value)
    {
        if(value.Item1 < 3)
        {
            value.Item1++;
            return value;
        }
        else
        {
            value.Item1 = 4;
            if(value.Item2 >= 1)
            {
                value.Item2++;
            }
            else
            {
                if (value.Item3 >= 1)
                {
                    value.Item3++;
                }
                else
                {

                }
            }
            return value;
        }
    }
    public static void ChangeMaterialOnly(Renderer mesh, Action<MaterialPropertyBlock> action)
    {
        if (mesh == null)
            return;
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        action.Invoke(propertyBlock);
        mesh.SetPropertyBlock(propertyBlock);
    }
}

[Serializable]
public class FloatEvent : UnityEvent<DataFloatEvent>
{

}
[Serializable]
public class DataFloatEvent
{
    public float data;
    public DataFloatEvent(float _value)
    {
        data = _value;
    }
}