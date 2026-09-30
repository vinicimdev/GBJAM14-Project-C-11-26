using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Polyzero.ElectricCables.Editor
{
    [InitializeOnLoad]
    public static class ElectricCablePipelineAdapter
    {
        private const string DemoScenePath = "Assets/Polyzero/ElectricCables/Demo/DemoHDRP.unity";
        private const string CableMaterialPath = "Assets/Polyzero/ElectricCables/Demo/Material.mat";
        private const string PoleMaterialPath = "Assets/Polyzero/ElectricCables/Demo/Materials/Electric_poleHDRP.mat";
        private const string GroundMaterialPath = "Assets/Polyzero/ElectricCables/Demo/Materials/Ground_HDRP.mat";
        private const string PoleBaseMapPath = "Assets/Polyzero/ElectricCables/Demo/Textures/Electric_pole_BaseMap.png";
        private const string PoleNormalMapPath = "Assets/Polyzero/ElectricCables/Demo/Textures/Electric_pole_Normal.png";
        private const string PoleMaskMapPath = "Assets/Polyzero/ElectricCables/Demo/Textures/Electric_pole_MaskMap.png";
        private const string GroundBaseMapPath = "Assets/Polyzero/ElectricCables/Demo/Textures/Ground_BaseMap.png";
        private const string SessionKeyPrefix = "Polyzero.ElectricCables.PipelineAdapter.v1.";

        private enum PipelineKind
        {
            BuiltIn,
            Universal,
            HighDefinition,
            Unsupported
        }

        static ElectricCablePipelineAdapter()
        {
            EditorApplication.delayCall += ApplyOncePerSession;
        }

        [MenuItem("Tools/Polyzero/Electric Cables/Refresh Render Pipeline Compatibility")]
        public static void ApplyNow()
        {
            ApplyCompatibility(true, false);
        }

        private static void ApplyOncePerSession()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ApplyOncePerSession;
                return;
            }

            PipelineKind pipeline = DetectPipeline();
            string sessionKey = SessionKeyPrefix + Application.dataPath.GetHashCode() + "." + pipeline;
            if (SessionState.GetBool(sessionKey, false))
            {
                return;
            }

            SessionState.SetBool(sessionKey, true);
            ApplyCompatibility(false, true);
        }

        private static void ApplyCompatibility(bool logResult, bool automatic)
        {
            PipelineKind pipeline = DetectPipeline();
            Shader shader = FindCompatibleShader(pipeline);
            if (shader == null)
            {
                Debug.LogWarning("[Electric Cables] No compatible Lit shader was found for the active render pipeline.");
                return;
            }

            int changedMaterials = 0;
            changedMaterials += ConfigureMaterial(
                CableMaterialPath,
                shader,
                new Color(0.4528302f, 0.4528302f, 0.4528302f, 1f),
                0.074f,
                null,
                null,
                null);
            changedMaterials += ConfigureMaterial(
                PoleMaterialPath,
                shader,
                Color.white,
                0.5f,
                AssetDatabase.LoadAssetAtPath<Texture2D>(PoleBaseMapPath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(PoleNormalMapPath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(PoleMaskMapPath));
            changedMaterials += ConfigureMaterial(
                GroundMaterialPath,
                shader,
                Color.white,
                0f,
                AssetDatabase.LoadAssetAtPath<Texture2D>(GroundBaseMapPath),
                null,
                null);

            bool sceneChanged = AdaptDemoScene(pipeline, automatic);
            if (changedMaterials > 0)
            {
                AssetDatabase.SaveAssets();
            }

            if (logResult || changedMaterials > 0 || sceneChanged)
            {
                Debug.Log(
                    "[Electric Cables] Render pipeline compatibility ready for " +
                    GetPipelineDisplayName(pipeline) + ". Materials updated: " + changedMaterials +
                    ", demo updated: " + (sceneChanged ? "yes" : "no") + ".");
            }
        }

        private static int ConfigureMaterial(
            string path,
            Shader shader,
            Color color,
            float smoothness,
            Texture baseMap,
            Texture normalMap,
            Texture maskMap)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                return 0;
            }

            bool changed = material.shader != shader;
            if (changed)
            {
                material.shader = shader;
            }

            changed |= SetColorIfDifferent(material, "_BaseColor", color);
            changed |= SetColorIfDifferent(material, "_Color", color);
            changed |= SetFloatIfDifferent(material, "_Metallic", 0f);
            changed |= SetFloatIfDifferent(material, "_Smoothness", smoothness);
            changed |= SetFloatIfDifferent(material, "_Glossiness", smoothness);
            changed |= SetTextureIfDifferent(material, "_BaseColorMap", baseMap);
            changed |= SetTextureIfDifferent(material, "_BaseMap", baseMap);
            changed |= SetTextureIfDifferent(material, "_MainTex", baseMap);
            changed |= SetTextureIfDifferent(material, "_NormalMap", normalMap);
            changed |= SetTextureIfDifferent(material, "_BumpMap", normalMap);
            changed |= SetTextureIfDifferent(material, "_MaskMap", maskMap);
            changed |= SetTextureIfDifferent(material, "_MetallicGlossMap", maskMap);

            if (normalMap != null)
            {
                material.EnableKeyword("_NORMALMAP");
            }

            if (maskMap != null)
            {
                material.EnableKeyword("_MASKMAP");
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            if (!changed)
            {
                return 0;
            }

            EditorUtility.SetDirty(material);
            return 1;
        }

        private static bool AdaptDemoScene(PipelineKind pipeline, bool automatic)
        {
            if (pipeline == PipelineKind.HighDefinition ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath) == null)
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByPath(DemoScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (!openedHere && automatic && scene.isDirty)
            {
                return false;
            }

            if (openedHere)
            {
                scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Additive);
            }

            bool changed = false;
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    GameObject root = roots[rootIndex];
                    if (root == null)
                    {
                        continue;
                    }

                    if (root.name == "Sky and Fog Global Volume")
                    {
                        UnityEngine.Object.DestroyImmediate(root);
                        changed = true;
                        continue;
                    }

                    Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                    for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                    {
                        GameObject current = transforms[transformIndex].gameObject;
                        int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(current);
                        if (missingCount > 0)
                        {
                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(current);
                            changed = true;
                        }

                        Component[] components = current.GetComponents<Component>();
                        for (int componentIndex = components.Length - 1; componentIndex >= 0; componentIndex--)
                        {
                            Component component = components[componentIndex];
                            if (component == null || !IsHighDefinitionComponent(component.GetType()))
                            {
                                continue;
                            }

                            UnityEngine.Object.DestroyImmediate(component);
                            changed = true;
                        }

                        if (current.name == "Directional Light" && current.TryGetComponent(out Light light))
                        {
                            const float compatibleIntensity = 1.2f;
                            if (!Mathf.Approximately(light.intensity, compatibleIntensity))
                            {
                                light.intensity = compatibleIntensity;
                                changed = true;
                            }
                        }
                    }
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            return changed;
        }

        private static bool IsHighDefinitionComponent(Type type)
        {
            string assemblyName = type.Assembly.GetName().Name;
            return assemblyName.IndexOf("HighDefinition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   type.FullName.IndexOf("HighDefinition", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static PipelineKind DetectPipeline()
        {
            RenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline;
            if (asset == null)
            {
                return PipelineKind.BuiltIn;
            }

            string typeName = asset.GetType().Name;
            if (typeName.IndexOf("HDRenderPipeline", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return PipelineKind.HighDefinition;
            }

            if (typeName.IndexOf("UniversalRenderPipeline", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return PipelineKind.Universal;
            }

            return PipelineKind.Unsupported;
        }

        private static Shader FindCompatibleShader(PipelineKind pipeline)
        {
            switch (pipeline)
            {
                case PipelineKind.HighDefinition:
                    return FindFirstSupportedShader("HDRP/Lit", "HDRP/Unlit");
                case PipelineKind.Universal:
                    return FindFirstSupportedShader("Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit");
                case PipelineKind.BuiltIn:
                    return FindFirstSupportedShader("Standard", "Legacy Shaders/Diffuse", "Unlit/Color");
                default:
                    return null;
            }
        }

        private static Shader FindFirstSupportedShader(params string[] shaderNames)
        {
            for (int i = 0; i < shaderNames.Length; i++)
            {
                Shader shader = Shader.Find(shaderNames[i]);
                if (shader != null && shader.isSupported && shader.name != "Hidden/InternalErrorShader")
                {
                    return shader;
                }
            }

            return null;
        }

        private static string GetPipelineDisplayName(PipelineKind pipeline)
        {
            switch (pipeline)
            {
                case PipelineKind.BuiltIn:
                    return "Built-in Render Pipeline";
                case PipelineKind.Universal:
                    return "Universal Render Pipeline";
                case PipelineKind.HighDefinition:
                    return "High Definition Render Pipeline";
                default:
                    return "an unsupported custom render pipeline";
            }
        }

        private static bool SetColorIfDifferent(Material material, string propertyName, Color value)
        {
            if (!material.HasProperty(propertyName) || material.GetColor(propertyName) == value)
            {
                return false;
            }

            material.SetColor(propertyName, value);
            return true;
        }

        private static bool SetFloatIfDifferent(Material material, string propertyName, float value)
        {
            if (!material.HasProperty(propertyName) || Mathf.Approximately(material.GetFloat(propertyName), value))
            {
                return false;
            }

            material.SetFloat(propertyName, value);
            return true;
        }

        private static bool SetTextureIfDifferent(Material material, string propertyName, Texture value)
        {
            if (!material.HasProperty(propertyName) || material.GetTexture(propertyName) == value)
            {
                return false;
            }

            material.SetTexture(propertyName, value);
            return true;
        }
    }
}
