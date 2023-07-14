using UnityEngine;

[System.Serializable]
public struct RayTracingMaterial
{
    public enum MaterialFlag
    {
        None,
        CheckerPattern,
        InvisibleLight
    }

    public Color color;
    public Color emissionColor;
    public Color specularColor;
    public float emissionStrength;
    [Range(0, 1)] public float smoothness;
	[Range(0, 1)] public float specularProbability;
    public MaterialFlag flag;

    public void SetDefaultValues()
    {
        color = Color.white;
        emissionColor = Color.white;
        specularColor = Color.white;
        emissionStrength = 0;
        smoothness = 0;
        //flag = MaterialFlag.None;
    }
}
