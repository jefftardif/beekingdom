#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.IO;

namespace ESSW.Editorcontroller
{
    [ExecuteAlways]
    public class WaterShaderController : MonoBehaviour
    {
        public Material waterMaterial;

        public enum FoamStyle { Soft, Hard }
        public enum ReflectionTex { TEX1, TEX2, TEX3, TEX4 }

        public FoamStyle foamStyle;
        public ReflectionTex SelectedTexture;

        [Header("Features")]
        public bool EnablePlanarRef = true;
        public bool enableRefraction = true;
        public bool enableFoam = true;
        public bool enableWaves = false;

        [Header("Refraction")]
        public Gradient refractionGradient;

        [Header("Surface")]
        public float Refraction_power = 0.01f;
        public float WaterDepth = 4f;
        public float DepthFade = 1.6f;
        public float Metallic = 0.1f;
        public float Smoothness = 0.99f;
        public float Darkness = 1.4f;

        [Header("Reflection")]
        public float ReflectionPower = 2.01f;
        public float ReflectionDistance = 8f;

        [Header("Normals")]
        public float DistortionPower = 0.08f;
        public float NormalsPower = 0.15f;
        public float NormalTextureScale = 30f;
        public int TextureSpeed = 7;

        [Header("Waves")]
        public float WaveLength = 0.1f;
        public float WaveHeight = 0.1f;
        public float PeakSharpness = 0.1f;
        public float WaveSpeed = 0.32f;

        [Header("Textures")]
        public Texture2D FirstNormalTexture;
        public Texture2D SecondNormalTexture;
        public Texture2D Foam_Texture;

        [Header("Foam")]
        public float foamCutoff = 0.97f;
        public float FoamIntensity = 3.5f;
        public float FoamSpeed = 0.05f;
        public float FoamScale = 0.32f;

        [HideInInspector] public Texture2D gradientTexture;

        const int GRADIENT_RESOLUTION = 128;

        // ================= UNITY =================

        void OnEnable()
        {
            ApplySettings();
            ApplyPlanarReflectionRuntime();
        }

        void Awake()
        {
            ApplySettings();
            ApplyPlanarReflectionRuntime();
        }

        void OnValidate()
        {
            ApplySettings();

#if UNITY_EDITOR
            EditorApplication.delayCall -= ApplyPlanarReflectionSafe;
            EditorApplication.delayCall += ApplyPlanarReflectionSafe;
#endif
        }

#if UNITY_EDITOR
        void OnDisable()
        {
            EditorApplication.delayCall -= ApplyPlanarReflectionSafe;
        }
#endif

        void Reset()
        {
            var r = GetComponent<Renderer>();
            if (r != null)
                waterMaterial = r.sharedMaterial;

#if UNITY_EDITOR
            Foam_Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Houidisoft technology/ESSW EASY SETUP STYLIZED WATER 2.0/Materials/Resources/FoamNoise.png");
            FirstNormalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Houidisoft technology/ESSW EASY SETUP STYLIZED WATER 2.0/Materials/Resources/Normalmap.png");
            SecondNormalTexture = FirstNormalTexture;

	    gradientTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Houidisoft technology/ESSW EASY SETUP STYLIZED WATER 2.0/Materials/Resources/Water_Surface_DepthGradient.png"); 
#endif
        }

        // ================= SETTINGS =================

        public void ApplySettings()
        {
            if (waterMaterial == null)
                return;

            // Ensure shared material is used
            var r = GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != waterMaterial)
                r.sharedMaterial = waterMaterial;

            // Foam style
            waterMaterial.DisableKeyword("_FOAM_STYLE_STYLE1");
            waterMaterial.DisableKeyword("_FOAM_STYLE_STYLE2");
            if (foamStyle == FoamStyle.Soft)
                waterMaterial.EnableKeyword("_FOAM_STYLE_STYLE1");
            else
                waterMaterial.EnableKeyword("_FOAM_STYLE_STYLE2");

            // Reflection texture ID (explicit & safe)
            waterMaterial.DisableKeyword("_TEXTUREID_TEX1");
            waterMaterial.DisableKeyword("_TEXTUREID_TEX2");
            waterMaterial.DisableKeyword("_TEXTUREID_TEX3");
            waterMaterial.DisableKeyword("_TEXTUREID_TEX4");

            switch (SelectedTexture)
            {
                case ReflectionTex.TEX1: waterMaterial.EnableKeyword("_TEXTUREID_TEX1"); break;
                case ReflectionTex.TEX2: waterMaterial.EnableKeyword("_TEXTUREID_TEX2"); break;
                case ReflectionTex.TEX3: waterMaterial.EnableKeyword("_TEXTUREID_TEX3"); break;
                case ReflectionTex.TEX4: waterMaterial.EnableKeyword("_TEXTUREID_TEX4"); break;
            }

            SetKeyword("_REFRACTION", enableRefraction);
            SetKeyword("_FOAM", enableFoam);
            SetKeyword("_ENABLE_WAVES", enableWaves);

            // Properties
            waterMaterial.SetFloat("_Refraction_power",Refraction_power);
            waterMaterial.SetFloat("_Smoothness", Smoothness);
            waterMaterial.SetFloat("_Darkness", Darkness);
            waterMaterial.SetFloat("_Depth1", WaterDepth);
            waterMaterial.SetFloat("_Depth_Fade", DepthFade);
            waterMaterial.SetFloat("_Metallic", Metallic);
            waterMaterial.SetFloat("_Reflect_power", ReflectionPower);
            waterMaterial.SetFloat("_Reflection_Distance", ReflectionDistance);

            waterMaterial.SetFloat("_Distortion_Power", DistortionPower);
            waterMaterial.SetFloat("_Normal_strength", NormalsPower);
            waterMaterial.SetFloat("_Texture_scale", NormalTextureScale);
            waterMaterial.SetInt("_water_movement_speed", TextureSpeed);

            waterMaterial.SetFloat("_Wave_length", WaveLength);
            waterMaterial.SetFloat("_Wave_Height", WaveHeight);
            waterMaterial.SetFloat("_Peak_Sharpness", PeakSharpness);
            waterMaterial.SetFloat("_Wave_Speed", WaveSpeed);

            waterMaterial.SetTexture("_First_Normal_Texture", FirstNormalTexture);
            waterMaterial.SetTexture("_Second_Normal_Texture", SecondNormalTexture);
            waterMaterial.SetTexture("_Foam_Texture", Foam_Texture);
                 switch (foamStyle)
                                {
                                  case FoamStyle.Soft: 
                                            waterMaterial.EnableKeyword("_FOAM_STYLE_STYLE1");

                                          waterMaterial.SetFloat("_foam_cutoff",foamCutoff); 
                                          waterMaterial.SetFloat("_Foam_Intensity",FoamIntensity);
                                          waterMaterial.SetFloat("_Foam_Speed",FoamSpeed);
                                          waterMaterial.SetFloat("_Foam_Scale",FoamScale);
                                          
                                            break;     
                                  case FoamStyle.Hard:
                                            waterMaterial.EnableKeyword("_FOAM_STYLE_STYLE2");
                                          //  FoamIntensity*=0.57f;
                                          waterMaterial.SetFloat("_foam_cutoff",foamCutoff); 
                                          waterMaterial.SetFloat("_Foam_Intensity",FoamIntensity*0.57f);
                                          waterMaterial.SetFloat("_Foam_Speed",FoamSpeed*2);
                                          waterMaterial.SetFloat("_Foam_Scale",FoamScale/2);
                                         

                                            break;      
                                    
                                } 






            if (gradientTexture != null)
                waterMaterial.SetTexture("_DepthGradientTex", gradientTexture);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(waterMaterial);
#endif
        }

        void SetKeyword(string keyword, bool state)
        {
            if (state) waterMaterial.EnableKeyword(keyword);
            else waterMaterial.DisableKeyword(keyword);
        }

        // ================= GRADIENT =================

#if UNITY_EDITOR
        public void RegenerateGradientAsAsset()
        {
            if (refractionGradient == null)
                refractionGradient = new Gradient();

            var tex = new Texture2D(GRADIENT_RESOLUTION, 1, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < GRADIENT_RESOLUTION; x++)
            {
                float t = x / (float)(GRADIENT_RESOLUTION - 1);
                tex.SetPixel(x, 0, refractionGradient.Evaluate(t));
            }

            tex.Apply();

            const string root = "Assets/Houidisoft technology/ESSW EASY SETUP STYLIZED WATER 2.0";
            const string folder = root + "/Generated";

            if (!AssetDatabase.IsValidFolder(root))
                AssetDatabase.CreateFolder("Assets/Houidisoft technology", "ESSW EASY SETUP STYLIZED WATER 2.0");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(root, "Generated");

            string safeName = gameObject.name.Replace(" ", "_");
            string path = $"{folder}/{safeName}_DepthGradient.png";

            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            gradientTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            ApplySettings();
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(waterMaterial);
            AssetDatabase.SaveAssets();
        }
#endif

        // ================= PLANAR REFLECTION =================

#if UNITY_EDITOR
        void ApplyPlanarReflectionSafe()
        {
            if (this == null) return;
            ApplyPlanarReflectionRuntime();
        }
#endif

        void ApplyPlanarReflectionRuntime()
        {
            var pr = GetComponent<PlanarReflections>();

            if (EnablePlanarRef)
            {
                if (pr == null)
                    EnsurePlanarReflectionComponent();
                else
                    pr.enabled = true;

                ForcePlanarReflectionRefresh();
            }
            else
            {
                if (pr != null)
                    pr.enabled = false;
            }
        }

        void ForcePlanarReflectionRefresh()
        {
            var pr = GetComponent<PlanarReflections>();
            if (pr == null) return;

            pr.enabled = false;
            pr.enabled = true;
        }

        void EnsurePlanarReflectionComponent()
        {
            var pr = GetComponent<PlanarReflections>();

            if (pr == null)
            {
#if UNITY_EDITOR
                Undo.AddComponent<PlanarReflections>(gameObject);
#else
                gameObject.AddComponent<PlanarReflections>();
#endif
            }
            else pr.enabled = true;
        }
    }
}