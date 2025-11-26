using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
public static class HighUtils
{
    private static IEnumerator CoAction(System.Action action, float wait, bool ignoreTimeScale)
    {
        if (ignoreTimeScale)
        {
            yield return new WaitForSecondsRealtime(wait);
        }
        else
        {
            yield return new WaitForSeconds(wait);
        }

        if (action != null) action();
    }

    public static Coroutine DoAction(MonoBehaviour go, System.Action action, float wait, bool ignoreTimeScale = false)
    {
        if (go != null && go.isActiveAndEnabled)
        {
            return go.StartCoroutine(CoAction(action, wait, ignoreTimeScale));
        }
        return null;
    }

    public static string GetActiveSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    public static bool CheckLayerMask(LayerMask mask, int layer)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    public static void SetLayer(this Transform t, int layer, bool recursive)
    {
        t.gameObject.layer = layer;
        if (recursive)
        {
            for (int i = 0; i < t.childCount; ++i)
            {
                var child = t.GetChild(i);
                SetLayer(child, layer, recursive);
            }
        }
    }

    public static bool IsOutOfRange(Vector3 originalPos, Vector3 checkPos, float range)
    {
        originalPos.y = checkPos.y = 0;
        if (Vector3.SqrMagnitude(originalPos - checkPos) > range * range)
            return true;

        return false;
    }

    public static bool IsOutOfRange(BoxCollider boxCollider, Vector3 pos, float range, out Vector3 _closestPoint)
    {
        Vector3 closestPoint = boxCollider.ClosestPointOnBounds(pos);
        closestPoint.y = boxCollider.center.y;
        pos.y = boxCollider.center.y;
        _closestPoint = closestPoint;
        if (Vector3.SqrMagnitude(pos - closestPoint) > (range * range))
        {
            Debug.DrawLine(closestPoint, pos, Color.black, 2);
            return true;
        }
        Debug.DrawLine(closestPoint, pos, Color.white, 2);
        return false;
    }

    public static bool IsInSideOfRange(Vector3 originalPos, Vector3 checkPos, float range, bool ignoreHigh = true)
    {
        if (ignoreHigh)
            originalPos.y = checkPos.y;

        if (Vector3.SqrMagnitude(checkPos - originalPos) <= (range * range))
            return true;
        else
            return false;
    }

    public static List<int> GetRandomListIndex(int maxIndex, int n)
    {
        if (n > maxIndex)
            n = maxIndex;

        List<int> result = new List<int>();
        while (result.Count < n)
        {
            int _random = Random.Range(0, maxIndex - result.Count);

            int t = _random;
            for (int i = 0; i <= t; ++i)
            {
                if (result.Contains(i))
                {
                    t++;
                }
            }

            result.Add(t);
        }

        return result;
    }

    public static bool GetPath(NavMeshPath path, Vector3 fromPos, Vector3 toPos, int passableMask)
    {
        path.ClearCorners();

        if (NavMesh.CalculatePath(fromPos, toPos, passableMask, path) == false)
            return false;

        return true;
    }

    public static float GetPathLength(NavMeshPath path)
    {
        float lng = 0.0f;

        if ((path.status != NavMeshPathStatus.PathInvalid) && (path.corners.Length > 0))
        {
            for (int i = 1; i < path.corners.Length; ++i)
            {
                //                Debug.DrawLine(path.corners[i - 1], path.corners[i], Color.cyan);
                lng += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            //            EGDebug.CLog("CalculateDistancePath", lng);
        }
        else
        {
            lng = -1;
        }

        return lng;
    }
}
