#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SkaterSeoul.Editor
{
    public static class SkaterSeoulSceneBuilder
    {
        private const string Root = "Assets/SkaterSeoul";
        private const string ModelPath = Root + "/Art/Blender/SeoulSkater.fbx";
        private const string IdlePath = Root + "/ThirdParty/KenneyAnimatedProtagonists/Animations/idle.fbx";
        private const string RunPath = Root + "/ThirdParty/KenneyAnimatedProtagonists/Animations/run.fbx";
        private const string JumpPath = Root + "/ThirdParty/KenneyAnimatedProtagonists/Animations/jump.fbx";
        private const string ConcreteBaseColorPath = Root + "/Art/GeneratedSeoul/concrete_pavement_base_color_2k.png";
        private const string ConcreteNormalPath = Root + "/Art/GeneratedSeoul/concrete_pavement_normal_gl_2k.png";
        private const string ConcreteMaskPath = Root + "/Art/GeneratedSeoul/concrete_pavement_mask_2k.png";
        private const string PanoramaPath = Root + "/Art/GeneratedSeoul/seoul_han_riverside_panorama.png";
        private const string EnvironmentPath = Root + "/Art/Blender/HangangRiversideSet.fbx";
        private const string BoardPath = Root + "/Art/Blender/SeoulBoard.fbx";
        private const string DeckArtworkPath = Root + "/Art/GeneratedSeoul/seoul_deck_artwork.png";
        private const string ControllerPath = Root + "/Animations/SkaterLocomotion.controller";
        private const string AvatarPath = Root + "/Animations/SeoulSkaterHumanoid.asset";
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        [MenuItem("Tools/SKATER SEOUL/1 - Build Seoul Visual Slice")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareAssets();
            CreateScene(true);
        }

        public static void BuildBatch()
        {
            PrepareAssets();
            CreateScene(false);
        }

        [MenuItem("Tools/SKATER SEOUL/2 - Prepare CC0 Skater Rig")]
        public static void PrepareAssets()
        {
            EnsureFolders();
            foreach (string path in new[] { ModelPath, IdlePath, RunPath, JumpPath, ConcreteBaseColorPath, ConcreteNormalPath,
                ConcreteMaskPath, PanoramaPath, DeckArtworkPath, EnvironmentPath, BoardPath })
                if (!File.Exists(path)) throw new FileNotFoundException("Required visual-slice asset is missing: " + path);

            ConfigureTexture(ConcreteBaseColorPath, TextureWrapMode.Repeat, 2048, FilterMode.Trilinear);
            ConfigureTexture(ConcreteNormalPath, TextureWrapMode.Repeat, 2048, FilterMode.Trilinear, false, TextureImporterType.NormalMap);
            ConfigureTexture(ConcreteMaskPath, TextureWrapMode.Repeat, 2048, FilterMode.Trilinear, false);
            ConfigureTexture(PanoramaPath, TextureWrapMode.Clamp, 4096, FilterMode.Bilinear);
            ConfigureTexture(DeckArtworkPath, TextureWrapMode.Clamp, 2048, FilterMode.Bilinear);
            ModelImporter model = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            if (model.animationType != ModelImporterAnimationType.Generic ||
                model.avatarSetup != ModelImporterAvatarSetup.NoAvatar || model.importAnimation || model.optimizeGameObjects)
            {
                model.animationType = ModelImporterAnimationType.Generic;
                model.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                model.importAnimation = false;
                model.optimizeGameObjects = false;
                model.SaveAndReimport();
            }
            ModelImporter board = (ModelImporter)AssetImporter.GetAtPath(BoardPath);
            if (board.animationType != ModelImporterAnimationType.Generic || board.avatarSetup != ModelImporterAvatarSetup.NoAvatar || board.importAnimation)
            {
                board.animationType = ModelImporterAnimationType.Generic;
                board.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                board.importAnimation = false;
                board.SaveAndReimport();
            }
            BuildHoodieAvatar();

            foreach (string path in new[] { IdlePath, RunPath, JumpPath }) ConfigureAnimation(path);
            AnimationClip idle = FindClip(IdlePath);
            AnimationClip run = FindClip(RunPath);
            AnimationClip jump = FindClip(JumpPath);
            if (idle == null || run == null || jump == null)
                throw new InvalidOperationException("The imported skater FBX files need idle, run and jump animation clips.");
            CreateAnimatorController(idle, run, jump);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureTexture(string path, TextureWrapMode wrap, int size, FilterMode filter,
            bool srgb = true, TextureImporterType type = TextureImporterType.Default)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            bool changed = importer.textureType != type || importer.sRGBTexture != srgb ||
                !importer.mipmapEnabled || importer.wrapMode != wrap || importer.filterMode != filter ||
                importer.maxTextureSize != size || importer.textureCompression != TextureImporterCompression.CompressedHQ ||
                (type == TextureImporterType.NormalMap && importer.flipGreenChannel);
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            if (type == TextureImporterType.NormalMap) importer.flipGreenChannel = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = wrap;
            importer.filterMode = filter;
            importer.maxTextureSize = size;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (changed) importer.SaveAndReimport();
        }

        private static void ConfigureAnimation(string path)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Cannot import animation: " + path);
            bool jump = path == JumpPath;
            bool changed = importer.animationType != ModelImporterAnimationType.Human ||
                importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel || !importer.importAnimation;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                if (clip.loopTime == jump) changed = true;
                clip.loopTime = !jump;
                clip.loopPose = !jump;
            }
            importer.clipAnimations = clips;
            if (changed) importer.SaveAndReimport();
        }

        private static void BuildHoodieAvatar()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            try
            {
                RepairHoodieHierarchy(model);
                Transform[] transforms = model.GetComponentsInChildren<Transform>(true);
                SkeletonBone[] skeleton = new SkeletonBone[transforms.Length];
                for (int i = 0; i < transforms.Length; i++)
                    skeleton[i] = new SkeletonBone { name = transforms[i].name, position = transforms[i].localPosition,
                        rotation = transforms[i].localRotation, scale = transforms[i].localScale };
                HumanDescription description = new HumanDescription { human = BuildHoodieHumanMap(), skeleton = skeleton,
                    upperArmTwist = .5f, lowerArmTwist = .5f, upperLegTwist = .5f, lowerLegTwist = .5f,
                    armStretch = .05f, legStretch = .05f, feetSpacing = 0f, hasTranslationDoF = false };
                Avatar avatar = AvatarBuilder.BuildHumanAvatar(model, description);
                if (avatar == null || !avatar.isValid || !avatar.isHuman)
                {
                    if (avatar != null) UnityEngine.Object.DestroyImmediate(avatar);
                    throw new InvalidOperationException("Could not build a Humanoid Avatar from the repaired Quaternius hoodie rig.");
                }
                avatar.name = "Seoul Skater Humanoid Avatar";
                if (AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath) != null) AssetDatabase.DeleteAsset(AvatarPath);
                AssetDatabase.CreateAsset(avatar, AvatarPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(model); }
        }

        private static HumanBone[] BuildHoodieHumanMap()
        {
            string[,] map =
            {
                { "Hips", "Hips" }, { "Spine", "Abdomen" }, { "Chest", "Chest" }, { "Neck", "Neck" }, { "Head", "Head" },
                { "LeftShoulder", "Shoulder.L" }, { "LeftUpperArm", "UpperArm.L" }, { "LeftLowerArm", "LowerArm.L" },
                { "LeftHand", "Wrist.L" }, { "RightShoulder", "Shoulder.R" }, { "RightUpperArm", "UpperArm.R" },
                { "RightLowerArm", "LowerArm.R" }, { "RightHand", "Wrist.R" }, { "LeftUpperLeg", "UpperLeg.L" },
                { "LeftLowerLeg", "LowerLeg.L" }, { "LeftFoot", "Foot.L" }, { "LeftToes", "Foot.L_end" },
                { "RightUpperLeg", "UpperLeg.R" }, { "RightLowerLeg", "LowerLeg.R" }, { "RightFoot", "Foot.R" },
                { "RightToes", "Foot.R_end" }
            };
            HumanBone[] bones = new HumanBone[map.GetLength(0)];
            for (int i = 0; i < bones.Length; i++)
                bones[i] = new HumanBone { humanName = map[i, 0], boneName = map[i, 1], limit = new HumanLimit { useDefaultValues = true } };
            return bones;
        }

        private static void RepairHoodieHierarchy(GameObject model)
        {
            Transform hips = FindBone(model, "Hips");
            ReparentPreservingPose(FindBone(model, "UpperLeg.L"), hips);
            ReparentPreservingPose(FindBone(model, "UpperLeg.R"), hips);
            ReparentPreservingPose(FindBone(model, "Foot.L"), FindBone(model, "LowerLeg.L"));
            ReparentPreservingPose(FindBone(model, "Foot.R"), FindBone(model, "LowerLeg.R"));
        }

        private static Transform FindBone(GameObject model, string name)
        {
            foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            throw new InvalidOperationException("Required Quaternius hoodie bone is missing: " + name);
        }

        private static void ReparentPreservingPose(Transform bone, Transform parent)
        {
            if (bone.parent != parent) bone.SetParent(parent, true);
        }

        private static AnimationClip FindClip(string path)
        {
            AnimationClip longest = null;
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                    (longest == null || clip.length > longest.length)) longest = clip;
            return longest;
        }

        private static void CreateAnimatorController(AnimationClip idle, AnimationClip run, AnimationClip jump)
        {
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null) AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Push", AnimatorControllerParameterType.Float);
            controller.AddParameter("Trick", AnimatorControllerParameterType.Trigger);
            BlendTree blend = new BlendTree { name = "Ride Idle To Cruise", blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed", useAutomaticThresholds = false };
            blend.AddChild(idle, 0f);
            blend.AddChild(run, 1f);
            AssetDatabase.AddObjectToAsset(blend, controller);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState ride = machine.AddState("Ride", new Vector3(250f, 80f, 0f));
            ride.motion = blend;
            machine.defaultState = ride;
            AnimatorState trick = machine.AddState("Trick", new Vector3(550f, 80f, 0f));
            trick.motion = jump;
            AnimatorStateTransition enter = machine.AddAnyStateTransition(trick);
            enter.hasExitTime = false;
            enter.duration = 0.06f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Trick");
            AnimatorStateTransition exit = trick.AddTransition(ride);
            exit.hasExitTime = true;
            exit.exitTime = 0.92f;
            exit.duration = 0.08f;
            AnimatorControllerLayer layer = controller.layers[0];
            layer.iKPass = true;
            controller.layers = new[] { layer };
            EditorUtility.SetDirty(controller);
        }

        private static void CreateScene(bool interactive)
        {
            Materials.Clear();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string scenePath = NextScenePath();
            ConfigureLighting();

            Texture2D concrete = AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteBaseColorPath);
            Texture2D concreteNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteNormalPath);
            Texture2D concreteMask = AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteMaskPath);
            if (concrete == null || concreteNormal == null || concreteMask == null)
                throw new InvalidOperationException("The concrete PBR texture set failed to import.");
            Texture2D panorama = AssetDatabase.LoadAssetAtPath<Texture2D>(PanoramaPath);
            Material concreteMat = CreateMaterial("Hangang Plaza Concrete", Color.white, 1f);
            concreteMat.SetTexture("_BaseMap", concrete);
            concreteMat.SetTexture("_BumpMap", concreteNormal);
            concreteMat.SetTexture("_MetallicGlossMap", concreteMask);
            concreteMat.SetTexture("_OcclusionMap", concreteMask);
            concreteMat.SetFloat("_Metallic", 0f);
            concreteMat.SetFloat("_Smoothness", 1f);
            concreteMat.SetFloat("_SmoothnessTextureChannel", 0f);
            concreteMat.SetFloat("_BumpScale", 0.38f);
            concreteMat.SetFloat("_OcclusionStrength", 0.55f);
            concreteMat.EnableKeyword("_NORMALMAP");
            concreteMat.EnableKeyword("_METALLICSPECGLOSSMAP");
            concreteMat.EnableKeyword("_OCCLUSIONMAP");
            Vector2 pavementTiling = new Vector2(110f / 1.8f, 6f / 1.8f);
            concreteMat.SetTextureScale("_BaseMap", pavementTiling);
            concreteMat.SetTextureScale("_BumpMap", pavementTiling);
            concreteMat.SetTextureScale("_MetallicGlossMap", pavementTiling);
            concreteMat.SetTextureScale("_OcclusionMap", pavementTiling);
            EditorUtility.SetDirty(concreteMat);
            Material warmStone = CreateMaterial("Sandstone", new Color(0.62f, 0.55f, 0.44f), 0.08f);
            Material paleStone = CreateMaterial("Pale Concrete", new Color(0.74f, 0.70f, 0.62f), 0.1f);
            Material darkMetal = CreateMaterial("Park Metal", new Color(0.24f, 0.31f, 0.31f), 0.5f);
            Material warmMetal = CreateMaterial("Warm Metal", new Color(0.47f, 0.38f, 0.28f), 0.58f);
            Material leafDark = CreateMaterial("Ginkgo Deep Green", new Color(0.20f, 0.31f, 0.25f), 0.0f);
            Material leafLight = CreateMaterial("Ginkgo Light Green", new Color(0.48f, 0.51f, 0.35f), 0.0f);
            Material water = CreateMaterial("Han River", new Color(0.22f, 0.38f, 0.39f), 0.2f);
            Material sky = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Original Seoul Panorama" };
            sky.SetTexture("_BaseMap", panorama);
            sky.SetColor("_BaseColor", Color.white);
            sky.SetFloat("_Cull", 0f);
            sky = SaveMaterial(sky, "OriginalSeoulPanorama");

            CreateFloor(concreteMat);
            CreateRiverAndSky(water, sky);
            GameObject park = new GameObject("Han River Skatepark Set");
            CreateBlenderEnvironment(park.transform);
            CreateLedge(park.transform, 3.2f, 0f, 0f, 2.3f, 0.92f, 0.26f, paleStone, darkMetal);
            CreateRiderAndBoard();
            GameObject cameraGo = CreateCamera();
            Transform panoramaTransform = GameObject.Find("Original Seoul riverside panorama")?.transform;
            if (panoramaTransform != null)
            {
                Camera camera = cameraGo.GetComponent<Camera>();
                panoramaTransform.SetParent(cameraGo.transform, false);
                panoramaTransform.localPosition = new Vector3(0f, 0f, 90f);
                panoramaTransform.localScale = new Vector3(camera.orthographicSize * 6f, camera.orthographicSize * 2f, 1f);
            }
            SkaterSeoulPrototype prototype = GameObject.Find("Skater Physics Root").GetComponent<SkaterSeoulPrototype>();
            prototype.followCamera = cameraGo.GetComponent<Camera>();
            cameraGo.GetComponent<SkaterSeoulCameraRig>().target = prototype.GetComponent<Rigidbody>();
            foreach (Renderer renderer in park.GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled) LogScreenBounds(renderer, cameraGo.GetComponent<Camera>());

            SavePrefab(park, Root + "/Prefabs/HanRiverSkatepark.prefab");
            EditorUtility.SetDirty(prototype.gameObject);
            if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException("Failed to save scene at " + scenePath);
            AddBuildScene(scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("SKATER SEOUL vertical slice created: " + scenePath);
            if (interactive)
                EditorUtility.DisplayDialog("Visual slice ready", "A new URP scene and prefabs were created without replacing older scenes:\n" + scenePath +
                    "\nPlay: hold PUSH and swipe up, up-left, or up-right.", "OK");
        }

        private static void ConfigureLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.60f, 0.64f, 0.61f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.45f, 0.40f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.23f, 0.21f);
            RenderSettings.ambientIntensity = 0.78f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.63f, 0.58f, 0.50f);
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 165f;
            GameObject sun = new GameObject("Han River Sunset Key Light");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.89f, 0.75f);
            light.intensity = 1.18f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.52f;
            light.shadowResolution = LightShadowResolution.High;
            sun.transform.rotation = Quaternion.Euler(33f, -31f, 12f);
            QualitySettings.shadowDistance = 48f;
            QualitySettings.antiAliasing = 2;
        }

        private static void CreateFloor(Material material)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Textured plaza · collision surface";
            floor.transform.position = new Vector3(40f, -0.006f, -1.8f);
            floor.transform.localScale = new Vector3(11f, 1f, .6f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = material;
            floor.GetComponent<MeshRenderer>().receiveShadows = true;
            Material slabEdge = CreateMaterial("Hangang plaza cut concrete edge", new Color(.42f, .47f, .46f), .02f);
            CreateBox("Continuous concrete plaza edge", new Vector3(40f, -.08f, -1.8f),
                new Vector3(110f, .14f, 6f), slabEdge, false);
        }

        private static void CreateRiverAndSky(Material water, Material sky)
        {
            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "Original Seoul riverside panorama";
            backdrop.transform.position = new Vector3(0f, 2.9f, 58f);
            backdrop.transform.localScale = new Vector3(100f, 33.33f, 1f);
            UnityEngine.Object.DestroyImmediate(backdrop.GetComponent<Collider>());
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = sky;
            backdrop.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            backdrop.GetComponent<MeshRenderer>().receiveShadows = false;
        }

        private static void LogScreenBounds(Renderer renderer, Camera camera)
        {
            Bounds bounds = renderer.bounds;
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                        Vector3 screen = camera.WorldToScreenPoint(corner);
                        minX = Mathf.Min(minX, screen.x); maxX = Mathf.Max(maxX, screen.x);
                        minY = Mathf.Min(minY, screen.y); maxY = Mathf.Max(maxY, screen.y);
                    }
            Debug.Log("SKATER ENV SCREEN BOUNDS | " + renderer.name + " | x=" + minX.ToString("F0") + ".." + maxX.ToString("F0") +
                " y=" + minY.ToString("F0") + ".." + maxY.ToString("F0"));
        }

        private static void CreateBlenderEnvironment(Transform parent)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPath);
            if (asset == null) throw new InvalidOperationException("Blender Hangang environment failed to import: " + EnvironmentPath);
            GameObject environment = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            environment.name = "Blender Hangang promenade, bridge and skate obstacles";
            environment.transform.SetParent(parent, false);
            environment.transform.localPosition = Vector3.zero;
            environment.transform.localRotation = Quaternion.identity;
            environment.transform.localScale = new Vector3(1f, 1f, -1f);

            foreach (Renderer renderer in environment.GetComponentsInChildren<Renderer>(true))
            {
                string groupName = renderer.gameObject.name.ToLowerInvariant();
                float targetDepth = float.NaN;
                if (groupName == "riverside walk")
                {
                    renderer.enabled = false;
                    continue;
                }
                if (groupName == "riverside railing")
                {
                    renderer.enabled = false;
                    continue;
                }
                if (groupName == "hangang bridge" || groupName == "far seoul hills" ||
                    groupName == "far seoul skyline" || groupName == "han river")
                {
                    renderer.enabled = false;
                    continue;
                }
                if (groupName == "ginkgo trees") targetDepth = 8f;
                else if (groupName == "street lights") targetDepth = 6f;
                else if (groupName == "hangang bridge") targetDepth = 12f;
                else if (groupName == "far seoul hills") targetDepth = 20f;
                else if (groupName == "far seoul skyline") targetDepth = 24f;
                if (!float.IsNaN(targetDepth))
                    renderer.transform.position += Vector3.forward * (targetDepth - renderer.bounds.center.z);
                if (!float.IsNaN(targetDepth)) Debug.Log("SKATER ENV BOUNDS | " + groupName + " | " + renderer.bounds);
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = ResolveEnvironmentMaterial(materials[i] != null ? materials[i].name : string.Empty);
                renderer.sharedMaterials = materials;
            }
        }

        private static Material ResolveEnvironmentMaterial(string sourceName)
        {
            string name = sourceName.ToLowerInvariant();
            if (name.Contains("lightconcrete")) return CreateMaterial("Seoul Set · Light Concrete", new Color(.68f, .66f, .59f), .12f);
            if (name.Contains("concrete")) return CreateMaterial("Seoul Set · Concrete", new Color(.49f, .53f, .51f), .1f);
            if (name.Contains("coping")) return CreateMaterial("Seoul Set · Coping Steel", new Color(.29f, .37f, .37f), .62f);
            if (name.Contains("teal")) return CreateMaterial("Seoul Set · Deep Teal", new Color(.08f, .37f, .35f), .18f);
            if (name.Contains("copper") || name.Contains("paint")) return CreateMaterial("Seoul Set · Copper", new Color(.68f, .37f, .22f), .24f);
            if (name.Contains("metal") || name.Contains("bridge")) return CreateMaterial("Seoul Set · River Steel", new Color(.20f, .29f, .31f), .58f);
            if (name.Contains("teak") || name.Contains("bark")) return CreateMaterial("Seoul Set · Bark and Teak", new Color(.31f, .23f, .17f), .08f);
            if (name.Contains("riverglint")) return CreateMaterial("Seoul Set · Sunset Water Glint", new Color(.76f, .47f, .29f), .34f);
            if (name.Contains("river")) return CreateMaterial("Seoul Set · Han River", new Color(.16f, .31f, .33f), .46f);
            if (name.Contains("ginkgodeep")) return DoubleSided(CreateMaterial("Seoul Set · Ginkgo Deep", new Color(.20f, .34f, .29f), .04f));
            if (name.Contains("ginkgo")) return DoubleSided(CreateMaterial("Seoul Set · Ginkgo Light", new Color(.52f, .54f, .38f), .04f));
            if (name.Contains("lamp")) return CreateMaterial("Seoul Set · Lamp Glow", new Color(1f, .63f, .35f), .25f);
            if (name.Contains("hillfar")) return CreateMaterial("Seoul Set · Distant Ridge", new Color(.42f, .50f, .46f), .04f);
            if (name.Contains("hill")) return CreateMaterial("Seoul Set · Achasan", new Color(.31f, .41f, .37f), .04f);
            if (name.Contains("warm")) return CreateMaterial("Seoul Set · Warm Facade", new Color(.55f, .44f, .34f), .16f);
            if (name.Contains("glass")) return CreateMaterial("Seoul Set · Window Glints", new Color(.77f, .60f, .42f), .35f);
            return CreateMaterial("Seoul Set · Facade", new Color(.36f, .45f, .47f), .12f);
        }

        private static Material DoubleSided(Material material)
        {
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            return material;
        }

        private static void CreateMidground(Transform parent, Material stone, Material pale, Material metal,
            Material warmMetal, Material darkLeaves, Material lightLeaves)
        {
            CreateBank(parent, 23f, 2.9f, stone);
            CreateLedge(parent, 3.2f, 0f, 0f, 2.3f, 0.92f, 0.26f, pale, metal);
            CreateRail(parent, -8.5f, 2.8f, 4.0f, 1.25f, metal);
            CreateBench(parent, -3.4f, -2.1f, metal, warmMetal, stone);
            CreateBench(parent, 6.0f, 3.0f, metal, warmMetal, stone);
            CreateLamp(parent, 9.2f, 3.2f, metal, warmMetal);
            CreateLamp(parent, 20.5f, -2.4f, metal, warmMetal);
            CreateSign(parent, 4.2f, 3.3f, metal, pale);
            CreateTree(parent, -8f, 3.5f, darkLeaves, lightLeaves, warmMetal);
            CreateTree(parent, 16.5f, 4.2f, darkLeaves, lightLeaves, warmMetal);
            CreateTree(parent, 21.5f, 3.5f, darkLeaves, lightLeaves, warmMetal);
            CreateTree(parent, 30.0f, 4.0f, darkLeaves, lightLeaves, warmMetal);
            CreatePlanter(parent, 14.5f, -1.9f, pale, darkLeaves);
        }

        private static void CreateBank(Transform parent, float x, float z, Material material)
        {
            Mesh mesh = new Mesh { name = "Low concrete bank" };
            mesh.vertices = new[] {
                new Vector3(x, 0.03f, z - 0.8f), new Vector3(x + 3.2f, 0.03f, z - 0.8f),
                new Vector3(x + 3.2f, 0.52f, z - 0.8f), new Vector3(x, 0.03f, z + 0.8f),
                new Vector3(x + 3.2f, 0.03f, z + 0.8f), new Vector3(x + 3.2f, 0.52f, z + 0.8f)
            };
            mesh.triangles = new[] { 0, 1, 2, 3, 5, 4, 1, 4, 5, 1, 5, 2, 0, 3, 4, 0, 4, 1 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = SaveMeshAsset(mesh, "LowConcreteBank");
            GameObject ramp = CreateMeshObject("Low bank · skate surface", parent, mesh, material, false);
            ramp.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void CreateLedge(Transform parent, float x, float y, float z, float length, float width,
            float height, Material stone, Material metal)
        {
            GameObject collider = CreateBox("Low ledge · skateable top", new Vector3(x, y + height * .5f, z),
                new Vector3(length, height, width), stone, true, parent);
            collider.GetComponent<MeshRenderer>().enabled = false;
        }

        private static void CreateRail(Transform parent, float x, float z, float length, float height, Material metal)
        {
            CreateBar("Park rail", new Vector3(x, height, z), new Vector3(x + length, height, z), .035f, metal, parent);
            for (int i = 0; i < 4; i++)
            {
                float dx = i * length / 3f;
                CreateBar("Rail post", new Vector3(x + dx, .08f, z), new Vector3(x + dx, height, z), .025f, metal, parent);
            }
        }

        private static void CreateBench(Transform parent, float x, float z, Material metal, Material wood, Material stone)
        {
            for (int i = 0; i < 4; i++)
                CreateBox("Bench seat slat", new Vector3(x, .48f + i * .055f, z + (i - 1.5f) * .11f),
                    new Vector3(1.65f, .035f, .075f), i % 2 == 0 ? wood : stone, false, parent);
            for (int i = 0; i < 3; i++)
                CreateBox("Bench back slat", new Vector3(x, .77f + i * .10f, z + .20f),
                    new Vector3(1.65f, .04f, .055f), wood, false, parent);
            foreach (float side in new[] { -0.62f, 0.62f })
            {
                CreateBar("Bench leg", new Vector3(x + side, .08f, z - .1f), new Vector3(x + side, .55f, z - .1f), .035f, metal, parent);
                CreateBar("Bench leg", new Vector3(x + side, .08f, z + .2f), new Vector3(x + side, .82f, z + .2f), .035f, metal, parent);
                CreateBar("Bench brace", new Vector3(x + side, .22f, z - .1f), new Vector3(x + side, .22f, z + .2f), .025f, metal, parent);
            }
        }

        private static void CreateLamp(Transform parent, float x, float z, Material metal, Material warmMetal)
        {
            CreateBar("Street lamp pole", new Vector3(x, 0f, z), new Vector3(x, 5.3f, z), .055f, metal, parent);
            CreateBar("Lamp arm", new Vector3(x, 5.1f, z), new Vector3(x + .55f, 5.25f, z), .045f, warmMetal, parent);
            CreateBox("Warm street lamp", new Vector3(x + .58f, 5.18f, z), new Vector3(.32f, .10f, .18f), warmMetal, false, parent);
        }

        private static void CreateTree(Transform parent, float x, float z, Material dark, Material light, Material trunk)
        {
            CreateBar("Ginkgo trunk", new Vector3(x, 0f, z), new Vector3(x + .12f, 2.9f, z), .16f, trunk, parent);
            CreateBar("Ginkgo branch", new Vector3(x + .07f, 1.8f, z), new Vector3(x - .55f, 3.0f, z), .065f, trunk, parent);
            CreateBar("Ginkgo branch", new Vector3(x + .10f, 2.0f, z), new Vector3(x + .67f, 3.15f, z), .065f, trunk, parent);
            CreateEllipsoid("Ginkgo canopy", new Vector3(x - .12f, 3.35f, z), new Vector3(1.65f, 1.0f, .80f), dark, parent);
            CreateEllipsoid("Ginkgo canopy", new Vector3(x - .70f, 3.55f, z + .08f), new Vector3(.82f, .70f, .76f), light, parent);
            CreateEllipsoid("Ginkgo canopy", new Vector3(x + .57f, 3.75f, z - .08f), new Vector3(.88f, .72f, .80f), light, parent);
            CreateEllipsoid("Ginkgo canopy", new Vector3(x + .05f, 4.02f, z + .12f), new Vector3(.95f, .66f, .77f), dark, parent);
        }

        private static void CreatePlanter(Transform parent, float x, float z, Material stone, Material leaves)
        {
            CreateBox("Stone planter", new Vector3(x, .26f, z), new Vector3(1.45f, .52f, .65f), stone, false, parent);
            CreateEllipsoid("Planter grass", new Vector3(x, .63f, z), new Vector3(1.12f, .22f, .42f), leaves, parent);
        }

        private static void CreateSign(Transform parent, float x, float z, Material metal, Material signMat)
        {
            CreateBar("Signpost", new Vector3(x, .05f, z), new Vector3(x, 2.65f, z), .04f, metal, parent);
            CreateBox("Han River spot sign", new Vector3(x, 2.25f, z), new Vector3(1.8f, .52f, .12f), signMat, false, parent);
            GameObject textGo = new GameObject("Hangang sign typography");
            textGo.transform.SetParent(parent, false);
            textGo.transform.position = new Vector3(x, 2.25f, z - .075f);
            TextMesh text = textGo.AddComponent<TextMesh>();
            text.text = "HANGANG · SKATE PLAZA";
            text.fontSize = 46;
            text.characterSize = .026f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(.12f, .20f, .19f);
            textGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        private static void CreateRiderAndBoard()
        {
            GameObject root = new GameObject("Skater Physics Root");
            root.transform.position = new Vector3(0f, .18f, 0f);
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 58f;
            body.linearDamping = .02f;
            body.angularDamping = .45f;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            BoxCollider envelope = root.AddComponent<BoxCollider>();
            envelope.center = new Vector3(0f, -.08f, 0f);
            envelope.size = new Vector3(.92f, .20f, .34f);
            envelope.sharedMaterial = CreateRollingContactMaterial();

            Transform board = new GameObject("Board Visual Pivot").transform;
            board.SetParent(root.transform, false);
            board.localPosition = new Vector3(0f, -.08f, 0f);
            Transform[] wheels = CreateBoardVisuals(board);

            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelPrefab == null) throw new InvalidOperationException("Quaternius Humanoid model is not imported: " + ModelPath);
            GameObject riderRoot = new GameObject("SkaterCharacterVisual");
            riderRoot.transform.SetParent(root.transform, false);
            riderRoot.transform.localRotation = Quaternion.Euler(0f, 100f, 0f);
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            model.name = "Rigged Seoul Skater · Blender Clothing Pass";
            model.transform.SetParent(riderRoot.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            RepairHoodieHierarchy(model);
            ApplySkaterMaterials(model);
            float height = GetRendererBounds(model).size.y;
            float scale = height > .001f ? 1.63f / height : 1f;
            model.transform.localScale = Vector3.one * scale;

            Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
            Animator animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Bounds bounds = GetRendererBounds(model);

            float deckTopWorld = root.transform.position.y + board.localPosition.y + .025f;
            riderRoot.transform.position += Vector3.up * (deckTopWorld - bounds.min.y);
            SkaterSeoulBoardRig rig = root.AddComponent<SkaterSeoulBoardRig>();
            rig.body = body;
            rig.wheelRadius = .039f;
            rig.wheels = wheels;
            rig.contactShadow = CreateContactShadow(root.transform);
            SkaterSeoulRiderMotion motion = root.AddComponent<SkaterSeoulRiderMotion>();
            motion.animator = animator;
            motion.visualRoot = riderRoot.transform;
            motion.boardPivot = board;
            root.AddComponent<SkaterSeoulHud>();
            SkaterSeoulPrototype prototype = root.AddComponent<SkaterSeoulPrototype>();
            prototype.boardPivot = board;
            prototype.riderVisual = riderRoot.transform;
            prototype.cruiseSpeed = 5.4f;
            prototype.jumpVelocity = 4.65f;
            prototype.respawnAtX = 100f;
            string boardPrefab = Root + "/Prefabs/BoardVisual.prefab";
            SavePrefab(board.gameObject, boardPrefab);
            SavePrefab(riderRoot, Root + "/Prefabs/SkaterCharacterVisual.prefab");
            Selection.activeGameObject = root;
            EditorUtility.SetDirty(root);
        }

        private static void ApplySkaterMaterials(GameObject model)
        {
            Material hoodie = CreateMaterial("Skater · Deep Teal Zip Hoodie", new Color(.12f, .25f, .30f), .08f);
            Material hoodieTrim = CreateMaterial("Skater · Hoodie Trim", new Color(.37f, .52f, .54f), .08f);
            Material zipper = CreateMaterial("Skater · Brushed Steel Zipper", new Color(.48f, .52f, .49f), .42f);
            Material denim = CreateMaterial("Skater · Indigo Denim", new Color(.045f, .105f, .20f), .12f);
            Material stitch = CreateMaterial("Skater · Denim Stitch", new Color(.24f, .35f, .46f), .06f);
            Material shoe = CreateMaterial("Skater · Skate Shoe", new Color(.10f, .13f, .15f), .08f);
            Material sole = CreateMaterial("Skater · Vulcanized Sole", new Color(.78f, .75f, .67f), .04f);
            Material skin = CreateMaterial("Skater · Warm Skin", new Color(.53f, .34f, .25f), .02f);
            Material hair = CreateMaterial("Skater · Dark Hair", new Color(.075f, .065f, .06f), .02f);
            Material eyes = CreateMaterial("Skater · Eyes", new Color(.045f, .035f, .03f), .08f);

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                string meshName = renderer.name.ToLowerInvariant();
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string sourceName = materials[i] != null ? materials[i].name.ToLowerInvariant() : string.Empty;
                    if (sourceName.Contains("hair") || meshName.Contains("hairstyle")) materials[i] = hair;
                    else if (meshName.Contains("casual_body")) materials[i] = sourceName.Contains("skin") ? skin : hoodie;
                    else if (meshName.Contains("casual_head"))
                    {
                        if (sourceName.Contains("skin")) materials[i] = skin;
                        else if (sourceName.Contains("hair") || sourceName.Contains("eyebrow")) materials[i] = hair;
                        else if (sourceName.Contains("eye")) materials[i] = eyes;
                    }
                    else if (meshName.Contains("casual_feet")) materials[i] = sourceName.Contains("white") ? sole : shoe;
                    else if (sourceName.Contains("denim")) materials[i] = sourceName.Contains("stitch") ? stitch : denim;
                    else if (sourceName.Contains("hoodie_trim")) materials[i] = hoodieTrim;
                    else if (sourceName.Contains("hoodie")) materials[i] = hoodie;
                    else if (sourceName.Contains("zipper")) materials[i] = zipper;
                    else if (sourceName.Contains("sole")) materials[i] = sole;
                    else if (sourceName.Contains("shoe")) materials[i] = shoe;
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Bounds GetRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
            bool hasBounds = false;
            foreach (Renderer renderer in renderers)
            {
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private static GameObject CreateEllipsoid(string name, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material)
        {
            GameObject shape = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPosition;
            shape.transform.localScale = localScale;
            shape.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(shape.GetComponent<Collider>());
            return shape;
        }

        private static Transform[] CreateBoardVisuals(Transform board)
        {
            Material deck = CreateMaterial("Board · Natural Maple", new Color(148f / 255f, 114f / 255f, 85f / 255f), .32f);
            Material grip = CreateMaterial("Board · Charcoal Grip", new Color(.10f, .14f, .15f), .04f);
            Material truck = CreateMaterial("Board · Brushed Steel", new Color(.49f, .56f, .55f), .62f);
            Material wheel = CreateMaterial("Board · Ivory Urethane", new Color(.89f, .83f, .70f), .13f);
            Material bearing = CreateMaterial("Board · Bearing Steel", new Color(.18f, .22f, .23f), .75f);
            Material bushing = CreateMaterial("Board · Black Urethane Bushings", new Color(.12f, .17f, .18f), .28f);
            Material graphic = CreateMaterial("Board · Hangang Sunset Screen Print", Color.white, .40f);
            graphic.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DeckArtworkPath));
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(BoardPath);
            if (asset == null) throw new InvalidOperationException("Blender skateboard failed to import: " + BoardPath);
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            model.name = "Blender skateboard · concave maple, trucks and urethane wheels";
            model.transform.SetParent(board, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string sourceName = materials[i] != null ? materials[i].name : renderer.name;
                    string key = sourceName.ToLowerInvariant();
                    if (key.Contains("print")) materials[i] = graphic;
                    else if (key.Contains("grip")) materials[i] = grip;
                    else if (key.Contains("wheel")) materials[i] = wheel;
                    else if (key.Contains("bearing")) materials[i] = bearing;
                    else if (key.Contains("bushing")) materials[i] = bushing;
                    else if (key.Contains("hardware")) materials[i] = truck;
                    else if (key.Contains("maple") || key.Contains("deck")) materials[i] = deck;
                    else throw new InvalidOperationException("Unmapped skateboard material '" + sourceName + "' on " + renderer.name);
                    Material mapped = materials[i];
                    Texture baseMap = mapped.HasProperty("_BaseMap") ? mapped.GetTexture("_BaseMap") : null;
                    Debug.Log("SKATER BOARD MATERIAL | renderer=" + renderer.name + " | source=" + sourceName + " | output=" +
                        mapped.name + " | shader=" + mapped.shader.name + " | baseMap=" + (baseMap != null ? baseMap.name : "None"));
                }
                renderer.sharedMaterials = materials;
            }
            List<Transform> wheelTransforms = new List<Transform>(4);
            foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
                if (child.name.StartsWith("Wheel_", StringComparison.Ordinal)) wheelTransforms.Add(child);
            wheelTransforms.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            if (wheelTransforms.Count != 4) throw new InvalidOperationException("Blender skateboard must contain four wheel pivots.");
            return wheelTransforms.ToArray();
        }

        private static Transform CreateContactShadow(Transform parent)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("Universal Render Pipeline/Unlit shader is not available.");
            Material shadow = new Material(shader) { name = "Rider · Ground Contact Shadow" };
            shadow.SetColor("_BaseColor", new Color(.12f, .08f, .07f, .26f));
            shadow.SetFloat("_Surface", 1f);
            shadow.SetFloat("_Blend", 0f);
            shadow.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            shadow.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            shadow.SetFloat("_ZWrite", 0f);
            shadow.SetOverrideTag("RenderType", "Transparent");
            shadow.renderQueue = (int)RenderQueue.Transparent;
            shadow.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            shadow = SaveMaterial(shadow, "RiderGroundContactShadow");

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Rider · Ground Contact Shadow";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, -.172f, .03f);
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            go.transform.localScale = new Vector3(.78f, .085f, 1f);
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = shadow;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.SetActive(false);
            return go.transform;
        }

        private static GameObject CreateCamera()
        {
            GameObject go = new GameObject("Side Follow Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(1.8f, 2.25f, -18f);
            go.transform.rotation = Quaternion.Euler(2f, 0f, 0f);
            Camera camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.61f, .57f, .51f);
            camera.nearClipPlane = .03f;
            camera.farClipPlane = 160f;
            camera.orthographic = true;
            camera.orthographicSize = 2.2f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            go.AddComponent<AudioListener>();
            SkaterSeoulCameraRig rig = go.AddComponent<SkaterSeoulCameraRig>();
            rig.followHeight = 2.25f;
            return go;
        }

        private static Mesh CreateBarMesh(string name)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Mesh mesh = UnityEngine.Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);
            mesh.name = name;
            UnityEngine.Object.DestroyImmediate(temp);
            return mesh;
        }

        private static void CreateBar(string name, Vector3 start, Vector3 end, float radius, Material material, Transform parent)
        {
            Vector3 delta = end - start;
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bar.name = name;
            bar.transform.SetParent(parent, false);
            bar.transform.position = (start + end) * .5f;
            bar.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            bar.transform.localScale = new Vector3(radius * 2f, delta.magnitude * .5f, radius * 2f);
            bar.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 size, Material material,
            bool collider, Transform parent = null)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            if (parent != null) box.transform.SetParent(parent, true);
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            else box.isStatic = true;
            return box;
        }

        private static void CreateEllipsoid(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.position = position;
            sphere.transform.localScale = scale;
            sphere.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
        }

        private static GameObject CreateMeshObject(string name, Transform parent, Mesh mesh, Material material, bool collider)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        private static Material CreateMaterial(string name, Color color, float smoothness)
        {
            string key = name + ColorUtility.ToHtmlStringRGBA(color);
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Universal Render Pipeline/Lit shader is not available.");
            Material material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material = SaveMaterial(material, name.Replace(' ', '_').Replace('·', '_') + "_" + ColorUtility.ToHtmlStringRGBA(color));
            Materials.Add(key, material);
            return material;
        }

        private static PhysicsMaterial CreateRollingContactMaterial()
        {
            const string path = Root + "/Materials/Generated/SkateRollingContact.physicMaterial";
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material == null)
            {
                material = new PhysicsMaterial("Skate Rolling Contact");
                AssetDatabase.CreateAsset(material, path);
            }
            material.staticFriction = .01f;
            material.dynamicFriction = .01f;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounciness = 0f;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material SaveMaterial(Material material, string file)
        {
            string dir = Root + "/Materials/Generated";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                EnsureFolders();
                AssetDatabase.CreateFolder(Root + "/Materials", "Generated");
            }
            string path = dir + "/" + file + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(material, existing);
                UnityEngine.Object.DestroyImmediate(material);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Mesh SaveMeshAsset(Mesh mesh, string file)
        {
            string dir = Root + "/Art/Board/Meshes";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                if (!AssetDatabase.IsValidFolder(Root + "/Art/Board")) AssetDatabase.CreateFolder(Root + "/Art", "Board");
                AssetDatabase.CreateFolder(Root + "/Art/Board", "Meshes");
            }
            string path = dir + "/" + file + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                UnityEngine.Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void SavePrefab(GameObject go, string path)
        {
            string directory = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(directory)) AssetDatabase.CreateFolder(Root, "Prefabs");
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }

        private static void EnsureFolders()
        {
            string[] folders = {
                Root + "/Animations", Root + "/Prefabs", Root + "/Materials", Root + "/Materials/Generated",
                Root + "/Art", Root + "/Art/Board", Root + "/Art/Board/Meshes", Root + "/Scenes"
            };
            foreach (string folder in folders)
            {
                if (AssetDatabase.IsValidFolder(folder)) continue;
                string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                string name = Path.GetFileName(folder);
                if (AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void AddBuildScene(string path)
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        }

        private static string NextScenePath()
        {
            return Root + "/Scenes/SkaterSeoul_VisualSlice.unity";
        }
    }
}
#endif
