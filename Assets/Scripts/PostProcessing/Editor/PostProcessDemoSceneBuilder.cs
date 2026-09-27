using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.PostProcessing.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Core.PostProcessing.Editor
{
    public static partial class PostProcessDemoSceneBuilder
    {
        private const string DemoRoot = "Assets/PostProcessingDemo";
        private const string ProfileFolder = DemoRoot + "/Profiles";
        private const string MaterialFolder = DemoRoot + "/Materials";
        private const string TextureFolder = DemoRoot + "/Textures";
        private const string ScenePath = "Assets/Scenes/PostProcessDemo.unity";
        private const int LutSize = 32;

        private enum DemoEffectKind
        {
            Bloom,
            Vignette,
            Chromatic,
            LensDistortion,
            FilmGrain,
            ColorAdjustments,
            WhiteBalance,
            DepthOfField,
            MotionBlur,
            Tonemapping,
            ColorLookup,
            Cinematic
        }

        private sealed class DemoDefinition
        {
            public DemoEffectKind Kind { get; }
            public string Name => Kind.ToString();
            public string Label { get; }
            public string Description { get; }
            public bool AnimateCamera { get; }

            public DemoDefinition(DemoEffectKind kind, string label, string description, bool animateCamera = false)
            {
                Kind = kind;
                Label = label;
                Description = description;
                AnimateCamera = animateCamera;
            }
        }

        private static readonly DemoDefinition[] Definitions =
        {
            new(DemoEffectKind.Bloom, "BLOOM", "Bright surfaces bloom beyond their edges."),
            new(DemoEffectKind.Vignette, "VIGNETTE", "Soft edge falloff draws attention to the center."),
            new(DemoEffectKind.Chromatic, "CHROMATIC", "Color channels separate toward the frame edges."),
            new(DemoEffectKind.LensDistortion, "LENS DISTORT", "The image curves like a wide camera lens."),
            new(DemoEffectKind.FilmGrain, "FILM GRAIN", "Subtle analog noise adds texture to the image."),
            new(DemoEffectKind.ColorAdjustments, "COLOR GRADE", "Exposure, contrast, hue and filter shape the mood."),
            new(DemoEffectKind.WhiteBalance, "WHITE BALANCE", "Temperature and tint cool the environment."),
            new(DemoEffectKind.DepthOfField, "DEPTH OF FIELD", "Near and far objects blur around the focal plane."),
            new(DemoEffectKind.MotionBlur, "MOTION BLUR", "The camera moves to reveal temporal blur.", true),
            new(DemoEffectKind.Tonemapping, "TONEMAPPING", "ACES remaps highlights into a filmic range."),
            new(DemoEffectKind.ColorLookup, "COLOR LOOKUP", "A custom LUT shifts the entire color palette."),
            new(DemoEffectKind.Cinematic, "CINEMATIC MIX", "Several overrides combine into one look.")
        };

        [MenuItem("Tools/Post Processing/Build Demo Scene")]
        public static void Build()
        {
            EnsureFolders();
            var lut = GetOrCreateLut();
            var presets = CreatePresets(lut);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var environment = new GameObject("ProBuilder Environment").transform;
            CreateEnvironment(environment);
            var sceneCamera = CreateCamera();
            CreateLighting();

            var serviceObject = new GameObject("Post Process Service");
            var volume = serviceObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;
            volume.sharedProfile = GetOrCreateProfile("Baseline");
            var service = serviceObject.AddComponent<PostProcessVolumeExtension>();

            var controllerObject = new GameObject("Demo Controller");
            var controller = controllerObject.AddComponent<PostProcessDemoController>();
            CreateCanvas(controller, sceneCamera, presets);

            var scopeObject = new GameObject("Demo Lifetime Scope");
            var scope = scopeObject.AddComponent<PostProcessDemoLifetimeScope>();
            scope.ConfigureReferences(service, controller);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(scope);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = serviceObject;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "PostProcessingDemo");
            EnsureFolder(DemoRoot, "Profiles");
            EnsureFolder(DemoRoot, "Materials");
            EnsureFolder(DemoRoot, "Textures");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static Texture2D GetOrCreateLut()
        {
            var path = TextureFolder + "/TealAmberLut.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null) return texture;

            var pixels = new Color[LutSize * LutSize * LutSize];
            for (var blue = 0; blue < LutSize; blue++)
            {
                for (var green = 0; green < LutSize; green++)
                {
                    for (var red = 0; red < LutSize; red++)
                    {
                        var r = red / (float)(LutSize - 1);
                        var g = green / (float)(LutSize - 1);
                        var b = blue / (float)(LutSize - 1);
                        var x = blue * LutSize + red;
                        var y = green;
                        pixels[y * LutSize * LutSize + x] = new Color(
                            Mathf.Clamp01(r * 1.08f + g * 0.025f),
                            Mathf.Clamp01(g * 0.98f + b * 0.07f),
                            Mathf.Clamp01(b * 0.85f + g * 0.12f),
                            1f);
                    }
                }
            }

            var generated = new Texture2D(LutSize * LutSize, LutSize, TextureFormat.RGBA32, false, true);
            generated.SetPixels(pixels);
            generated.Apply();
            var absolutePath = Path.Combine(Application.dataPath, "PostProcessingDemo/Textures/TealAmberLut.png");
            File.WriteAllBytes(absolutePath, generated.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static PostProcessPreset[] CreatePresets(Texture2D lut)
        {
            var presets = new PostProcessPreset[Definitions.Length];
            for (var index = 0; index < Definitions.Length; index++)
            {
                var definition = Definitions[index];
                var name = definition.Name;
                var profile = GetOrCreateProfile(name);
                if (profile.components.Count == 0 || profile.components.Exists(component => component == null))
                {
                    profile.components.Clear();
                    ConfigureProfile(definition.Kind, profile, lut);
                }
                presets[index] = GetOrCreatePreset(name, profile);
            }

            return presets;
        }

        private static VolumeProfile GetOrCreateProfile(string name)
        {
            var path = ProfileFolder + "/" + name + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null) return profile;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        private static PostProcessPreset GetOrCreatePreset(string name, VolumeProfile profile)
        {
            var path = ProfileFolder + "/" + name + "Preset.asset";
            var preset = AssetDatabase.LoadAssetAtPath<PostProcessPreset>(path);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<PostProcessPreset>();
                AssetDatabase.CreateAsset(preset, path);
            }

            var serialized = new SerializedObject(preset);
            serialized.FindProperty("_profile").objectReferenceValue = profile;
            serialized.FindProperty("_channel").enumValueIndex = (int)PostProcessPresetChannel.Environment;
            serialized.FindProperty("_priorityOffset").floatValue = 10f;
            serialized.FindProperty("_stackable").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return preset;
        }

        private static void ConfigureProfile(DemoEffectKind kind, VolumeProfile profile, Texture2D lut)
        {
            switch (kind)
            {
                case DemoEffectKind.Bloom:
                    var bloom = profile.Add<Bloom>(true);
                    bloom.intensity.Override(2.1f);
                    bloom.threshold.Override(0.72f);
                    bloom.scatter.Override(0.82f);
                    bloom.tint.Override(new Color(0.62f, 0.9f, 1f));
                    break;
                case DemoEffectKind.Vignette:
                    var vignette = profile.Add<Vignette>(true);
                    vignette.intensity.Override(0.48f);
                    vignette.smoothness.Override(0.72f);
                    vignette.color.Override(new Color(0.08f, 0.025f, 0.15f));
                    break;
                case DemoEffectKind.Chromatic:
                    profile.Add<ChromaticAberration>(true).intensity.Override(0.85f);
                    break;
                case DemoEffectKind.LensDistortion:
                    profile.Add<LensDistortion>(true).intensity.Override(-0.42f);
                    break;
                case DemoEffectKind.FilmGrain:
                    profile.Add<FilmGrain>(true).intensity.Override(0.8f);
                    break;
                case DemoEffectKind.ColorAdjustments:
                    var color = profile.Add<ColorAdjustments>(true);
                    color.postExposure.Override(0.35f);
                    color.contrast.Override(27f);
                    color.saturation.Override(-24f);
                    color.hueShift.Override(11f);
                    color.colorFilter.Override(new Color(0.78f, 0.91f, 1f));
                    break;
                case DemoEffectKind.WhiteBalance:
                    var whiteBalance = profile.Add<WhiteBalance>(true);
                    whiteBalance.temperature.Override(-45f);
                    whiteBalance.tint.Override(26f);
                    break;
                case DemoEffectKind.DepthOfField:
                    var depthOfField = profile.Add<DepthOfField>(true);
                    depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
                    depthOfField.focusDistance.Override(14f);
                    depthOfField.aperture.Override(1.6f);
                    depthOfField.focalLength.Override(85f);
                    break;
                case DemoEffectKind.MotionBlur:
                    var motionBlur = profile.Add<MotionBlur>(true);
                    motionBlur.mode.Override(MotionBlurMode.CameraOnly);
                    motionBlur.intensity.Override(0.95f);
                    motionBlur.clamp.Override(0.08f);
                    break;
                case DemoEffectKind.Tonemapping:
                    profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                    break;
                case DemoEffectKind.ColorLookup:
                    var lookup = profile.Add<ColorLookup>(true);
                    lookup.texture.Override(lut);
                    lookup.contribution.Override(1f);
                    break;
                case DemoEffectKind.Cinematic:
                    var cinematicBloom = profile.Add<Bloom>(true);
                    cinematicBloom.intensity.Override(1.3f);
                    cinematicBloom.threshold.Override(0.8f);
                    var cinematicColor = profile.Add<ColorAdjustments>(true);
                    cinematicColor.contrast.Override(19f);
                    cinematicColor.saturation.Override(-10f);
                    profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                    var cinematicVignette = profile.Add<Vignette>(true);
                    cinematicVignette.intensity.Override(0.25f);
                    cinematicVignette.smoothness.Override(0.7f);
                    break;
            }

            for (var index = 0; index < profile.components.Count; index++)
            {
                var component = profile.components[index];
                if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile);
                EditorUtility.SetDirty(component);
            }

            EditorUtility.SetDirty(profile);
        }

        private static void CreateEnvironment(Transform root)
        {
            var floor = GetOrCreateMaterial("Floor", new Color(0.045f, 0.085f, 0.12f));
            var platform = GetOrCreateMaterial("Platform", new Color(0.14f, 0.22f, 0.27f));
            var teal = GetOrCreateMaterial("TealGlow", new Color(0.08f, 0.52f, 0.56f), new Color(0.08f, 2.6f, 2.8f));
            var amber = GetOrCreateMaterial("AmberGlow", new Color(0.8f, 0.32f, 0.09f), new Color(2.5f, 0.75f, 0.1f));
            var porcelain = GetOrCreateMaterial("Porcelain", new Color(0.62f, 0.73f, 0.71f));

            CreateShape(typeof(Cube), "Stage Floor", root, new Vector3(0f, -0.3f, 2f), new Vector3(24f, 0.4f, 18f), floor);
            CreateShape(typeof(Cube), "Central Plinth", root, new Vector3(0f, 0.2f, 2f), new Vector3(7f, 0.65f, 5f), platform);
            CreateShape(typeof(Cube), "Plinth Rim", root, new Vector3(0f, 0.59f, 2f), new Vector3(7.3f, 0.12f, 5.3f), teal);
            CreateShape(typeof(Cube), "Front Step", root, new Vector3(0f, -0.06f, -1.2f), new Vector3(4.3f, 0.26f, 1.25f), platform);

            for (var side = -1; side <= 1; side += 2)
            {
                CreateShape(typeof(Cube), $"Pillar {side}", root,
                    new Vector3(side * 5.1f, 2.2f, 3.8f), new Vector3(0.65f, 4.8f, 0.65f), platform);
                CreateShape(typeof(Cube), $"Pillar Light {side}", root,
                    new Vector3(side * 5.1f, 4.55f, 3.8f), new Vector3(0.85f, 0.12f, 0.85f), amber);
                CreateShape(typeof(Cylinder), $"Light Column {side}", root,
                    new Vector3(side * 3.25f, 1.15f, 6f), new Vector3(0.42f, 2.4f, 0.42f), teal);
                CreateShape(typeof(Sphere), $"Orb {side}", root,
                    new Vector3(side * 3.25f, 2.62f, 6f), new Vector3(0.75f, 0.75f, 0.75f), porcelain);
            }

            CreateShape(typeof(Cylinder), "Center Pedestal", root,
                new Vector3(0f, 1.15f, 2f), new Vector3(1.75f, 1.4f, 1.75f), platform);
            CreateShape(typeof(Sphere), "Hero Sphere", root,
                new Vector3(0f, 2.45f, 2f), new Vector3(2.2f, 2.2f, 2.2f), porcelain);
            CreateShape(typeof(Torus), "Halo Ring", root,
                new Vector3(0f, 2.45f, 2f), new Vector3(2.7f, 2.7f, 0.25f), amber);

            for (var index = 0; index < 7; index++)
            {
                var x = -7.5f + index * 2.5f;
                CreateShape(typeof(Cube), $"Rear Fin {index + 1}", root,
                    new Vector3(x, 1.8f, 9f), new Vector3(0.45f, 3.8f + index % 2, 0.7f), platform);
            }
        }

        private static GameObject CreateShape(
            Type shapeType,
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var mesh = ShapeFactory.Instantiate(shapeType, PivotLocation.Center);
            var shapeObject = mesh.gameObject;
            shapeObject.name = name;
            shapeObject.transform.SetParent(parent, false);
            shapeObject.transform.localPosition = position;
            shapeObject.transform.localScale = scale;
            shapeObject.GetComponent<MeshRenderer>().sharedMaterial = material;
            return shapeObject;
        }

        private static Material GetOrCreateMaterial(string name, Color baseColor, Color emission = default)
        {
            var path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", 0.66f);
            material.SetFloat("_Metallic", 0.28f);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 4.2f, -12.5f);
            cameraObject.transform.LookAt(new Vector3(0f, 1.7f, 3f));
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 52f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.055f, 0.09f);
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            return camera;
        }

        private static void CreateLighting()
        {
            var sun = new GameObject("Key Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.87f, 0.72f);
            sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(38f, -35f, 0f);

            CreatePointLight("Teal Fill", new Vector3(-5f, 3.6f, -1f), new Color(0.16f, 0.95f, 0.98f), 55f, 13f);
            CreatePointLight("Amber Fill", new Vector3(5f, 3.4f, 4f), new Color(1f, 0.46f, 0.18f), 45f, 12f);
        }

        private static void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = LightType.Point;
            light.transform.position = position;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        private static void AddToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (var index = 0; index < scenes.Count; index++)
            {
                if (scenes[index].path == path) return;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
