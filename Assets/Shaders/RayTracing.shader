Shader "Custom/RayTracing"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // --- Settings and constants ---

            static const float DOUBLE_PI = 2 * 3.1415;

            // Raytracing Settings
            int MaxBounceCount;
            int NumRaysPerPixel;
            int Frame;

            // Camera Settings
            float3 ViewParams;
            float4x4 CamLocalToWorldMatrix;
            float DivergeStrength;
            float DefocusStrength;
            int ShowFocusPlane;

			// Environment Settings
			int EnvironmentEnabled;
			float4 GroundColor;
			float4 SkyColorHorizon;
			float4 SkyColorZenith;
			float SunFocus;
			float SunIntensity;

            static const int CheckerPattern = 1;
            static const int InvisibleLightSource = 2;

            // --- Structures ---
            struct Ray
            {
                float3 origin;
                float3 dir;
            };

            struct RayTracingMaterial
            {
                float4 color;
                float4 emissionColor;
                float4 specularColor;
                float emissionStrength;
                float smoothness;
                float specularProbability;
                int flag;

            };

            struct Sphere
            {
                float3 position;
                float radius;
                RayTracingMaterial material;
            };

			struct Triangle
			{
				float3 posA, posB, posC;
				float3 normalA, normalB, normalC;
			};

			struct MeshInfo
			{
				uint firstTriangleIndex;
				uint numTriangles;
				RayTracingMaterial material;
				float3 boundsMin;
				float3 boundsMax;
			};

            struct HitInfo
            {
                bool didHit;
                float distance;
                float3 hitPoint;
                float3 normal;
                RayTracingMaterial material;
            };

            // --- Buffers ---	
            StructuredBuffer<Sphere> Spheres;
            int NumSpheres;

			StructuredBuffer<Triangle> Triangles;
			StructuredBuffer<MeshInfo> AllMeshInfo;
			int NumMeshes;

            // --- Ray Intersection Functions ---
        
            // Calculate the intersection of a ray with a sphere
            HitInfo RaySphere(Ray ray, float3 sphereCenter, float sphereRadius)
            {
                HitInfo hitInfo = (HitInfo)0;
                float3 offsetRayOrigin = ray.origin - sphereCenter;

                float a = dot(ray.dir, ray.dir);
                float b = 2 * dot(offsetRayOrigin, ray.dir);
                float c = dot(offsetRayOrigin, offsetRayOrigin) - sphereRadius * sphereRadius;

                float discriminant = b * b - 4 * a * c;

                if(discriminant >= 0) {
                    float distance = (-b - sqrt(discriminant)) / (2 * a);

                    if (distance >= 0) {
                        hitInfo.didHit = true;
                        hitInfo.distance = distance;
                        hitInfo.hitPoint = ray.origin + ray.dir * distance;
                        hitInfo.normal = normalize(hitInfo.hitPoint - sphereCenter);
                    }
                }

                return hitInfo;
            }

			// Calculate the intersection of a ray with a triangle using Möller–Trumbore algorithm
			// Thanks to https://stackoverflow.com/a/42752998
			HitInfo RayTriangle(Ray ray, Triangle tri)
			{
				float3 edgeAB = tri.posB - tri.posA;
				float3 edgeAC = tri.posC - tri.posA;
				float3 normalVector = cross(edgeAB, edgeAC);
				float3 ao = ray.origin - tri.posA;
				float3 dao = cross(ao, ray.dir);

				float determinant = -dot(ray.dir, normalVector);
				float invDet = 1 / determinant;
				
				// Calculate dst to triangle & barycentric coordinates of intersection point
				float dst = dot(ao, normalVector) * invDet;
				float u = dot(edgeAC, dao) * invDet;
				float v = -dot(edgeAB, dao) * invDet;
				float w = 1 - u - v;
				
				// Initialize hit info
				HitInfo hitInfo;
				hitInfo.didHit = determinant >= 1E-6 && dst >= 0 && u >= 0 && v >= 0 && w >= 0;
				hitInfo.hitPoint = ray.origin + ray.dir * dst;
				hitInfo.normal = normalize(tri.normalA * w + tri.normalB * u + tri.normalC * v);
				hitInfo.distance = dst;
				return hitInfo;
			}

			// Thanks to https://gist.github.com/DomNomNom/46bb1ce47f68d255fd5d
			bool RayBoundingBox(Ray ray, float3 boxMin, float3 boxMax)
			{
				float3 invDir = 1 / ray.dir;
				float3 tMin = (boxMin - ray.origin) * invDir;
				float3 tMax = (boxMax - ray.origin) * invDir;
				float3 t1 = min(tMin, tMax);
				float3 t2 = max(tMin, tMax);
				float tNear = max(max(t1.x, t1.y), t1.z);
				float tFar = min(min(t2.x, t2.y), t2.z);
				return tNear <= tFar;
			};

            // --- RNG Stuff ---

            float RandomValue(inout uint state)
            {
                //state *= 9694;
                //state *= state;
                
                //state *= (state + 195439) * (state + 124395) * (state + 845921);

                state = state * 747796405 + 2891336453;
                uint result = ((state >> ((state >> 28) + 4)) ^ state) * 277803737;
                result = (result >> 22) ^ result;
                return result / 4294967295.0;
            }

            float RandomValueNormalDistribution(inout uint state)
            {
                float theta = 2 * 3.1415926 * RandomValue(state);
                float rho = sqrt(-2 * log(RandomValue(state)));
                return rho * cos(theta);
            }

            float3 RandomDirection(inout uint state)
            {
                    float x = RandomValueNormalDistribution(state);
                    float y = RandomValueNormalDistribution(state);
                    float z = RandomValueNormalDistribution(state);
                    return normalize(float3(x,y,z));
            }

            float3 RandomHemisphereDirection(float3 normal, inout uint rngState)
            {
                float3 dir = RandomDirection(rngState);
                return dir * sign(dot(normal, dir));
            }

            float2 RandomPointInCircle(inout uint rngState)
            {
                float angle = RandomValue(rngState) * DOUBLE_PI;
                float2 pointOnCircle = float2(cos(angle), sin(angle));
                return pointOnCircle * sqrt(RandomValue(rngState));
            }

            float2 mod2(float2 x, float2 y)
            {
                return x - y * floor(x/y);
            }

            float3 GetEnvironmentLight(Ray ray)
            {
                if(!EnvironmentEnabled) {
                    return 0;
                }

				float skyGradientT = pow(smoothstep(0, 0.4, ray.dir.y), 0.35);
				float groundToSkyT = smoothstep(-0.01, 0, ray.dir.y);
				float3 skyGradient = lerp(SkyColorHorizon, SkyColorZenith, skyGradientT);
				float sun = pow(max(0, dot(ray.dir, _WorldSpaceLightPos0.xyz)), SunFocus) * SunIntensity;
				// Combine ground, sky, and sun
                float sunMask = groundToSkyT >= 1;
				return lerp(GroundColor, skyGradient, groundToSkyT) + sun * sunMask;
            }

            // --- Ray Tracing Stuff ---

            // Find the first point that the given ray collides with, and return hit info
            HitInfo CalculateRayCollision(Ray ray)
            {
                HitInfo closestHit = (HitInfo)0;
                closestHit.distance = 1.#INF;

                for (int i = 0; i < NumSpheres; i++)
                {
                    Sphere sphere = Spheres[i];
                    HitInfo hitInfo = RaySphere(ray, sphere.position, sphere.radius);

                    if(hitInfo.didHit && hitInfo.distance < closestHit.distance)
                    {
                        closestHit = hitInfo;
                        closestHit.material = sphere.material;
                    }
                }

				// Raycast against all meshes and keep info about the closest hit
				for (int meshIndex = 0; meshIndex < NumMeshes; meshIndex ++)
				{
					MeshInfo meshInfo = AllMeshInfo[meshIndex];
					if (!RayBoundingBox(ray, meshInfo.boundsMin, meshInfo.boundsMax)) {
						continue;
					}

					for (uint i = 0; i < meshInfo.numTriangles; i ++) {
						int triIndex = meshInfo.firstTriangleIndex + i;
						Triangle tri = Triangles[triIndex];
						HitInfo hitInfo = RayTriangle(ray, tri);
	
						if (hitInfo.didHit && hitInfo.distance < closestHit.distance)
						{
							closestHit = hitInfo;
							closestHit.material = meshInfo.material;
						}
					}
				}

                return closestHit;
            }

            float3 Trace(Ray ray, inout uint rngState)
            {
                float3 incomingLight = 0;
                float3 rayColor = 1;

                for(int bounceIndex = 0; bounceIndex <= MaxBounceCount; bounceIndex++)
                {
                    HitInfo hitInfo = CalculateRayCollision(ray);

                    if(hitInfo.didHit)
                    {
                        RayTracingMaterial material = hitInfo.material;

                        float3 originalOrigin = ray.origin;
                        float3 originalDir = ray.dir;

                        if (material.flag == CheckerPattern)
                        {
                            float2 c = mod2(floor(hitInfo.hitPoint.xz), 2.0);
                            material.color = c.x == c.y ? material.color : material.emissionColor;
                        }
                        else if (material.flag == InvisibleLightSource && bounceIndex == 0)
						{
							ray.origin = hitInfo.hitPoint + ray.dir * 0.001;
                            if (ShowFocusPlane && bounceIndex == 0) {
                                rayColor += float4(1,0,0,0.5);
                            }
							continue;
						}

                        ray.origin = hitInfo.hitPoint;
                        //ray.dir = hitInfo.normal;
                        //ray.dir = RandomHemisphereDirection(hitInfo.normal, rngState);
                        //ray.dir = normalize(hitInfo.normal + RandomDirection(rngState));
                        float3 diffuseDir = normalize(hitInfo.normal + RandomDirection(rngState));
                        float3 specularDir = reflect(ray.dir, hitInfo.normal);
                        bool isSpecularBounce = material.specularProbability >= RandomValue(rngState);
                        // ray.dir = lerp(diffuseDir, specularDir, material.smoothness * isSpecularBounce);
                        ray.dir = normalize(lerp(diffuseDir, specularDir, material.smoothness * isSpecularBounce));

                        float3 emittedLight = material.emissionColor * material.emissionStrength;
                        //float lightStrength = dot(hitInfo.normal, ray.dir);
                        incomingLight += emittedLight * rayColor;
                        // rayColor *= material.color;
                        //rayColor *= material.color * lightStrength * 2;
                        rayColor *= lerp(material.color, material.specularColor, isSpecularBounce);

                        if (ShowFocusPlane && bounceIndex == 0) {
                            if (hitInfo.distance > ViewParams.z) {
                                rayColor += float4(1,0,0,0.5);
                            }
                        }
                    } else {
                        if (ShowFocusPlane && bounceIndex == 0) {
                            rayColor += float4(1,0,0,0.5);
                        }
                        incomingLight += GetEnvironmentLight(ray) * rayColor;
                        break;
                    }
                }

                return incomingLight;
            }

            float4 renderSingleRayPerPixel(v2f i)
            {
                uint2 numPixels = _ScreenParams.xy;
                uint2 pixelCoord = i.uv * numPixels;
                uint pixelIndex = pixelCoord.y * numPixels.x + pixelCoord.x; 
                uint rngState = pixelIndex + Frame * 719393;

                float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
                float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1));

                Ray ray;
                ray.origin = _WorldSpaceCameraPos;
                ray.dir = normalize(viewPoint - ray.origin);

                float3 pixelCol = Trace(ray, rngState);
                return float4(pixelCol, 1);
            }

            float4 renderMultipleRaysPerPixel(v2f i)
            {
                uint2 numPixels = _ScreenParams.xy;
                uint2 pixelCoord = i.uv * numPixels;
                uint pixelIndex = pixelCoord.y * numPixels.x + pixelCoord.x; 
                uint rngState = pixelIndex + Frame * 719393;

                float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
                float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1));

                Ray ray;
                ray.origin = _WorldSpaceCameraPos;
                ray.dir = normalize(viewPoint - ray.origin);

                float3 totalIncomingLight = 0;

                for (int rayIndex = 0; rayIndex < NumRaysPerPixel; rayIndex++)
                {
                    totalIncomingLight += Trace(ray, rngState);
                }

                float3 pixelCol = totalIncomingLight / NumRaysPerPixel;
                return float4(pixelCol, 1);
            }

            float4 renderMultipleRaysPerPixelWithJittering(v2f i)
            {
                uint2 numPixels = _ScreenParams.xy;
                uint2 pixelCoord = i.uv * numPixels;
                uint pixelIndex = pixelCoord.y * numPixels.x + pixelCoord.x; 
                uint rngState = pixelIndex + Frame * 719393;

                float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
                float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1));

                float3 totalIncomingLight = 0;

				float3 camRight = CamLocalToWorldMatrix._m00_m10_m20;
				float3 camUp = CamLocalToWorldMatrix._m01_m11_m21;

                Ray ray;

                for (int rayIndex = 0; rayIndex < NumRaysPerPixel; rayIndex++)
                {
                    //ray.origin = _WorldSpaceCameraPos;
                    float2 defocusJitter = RandomPointInCircle(rngState) * DefocusStrength / numPixels.x;
                    ray.origin = _WorldSpaceCameraPos + camRight * defocusJitter.x + camUp * defocusJitter.y;

                    float2 jitter = RandomPointInCircle(rngState) * DivergeStrength / numPixels.x;
					float3 jitteredFocusPoint = viewPoint + camRight * jitter.x + camUp * jitter.y;

                    ray.dir = normalize(jitteredFocusPoint - ray.origin);
                    totalIncomingLight += Trace(ray, rngState);
                }

                float3 pixelCol = totalIncomingLight / NumRaysPerPixel;
                return float4(pixelCol, 1);
            }

            // Run for every pixel in the display
            float4 frag (v2f i) : SV_Target
            {
                //float2 uv = i.uv*2-1;
                //uv.x *= _ScreenParams.x / _ScreenParams.y;
                //return length(uv);

                return renderSingleRayPerPixel(i);

                /*if(i.uv.x < 0.25) {
                    return i.uv.y;
                } else if (i.uv.x < 0.5) {
                    return smoothstep(0, 0.4, i.uv.y);
                } else if (i.uv.x < 0.75) {
                    return pow(i.uv.y, 0.35);
                } else {
                    return pow(smoothstep(0, 0.4, i.uv.y), 0.35);
                }*/
                /*float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
                float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1));

                Ray ray;
                ray.origin = _WorldSpaceCameraPos;
                ray.dir = normalize(viewPoint - ray.origin);

                //return float4(ray.dir, 0);
                //return RaySphere(ray, 0, 1).didHit;
                return CalculateRayCollision(ray).material.color;*/

                uint2 numPixels = _ScreenParams.xy;
                uint2 pixelCoord = i.uv * numPixels;
                uint pixelIndex = pixelCoord.y * numPixels.x + pixelCoord.x; 

                //return pixelIndex / (float)(numPixels.x * numPixels.y);
                //uint rngState = pixelIndex;
                //return RandomValue(rngState);
                /*float r = RandomValue(rngState);
                float g = RandomValue(rngState);
                float b = RandomValue(rngState);
                return float4(r,g,b,1);*/
                uint rngState = pixelIndex + Frame * 719393;

                float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
                float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1));

                /* Ray ray;
                ray.origin = _WorldSpaceCameraPos;
                ray.dir = normalize(viewPoint - ray.origin); */

                float3 totalIncomingLight = 0;

                /*for (int rayIndex = 0; rayIndex < NumRaysPerPixel; rayIndex++)
                {
                    totalIncomingLight += Trace(ray, rngState);
                }*/

				float3 camRight = CamLocalToWorldMatrix._m00_m10_m20;
				float3 camUp = CamLocalToWorldMatrix._m01_m11_m21;

                Ray ray;
                for (int rayIndex = 0; rayIndex < NumRaysPerPixel; rayIndex++)
                {
                    //ray.origin = _WorldSpaceCameraPos;
                    float2 defocusJitter = RandomPointInCircle(rngState) * DefocusStrength / numPixels.x;
                    ray.origin = _WorldSpaceCameraPos + camRight * defocusJitter.x + camUp * defocusJitter.y;

                    float2 jitter = RandomPointInCircle(rngState) * DivergeStrength / numPixels.x;
					float3 jitteredFocusPoint = viewPoint + camRight * jitter.x + camUp * jitter.y;

                    ray.dir = normalize(jitteredFocusPoint - ray.origin);
                    totalIncomingLight += Trace(ray, rngState);
                }

                //float3 pixelCol = Trace(ray, rngState);
                float3 pixelCol = totalIncomingLight / NumRaysPerPixel;
                return float4(pixelCol, 1);
            }
            
            ENDCG
        }
    }
}
