void BuildInputData(Varyings input, float3 normal, out InputData inputData)
{
    inputData.positionWS = input.positionWS;
#ifdef _NORMALMAP

#if _NORMAL_DROPOFF_TS
	// IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normal, half3x3(input.tangentWS.xyz, bitangent, input.normalWS.xyz));
#elif _NORMAL_DROPOFF_OS
	inputData.normalWS = TransformObjectToWorldNormal(normal);
#elif _NORMAL_DROPOFF_WS
	inputData.normalWS = normal;
#endif
    
#else
    inputData.normalWS = input.normalWS;
#endif
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = SafeNormalize(input.viewDirectionWS);

#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.sh, inputData.normalWS);
}

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET 
{    
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    InputData inputData;
    BuildInputData(unpacked, surfaceDescription.Normal, inputData);

    half4 color;
    #if LIGHTING_PHYSICAL_BASED
        color = PoseidonFragmentPBR(
			    inputData,
			    surfaceDescription.Albedo,
                surfaceDescription.Specular,
			    surfaceDescription.Smoothness,
			    surfaceDescription.Alpha);
    #elif LIGHTING_BLINN_PHONG
        color = PoseidonFragmentBlinnPhong(
                inputData,
                surfaceDescription.Albedo,
                half4(surfaceDescription.Specular, 1),
                surfaceDescription.Smoothness,
                surfaceDescription.Alpha);
    #elif LIGHTING_LAMBERT
        color = PoseidonFragmentLambert(
                inputData,
                surfaceDescription.Albedo,
                surfaceDescription.Alpha);
    #else
        color = 0;
    #endif

    color.rgb = MixFog(color.rgb, inputData.fogCoord); 
    return color;
}
