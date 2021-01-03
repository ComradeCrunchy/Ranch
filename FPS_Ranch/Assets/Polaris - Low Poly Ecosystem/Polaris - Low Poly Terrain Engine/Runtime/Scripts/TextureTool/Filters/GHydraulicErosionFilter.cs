using UnityEngine;

namespace Pinwheel.Griffin.TextureTool
{
    public class GHydraulicErosionFilter : IGTextureFilter
    {
        private static RenderTexture bgRt0;
        private static RenderTexture bgRt1;

        private static Material mat;
        private static Material Mat
        {
            get
            {
                if (mat == null)
                {
                    mat = new Material(GRuntimeSettings.Instance.internalShaders.hydraulicErosionFilter);
                }
                return mat;
            }
        }

        public void Apply(RenderTexture targetRt, GTextureFilterParams param)
        {
            GHydraulicErosionParams erosionParam = param.HydraulicErosion;
            RenderTexture writeRt = null; //buffer to write to
            RenderTexture readRt = null; //buffer to read from
            RenderTexture tmp = null; //temp buffer for swapping
            CloneBg(targetRt, out writeRt, out readRt);

            //init height field & water level
            Mat.SetTexture("_HeightMap", targetRt);
            GCommon.DrawQuad(writeRt, GCommon.FullRectUvPoints, Mat, 0);
            GCommon.DrawQuad(readRt, GCommon.FullRectUvPoints, Mat, 0);

            //simulate
            for (int i = 0; i < erosionParam.Iteration; ++i)
            {
                Mat.SetTexture("_HeightMap", readRt);
                Mat.SetVector("_Dimension", erosionParam.Dimension);
                Mat.SetFloat("_Rain", erosionParam.Rain);
                Mat.SetFloat("_Transportation", erosionParam.Transportation);
                Mat.SetFloat("_MinAngle", erosionParam.AngleMin);
                Mat.SetFloat("_Evaporation", erosionParam.Evaporation);
                Mat.SetTexture("_WaterSourceMap", erosionParam.WaterSourceMap);
                Mat.SetTexture("_HardnessMap", erosionParam.HardnessMap);
                //Mat.SetFloat("_Seed", Random.value);
                GCommon.DrawQuad(writeRt, GCommon.FullRectUvPoints, Mat, 1);

                //swap buffer
                tmp = readRt;
                readRt = writeRt;
                writeRt = tmp;
            }

            //copy result
            GCommon.CopyToRT(writeRt, targetRt);
        }

        private void CloneBg(RenderTexture targetRt, out RenderTexture bg0, out RenderTexture bg1)
        {
            if (bgRt0 == null)
            {
                bgRt0 = new RenderTexture(targetRt);
            }
            else if (bgRt0.width != targetRt.width || bgRt0.height != targetRt.height || bgRt0.format != targetRt.format)
            {
                bgRt0.Release();
                GUtilities.DestroyObject(bgRt0);
                bgRt0 = new RenderTexture(targetRt);
            }

            if (bgRt1 == null)
            {
                bgRt1 = new RenderTexture(targetRt);
            }
            else if (bgRt1.width != targetRt.width || bgRt1.height != targetRt.height || bgRt1.format != targetRt.format)
            {
                bgRt1.Release();
                GUtilities.DestroyObject(bgRt1);
                bgRt1 = new RenderTexture(targetRt);
            }

            //GCommon.CopyToRT(targetRt, bgRt0);
            //GCommon.CopyToRT(targetRt, bgRt1);

            bg0 = bgRt0;
            bg1 = bgRt1;
        }
    }
}
