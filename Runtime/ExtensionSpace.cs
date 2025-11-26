using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
namespace High
{
    public static class ExtensionSpace
    {
        public static Vector2 ToVector2XY(this Vector3 posV3)
        {
            return new Vector2(posV3.x, posV3.y);
        }
        public static Vector2 ToVector2XZ(this Vector3 posV3)
        {
            return new Vector2(posV3.x, posV3.z);
        }
        public static Vector2 ToVector2YZ(this Vector3 posV3)
        {
            return new Vector2(posV3.y, posV3.z);
        }
        public static Vector2 ReverseValue(this Vector2 posV2)
        {
            return new Vector2(posV2.y, posV2.x);
        }
        public static void Rescale(this Transform obj, Vector3 newScale)
        {
            if (obj.root != obj)
            {
                Transform parent = obj.parent;
                obj.SetParent(null);
                obj.localScale = newScale;
                obj.SetParent(parent, true);
            }
        }
        public static void RotateDirection2D(this Transform obj, Vector2 Direction)
        {
            float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
            obj.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        public static float Angle2D(Vector3 vectorA, Vector3 vectorB)
        {
            Vector2 direction = ((Vector2)vectorA - (Vector2)vectorB).normalized;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
        public static void RotateTowards(this Transform rotateTransform, Transform vectorA, Transform vectorB, bool flip = false)
        {
            var angle = Angle2D(vectorA.position, vectorB.position);
            var offset = 0;
            if (flip)
            {
                angle = angle + 180;
            }
            rotateTransform.rotation = Quaternion.Euler(Vector3.forward * (angle + offset));
        }
        public static Vector3 RandomDirectionInPivot(Vector3 pivot)
        {
            return new Vector3(pivot.x + UnityEngine.Random.Range(-1, 1), pivot.y + UnityEngine.Random.Range(-1, 1), pivot.z + UnityEngine.Random.Range(-1, 1)) - pivot;
        }
        public static Vector3 GetPivotDirectionIfMaxRange(Vector2 target, Vector2 B, float distance)
        {
            // Tính khoảng cách AB
            float AB = Vector2.Distance(target, B);

            // Nếu A và B trùng nhau
            if (AB == 0)
            {
                Debug.LogWarning("A and B are the same point. Returning A as C.");
                return target;
            }

            // Tính tham số t sao cho AC <= maxDistance
            float t = distance / AB;
            if (t > 1) t = 1; // Đảm bảo C nằm trong đoạn AB

            // Tính tọa độ C bằng cách nội suy tuyến tính
            return Vector2.Lerp(target, B, t);
        }
        public static Vector2 FindEllipsePoint(Vector2 A, Vector2 B, float a, float b)
        {
            // Vector D = B - A
            Vector2 D = B - A;

            // Tính t dựa trên phương trình elipse
            float Dx = D.x;
            float Dy = D.y;
            float denominator = (Dx * Dx) / (a * a) + (Dy * Dy) / (b * b);

            if (denominator == 0)
            {
                return A; // Trả về A nếu không có giao điểm hợp lệ
            }

            float t = Mathf.Sqrt(1f / denominator);

            // Kiểm tra nếu t hợp lệ (t nằm trong [0, 1] để C nằm giữa A và B)
            if (t > 1f)
            {
                return B; // In Range
            }

            // Tính tọa độ C
            Vector2 C = A + t * D;
            return C;
        }
        public static Vector3 PointInNav(Vector3 pivot, int Arena)
        {
            NavMeshHit hit;
            // Check if the random point is on the NavMesh
            if (NavMesh.SamplePosition(pivot, out hit, 100f, Arena))
            {
                return hit.position; // Exit after successful spawn
            }
            return pivot;
        }
        public static Vector2 WorldToRectPoint2D(Vector3 worldPoint, Camera cameraView, RectTransform Canvas)
        {
            if (cameraView == null || Canvas == null)
            {
                Debug.LogWarning("MainCamera or CanvasRect is null.");
                return Vector2.zero;
            }

            // Step 1: Convert 2D world point to screen point
            Vector3 screenPoint = cameraView.WorldToScreenPoint(worldPoint);

            // Step 2: Convert screen point to local point in RectTransform
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Canvas, screenPoint, null, out localPoint);

            return localPoint;
        }
        public static Vector2 FindClosestVector(this Vector2 vectorA, List<Vector2> vectorList)
        {
            if (vectorList == null || vectorList.Count == 0)
            {
                throw new ArgumentException("Danh sách vector không được rỗng.");
            }

            return vectorList.OrderBy(v => Vector2.Distance(vectorA, v)).First();
        }
        public static Vector2 RandomInRange(this Vector2 vector, float x, float y)
        {
            return vector + new Vector2(UnityEngine.Random.Range(-x, x), UnityEngine.Random.Range(-y, y));
        }
        public static Vector3 RandomInRange(this Vector3 vector, float x, float y, float z)
        {
            return vector + new Vector3(UnityEngine.Random.Range(-x, x), UnityEngine.Random.Range(-y, y), UnityEngine.Random.Range(-z, z));
        }
    }
}