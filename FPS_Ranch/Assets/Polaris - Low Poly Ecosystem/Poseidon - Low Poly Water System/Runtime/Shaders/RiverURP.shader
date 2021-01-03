Shader "Poseidon/URP/RiverURP"
{
    Properties
    {
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
        [HideInInspector] _SlopeFoamStrength("Slope Foam Strength", Float) = 0.5
        [HideInInspector] _SlopeFoamFlowSpeed("Slope Foam Flow Speed", Float) = 20
        [HideInInspector] _SlopeFoamDistance("Slope Foam Distance", Float) = 100

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

        [HideInInspector] _RefractionTex("Refraction Texture", 2D) = "black" { }
        [HideInInspector] _RefractionDistortionStrength("Refraction Distortion Strength", Float) = 1

        [HideInInspector] _CausticTex("Caustic Texture", 2D) = "black" { }
        [HideInInspector] _CausticSize("Caustic Size", Float) = 1
        [HideInInspector] _CausticStrength("Caustic Strength", Range(0.0, 1.0)) = 1
        [HideInInspector] _CausticDistortionStrength("Caustic Distortion Strength", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+0" }
        
        Pass 
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForward" }
            
            // Render State
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Back
            ZTest LEqual
            ZWrite Off
            // ColorMask: <None>
            
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            // Pragmas
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            // Keywords
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            // GraphKeywords: <None>
            
            // Defines
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _SPECULAR_SETUP
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_COLOR
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define VARYINGS_NEED_SCREENPOSITION
            #define FEATURES_GRAPH_VERTEX
            #define SHADERPASS_FORWARD

            #pragma shader_feature_local WAVE
            #pragma shader_feature_local LIGHT_ABSORPTION
            #pragma shader_feature_local FOAM
            #pragma shader_feature_local FOAM_HQ
            #pragma shader_feature_local FOAM_CREST
            #pragma shader_feature_local FOAM_SLOPE
            #pragma shader_feature_local CAUSTIC
            #pragma shader_feature_local LIGHTING_PHYSICAL_BASED
            #pragma shader_feature_local LIGHTING_BLINN_PHONG
            #pragma shader_feature_local LIGHTING_LAMBERT
            
            #if LIGHT_ABSORPTION || FOAM
                #define REQUIRE_DEPTH_TEXTURE
            #endif
            #define REQUIRE_OPAQUE_TEXTURE
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
            
            #define POSEIDON_RIVER
            #define POSEIDON_SRP
            #include "./CGIncludes/PUniforms.cginc"
            #include "./CGIncludes/PRipple.cginc"
            #include "./CGIncludes/PWave.cginc"
            #include "./CGIncludes/PDepth.cginc"
            #include "./CGIncludes/PLightAbsorption.cginc"
            #include "./CGIncludes/PFresnel.cginc"
            #include "./CGIncludes/PFoam.cginc"
            #include "./CGIncludes/PRefraction.cginc"
            #include "./CGIncludes/PCaustic.cginc"
            #include "./CGIncludes/PCommon.cginc"
            #include "./CGIncludes/PLightingSRP.cginc"
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END
            
            // Graph Functions
            // GraphFunctions: <None>
            
            // Graph Vertex
            struct VertexDescriptionInputs
            {
                float3 ObjectSpaceNormal;
                float3 ObjectSpacePosition;
                float4 ObjectSpaceTangent;
                float4 UV0;
                float4 UV1;
                float4 VertexColor;
            };
            
            struct VertexDescription
            {
                float3 VertexPosition;
                float3 VertexNormal;
                float3 VertexTangent;
                float CrestMask;
            };
            
            VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
            {
                VertexDescription description = (VertexDescription)0;

                float4 positionOS = float4(IN.ObjectSpacePosition.xyz, 1);
                float4 uv0 = IN.UV0;
                float4 uv1 = IN.UV1;
                float4 vertexColor = IN.VertexColor;
                float3 normalOS = IN.ObjectSpaceNormal;
                float4 tangentOS = IN.ObjectSpaceTangent;
                float crestMask = 0;
                #if WAVE
                    ApplyWaveHQ(positionOS, uv0, vertexColor, crestMask);
                #endif

                ApplyRipple(positionOS, uv0, vertexColor, uv1);
                CalculateNormal(positionOS, uv0, vertexColor, normalOS);
                
                description.VertexPosition = positionOS;
                description.VertexNormal = normalOS;
                description.VertexTangent = tangentOS;
                description.CrestMask = crestMask;
                return description;
            }
            
            // Graph Pixel
            struct SurfaceDescriptionInputs
            {
                float3 WorldSpacePosition;
                float3 WorldSpaceNormal;
                float4 ScreenPosition;
                float CrestMask;
            };
            
            struct SurfaceDescription
            {
                float3 Albedo;
                float3 Normal;
                float3 Specular;
                float Smoothness;
                float Alpha;
            };
            
            SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
            {
                half fresnel;
                CalculateFresnelFactor(IN.WorldSpacePosition, IN.WorldSpaceNormal, fresnel);

                #if LIGHT_ABSORPTION || FOAM || CAUSTIC
                    float sceneDepth = GetSceneDepth(IN.ScreenPosition);
                    float surfaceDepth = GetSurfaceDepth(float4(IN.WorldSpacePosition, 1));
                #endif

                half4 waterColor;
                half4 tintColor = _Color;
                #if LIGHT_ABSORPTION
                    CalculateDeepWaterColor(sceneDepth, surfaceDepth, tintColor);
                #endif
                    
                half4 refrColor = _DepthColor;
                    SampleRefractionTexture(IN.ScreenPosition, IN.WorldSpaceNormal, refrColor);
                    
                half4 causticColor = half4(0, 0, 0, 0);
                #if CAUSTIC
                    SampleCausticTexture(sceneDepth, surfaceDepth, IN.WorldSpacePosition, IN.WorldSpaceNormal, causticColor);
                #endif
                refrColor += causticColor;

                waterColor = lerp(refrColor, tintColor, tintColor.a * fresnel);

                half4 foamColor = float4(0, 0, 0, 0);
                #if FOAM
                    #if FOAM_HQ
                        CalculateFoamColorHQ(sceneDepth, surfaceDepth, IN.WorldSpacePosition, IN.WorldSpaceNormal, IN.CrestMask, foamColor);
                    #else
                        CalculateFoamColor(sceneDepth, surfaceDepth, IN.WorldSpacePosition,IN.WorldSpaceNormal, IN.CrestMask, foamColor);
                    #endif
                #endif 

                half3 Albedo = lerp(waterColor.rgb, foamColor.rgb * 1.5, foamColor.a);
                half3 Specular = _Specular.rgb;
                half Smoothness = saturate(_Smoothness - foamColor.a);
                half Alpha = 1;

                SurfaceDescription surface = (SurfaceDescription)0;
                surface.Albedo = IsGammaSpace() ? Albedo : SRGBToLinear(Albedo);
                surface.Normal = float3(0, 0, 1);
                surface.Specular = IsGammaSpace() ? Specular : SRGBToLinear(Specular);
                surface.Smoothness = Smoothness;
                surface.Alpha = Alpha;
                return surface;
            }
            
            // --------------------------------------------------
            // Structs and Packing
            
            // Generated Type: Attributes
            struct Attributes
            {
                float3 positionOS: POSITION;
                float3 normalOS: NORMAL;
                float4 tangentOS: TANGENT;
                float4 uv0: TEXCOORD0;
                float4 uv1: TEXCOORD1;
                float4 color : COLOR;
                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID: INSTANCEID_SEMANTIC;
                #endif
            };
            
            // Generated Type: Varyings
            struct Varyings
            {
                float4 positionCS: SV_POSITION;
                float3 positionWS;
                float3 normalWS;
                float4 tangentWS;
                float3 viewDirectionWS;
                float4 screenPosition;
                #if defined(LIGHTMAP_ON)
                    float2 lightmapUV;
                #endif
                #if !defined(LIGHTMAP_ON)
                    float3 sh;
                #endif
                float4 fogFactorAndVertexLight;
                float4 shadowCoord;
                float crestMask;

                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID: CUSTOM_INSTANCE_ID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    uint stereoTargetEyeIndexAsRTArrayIdx: SV_RenderTargetArrayIndex;
                #endif
                #if(defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    uint stereoTargetEyeIndexAsBlendIdx0: BLENDINDICES0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    FRONT_FACE_TYPE cullFace: FRONT_FACE_SEMANTIC;
                #endif
            };
            
            // Generated Type: PackedVaryings
            struct PackedVaryings
            {
                float4 positionCS: SV_POSITION;
                #if defined(LIGHTMAP_ON)
                #endif
                #if !defined(LIGHTMAP_ON)
                #endif
                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID: CUSTOM_INSTANCE_ID;
                #endif
                float3 interp00: TEXCOORD0;
                float3 interp01: TEXCOORD1;
                float4 interp02: TEXCOORD2;
                float3 interp03: TEXCOORD3;
                float3 interp04: TEXCOORD4;
                float4 interp05: TEXCOORD5;
                float4 interp06: TEXCOORD6;
                float4 interp07: TEXCOORD7;
                float4 interp08: TEXCOORD8;
                float interp09 : TEXCOORD9;
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    uint stereoTargetEyeIndexAsRTArrayIdx: SV_RenderTargetArrayIndex;
                #endif
                #if(defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    uint stereoTargetEyeIndexAsBlendIdx0: BLENDINDICES0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    FRONT_FACE_TYPE cullFace: FRONT_FACE_SEMANTIC;
                #endif
            };
            
            // Packed Type: Varyings
            PackedVaryings PackVaryings(Varyings input)
            {
                PackedVaryings output = (PackedVaryings)0;
                output.positionCS = input.positionCS;
                output.interp00.xyz = input.positionWS;
                output.interp01.xyz = input.normalWS;
                output.interp02.xyzw = input.tangentWS;
                output.interp03.xyz = input.viewDirectionWS;
#if defined(LIGHTMAP_ON)
                output.interp04.xy = input.lightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
                output.interp05.xyz = input.sh;
#endif
                output.interp06.xyzw = input.fogFactorAndVertexLight;
                output.interp07.xyzw = input.shadowCoord;
                output.interp08.xyzw = input.screenPosition;
                output.interp09.x = input.crestMask;

                #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                #endif
                #if(defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                #endif
                return output;
            }
            
            // Unpacked Type: Varyings
            Varyings UnpackVaryings(PackedVaryings input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = input.positionCS;
                output.positionWS = input.interp00.xyz;
                output.normalWS = input.interp01.xyz;
                output.tangentWS = input.interp02.xyzw;
                output.viewDirectionWS = input.interp03.xyz;
#if defined(LIGHTMAP_ON)
                output.lightmapUV = input.interp04.xy;
#endif
#if !defined(LIGHTMAP_ON)
                output.sh = input.interp05.xyz;
#endif
                output.fogFactorAndVertexLight = input.interp06.xyzw;
                output.shadowCoord = input.interp07.xyzw;
                output.screenPosition = input.interp08.xyzw;
                output.crestMask = input.interp09.x;

                #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                #endif
                #if(defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                #endif
                return output;
            }
            
            // --------------------------------------------------
            // Build Graph Inputs
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
            {
                VertexDescriptionInputs output;
                ZERO_INITIALIZE(VertexDescriptionInputs, output);
            
                output.ObjectSpaceNormal = input.normalOS;
                output.ObjectSpacePosition = input.positionOS;
                output.ObjectSpaceTangent = input.tangentOS;
                output.UV0 = input.uv0;
                output.UV1 = input.uv1;
                output.VertexColor = input.color;
            
                return output;
            }
            
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
            {
                SurfaceDescriptionInputs output;
                ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

                output.WorldSpacePosition = input.positionWS;
                output.WorldSpaceNormal = input.normalWS;
                output.ScreenPosition = input.screenPosition;
                output.CrestMask = input.crestMask;

                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign = IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                    #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                return output;
            }
            
            // --------------------------------------------------
            // Main
            #include "./CGIncludes/PUniversalVaryings.hlsl"
            #include "./CGIncludes/PUniversalForwardPass.hlsl"
            
            ENDHLSL
            
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
}
