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
