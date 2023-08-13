using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RayTracedBoundingBox : MonoBehaviour
{
    public GameObject[] children;
    
    public Collider GetCollider() {
        return GetComponent<Collider>();
    }
}
