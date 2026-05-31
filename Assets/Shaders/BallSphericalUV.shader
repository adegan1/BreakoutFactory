Shader "Custom/BallSphericalUV"
{
    // Spherical UV (longitude-latitude) shader for ball meshes.
    // UVs are recalculated per-fragment from the object-space normal,
    // so the texture maps cleanly regardless of the mesh's baked UV layout.
    // Supports _BaseColor and _BaseMap_ST (used by BallController's MaterialPropertyBlock
    // for colour tinting and sprite-sheet texture animation).

    Properties
    {
        _BaseMap   ("Texture",  2D)    = "white" {}
        _BaseColor ("Color",    Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

        // ─────────────────────────────────────────────────────────────────────
        // Forward Lit pass
        // ─────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalOS   : TEXCOORD0; // passed to frag for per-pixel UV calc
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            // Longitude-latitude projection from an object-space unit normal.
            //   U  =  0 (left)  →  1 (right)  around the equator
            //   V  =  0 (south pole)  →  1 (north pole)
            float2 SphericalUV(float3 n)
            {
                float u = atan2(n.z, n.x) * (0.5 / PI) + 0.5;
                float v = asin(clamp(n.y, -1.0, 1.0)) * (1.0 / PI) + 0.5;
                return float2(u, v);
            }

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                o.normalOS   = normalize(input.normalOS);
                o.fogFactor  = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Recalculate UV per-fragment from the interpolated object-space normal.
                // Doing this in the fragment shader lets us use ddx to detect and fix
                // the atan2 seam where U wraps from 1 back to 0.
                float3 n  = normalize(input.normalOS);
                float2 uv = SphericalUV(n);

                // Seam fix: if adjacent pixels differ by > 0.5 in U, we are straddling
                // the wrap discontinuity — shift this pixel to the correct side.
                float du = ddx(uv.x);
                if (abs(du) > 0.5)
                    uv.x += (du > 0.0 ? 1.0 : -1.0);

                // Apply _BaseMap_ST (tiling = .xy, offset = .zw).
                // BallController drives this for sprite-sheet frame animation.
                uv = uv * _BaseMap_ST.xy + _BaseMap_ST.zw;

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;

                // Flat shading: derive a constant face normal per triangle from the
                // screen-space derivatives of world position. This makes each polygon
                // face uniformly lit, giving the low-poly / faceted look.
                float3 faceNormalWS = normalize(cross(ddx(input.positionWS), ddy(input.positionWS)));
                // Align with the interpolated vertex normal to guarantee outward facing.
                faceNormalWS *= sign(dot(faceNormalWS, normalize(input.normalWS)));

                // Main directional light.
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                #else
                    Light mainLight = GetMainLight();
                #endif

                float  NdotL    = saturate(dot(faceNormalWS, mainLight.direction));
                float3 radiance = mainLight.color * mainLight.shadowAttenuation * NdotL;

                // Additional lights (second directional, point lights, spot lights).
                #if defined(_ADDITIONAL_LIGHTS)
                    uint additionalCount = GetAdditionalLightsCount();
                    for (uint i = 0u; i < additionalCount; ++i)
                    {
                        Light addLight  = GetAdditionalLight(i, input.positionWS);
                        float addNdotL  = saturate(dot(faceNormalWS, addLight.direction));
                        radiance       += addLight.color
                                       * addLight.distanceAttenuation
                                       * addLight.shadowAttenuation
                                       * addNdotL;
                    }
                #endif

                radiance  += unity_AmbientSky.rgb * 0.3;
                color.rgb *= radiance;

                color.rgb = MixFog(color.rgb, input.fogFactor);
                // Alpha comes from texture * _BaseColor alpha — drives transparency.
                return color;
            }
            ENDHLSL
        }

        // Shadow casting is intentionally omitted for transparent objects.
        // Transparent meshes typically should not cast opaque shadows.
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
