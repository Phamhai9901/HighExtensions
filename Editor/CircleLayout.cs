using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleLayout : MonoBehaviour
{
    [SerializeField] public float radius = 5.0f;
    public float StartAngle;

    public void SetPosition()
    {
        int numObjects = this.transform.childCount;
        float angleStep = 360.0f / numObjects;

        for (int i = 0; i < numObjects; i++)
        {
            float angle = i * angleStep + StartAngle;
            float x = -Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            Vector3 position = this.transform.position + new Vector3(x, y, 0);
            this.transform.GetChild(i).transform.position = position;
        }
    }
}
