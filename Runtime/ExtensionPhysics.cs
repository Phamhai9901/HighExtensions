using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
namespace High
{
    public static class ExtensionPhysics
    {
        public static float DmgWithF(float parameter, Rigidbody2D rbCollision1, Rigidbody2D rbCollision2)
        {
            var a1 = rbCollision1.linearVelocity.magnitude;
            var Fmain1 = a1 * rbCollision1.mass;
            float a2 = 0;
            float Fmain2 = 0;
            if (rbCollision2)
            {
                a2 = rbCollision2.linearVelocity.magnitude;
                Fmain2 = a2 * rbCollision2.mass;
            }
            return Fmain1 * parameter + Fmain2 * parameter;
        }
        public static float DmgWithCollisionF(float parameter, Collision2D Impact)
        {
            return Impact.relativeVelocity.magnitude * parameter;
        }
        public static void AddExplosionForce(this Rigidbody2D rb, float explosionForce, Vector2 explosionPosition, float upwardsModifier = 0.0F, ForceMode2D mode = ForceMode2D.Force)
        {
            var explosionDir = rb.position - explosionPosition;
            var explosionDistance = explosionDir.magnitude;

            // Normalize without computing magnitude again
            if (upwardsModifier == 0)
                explosionDir /= explosionDistance;
            else
            {
                // From Rigidbody.AddExplosionForce doc:
                // If you pass a non-zero value for the upwardsModifier parameter, the direction
                // will be modified by subtracting that value from the Y component of the centre point.
                explosionDir.y += upwardsModifier;
                explosionDir.Normalize();
            }
            rb.AddForce(Mathf.Lerp(0, explosionForce, explosionDistance) * explosionDir.normalized, mode);
        }
        public static float TimeForSnV(this Vector3 start, float vanToc, Vector3 end)
        {
            var distance = Vector3.Distance(start, end);
            return distance / vanToc;
        }
        public static List<Vector2> points(this ContactPoint2D[] contactPoints)
        {
            var list = new List<Vector2>() { };
            foreach (var item in contactPoints)
            {
                list.Add(item.point);
            }
            return list;
        }
        public static void TorqueLookAtPoint(Rigidbody2D rigidbody, Vector2 point, float force, float damper = 0f)
        {
            Vector2 direction = point - rigidbody.position;
            TorqueLookToward(rigidbody, direction, force, damper);
        }
        public static void TorqueLookToward(Rigidbody2D rigidbody, Vector2 direction, float force, float damper = 0f)
        {

            Vector2 p = rigidbody.position;
            Vector2 forward = rigidbody.transform.forward; // axis we are rotating

            Vector2 cross = Vector3.Cross(forward, direction);

            float angleDiff = Vector3.Angle(forward, direction);
            angleDiff = Mathf.Sqrt(angleDiff);

            rigidbody.AddTorque(cross.magnitude * angleDiff * force);
            rigidbody.AddTorque(-rigidbody.angularVelocity * damper);
            //rigidbody.AddTorque(cross * angleDiff * force);
            //Debug.Log(direction);
            //Debug.DrawLine(p, p + direction.normalized, Color.yellow, .05f);
            //Debug.DrawLine(p, p * rigidbody.angularVelocity, Color.yellow);
            //Debug.DrawLine(p, p + new Vector3(rigidbody.angularVelocity.x, 0, 0), Color.red);
            //Debug.DrawLine(p, p + new Vector3(0, rigidbody.angularVelocity.y, 0), Color.green);
            //Debug.DrawLine(p, p + new Vector3(0, 0, rigidbody.angularVelocity.z), Color.blue);
        }
    }
}
