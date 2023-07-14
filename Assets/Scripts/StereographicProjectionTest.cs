using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

public class StereographicProjectionTest : MonoBehaviour
{
    [SerializeField] Transform sphere;
    [SerializeField, Range(-0.5f,0.5f)] float x;
    [SerializeField, Range(-0.5f,0.5f)] float y;
    [SerializeField] GameObject pointPrefab;

    float radius;

    GameObject originPoint;
    GameObject projectedPoint;
    GameObject planePoint;

    // Start is called before the first frame update
    void Start()
    {
        radius = sphere.localScale.x/2;
        originPoint = Instantiate(pointPrefab, sphere.position, Quaternion.identity);
        projectedPoint = Instantiate(pointPrefab, sphere.forward * radius, Quaternion.identity);
        planePoint = Instantiate(pointPrefab, sphere.forward * radius, Quaternion.identity);
        
        //Debug.DrawLine(originPoint.transform.position, projectedPoint.transform.position, Color.white, 15f);
        //Debug.Log(Vector3.Normalize(projectedPoint.transform.position - originPoint.transform.position));

        //Debug.DrawRay(sphere.position, projectedPoint.transform.position - originPoint.transform.position, Color.green);
        
        /*float x = (temp.x / Sqrt(Pow(temp.x, 2) + Pow(temp.y, 2))) * radius;
        float y = (temp.y / Sqrt(Pow(temp.x, 2) + Pow(temp.y, 2))) * radius;
        float z = Sqrt(Pow(radius, 2) - Pow(x, 2) - Pow(y, 2));
        projectedPoint = Instantiate(pointPrefab, new Vector3(x,y,z), Quaternion.identity);*/
    }

    void Update()
    {
        Vector3 pointOnPlane = new Vector3(x,-radius,y);
        Vector3 diff = pointOnPlane - originPoint.transform.position;
        Vector3 dir = Vector3.Normalize(diff);

        Debug.DrawRay(sphere.position, dir * radius, Color.green);
        Debug.Log(Vector3.Distance(originPoint.transform.position, pointOnPlane));
        Debug.Log(Sqrt(Pow(diff.x,2) + Pow(diff.y,2) + Pow(diff.z,2)));

        projectedPoint.transform.position = dir * radius;
        planePoint.transform.position = dir * Vector3.Distance(originPoint.transform.position, pointOnPlane);
    }

    void OnDisable()
    {
        if(originPoint) {
            DestroyImmediate(originPoint);
        }

        if(projectedPoint) {
            DestroyImmediate(projectedPoint);
        }
    }
}
