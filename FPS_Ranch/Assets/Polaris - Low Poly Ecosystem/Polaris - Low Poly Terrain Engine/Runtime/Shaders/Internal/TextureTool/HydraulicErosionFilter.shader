Shader "Hidden/Griffin/HydraulicErosionFilter"
{
    Properties
    {
		_HeightMap ("Height Map", 2D) = "black" {}
		_Dimension ("Dimension", Vector) = (1,1,1,1)
		_Rain ("Rain", Range(0.0, 1.0)) = 0.5
		_Transportation ("Transportation", Range(0.0, 1.0)) = 0.1
		_MinAngle ("Min Angle", Range(0.0, 45.0)) = 5
		_Evaporation ("Evaporation", Range(0.0, 1.0)) = 0.1
		_WaterSourceMap ("Water Source", 2D) = "black" {}
		_HardnessMap ("Hardness Map", 2D) = "black" {}
		_Seed ("Seed", Float) = 1
    }

	CGINCLUDE
    #include "UnityCG.cginc"
#include "TextureToolCommon.cginc"

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

	sampler2D _HeightMap;
	float4 _HeightMap_TexelSize;
	float4 _Dimension;
	float _Rain;
	float _Transportation;
	float _MinAngle;
	float _Evaporation;
	sampler2D _WaterSourceMap;
	sampler2D _HardnessMap;

	float _Seed;

	#define TEXEL _HeightMap_TexelSize
	#define LEFT_UV uv - float2(TEXEL.x, 0)
	#define TOP_UV uv + float2(0, TEXEL.y)
	#define RIGHT_UV uv + float2(TEXEL.x, 0)
	#define BOTTOM_UV uv - float2(0, TEXEL.y)
	#define DELTA_TIME 0.0167
	#define G 9.8
	#define PIPE_LENGTH_X TEXEL.x
	#define PIPE_LENGTH_Z TEXEL.y
	#define AVG_PIPE_LENGTH (TEXEL.x + TEXEL.y)*0.5
	#define PIPE_CROSS_SECTIONAL_AREA TEXEL.x*TEXEL.y

	#define SUM_HW(data) data##.r + data##.g
	#define HEIGHT(data) data##.r
	#define WATER(data) data##.g
	#define OUTFLOW(data) data##.b

	v2f vert (appdata v)
    {
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
    }

	float4 fragInit (v2f i) : SV_Target
    {
		float4 col = tex2D(_HeightMap, i.uv);
		return float4(col.r, 0, 0, 0);
	}

	void SimulateWaterSource(float2 uv, inout float4 data)
	{
		float rainRandom = RandomValue(uv.x*_Seed, uv.y*_Seed);
		WATER(data) += (rainRandom < _Rain)*DELTA_TIME;
		WATER(data) += tex2D(_WaterSourceMap, uv).r*DELTA_TIME;
	}

	float CalculateFlowAmount(float4 data, float4 desData)
	{
		return max(0,OUTFLOW(data) + DELTA_TIME*PIPE_CROSS_SECTIONAL_AREA*G*(SUM_HW(data)-SUM_HW(desData))/AVG_PIPE_LENGTH);
	}

	void SimulateWaterFlow(float2 uv, inout float4 data)
	{
		float4 left = tex2D(_HeightMap, LEFT_UV);
		float4 top = tex2D(_HeightMap, TOP_UV);
		float4 right = tex2D(_HeightMap, RIGHT_UV);
		float4 bottom = tex2D(_HeightMap, BOTTOM_UV);
		
		float foL = CalculateFlowAmount(data, left);
		float foT = CalculateFlowAmount(data, top);
		float foR = CalculateFlowAmount(data, right);
		float foB = CalculateFlowAmount(data, bottom);

		float scaleFactor = min(1, WATER(data)/((foL+foT+foR+foB)*DELTA_TIME));
		foL *= scaleFactor;
		foT *= scaleFactor;
		foR *= scaleFactor;
		foB *= scaleFactor;

		OUTFLOW(data) = OUTFLOW(data) + foL + foT + foR + foB;

		float fiL = CalculateFlowAmount(left, data);
		float fiT = CalculateFlowAmount(top, data);
		float fiR = CalculateFlowAmount(right, data);
		float fiB = CalculateFlowAmount(bottom, data);

		float waterChange = fiL + fiT + fiR + fiB - foL - foT - foR - foB;

		WATER(data) = max(0, WATER(data)+waterChange/AVG_PIPE_LENGTH);
	}

	void SimulateEvaporation(inout float4 data)
	{
		WATER(data) = WATER(data)*(1-_Evaporation*DELTA_TIME);
		WATER(data) = max(0, WATER(data));
	}

	float4 fragErode (v2f i) : SV_Target
	{
		float4 data = tex2D(_HeightMap, i.uv);

		SimulateWaterSource(i.uv, data);
		SimulateWaterFlow(i.uv, data);

		SimulateEvaporation(data);
		//data.b = SUM_HW(data);

		return saturate(data);
	}

	ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }
		
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragInit
            ENDCG
        }

		Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragErode
            ENDCG
        }
    }
}
