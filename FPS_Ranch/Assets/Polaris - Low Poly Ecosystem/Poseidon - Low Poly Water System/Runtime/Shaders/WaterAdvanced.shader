Shader "Poseidon/Default/WaterAdvanced"
{
	Properties
	{
		[HideInInspector] _MeshNoise("Mesh Noise", Range(0.0, 1.0)) = 0

		[HideInInspector] _Color("Color", Color) = (0.0, 0.8, 1.0, 0.5)
		[HideInInspector] _Specular("Specular Color", Color) = (0.1, 0.1, 0.1, 1)
		[HideInInspector] _Smoothness("Smoothness", Range(0.0, 1.0)) = 1

		[HideInInspector] _DepthColor("Depth Color", Color) = (0.0, 0.45, 0.65, 0.85)
		[HideInInspector] _MaxDepth("Max Depth", Float) = 5

		[HideInInspector] _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
		[HideInInspector] _FoamDistance("Foam Distance", Float) = 1.2
		[HideInInspector] _FoamNoiseScaleHQ("Foam Noise Scale HQ", Float) = 3
		[HideInInspector] _FoamNoiseSpeedHQ("Foam Noise Speed HQ", Float) = 1
		[HideInInspector] _ShorelineFoamStrength("Shoreline Foam Strength", Float) = 1
		[HideInInspector] _CrestFoamStrength("Crest Foam Strength", Float) = 1
		[HideInInspector] _CrestMaxDepth("Crest Max Depth", Float) = 1

		[HideInInspector] _RippleHeight("Ripple Height", Range(0, 1)) = 0.1
		[HideInInspector] _RippleSpeed("Ripple Speed", Float) = 5
		[HideInInspector] _RippleNoiseScale("Ripple Noise Scale", Float) = 1

		[HideInInspector] _WaveDirection("Wave Direction", Vector) = (1, 0, 0, 0)
		[HideInInspector] _WaveSpeed("Wave Speed", Float) = 1
		[HideInInspector] _WaveHeight("Wave Height", Float) = 1
		[HideInInspector] _WaveLength("Wave Length", Float) = 1
		[HideInInspector] _WaveSteepness("Wave Steepness", Float) = 1
		[HideInInspector] _WaveDeform("Wave Deform", Float) = 0.3

		[HideInInspector] _FresnelStrength("Fresnel Strength", Range(0.0, 5.0)) = 1
		[HideInInspector] _FresnelBias("Fresnel Bias", Range(0.0, 1.0)) = 0

		[HideInInspector] _ReflectionTex("Reflection Texture", 2D) = "black" { }
		[HideInInspector] _ReflectionDistortionStrength("Reflection Distortion Strength", Float) = 1

		[HideInInspector] _RefractionDistortionStrength("Refraction Distortion Strength", Float) = 1

		[HideInInspector] _CausticTex("Caustic Texture", 2D) = "black" { }
		[HideInInspector] _CausticSize("Caustic Size", Float) = 1
		[HideInInspector] _CausticStrength("Caustic Strength", Range(0.0, 1.0)) = 1
		[HideInInspector] _CausticDistortionStrength("Caustic Distortion Strength", Float) = 1
	}

	SubShader
	{
		GrabPass
		{
			"_RefractionTex"
		}

		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
		Blend One Zero
		Cull Back
		ZTest LEqual
		ZWrite On

		CGPROGRAM

		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"

		#pragma shader_feature_local MESH_NOISE
		#pragma shader_feature_local WAVE
		#pragma shader_feature_local LIGHT_ABSORPTION
		#pragma shader_feature_local FOAM
		#pragma shader_feature_local FOAM_HQ
		#pragma shader_feature_local FOAM_CREST
		#pragma shader_feature_local REFLECTION
		#pragma shader_feature_local REFRACTION
		#pragma shader_feature_local CAUSTIC

		#define POSEIDON_WATER_ADVANCED
		#undef POSEIDON_SRP
		#include "./CGIncludes/PUniforms.cginc"
		#include "./CGIncludes/PMeshNoise.cginc"
		#include "./CGIncludes/PWave.cginc"
		#include "./CGIncludes/PLightAbsorption.cginc"
		#include "./CGIncludes/PFoam.cginc"
		#include "./CGIncludes/PRipple.cginc"
		#include "./CGIncludes/PFresnel.cginc"
		#include "./CGIncludes/PReflection.cginc"
		#include "./CGIncludes/PRefraction.cginc"
		#include "./CGIncludes/PCaustic.cginc"
		#include "./CGIncludes/PCore.cginc"

		#pragma multi_compile_fog
		#pragma multi_compile_instancing
		#pragma surface surfAdvanced StandardSpecular nolightmap nodynlightmap nodirlightmap noshadow vertex:vertexFunction finalcolor:finalColorFunction

		void surfAdvanced(Input i, inout SurfaceOutputStandardSpecular o)
		{
			#if LIGHT_ABSORPTION || CAUSTIC || FOAM
				float sceneDepth = GetSceneDepth(i.screenPos);
				float surfaceDepth = GetSurfaceDepth(float4(i.worldPos, 1));
			#endif

			float4 waterColor;
			float4 tintColor = _Color;
			#if LIGHT_ABSORPTION
				CalculateDeepWaterColor(sceneDepth, surfaceDepth, tintColor);
			#endif

			float3 worldNormal = UnityObjectToWorldNormal(i.normal);
			float fresnel;
			CalculateFresnelFactor(i.worldPos, worldNormal, fresnel);

			float4 reflColor = _Color;
			#if REFLECTION && !UNITY_SINGLE_PASS_STEREO && !STEREO_INSTANCING_ON && !UNITY_STEREO_MULTIVIEW_ENABLED
				SampleReflectionTexture(i.screenPos, worldNormal, reflColor);
			#endif

			float4 refrColor = _DepthColor;
			#if REFRACTION
				SampleRefractionTexture(i.screenPos, worldNormal, refrColor);
			#endif

			half4 causticColor = half4(0, 0, 0, 0);
			#if CAUSTIC
				SampleCausticTexture(sceneDepth, surfaceDepth, float4(i.worldPos, 1), worldNormal, causticColor);
			#endif
			refrColor += causticColor;

			waterColor = tintColor * lerp(refrColor, reflColor, fresnel);
			waterColor = waterColor * tintColor.a + (1 - tintColor.a) * refrColor;
			waterColor = saturate(waterColor);
			half4 foamColor = half4(0, 0, 0, 0);
			#if FOAM
				#if FOAM_HQ
					CalculateFoamColorHQ(sceneDepth, surfaceDepth, float4(i.worldPos, 1), worldNormal, i.crestMask, foamColor);
				#else
					CalculateFoamColor(sceneDepth, surfaceDepth, float4(i.worldPos, 1), worldNormal, i.crestMask, foamColor);
				#endif
			#endif

			o.Albedo = lerp(waterColor.rgb, foamColor.rgb, foamColor.a);
			o.Alpha = lerp(tintColor.a, foamColor.a, foamColor.a);
			o.Specular = _Specular;
			o.Smoothness = saturate(_Smoothness - foamColor.a);
		}

		ENDCG
	}
	Fallback "Unlit/Color"
}
