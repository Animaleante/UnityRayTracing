using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteAlways]
public class CameraRayJitterTest : MonoBehaviour
{
    const float DOUBLE_PI = 2 * PI;

	[Header("Debug Settings")]
	[SerializeField] bool showMiddleOneOnly = false;

	[Header("Ray Tracing Settings")]
    [SerializeField, Range(1, 64)] int numRaysPerPixel = 10;
	[SerializeField, Min(0)] float divergeStrength = 0.3f;
	[SerializeField, Min(0)] float defocusStrength = 0;
	[SerializeField, Min(0)] float focalLength = 1;
	[SerializeField, Min(0)] float focusDistance = 1;

    // float planeHeight;
    // float planeWidth;
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        float planeHeight = focusDistance * Tan(cam.fieldOfView * 0.5f * Deg2Rad) * 2;
        float planeWidth = planeHeight * cam.aspect;
        Vector3 viewParams = new Vector3(planeWidth, planeHeight, focusDistance);

        // float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
        // float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1));
        Vector3 screenPos = new Vector3(-0.5f,-0.5f,1);
        Vector3 viewLocal = Vector3.Scale(screenPos, viewParams);
        // Vector3 viewPoint = cam.transform.localToWorldMatrix * new Vector4(viewLocal, 1);
        Vector4 viewPoint = cam.transform.localToWorldMatrix * (new Vector4(viewLocal.x, viewLocal.y, viewLocal.z, 1.0f));

        Debug.Log(viewPoint);
        Vector3 rayOrigin = cam.transform.position;
        Vector3 rayDir = (new Vector3(viewPoint.x, viewPoint.y, viewPoint.z) - cam.transform.position).normalized;
        Debug.Log(rayDir);
    }

    void Update()
    {
        // int imageWidth = 5;
        int imageWidth = Screen.width;
        // int imageHeight = (int)(imageWidth / cam.aspect);
        int imageHeight = Screen.height;

        float viewportHeight = 1.0f;
        float viewportWidth = cam.aspect * viewportHeight;

        uint rngState = 1;

        Vector3 origin = cam.transform.position;
        Vector3 camRight = cam.transform.right;
        Vector3 camUp = cam.transform.up;
        Vector3 camForward = cam.transform.forward;
        // Vector3 horizontal = new Vector3(viewportWidth, 0, 0);
        Vector3 horizontal = camRight * viewportWidth;
        // Vector3 vertical = new Vector3(0, viewportHeight, 0);
        Vector3 vertical = camUp * viewportHeight;
        Vector3 lowerLeftCorner = origin - horizontal/2 - vertical/2 + (camForward * focalLength);
        // Vector3 lowerLeftCorner = origin - horizontal/2 - vertical/2 + (camForward * focusDistance);

        Color c = Color.white * new Vector4(1,1,1, Mathf.Lerp(0.5f, 1f, 1.0f / numRaysPerPixel));

        for (int y = imageHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                if (showMiddleOneOnly && (y != Floor((imageHeight - 1) / 2) || x != Floor((imageWidth - 1) / 2))) {
                    continue;
                }

                float u = x / (float)(imageWidth-1);
                float v = y / (float)(imageHeight-1);

                Vector3 viewPoint = lowerLeftCorner + u * horizontal + v * vertical;
                // Vector3 dir = viewPoint - origin;

                for (int i = 0; i < numRaysPerPixel; i++)
                {
                    Vector2 defocusJitter = GetPointInCircle(ref rngState) * defocusStrength / imageWidth;
                    Vector3 jitteredOrigin = origin + camRight * defocusJitter.x + camUp * defocusJitter.y;

                    Vector2 jitter = GetPointInCircle(ref rngState) * divergeStrength / imageWidth;
                    Vector3 jitteredViewPoint = viewPoint + camRight * jitter.x + camUp * jitter.y;
                    // Vector3 dir = jitteredViewPoint - origin;
                    Vector3 dir = (jitteredViewPoint - jitteredOrigin).normalized;

                    // DebugExtension.DebugArrow(origin, dir, c);
                    // DebugExtension.DebugArrow(jitteredOrigin, dir * focusDistance, c);
                    // Debug.DrawRay(jitteredOrigin, dir * focusDistance, c);
                    // DebugExtension.DebugCone(jitteredOrigin + dir * focusDistance, -dir.normalized * .15f, c, 15);
                    DebugExtension.DebugWireSphere((jitteredViewPoint - jitteredOrigin) * focusDistance, Color.red, .1f);
                    DrawArrow(jitteredOrigin, dir, focusDistance, c);
                }
            }
        }


		DebugExtension.DebugBounds(new Bounds(camForward * focusDistance, new Vector3(focusDistance*cam.aspect,focusDistance,.1f)), Color.white);
    }

    Vector2 GetPointInCircle(ref uint rngState)
    {
        float angle = Random.RandomValue(ref rngState) * DOUBLE_PI;
        Vector2 pointOnCircle = new Vector2(Cos(angle), Sin(angle));
        float r = Random.RandomValue(ref rngState);
        return pointOnCircle * Sqrt(Random.RandomValue(ref rngState));
    }

    void DrawArrow(Vector3 origin, Vector3 direction, float length, Color color)
    {
        Debug.DrawRay(origin, direction * length, color);
        DebugExtension.DebugCone(origin + direction * length, -direction.normalized * .15f, color, 15);
    }
}
