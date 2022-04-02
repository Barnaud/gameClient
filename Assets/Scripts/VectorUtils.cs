using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectorUtils 
{

    public static Vector2 toVec2(Vector3 vec3)
    {
        return new Vector2(vec3.x, vec3.z);
    }

    public static Vector3 toVec3(Vector2 vec2, float yPosition = 0)
    {
        return new Vector3(vec2.x, yPosition, vec2.y);
    }
}
