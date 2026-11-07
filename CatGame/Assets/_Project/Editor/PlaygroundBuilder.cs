using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatGame.EditorTools
{
    public static class PlaygroundBuilder
    {
        const string SceneFolder = "Assets/_Project/Scenes";
        const string MaterialFolder = "Assets/_Project/Art/Materials";
        const string SettingsFolder = "Assets/_Project/Settings";
        const string ScenePath = SceneFolder + "/Playground.unity";
        const string VolumeProfilePath = SettingsFolder + "/PlaygroundPost.asset";

        static Material baseMaterial;

        [MenuItem("Cat Game/Build Playground Scene")]
        static void BuildFromMenu() => BuildPlayground(interactive: true);

        // Parameterless so -executeMethod can call it. Never shows a modal: fails fast on unsaved work instead.
        public static void Build() => BuildPlayground(interactive: false);

        static void BuildPlayground(bool interactive)
        {
            if (interactive)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                if (File.Exists(ScenePath) &&
                    !EditorUtility.DisplayDialog("Rebuild playground?", $"{ScenePath} already exists and will be replaced.", "Replace", "Cancel"))
                    return;
            }
            else if (Enumerable.Range(0, EditorSceneManager.sceneCount).Any(i => EditorSceneManager.GetSceneAt(i).isDirty))
            {
                throw new System.InvalidOperationException("An open scene has unsaved changes; save or discard them before rebuilding the playground.");
            }

            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(SettingsFolder);
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            baseMaterial = GetDefaultMaterial();

            var mats = new Materials
            {
                Ground = GetMaterial("Ground", Palette.Ground),
                Structure = GetMaterial("Structure", Palette.Structure),
                CatPrimary = GetMaterial("CatPrimary", Palette.CatPrimary),
                CatSecondary = GetMaterial("CatSecondary", Palette.CatSecondary),
                Props = GetMaterial("Props", Palette.Props),
                Shadow = GetMaterial("Shadow", Palette.Shadow),
                Accent = GetMaterial("Accent", Palette.Accent),
            };

            BuildLevel(mats);
            GameObject cat = BuildCat(mats);
            BuildDummies(mats);
            SetUpLighting();
            SetUpCamera(cat.transform);
            SetUpPostProcessing();
            TitleScreenBuilder.Build(cat, Camera.main);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Selection.activeGameObject = cat;
            Debug.Log($"Playground built at {ScenePath}. Press Play: WASD move, Shift run, Space jump, Left Click swipe, Esc frees the cursor.");
        }

        struct Materials
        {
            public Material Ground, Structure, CatPrimary, CatSecondary, Props, Shadow, Accent;
        }

        static void BuildLevel(Materials m)
        {
            var level = new GameObject("Level").transform;
            CreatePrimitive("Ground", PrimitiveType.Plane, level, Vector3.zero, new Vector3(6f, 1f, 6f), m.Ground);

            CreatePrimitive("Low Platform", PrimitiveType.Cube, level, new Vector3(6f, 0.4f, 6f), new Vector3(3f, 0.8f, 3f), m.Structure);
            CreatePrimitive("High Platform", PrimitiveType.Cube, level, new Vector3(6f, 0.6f, 10f), new Vector3(3f, 1.2f, 3f), m.Structure);

            var ramp = CreatePrimitive("Ramp", PrimitiveType.Cube, level, new Vector3(-6f, 0.9f, 4f), new Vector3(3f, 0.2f, 6f), m.Structure);
            ramp.transform.rotation = Quaternion.Euler(-17f, 0f, 0f);
            CreatePrimitive("Ramp Top", PrimitiveType.Cube, level, new Vector3(-6f, 0.9f, 8.8f), new Vector3(3f, 1.8f, 4f), m.Structure);

            CreatePrimitive("Garden Wall", PrimitiveType.Cube, level, new Vector3(0f, 1f, 14f), new Vector3(12f, 2f, 0.5f), m.Structure);
            CreatePrimitive("Wall Cap", PrimitiveType.Cube, level, new Vector3(0f, 2.05f, 14f), new Vector3(12.3f, 0.12f, 0.7f), m.Shadow);

            var props = new GameObject("Props").transform;
            props.SetParent(level, false);
            var crate = CreatePrimitive("Crate", PrimitiveType.Cube, props, new Vector3(3.2f, 0.3f, 1.5f), Vector3.one * 0.6f, m.Props);
            crate.transform.rotation = Quaternion.Euler(0f, 12f, 0f);
            var crate2 = CreatePrimitive("Crate (stacked)", PrimitiveType.Cube, props, new Vector3(3.3f, 0.85f, 1.45f), Vector3.one * 0.5f, m.Props);
            crate2.transform.rotation = Quaternion.Euler(0f, -9f, 0f);
            CreatePrimitive("Barrel", PrimitiveType.Cylinder, props, new Vector3(-3f, 0.45f, 2f), new Vector3(0.6f, 0.45f, 0.6f), m.Props);
            CreatePrimitive("Yarn Ball", PrimitiveType.Sphere, props, new Vector3(-1.5f, 0.2f, 2.5f), Vector3.one * 0.4f, m.Accent);
        }

        // Proportions follow the craft floor's worked quadruped, re-derived for a cat.
        static GameObject BuildCat(Materials m)
        {
            var cat = new GameObject("Cat");
            cat.transform.position = new Vector3(0f, 0.05f, 0f);

            var body = cat.AddComponent<CharacterController>();
            body.height = 1.0f;
            body.radius = 0.3f;
            body.center = new Vector3(0f, 0.5f, 0f);
            body.stepOffset = 0.25f;

            var visuals = new GameObject("Visuals").transform;
            visuals.SetParent(cat.transform, false);
            visuals.localScale = Vector3.one * 1.25f;

            var torso = new GameObject("Torso").transform;
            torso.SetParent(visuals, false);
            torso.localPosition = new Vector3(0f, -0.08f, 0f);
            CreateVisual("Body", PrimitiveType.Sphere, torso, new Vector3(0f, 0.42f, 0f), new Vector3(0.5f, 0.42f, 0.62f), m.CatPrimary);
            CreateVisual("Chest", PrimitiveType.Sphere, torso, new Vector3(0f, 0.36f, 0.2f), new Vector3(0.34f, 0.3f, 0.3f), m.CatSecondary);

            var head = CreateVisual("Head", PrimitiveType.Sphere, torso, new Vector3(0f, 0.7f, 0.3f), new Vector3(0.36f, 0.33f, 0.34f), m.CatPrimary).transform;
            head.localRotation = Quaternion.Euler(6f, 0f, 0f);
            var muzzle = CreateVisual("Muzzle", PrimitiveType.Sphere, torso, new Vector3(0f, 0.66f, 0.46f), new Vector3(0.16f, 0.1f, 0.1f), m.CatSecondary);
            muzzle.transform.localRotation = Quaternion.Euler(-4f, 0f, 0f);
            var nose = CreateVisual("Nose", PrimitiveType.Cube, torso, new Vector3(0f, 0.69f, 0.51f), Vector3.one * 0.05f, m.Shadow);
            nose.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            CreateVisual("Eye L", PrimitiveType.Sphere, torso, new Vector3(-0.08f, 0.75f, 0.45f), Vector3.one * 0.06f, m.Shadow);
            CreateVisual("Eye R", PrimitiveType.Sphere, torso, new Vector3(0.08f, 0.75f, 0.45f), Vector3.one * 0.06f, m.Shadow);

            foreach (float side in new[] { -1f, 1f })
            {
                string label = side < 0 ? "L" : "R";
                var ear = CreateVisual($"Ear {label}", PrimitiveType.Cube, torso, new Vector3(0.11f * side, 0.88f, 0.28f), new Vector3(0.1f, 0.18f, 0.04f), m.CatPrimary);
                ear.transform.localRotation = Quaternion.Euler(0f, 0f, -12f * side);
                var inner = CreateVisual($"Ear Inner {label}", PrimitiveType.Cube, torso, new Vector3(0.11f * side, 0.87f, 0.305f), new Vector3(0.05f, 0.11f, 0.02f), m.Shadow);
                inner.transform.localRotation = Quaternion.Euler(0f, 0f, -12f * side);
            }

            var tailPivot = new GameObject("Tail Pivot").transform;
            tailPivot.SetParent(torso, false);
            tailPivot.localPosition = new Vector3(0f, 0.5f, -0.3f);
            var tail = CreateVisual("Tail", PrimitiveType.Capsule, tailPivot, new Vector3(0f, 0.172f, -0.246f), new Vector3(0.08f, 0.3f, 0.08f), m.CatPrimary);
            tail.transform.localRotation = Quaternion.Euler(-55f, 0f, 0f);
            CreateVisual("Tail Tip", PrimitiveType.Sphere, tailPivot, new Vector3(0f, 0.344f, -0.491f), Vector3.one * 0.1f, m.CatSecondary);

            var legs = new GameObject("Legs").transform;
            legs.SetParent(visuals, false);
            foreach (float x in new[] { -0.14f, 0.14f })
            foreach (float z in new[] { -0.2f, 0.2f })
            {
                var leg = CreateVisual("Leg", PrimitiveType.Capsule, legs, new Vector3(x, 0.12f, z), new Vector3(0.09f, 0.12f, 0.09f), m.CatPrimary);
                leg.transform.localRotation = Quaternion.Euler(4f * Mathf.Sign(z), 0f, 3f * Mathf.Sign(x));
                CreateVisual("Paw", PrimitiveType.Sphere, legs, new Vector3(x * 1.05f, 0.035f, z * 1.05f), Vector3.one * 0.072f, m.CatSecondary);
            }

            cat.AddComponent<CatInput>();
            cat.AddComponent<CatController>();
            cat.AddComponent<CatCombat>();
            var idle = cat.AddComponent<CatIdleMotion>();
            SetReference(idle, "torso", torso);
            SetReference(idle, "tailPivot", tailPivot);
            return cat;
        }

        static void BuildDummies(Materials m)
        {
            var parent = new GameObject("Training Dummies").transform;
            Vector3[] positions = { new(-2f, 0.6f, 5f), new(0f, 0.6f, 6f), new(2f, 0.6f, 5f) };
            for (int i = 0; i < positions.Length; i++)
            {
                var dummy = CreatePrimitive($"Dummy {i + 1}", PrimitiveType.Capsule, parent, positions[i], Vector3.one * 0.6f, m.Props);
                var rb = dummy.AddComponent<Rigidbody>();
                rb.mass = 2f;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                dummy.AddComponent<Health>();
                dummy.AddComponent<DamageFlash>();
            }
        }

        static void SetUpLighting()
        {
            Light sun = Object.FindObjectsByType<Light>().First(l => l.type == LightType.Directional);
            sun.color = Palette.Sun;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.AmbientSky;
            RenderSettings.ambientEquatorColor = Palette.AmbientEquator;
            RenderSettings.ambientGroundColor = Palette.AmbientGround;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Palette.AmbientSky;
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 90f;

            RenderSettings.skybox = GetSkybox();

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.shadowDistance = 40f;
                EditorUtility.SetDirty(urp);
                if (!urp.supportsHDR)
                    Debug.LogWarning("HDR is off on the active URP asset; Bloom's 1.1 threshold will never fire.");
            }
        }

        static void SetUpCamera(Transform target)
        {
            Camera cam = Camera.main;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            cam.transform.SetPositionAndRotation(target.position + new Vector3(0f, 1.64f, -3.86f), Quaternion.Euler(15f, 0f, 0f));
            var rig = cam.gameObject.AddComponent<ThirdPersonCamera>();
            SetReference(rig, "target", target);
        }

        static void SetUpPostProcessing()
        {
            AssetDatabase.DeleteAsset(VolumeProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);

            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var bloom = profile.Add<Bloom>();
            bloom.intensity.Override(0.3f);
            bloom.threshold.Override(1.1f);

            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.2f);

            var color = profile.Add<ColorAdjustments>();
            color.saturation.Override(10f);
            color.contrast.Override(5f);

            foreach (var component in profile.components)
            {
                if (!AssetDatabase.Contains(component))
                    AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        static Material GetSkybox()
        {
            string path = $"{MaterialFolder}/Sky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                Shader shader = Shader.Find("Skybox/Procedural");
                if (shader == null)
                    throw new System.InvalidOperationException("Skybox/Procedural shader not found.");
                sky = new Material(shader) { name = "Sky" };
                AssetDatabase.CreateAsset(sky, path);
            }
            sky.SetColor("_SkyTint", Palette.SkyTint);
            sky.SetColor("_GroundColor", Palette.AmbientEquator);
            sky.SetFloat("_Exposure", 1.2f);
            sky.SetFloat("_AtmosphereThickness", 0.8f);
            EditorUtility.SetDirty(sky);
            return sky;
        }

        static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static GameObject CreateVisual(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = CreatePrimitive(name, type, parent, position, scale, material);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static Material GetDefaultMaterial()
        {
            Material material = GraphicsSettings.currentRenderPipeline != null
                ? GraphicsSettings.currentRenderPipeline.defaultMaterial
                : null;
            if (material == null)
                throw new System.InvalidOperationException("No render pipeline default material; is URP active?");
            return material;
        }

        static Material GetMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(baseMaterial) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static void SetReference(Object component, string field, Object value)
        {
            var so = new SerializedObject(component);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path))
                return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
