using System;
using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteAlways]
public class RandomInCircle : MonoBehaviour
{
    const float DOUBLE_PI = 2 * PI;

    [SerializeField, Range(1,10000)] int samplesCount;
    [SerializeField, Range(0,1.0f)] float lerpSqrt;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		DebugExtension.DebugCircle(new Vector3(0, 0, 0), Vector3.forward, Color.white, 1.0f);

        uint rngState = 1;
        Color green = new Color(0,1,0,.5f);

        for (int i = 0; i < samplesCount; i++)
        {
            Vector2 p = GetPointInCircle(ref rngState);
            DebugExtension.DebugPoint(new Vector3(p.x, p.y, 0), green, 0.05f);
        }
    }

    Vector2 GetPointInCircle(ref uint rngState)
    {
        float angle = Random.RandomValue(ref rngState) * DOUBLE_PI;
        Vector2 pointOnCircle = new Vector2(Cos(angle), Sin(angle));
        float r = Random.RandomValue(ref rngState);
        return pointOnCircle * Lerp(r, Sqrt(r), lerpSqrt);
    }
}
