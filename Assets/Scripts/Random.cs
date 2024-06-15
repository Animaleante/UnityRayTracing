using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Random
{
    public static float RandomValue(ref uint state)
    {
        // Debug.Log("state: " + state);
        state = state * 747796405 + 2891336453;
        uint result = ((state >> (int)((state >> (int)28) + 4)) ^ state) * 277803737;
        result = (result >> 22) ^ result;
        // Debug.Log("Result: " + (result / 4294967295.0f));
        return result / 4294967295.0f;
    }

    public static float RandomValue(ref uint state, float min, float max)
    {
        return min + (max - min) * RandomValue(ref state);
    }
}
