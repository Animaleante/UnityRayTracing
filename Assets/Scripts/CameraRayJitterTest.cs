using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteAlways]
public class CameraRayJitterTest : MonoBehaviour
{
	[Header("Ray Tracing Settings")]
    [SerializeField, Range(0, 64)] int numRaysPerPixel = 10;
	[SerializeField, Min(0)] float divergeStrength = 0.3f;
	[SerializeField, Min(0)] float defocusStrength = 0;
	[SerializeField, Min(0)] float focusDistance = 1;

    float planeHeight;
    float planeWidth;
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        planeHeight = focusDistance * Tan(cam.fieldOfView * 0.5f * Deg2Rad) * 2;
        planeWidth = planeHeight * cam.aspect;
    }

    void Update()
    {
        /*
        float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
        float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1));

        Ray ray;
        ray.origin = _WorldSpaceCameraPos;
        ray.dir = normalize(viewPoint - ray.origin);
        */

        /*Vector3 viewParams = new Vector3(1.15f,1.15f,1.0f);
        Matrix4x4 m = cam.transform.localToWorldMatrix;

        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                Vector2 uv = new Vector2(x,y);


                // Vector3 viewPointLocal = new Vector3(uv.x - 0.5f, uv.y - 0.5f) * viewParams;
                Vector3 viewPointLocal = new Vector3(uv.x - 0.5f, uv.y - 0.5f);
                Vector3 viewPoint = m.MultiplyPoint3x4(viewPointLocal);

		        // DebugExtension.DebugArrow(cam.transform.position, cam.transform.forward, Color.white);
		        DebugExtension.DebugArrow(cam.transform.position, (viewPoint - cam.transform.position).normalized, Color.white);
            }
        }*/

        /*
        const int image_width = 400;
        const int image_height = static_cast<int>(image_width / aspect_ratio);
        
        // Camera

        auto viewport_height = 2.0;
        auto viewport_width = aspect_ratio * viewport_height;
        auto focal_length = 1.0;

        auto origin = point3(0, 0, 0);
        auto horizontal = vec3(viewport_width, 0, 0);
        auto vertical = vec3(0, viewport_height, 0);
        auto lower_left_corner = origin - horizontal/2 - vertical/2 - vec3(0, 0, focal_length);

        // Render

        for (int j = image_height-1; j >= 0; --j) {
            std::cerr << "\rScanlines remaining: " << j << ' ' << std::flush;
            for (int i = 0; i < image_width; ++i) {
                auto u = double(i) / (image_width-1);
                auto v = double(j) / (image_height-1);
                ray r(origin, lower_left_corner + u*horizontal + v*vertical - origin);
                color pixel_color = ray_color(r);
                write_color(std::cout, pixel_color);
            }
        }*/

        int imageWidth = 5;
        int imageHeight = (int)(imageWidth / cam.aspect);

        float viewportHeight = 1.0f;
        float viewportWidth = cam.aspect * viewportHeight;
        float focalLength = 1.0f;

        Vector3 origin = cam.transform.position;
        // Vector3 horizontal = new Vector3(viewportWidth, 0, 0);
        Vector3 horizontal = cam.transform.right * viewportWidth;
        // Vector3 vertical = new Vector3(0, viewportHeight, 0);
        Vector3 vertical = cam.transform.up * viewportHeight;
        Vector3 lowerLeftCorner = origin - horizontal/2 - vertical/2 - (cam.transform.forward * -focalLength);

        for (int y = imageHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                float u = x / (float)(imageWidth-1);
                float v = y / (float)(imageHeight-1);
		        DebugExtension.DebugArrow(origin, lowerLeftCorner + u * horizontal + v * vertical - origin, Color.white);
            }
        }
    }
}
