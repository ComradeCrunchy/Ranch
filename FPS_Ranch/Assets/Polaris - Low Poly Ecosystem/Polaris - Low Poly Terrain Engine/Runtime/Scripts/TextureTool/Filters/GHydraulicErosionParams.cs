using UnityEngine;

namespace Pinwheel.Griffin.TextureTool
{
    [System.Serializable]
    public struct GHydraulicErosionParams
    {
        [SerializeField]
        private int iteration;
        public int Iteration
        {
            get
            {
                return iteration;
            }
            set
            {
                iteration = Mathf.Max(1, value);
            }
        }

        [SerializeField]
        private float rain;
        public float Rain
        {
            get
            {
                return rain;
            }
            set
            {
                rain = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float transportation;
        public float Transportation
        {
            get
            {
                return transportation;
            }
            set
            {
                transportation = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float angleMin;
        public float AngleMin
        {
            get
            {
                return angleMin;
            }
            set
            {
                angleMin = Mathf.Clamp(value, 1, 45);
            }
        }

        [SerializeField]
        private float evaporation;
        public float Evaporation
        {
            get
            {
                return evaporation;
            }
            set
            {
                evaporation = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private Vector3 dimension;
        public Vector3 Dimension
        {
            get
            {
                return dimension;
            }
            set
            {
                dimension = value;
            }
        }

        [SerializeField]
        private Texture2D waterSourceMap;
        public Texture2D WaterSourceMap
        {
            get
            {
                return waterSourceMap;
            }
            set
            {
                waterSourceMap = value;
            }
        }

        [SerializeField]
        private Texture2D hardnessMap;
        public Texture2D HardnessMap
        {
            get
            {
                return hardnessMap;
            }
            set
            {
                hardnessMap = value;
            }
        }

        public static GHydraulicErosionParams Create()
        {
            GHydraulicErosionParams param = new GHydraulicErosionParams();

            param.Iteration = 10;
            param.Rain = 0.5f;
            param.Transportation = 0.1f;
            param.AngleMin = 5;
            param.Evaporation = 0.1f;
            param.Dimension = Vector3.one;

            return param;
        }
    }
}
