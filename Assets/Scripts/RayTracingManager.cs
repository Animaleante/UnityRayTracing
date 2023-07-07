using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RayTracingManager : MonoBehaviour
{
	// Raytracer is currently *very* slow, so limit the number of triangles allowed per mesh
	public const int TriangleLimit = 1500;

	[Header("View Settings")]
    [SerializeField] bool useShaderInSceneView;

	[Header("Ray Tracing Settings")]
    [SerializeField, Range(0, 32)] int maxBounceCount = 4;
    [SerializeField, Range(0, 64)] int numRaysPerPixel = 10;
	[SerializeField] EnvironmentSettings environmentSettings;

	[Header("References")]
    [SerializeField] Shader rayTracingShader;
	[SerializeField] Shader accumulateShader;

	[Header("Info")]
	[SerializeField] int numRenderedFrames;
	[SerializeField] int numMeshChunks;
	[SerializeField] int numTriangles;

    Material rayTracingMaterial;
	Material accumulateMaterial;
	RenderTexture resultTexture;

	// Buffers
	ComputeBuffer sphereBuffer;
	ComputeBuffer triangleBuffer;
	ComputeBuffer meshInfoBuffer;

	List<Triangle> allTriangles;
	List<MeshInfo> allMeshInfo;

    void Start()
    {
        numRenderedFrames = 0;
    }
    
    void OnRenderImage(RenderTexture src, RenderTexture target)
    {
		bool isSceneCam = Camera.current.name == "SceneCamera";

		if (isSceneCam) {
			if (useShaderInSceneView) {
                ShaderHelper.InitMaterial(rayTracingShader, ref rayTracingMaterial);
                UpdateCameraParams(Camera.current);
                CreateSpheres();
		        CreateMeshes();
                SetShaderParams();
                Graphics.Blit(null, target, rayTracingMaterial);
			} else {
				Graphics.Blit(src, target); // Draw the unaltered camera render to the screen
			}
		} else {
		    // Create materials used in blits
            ShaderHelper.InitMaterial(rayTracingShader, ref rayTracingMaterial);
            ShaderHelper.InitMaterial(accumulateShader, ref accumulateMaterial);
            // Create result render texture
            ShaderHelper.CreateRenderTexture(ref resultTexture, Screen.width, Screen.height, FilterMode.Bilinear, ShaderHelper.RGBA_SFloat, "Result");

            // Update data
            UpdateCameraParams(Camera.current);
            CreateSpheres();
		    CreateMeshes();
            SetShaderParams();

			// Create copy of prev frame
			RenderTexture prevFrameCopy = RenderTexture.GetTemporary(src.width, src.height, 0, ShaderHelper.RGBA_SFloat);
			Graphics.Blit(resultTexture, prevFrameCopy);

			// Run the ray tracing shader and draw the result to a temp texture
			RenderTexture currentFrame = RenderTexture.GetTemporary(src.width, src.height, 0, ShaderHelper.RGBA_SFloat);
			Graphics.Blit(null, currentFrame, rayTracingMaterial);

			// Accumulate
			accumulateMaterial.SetInt("Frame", numRenderedFrames);
			accumulateMaterial.SetTexture("_PrevFrame", prevFrameCopy);
			Graphics.Blit(currentFrame, resultTexture, accumulateMaterial);

			// Draw result to screen
			Graphics.Blit(resultTexture, target);

			// Release temps
			RenderTexture.ReleaseTemporary(currentFrame);
			RenderTexture.ReleaseTemporary(prevFrameCopy);
			RenderTexture.ReleaseTemporary(currentFrame);

			numRenderedFrames += Application.isPlaying ? 1 : 0;
        }
    }

    void UpdateCameraParams(Camera cam)
    {
        float planeHeight = cam.nearClipPlane * Tan(cam.fieldOfView * 0.5f * Deg2Rad) * 2;
        float planeWidth = planeHeight * cam.aspect;

        rayTracingMaterial.SetVector("ViewParams", new Vector3(planeWidth, planeHeight, cam.nearClipPlane));
        rayTracingMaterial.SetMatrix("CamLocalToWorldMatrix", cam.transform.localToWorldMatrix);
    }

    void CreateSpheres()
	{
		// Create sphere data from the sphere objects in the scene
		RayTracedSphere[] sphereObjects = FindObjectsOfType<RayTracedSphere>();
		Sphere[] spheres = new Sphere[sphereObjects.Length];

		for (int i = 0; i < sphereObjects.Length; i++)
		{
			spheres[i] = new Sphere()
			{
				position = sphereObjects[i].transform.position,
				radius = sphereObjects[i].transform.localScale.x * 0.5f,
				material = sphereObjects[i].material
			};
		}

		// Create buffer containing all sphere data, and send it to the shader
		ShaderHelper.CreateStructuredBuffer(ref sphereBuffer, spheres);
		rayTracingMaterial.SetBuffer("Spheres", sphereBuffer);
		rayTracingMaterial.SetInt("NumSpheres", sphereObjects.Length);
	}

    void CreateMeshes()
    {
		allTriangles ??= new List<Triangle>();
		allMeshInfo ??= new List<MeshInfo>();
		allTriangles.Clear();
		allMeshInfo.Clear();

		RayTracedMesh[] meshObjects = FindObjectsOfType<RayTracedMesh>();

		for (int i = 0; i < meshObjects.Length; i++)
		{
			MeshChunk[] chunks = meshObjects[i].GetSubMeshes();
			foreach (MeshChunk chunk in chunks)
			{
				RayTracingMaterial material = meshObjects[i].GetMaterial(chunk.subMeshIndex);
				allMeshInfo.Add(new MeshInfo(allTriangles.Count, chunk.triangles.Length, material, chunk.bounds));
				allTriangles.AddRange(chunk.triangles);

			}
		}

		numMeshChunks = allMeshInfo.Count;
		numTriangles = allTriangles.Count;

		ShaderHelper.CreateStructuredBuffer(ref triangleBuffer, allTriangles);
		ShaderHelper.CreateStructuredBuffer(ref meshInfoBuffer, allMeshInfo);
		rayTracingMaterial.SetBuffer("Triangles", triangleBuffer);
		rayTracingMaterial.SetBuffer("AllMeshInfo", meshInfoBuffer);
		rayTracingMaterial.SetInt("NumMeshes", allMeshInfo.Count);
    }

    void SetShaderParams()
    {
		rayTracingMaterial.SetInt("MaxBounceCount", maxBounceCount);
        rayTracingMaterial.SetInt("NumRaysPerPixel", numRaysPerPixel);
		rayTracingMaterial.SetInt("Frame", numRenderedFrames);

		rayTracingMaterial.SetInteger("EnvironmentEnabled", environmentSettings.enabled ? 1 : 0);
		rayTracingMaterial.SetColor("GroundColor", environmentSettings.groundColor);
		rayTracingMaterial.SetColor("SkyColorHorizon", environmentSettings.skyColorHorizon);
		rayTracingMaterial.SetColor("SkyColorZenith", environmentSettings.skyColorZenith);
		rayTracingMaterial.SetFloat("SunFocus", environmentSettings.sunFocus);
		rayTracingMaterial.SetFloat("SunIntensity", environmentSettings.sunIntensity);
    }


	void OnDisable()
	{
		ShaderHelper.Release(sphereBuffer, triangleBuffer, meshInfoBuffer);
		ShaderHelper.Release(resultTexture);
	}

	void OnValidate()
	{
		maxBounceCount = Mathf.Max(0, maxBounceCount);
		numRaysPerPixel = Mathf.Max(1, numRaysPerPixel);
		environmentSettings.sunFocus = Mathf.Max(1, environmentSettings.sunFocus);
		environmentSettings.sunIntensity = Mathf.Max(0, environmentSettings.sunIntensity);

	}
}
