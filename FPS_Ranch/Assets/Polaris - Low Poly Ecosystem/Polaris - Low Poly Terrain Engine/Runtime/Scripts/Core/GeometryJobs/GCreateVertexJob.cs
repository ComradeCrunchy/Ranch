using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

namespace Pinwheel.Griffin
{
#if GRIFFIN_BURST
    [BurstCompile(CompileSynchronously = false)]
#endif
    public struct GCreateVertexJob : IJob
    {
        [ReadOnly]
        public NativeArray<GSubdivNode> nodes;
        [ReadOnly]
        public NativeArray<byte> creationState;

        public GTextureNativeDataDescriptor<Color32> hmC;
        public GTextureNativeDataDescriptor<Color32> hmL;
        public GTextureNativeDataDescriptor<Color32> hmT;
        public GTextureNativeDataDescriptor<Color32> hmR;
        public GTextureNativeDataDescriptor<Color32> hmB;
        public GTextureNativeDataDescriptor<Color32> hmBL;
        public GTextureNativeDataDescriptor<Color32> hmTL;
        public GTextureNativeDataDescriptor<Color32> hmTR;
        public GTextureNativeDataDescriptor<Color32> hmBR;

        public GTextureNativeDataDescriptor<Color32> albedoMap;
        public GAlbedoToVertexColorMode albedoToVertexColorMode;

        [WriteOnly]
        public NativeArray<Vector3> vertices;
        [WriteOnly]
        public NativeArray<Vector2> uvs;
        [WriteOnly]
        public NativeArray<int> triangles;
        [WriteOnly]
        public NativeArray<Vector3> normals;
        [WriteOnly]
        public NativeArray<Color32> colors;
        [WriteOnly]
        public NativeArray<int> metadata;

        public int meshBaseResolution;
        public int meshResolution;
        public int lod;
        public int displacementSeed;
        public float displacementStrength;

        public Vector3 terrainSize;
        public Rect chunkUvRect;
        public Vector3 chunkLocalPosition;

        public void Execute()
        {
            GSubdivNode n;
            Vector3 v0 = Vector3.zero;
            Vector3 v1 = Vector3.zero;
            Vector3 v2 = Vector3.zero;

            Vector2 uv0 = Vector2.zero;
            Vector2 uv1 = Vector2.zero;
            Vector2 uv2 = Vector2.zero;
            Vector2 uvc = Vector2.zero;

            Vector3 normal = Vector3.zero;
            Color32 color = new Color32();

            int i0 = 0;
            int i1 = 0;
            int i2 = 0;

            Color hmData0 = Color.black;
            Color hmData1 = Color.black;
            Color hmData2 = Color.black;
            float heightSample = 0;

            meshBaseResolution = Mathf.Max(0, meshBaseResolution - lod);

            int length = nodes.Length;
            int leafIndex = 0;
            int startIndex = GGeometryJobUtilities.GetStartIndex(ref meshBaseResolution);
            int removedLeafCount = 0;

            for (int i = startIndex; i < length; ++i)
            {
                if (creationState[i] != GGeometryJobUtilities.STATE_LEAF)
                    continue;
                n = nodes[i];
                ProcessTriangle(
                    ref n, ref leafIndex,
                    ref uv0, ref uv1, ref uv2, ref uvc,
                    ref v0, ref v1, ref v2,
                    ref i0, ref i1, ref i2,
                    ref normal, ref color,
                    ref hmData0, ref hmData1, ref hmData2,
                    ref heightSample, ref removedLeafCount);
                leafIndex += 1;
            }

            metadata[GGeometryJobUtilities.METADATA_LEAF_REMOVED] = removedLeafCount;
        }

        private void ProcessTriangle(
            ref GSubdivNode n, ref int leafIndex,
            ref Vector2 uv0, ref Vector2 uv1, ref Vector2 uv2, ref Vector2 uvc,
            ref Vector3 v0, ref Vector3 v1, ref Vector3 v2,
            ref int i0, ref int i1, ref int i2,
            ref Vector3 normal, ref Color32 color,
            ref Color hmData0, ref Color hmData1, ref Color hmData2,
            ref float heightSample, ref int removedLeafCount)
        {
            GGeometryJobUtilities.NormalizeToPoint(ref uv0, ref chunkUvRect, ref n.v0);
            GGeometryJobUtilities.NormalizeToPoint(ref uv1, ref chunkUvRect, ref n.v1);
            GGeometryJobUtilities.NormalizeToPoint(ref uv2, ref chunkUvRect, ref n.v2);

            if (displacementStrength > 0)
            {
                DisplaceUV(ref uv0);
                DisplaceUV(ref uv1);
                DisplaceUV(ref uv2);
            }

            GetHeightMapData(ref hmData0, ref uv0);
            GetHeightMapData(ref hmData1, ref uv1);
            GetHeightMapData(ref hmData2, ref uv2);

            GetHeightSample(ref heightSample, ref hmData0);
            v0.Set(
                uv0.x * terrainSize.x - chunkLocalPosition.x,
                heightSample * terrainSize.y,
                uv0.y * terrainSize.z - chunkLocalPosition.z);

            GetHeightSample(ref heightSample, ref hmData1);
            v1.Set(
                uv1.x * terrainSize.x - chunkLocalPosition.x,
                heightSample * terrainSize.y,
                uv1.y * terrainSize.z - chunkLocalPosition.z);

            GetHeightSample(ref heightSample, ref hmData2);
            v2.Set(
                uv2.x * terrainSize.x - chunkLocalPosition.x,
                heightSample * terrainSize.y,
                uv2.y * terrainSize.z - chunkLocalPosition.z);

            normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            i0 = leafIndex * 3 + 0;
            i1 = leafIndex * 3 + 1;
            i2 = leafIndex * 3 + 2;

            vertices[i0] = v0;
            vertices[i1] = v1;
            vertices[i2] = v2;

            uvs[i0] = uv0;
            uvs[i1] = uv1;
            uvs[i2] = uv2;

            normals[i0] = normal;
            normals[i1] = normal;
            normals[i2] = normal;

            if (hmData0.a >= 0.5 || hmData1.a >= 0.5 || hmData2.a >= 0.5)
            {
                triangles[i0] = i0;
                triangles[i1] = i0;
                triangles[i2] = i0;
                removedLeafCount += 1;
            }
            else
            {
                triangles[i0] = i0;
                triangles[i1] = i1;
                triangles[i2] = i2;
            }

            if (albedoToVertexColorMode == GAlbedoToVertexColorMode.Sharp)
            {
                uvc = (uv0 + uv1 + uv2) / 3f;
                color = GGeometryJobUtilities.GetColorBilinear(albedoMap, ref uvc);
                colors[i0] = color;
                colors[i1] = color;
                colors[i2] = color;
            }
            else if (albedoToVertexColorMode == GAlbedoToVertexColorMode.Smooth)
            {
                colors[i0] = GGeometryJobUtilities.GetColorBilinear(albedoMap, ref uv0);
                colors[i1] = GGeometryJobUtilities.GetColorBilinear(albedoMap, ref uv1);
                colors[i2] = GGeometryJobUtilities.GetColorBilinear(albedoMap, ref uv2);
            }
        }

        private void DisplaceUV(ref Vector2 uv)
        {
            if (uv.x == 0 || uv.y == 0 || uv.x == 1 || uv.y == 1)
                return;

            float noise0 = Mathf.PerlinNoise(displacementStrength + uv.x * 100, displacementSeed + uv.y * 100) - 0.5f;
            float noise1 = Mathf.PerlinNoise(displacementStrength - uv.x * 100, displacementSeed - uv.y * 100) - 0.5f;

            Vector2 v = new Vector2(noise0 * displacementStrength / terrainSize.x, noise1 * displacementStrength / terrainSize.z);
            uv.Set(
                Mathf.Clamp01(uv.x + v.x),
                Mathf.Clamp01(uv.y + v.y));
        }

        private void GetHeightMapData(ref Color data, ref Vector2 uv)
        {
            Color sample = Vector4.zero;
            float sampleCount = 0f;

            sample += GGeometryJobUtilities.GetColorBilinear(hmC, ref uv);
            sampleCount += 1;

            if (uv.x == 0 && uv.y == 0) //bottom left corner
            {
                if (hmB.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmB, Flip(ref uv, false, true));
                    sampleCount += 1;
                }
                if (hmL.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmL, Flip(ref uv, true, false));
                    sampleCount += 1;
                }
                if (hmBL.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmBL, Flip(ref uv, true, true));
                    sampleCount += 1;
                }
            }
            else if (uv.x == 0 && uv.y == 1) //top left corner
            {
                if (hmT.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmT, Flip(ref uv, false, true));
                    sampleCount += 1;
                }
                if (hmL.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmL, Flip(ref uv, true, false));
                    sampleCount += 1;
                }
                if (hmTL.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmTL, Flip(ref uv, true, true));
                    sampleCount += 1;
                }
            }
            else if (uv.x == 1 && uv.y == 1) //top right corner
            {
                if (hmT.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmT, Flip(ref uv, false, true));
                    sampleCount += 1;
                }
                if (hmR.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmR, Flip(ref uv, true, false));
                    sampleCount += 1;
                }
                if (hmTR.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmTR, Flip(ref uv, true, true));
                    sampleCount += 1;
                }
            }
            else if (uv.x == 1 && uv.y == 0) //bottom right corner
            {
                if (hmB.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmB, Flip(ref uv, false, true));
                    sampleCount += 1;
                }
                if (hmR.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmR, Flip(ref uv, true, false));
                    sampleCount += 1;
                }
                if (hmBR.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmBR, Flip(ref uv, true, true));
                    sampleCount += 1;
                }
            }
            else if (uv.x == 0) //left edge
            {
                if (hmL.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmL, Flip(ref uv, true, false));
                    sampleCount += 1;
                }
            }
            else if (uv.y == 1) //top edge
            {
                if (hmT.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmT, Flip(ref uv, false, true));
                    sampleCount += 1;
                }
            }
            else if (uv.x == 1) //right edge
            {
                if (hmR.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmR, Flip(ref uv, true, false));
                    sampleCount += 1;
                }
            }
            else if (uv.y == 0) //bottom edge
            {
                if (hmB.IsValid)
                {
                    sample += GGeometryJobUtilities.GetColorBilinear(hmB, Flip(ref uv, false, true));
                    sampleCount += 1;
                }
            }

            data = sample / sampleCount;
        }

        private float DecodeFloatRG(ref Vector2 enc)
        {
            Vector2 kDecodeDot = new Vector2(1.0f, 1f / 255.0f);
            return Vector2.Dot(enc, kDecodeDot);
        }

        private void GetHeightSample(ref float sample, ref Color data)
        {
            Vector2 enc = new Vector2(data.r, data.g);
            sample = DecodeFloatRG(ref enc);
        }

        private Vector2 Flip(ref Vector2 uv, bool flipX, bool flipY)
        {
            Vector2 v = new Vector2(
                flipX ? 1 - uv.x : uv.x,
                flipY ? 1 - uv.y : uv.y);
            return v;
        }
    }
}
