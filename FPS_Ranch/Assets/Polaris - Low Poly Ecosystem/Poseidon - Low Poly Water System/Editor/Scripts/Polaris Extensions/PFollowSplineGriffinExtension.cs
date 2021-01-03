#if UNITY_EDITOR && GRIFFIN && POSEIDON
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.Griffin.SplineTool;
using Pinwheel.Poseidon;
using Pinwheel.Griffin;

namespace Pinwheel.Poseidon.GriffinExtension
{
    public class PFollowSplineGriffinExtension
    {
        private const string GX_POSEIDON_FOLLOW_SPLINE = "http://bit.ly/36l4Zms";

        public static string GetExtensionName()
        {
            return "Poseidon - Follow Spline";
        }

        public static string GetPublisherName()
        {
            return "Pinwheel Studio";
        }

        public static string GetDescription()
        {
            return
                "Add water tiles which follow a spline on XZ-plane.";
        }

        public static string GetVersion()
        {
            return "v1.0.0";
        }

        public static void OpenSupportLink()
        {
            PEditorCommon.OpenEmailEditor(
                "customer@pinwheel.studio",
                "Griffin Extension - Poseidon",
                "YOUR_MESSAGE_HERE");
        }

        public static void OnGUI()
        {
            PFollowSplineConfig config = PFollowSplineConfig.Instance;
            config.Spline = EditorGUILayout.ObjectField("Spline", config.Spline, typeof(GSplineCreator), true) as GSplineCreator;
            config.Water = EditorGUILayout.ObjectField("Water", config.Water, typeof(PWater), true) as PWater;
            config.WaterLevel = EditorGUILayout.FloatField("Water Level", config.WaterLevel);
            EditorUtility.SetDirty(config);

            GUI.enabled = config.Spline != null && config.Water != null;
            if (GUILayout.Button("Follow Spline"))
            {
                GAnalytics.Record(GX_POSEIDON_FOLLOW_SPLINE);
                FollowSpline();
            }
            GUI.enabled = true;
        }

        private static void FollowSpline()
        {
            PFollowSplineConfig config = PFollowSplineConfig.Instance;
            if (config.Spline == null || config.Water == null)
                return;
            GSplineCreator spline = config.Spline;
            PWater water = config.Water;

            List<GSplineAnchor> anchors = spline.Spline.Anchors;
            Vector3 sumPosition = Vector3.zero;
            for (int i = 0; i < anchors.Count; ++i)
            {
                sumPosition += anchors[i].Position;
            }
            Vector3 avgPos = sumPosition / anchors.Count;
            water.transform.position = new Vector3(avgPos.x, config.WaterLevel, avgPos.z);

            List<PIndex2D> indices = new List<PIndex2D>();
            List<Vector4> verts = spline.GenerateVerticesWithFalloff();
            for (int i = 0; i < verts.Count; ++i)
            {
                PIndex2D index = water.WorldPointToTileIndex(verts[i]);
                indices.AddIfNotContains(index);
            }

            water.TileIndices = indices;
            water.ReCalculateBounds();
        }
    }
}
#endif
