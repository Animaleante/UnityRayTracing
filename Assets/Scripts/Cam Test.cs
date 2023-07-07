using UnityEngine;
using System.Collections.Generic;

public class CamTest : MonoBehaviour
{
    [SerializeField] Vector2 debugPointCount;
    [SerializeField] float debugRadius;
    [SerializeField] Color pointCol;

    List<Vector3> debugPoints = new List<Vector3>();

    void Update()
    {
        CameraRayTest();
    }
    
    void CameraRayTest()
    {
        Camera cam = Camera.main;
        Transform camT = cam.transform;

        float planeHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2;
        float planeWidth = planeHeight * cam.aspect;

        Vector3 bottomLeftLocal = new Vector3(-planeWidth / 2, -planeHeight / 2, cam.nearClipPlane);


        debugPoints.Clear();
        for (int x = 0; x < debugPointCount.x; x++)
        {
            for (int y = 0; y < debugPointCount.y; y++)
            {
                float tx = x / (debugPointCount.x - 1f);
                float ty = y / (debugPointCount.y - 1f);

                Vector3 pointLocal = bottomLeftLocal + new Vector3(planeWidth * tx, planeHeight * ty);
                Vector3 point = camT.position + camT.right * pointLocal.x + camT.up * pointLocal.y + camT.forward * pointLocal.z;

                DrawPoint(point);
            }
        }
    }

    void DrawPoint(Vector3 point)
    {
        debugPoints.Add(point);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = pointCol;
        for (int i = 0; i < debugPoints.Count; i++)
        {
            Gizmos.DrawSphere(debugPoints[i], debugRadius);
            Gizmos.DrawLine(Camera.main.transform.position, debugPoints[i]);
        }
    }
}
