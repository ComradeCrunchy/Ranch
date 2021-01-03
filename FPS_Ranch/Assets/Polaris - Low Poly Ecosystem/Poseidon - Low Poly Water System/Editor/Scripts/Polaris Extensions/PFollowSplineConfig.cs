#if UNITY_EDITOR && GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Griffin.SplineTool;

namespace Pinwheel.Poseidon.GriffinExtension
{
    //[CreateAssetMenu(menuName = "Poseidon/Follow Spline Config")]
    public class PFollowSplineConfig : ScriptableObject
    {
        private static PFollowSplineConfig instance;
        public static PFollowSplineConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<PFollowSplineConfig>("FollowSplineConfig");
                    if (instance == null)
                    {
                        instance = ScriptableObject.CreateInstance<PFollowSplineConfig>();
                    }
                }
                return instance;
            }
        }

        [SerializeField]
        private GSplineCreator spline;
        public GSplineCreator Spline
        {
            get
            {
                return spline;
            }
            set
            {
                spline = value;
            }
        }

        [SerializeField]
        private PWater water;
        public PWater Water
        {
            get
            {
                return water;
            }
            set
            {
                water = value;
            }
        }

        [SerializeField]
        private float waterLevel;
        public float WaterLevel
        {
            get
            {
                return waterLevel;
            }
            set
            {
                waterLevel = value;
            }
        }
    }
}
#endif
