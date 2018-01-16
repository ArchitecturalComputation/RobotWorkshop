using UnityEngine;

public struct Orient
{
    public Vector3 Center;
    public Quaternion Rotation;

    public Orient(Transform transform)
    {
        Center = transform.position;
        Rotation = transform.rotation;
    }

    public Orient(Vector3 position, Quaternion rotation)
    {
        Center = position;
        Rotation = rotation;
    }

    public Orient(float x, float y, float z, float angle)
    {
        Center = new Vector3(x, y, z);
        Rotation = Quaternion.Euler(0, angle, 0);
    }

    public Orient Transform(Orient other)
    {
        var center = other.Rotation * Center + other.Center;
        var rotation = other.Rotation * Rotation;
        return new Orient(center, rotation);
    }

    public Orient RotateAround(Vector3 origin, float angle)
    {
        var quat = Quaternion.Euler(0, angle, 0);
        var center = origin + quat * (Center - origin);
        var rotation = quat * Rotation;
        return new Orient(center, rotation);
    }

    public float[] ToFloats()
    {
        return new[]
        {
         Center.x * 1000f,
         Center.z * 1000f,
         Center.y * 1000f,
         -Rotation.w,
         Rotation.x,
         Rotation.z,
         Rotation.y
         };
    }

    public static float GetAngle(Vector3 a, Vector3 b)
    {
        var v1 = new Vector2(a.x, a.z).normalized;
        var v2 = new Vector2(b.x, b.z).normalized;
        var angle = Mathf.Atan2(v1.x * v2.y - v1.y * v2.x, v1.x * v2.x + v1.y * v2.y) * (180f / Mathf.PI);
        return angle;
    }
}
