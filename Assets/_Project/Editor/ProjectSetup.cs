using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace ARFunctionLab.Editor
{
    public static class ProjectSetup
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/SurfaceDemo.unity";

        public static void ImportFonts()
        {
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) return;
            string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui").resolvedPath;
            UnityEditor.AssetPackage.Package.Import(packageRoot + "/Package Resources/TMP Essential Resources.unitypackage", false);
            AssetDatabase.Refresh();
        }

        [MenuItem("AR Function Lab/Create Demo Scene")]
        public static void CreateDemo()
        {
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Fonts");
            Directory.CreateDirectory(Root + "/Settings");
            AssetDatabase.Refresh();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/MathFont.asset");
            if (font == null)
            {
                Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
                if (source == null) throw new InvalidOperationException("Import TMP Essential Resources before creating the scene.");
                font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                font.name = "MathFont";
                AssetDatabase.CreateAsset(font, Root + "/Fonts/MathFont.asset");
                AssetDatabase.AddObjectToAsset(font.material, font);
                for (int i = 0; i < font.atlasTextures.Length; i++) AssetDatabase.AddObjectToAsset(font.atlasTextures[i], font);
                font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+-*/=().,:[]|π×−√²ⁿ⁻¹");
            }
            Material surface = MaterialAsset("Surface", "AR Function Lab/Surface");
            Material line = MaterialAsset("Line", "AR Function Lab/Line");
            if (!File.Exists(ScenePath))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                LabBootstrap bootstrap = new GameObject("AR Function Lab").AddComponent<LabBootstrap>();
                bootstrap.font = font;
                bootstrap.surfaceMaterial = surface;
                bootstrap.lineMaterial = line;
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            }
            else EditorSceneManager.OpenScene(ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            ConfigurePlayer();
            ConfigureXR();
            AssetDatabase.SaveAssets();
            Debug.Log("AR Function Lab scene and project settings are ready.");
        }

        private static Material MaterialAsset(string name, string shaderName)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Missing shader: " + shaderName);
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ConfigurePlayer()
        {
            EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions");
            PlayerSettings.companyName = "AR Function Lab";
            PlayerSettings.productName = "AR Function Lab";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.arfunctionlab.weekone");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            SerializedObject player = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty input = player.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 1; player.ApplyModifiedPropertiesWithoutUndo(); }
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (pipeline != null)
            {
                pipeline.supportsHDR = false;
                pipeline.msaaSampleCount = 4;
                pipeline.renderScale = 1;
                GraphicsSettings.defaultRenderPipeline = pipeline;
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i);
                    QualitySettings.renderPipeline = pipeline;
                }
                EditorUtility.SetDirty(pipeline);
            }
        }

        private static void ConfigureXR()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogWarning("Android Build Support is not installed. You can still use Play mode in the Unity Editor. Quest settings will be completed by Build Quest APK after installing the Android modules.");
                return;
            }
            XRGeneralSettingsPerBuildTarget settings;
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out settings))
            {
                settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(settings, Root + "/Settings/XRSettings.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, settings, true);
            }
            if (!settings.HasSettingsForBuildTarget(BuildTargetGroup.Android)) settings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            if (!settings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android)) settings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            XRGeneralSettings android = settings.SettingsForBuildTarget(BuildTargetGroup.Android);
            android.InitManagerOnStart = true;
            if (!XRPackageMetadataStore.AssignLoader(android.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android))
                throw new InvalidOperationException("Could not assign the Android OpenXR loader.");
            OpenXRFeatureSetManager.InitializeFeatureSets();
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            OpenXRSettings openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXR == null) throw new InvalidOperationException("OpenXR Android settings were not created.");
            openXR.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            OpenXRFeature[] features = openXR.GetFeatures();
            int enabled = 0;
            for (int i = 0; i < features.Length; i++)
            {
                string name = features[i].GetType().Name;
                // Meta's hidden lifecycle feature also includes its native AR library in the APK.
                bool required = name == "MetaQuestFeature" || name == "ARSessionFeature" || name == "ARCameraFeature" || name == "OculusTouchControllerProfile" || name == "OpenXRCompositionLayersFeature" || name == "OpenXRLifeCycleFeature";
                features[i].enabled = required;
                if (required && features[i].enabled) enabled++;
                EditorUtility.SetDirty(features[i]);
            }
            if (enabled != 6) throw new InvalidOperationException("Required Quest OpenXR features are missing or disabled, including the Meta lifecycle dependency.");
            EditorUtility.SetDirty(openXR);
            EditorUtility.SetDirty(android);
            EditorUtility.SetDirty(android.Manager);
            EditorUtility.SetDirty(settings);
        }

        [MenuItem("AR Function Lab/Build Quest APK")]
        public static void BuildQuest()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Install Android Build Support, Android SDK and NDK Tools, and OpenJDK for Unity 6000.6.6f1 in Unity Hub.");
            ConfigurePlayer();
            ConfigureXR();
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Builds/Quest");
            BuildReport report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/Quest/ARFunctionLab.apk", BuildTarget.Android, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Quest APK build failed.");
        }
    }
}
