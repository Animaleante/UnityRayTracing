using System.Diagnostics;
using System;
using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteAlways]
public class RandomInCircleTest : MonoBehaviour
{
    const float DOUBLE_PI = 2 * PI;

    private enum Modes {
        Both,
        Standard,
        Stratified
    }

    [SerializeField] Modes mode;
    [SerializeField, Range(1,100)] int samplesCount;

    // Update is called once per frame
    void Update()
    {
		DebugExtension.DebugBounds(new Bounds(new Vector3(0, 0, 0), new Vector3(2, 2, 0.01f)), Color.white, 1.0f);
		DebugExtension.DebugCircle(new Vector3(0, 0, 0), Vector3.forward, Color.white, 1.0f);

        uint rngState = 1;
        Color green = new(0,1,0,.5f);
        Color blue = new(0,0,1,.5f);

        int insideCircle = 0;
        int insideCircleStratified = 0;
        for (int i = 0; i < samplesCount; i++) {
            for (int j = 0; j < samplesCount; j++) {
                float x = Random.RandomValue(ref rngState, -1.0f, 1.0f);
                float y = Random.RandomValue(ref rngState, -1.0f, 1.0f);

                if(x*x + y*y < 1) {
                    insideCircle++;
                }

                if (mode == Modes.Both || mode == Modes.Standard) {
                    DebugExtension.DebugWireSphere(new Vector3(x, y, 0), green, .01f);
                }

                x = 2 * ((i + Random.RandomValue(ref rngState)) / samplesCount) - 1;
                y = 2 * ((j + Random.RandomValue(ref rngState)) / samplesCount) - 1;

                if(x*x + y*y < 1) {
                    insideCircleStratified++;
                }

                if (mode == Modes.Both || mode == Modes.Stratified) {
                    DebugExtension.DebugWireSphere(new Vector3(x, y, 0), blue, .01f);
                }
            }
        }

        UnityEngine.Debug.Log("Inside Circle = " + insideCircle);
        UnityEngine.Debug.Log("Regular Estimate of Pi = " + (4.0 * insideCircle) / (samplesCount * samplesCount));
        UnityEngine.Debug.Log("Inside Circle Stratified = " + insideCircleStratified);
        UnityEngine.Debug.Log("Stratified Estimate of Pi = " + (4.0 * insideCircleStratified) / (samplesCount * samplesCount));
    }
}
