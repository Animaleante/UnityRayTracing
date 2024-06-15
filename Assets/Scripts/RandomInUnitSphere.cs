using System;
using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteAlways]
public class RandomInUnitSphere : MonoBehaviour
{
    const float DOUBLE_PI = 2 * PI;

    [SerializeField, Range(1,10000)] int samplesCount;

    // Update is called once per frame
    void Update()
    {
		// DebugExtension.DebugWireSphere(new Vector3(0, 0, 0), Color.white, 1.0f);

        uint rngState = 1;
        Color green = new Color(0,1,0,.5f);

        for (int i = 0; i < samplesCount; i++)
        {
            Vector3 p = GetPointInSphere(ref rngState);
            // DebugExtension.DebugPoint(new Vector3(p.x, p.y, 0), green, 0.05f);
            DebugExtension.DebugWireSphere(p, Color.green, .01f);
        }
    }

    Vector3 GetPointInSphere(ref uint rngState)
    {
        while(true) {
            Vector3 pointInSphere = new Vector3(
                Random.RandomValue(ref rngState, -1.0f, 1.0f),
                Random.RandomValue(ref rngState, -1.0f, 1.0f),
                Random.RandomValue(ref rngState, -1.0f, 1.0f)
            );
            if (Vector3.Dot(pointInSphere, pointInSphere) < 1) return pointInSphere;
        }
    }
}
