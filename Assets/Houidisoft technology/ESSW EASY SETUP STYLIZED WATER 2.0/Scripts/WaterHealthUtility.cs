using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ESSW.Editorcontroller;

namespace ESSW.Editor
{
    public static class WaterHealthUtility
    {
        public struct HealthReport
        {
            public bool hasWater;
            public bool missingMaterial;
            public bool missingGradient;
            public bool urpOpaqueMissing;
            public bool urpDepthMissing;
        }

        public static HealthReport Evaluate()
        {
            HealthReport report = new HealthReport();

#if UNITY_2023_1_OR_NEWER
            var waters = Object.FindObjectsByType<WaterShaderController>(
                FindObjectsSortMode.None);
#else
            var waters = Object.FindObjectsOfType<WaterShaderController>(true);
#endif
            report.hasWater = waters.Length > 0;

            foreach (var w in waters)
            {
                if (w.waterMaterial == null)
                    report.missingMaterial = true;

                var so = new SerializedObject(w);
                var grad = so.FindProperty("gradientTexture");
                if (grad == null || grad.objectReferenceValue == null)
                    report.missingGradient = true;
            }

            if (GraphicsSettings.currentRenderPipeline
                is UniversalRenderPipelineAsset urp)
            {
                report.urpOpaqueMissing = !urp.supportsCameraOpaqueTexture;
                report.urpDepthMissing = !urp.supportsCameraDepthTexture;
            }

            return report;
        }
    }
}