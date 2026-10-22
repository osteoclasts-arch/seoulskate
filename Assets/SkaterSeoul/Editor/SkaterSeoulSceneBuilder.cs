#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkaterSeoul.Editor
{
    public static class SkaterSeoulSceneBuilder
    {
        private static Dictionary<string, Material> palette;
        [MenuItem("Tools/SKATER SEOUL/1 - Create Playable MVP Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateScene(true);
        }

        public static void BuildBatch()
        {
            CreateScene(false);
        }

        private static void CreateScene(bool interactive)
        {
            palette = new Dictionary<string, Material>();
            string scenePath = NextScenePath();
            if (!AssetDatabase.IsValidFolder("Assets/SkaterSeoul/Scenes"))
                AssetDatabase.CreateFolder("Assets/SkaterSeoul", "Scenes");

            // SaveCurrentModifiedScenesIfUserWantsTo above protects in-memory scene edits.
            // A unique output path below prevents overwriting an existing scene asset.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.67f, 0.76f, 0.82f);
            RenderSettings.fog = false;

            CreateBlock("Concrete skating floor", new Vector3(140f, -0.12f, 0f),
                new Vector3(370f, 0.24f, 12f), new Color(0.49f, 0.57f, 0.59f), true);
            CreateBlock("Lane edge stripe", new Vector3(140f, 0.007f, 5.3f),
                new Vector3(370f, 0.012f, 0.09f), new Color(0.93f, 0.84f, 0.52f), false);
            CreateBlock("Rear lane stripe", new Vector3(140f, 0.007f, -5.3f),
                new Vector3(370f, 0.012f, 0.09f), new Color(0.93f, 0.84f, 0.52f), false);

            // Background set dressing is intentionally OFF the ride line.
            for (int i = 0; i < 11; i++)
            {
                float x = -22f + i * 32f;
                float height = 3f + (i % 4) * 1.65f;
                CreateBlock("Seoul skyline placeholder " + i,
                    new Vector3(x, height / 2f, 11f + (i % 2) * 2),
                    new Vector3(14f, height, 2.5f),
                    i % 3 == 0 ? new Color(0.30f, 0.43f, 0.48f) : new Color(0.40f, 0.52f, 0.57f), false);
            }
            // Decor-only prefab-like blocks; no claim of an actual Ttukseom reconstruction.
            for (int i = 0; i < 5; i++)
            {
                CreateBlock("Practice bank facade " + i,
                    new Vector3(13f + i * 36f, 0.2f, 4.15f),
                    new Vector3(4.8f, 0.4f, 1.3f), new Color(0.78f, 0.69f, 0.54f), false);
            }

            GameObject skater = new GameObject("Skater Physics Root");
            skater.transform.position = new Vector3(-8f, 0.42f, 0f);
            BoxCollider skateCollider = skater.AddComponent<BoxCollider>();
            skateCollider.center = new Vector3(0f, -0.16f, 0f);
            skateCollider.size = new Vector3(1.24f, 0.29f, 0.43f);
            Rigidbody rb = skater.AddComponent<Rigidbody>();
            rb.mass = 1.8f;
            rb.linearDamping = 0.04f;
            rb.angularDamping = 0.5f;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            Transform board = new GameObject("Board Visual Pivot (Y spin, X flip)").transform;
            board.SetParent(skater.transform, false);
            board.localPosition = Vector3.zero;
            CreateVisual("Deck", PrimitiveType.Cube, board, new Vector3(0f, -0.08f, 0f),
                new Vector3(1.33f, 0.075f, 0.32f), new Color(0.09f, 0.19f, 0.22f));
            CreateVisual("Grip tape", PrimitiveType.Cube, board, new Vector3(0f, -0.035f, 0f),
                new Vector3(1.18f, 0.012f, 0.30f), new Color(0.07f, 0.09f, 0.11f));
            for (int sign = -1; sign <= 1; sign += 2)
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject wheel = CreateVisual("Wheel", PrimitiveType.Cylinder, board,
                        new Vector3(sign * 0.47f, -0.23f, side * 0.18f),
                        new Vector3(0.076f, 0.045f, 0.076f), new Color(0.96f, 0.95f, 0.87f));
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }

            Transform rider = new GameObject("Placeholder Rider (replace with Humanoid)").transform;
            rider.SetParent(skater.transform, false);
            CreateVisual("Left shoe", PrimitiveType.Cube, rider, new Vector3(-0.32f, 0.14f, -0.03f),
                new Vector3(0.31f, 0.14f, 0.24f), Color.white);
            CreateVisual("Right shoe", PrimitiveType.Cube, rider, new Vector3(0.31f, 0.14f, 0.03f),
                new Vector3(0.31f, 0.14f, 0.24f), Color.white);
            CreateVisual("Left pant leg", PrimitiveType.Cube, rider, new Vector3(-0.29f, 0.41f, 0f),
                new Vector3(0.23f, 0.51f, 0.27f), new Color(0.11f, 0.16f, 0.31f));
            CreateVisual("Right pant leg", PrimitiveType.Cube, rider, new Vector3(0.27f, 0.41f, 0f),
                new Vector3(0.23f, 0.51f, 0.27f), new Color(0.11f, 0.16f, 0.31f));
            CreateVisual("Hips", PrimitiveType.Cube, rider, new Vector3(-0.03f, 0.68f, 0f),
                new Vector3(0.64f, 0.18f, 0.31f), new Color(0.11f, 0.16f, 0.31f));
            CreateVisual("Cream hoodie", PrimitiveType.Cube, rider, new Vector3(-0.05f, 1.03f, 0f),
                new Vector3(0.52f, 0.59f, 0.38f), new Color(0.91f, 0.86f, 0.72f));
            CreateVisual("Left arm", PrimitiveType.Cube, rider, new Vector3(-0.44f, 1.03f, 0f),
                new Vector3(0.20f, 0.56f, 0.24f), new Color(0.91f, 0.86f, 0.72f));
            CreateVisual("Right arm", PrimitiveType.Cube, rider, new Vector3(0.34f, 1.06f, 0f),
                new Vector3(0.20f, 0.56f, 0.24f), new Color(0.91f, 0.86f, 0.72f));
            CreateVisual("Face", PrimitiveType.Sphere, rider, new Vector3(0.04f, 1.52f, 0f),
                new Vector3(0.34f, 0.39f, 0.34f), new Color(0.73f, 0.52f, 0.38f));
            CreateVisual("Teal cap", PrimitiveType.Cylinder, rider, new Vector3(0.05f, 1.73f, 0f),
                new Vector3(0.23f, 0.065f, 0.23f), new Color(0.05f, 0.49f, 0.48f));
            CreateVisual("Cap visor", PrimitiveType.Cube, rider, new Vector3(0.24f, 1.68f, 0f),
                new Vector3(0.34f, 0.04f, 0.26f), new Color(0.05f, 0.49f, 0.48f));

            GameObject cameraObject = new GameObject("Side View Camera");
            cameraObject.tag = "MainCamera";
            Camera cam = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(-6f, 3.25f, -19f);
            cameraObject.transform.rotation = Quaternion.identity;
            cam.orthographic = true;
            cam.orthographicSize = 4.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.54f, 0.74f, 0.78f);
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 120f;

            GameObject sun = new GameObject("Soft sunlight");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            sun.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

            SkaterSeoulPrototype game = skater.AddComponent<SkaterSeoulPrototype>();
            game.boardPivot = board;
            game.riderVisual = rider;
            game.followCamera = cam;

            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new IOException("Could not save the new SKATER SEOUL scene at " + scenePath);
            bool exists = false;
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
                if (existing.path == scenePath) exists = true;
            if (!exists)
            {
                var entries = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                entries.Insert(0, new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = entries.ToArray();
            }
            Selection.activeGameObject = skater;
            Debug.Log("SKATER SEOUL scene created: " + scenePath);
            if (interactive)
                EditorUtility.DisplayDialog("Scene ready", "New scene saved; existing scene assets are preserved:\n" + scenePath +
                    "\nPress Play. Flick the left pad for Ollie/Kickflip/Tre Flip; hold the right PUSH button to gain speed.", "OK");
        }

        private static string NextScenePath()
        {
            string path = "Assets/SkaterSeoul/Scenes/SkaterSeoul_MVP.unity";
            for (int version = 2; File.Exists(path); version++)
                path = "Assets/SkaterSeoul/Scenes/SkaterSeoul_MVP_" + version + ".unity";
            return path;
        }

        private static GameObject CreateBlock(string label, Vector3 pos, Vector3 scale, Color color, bool collide)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = label;
            g.transform.position = pos;
            g.transform.localScale = scale;
            if (!collide) Object.DestroyImmediate(g.GetComponent<Collider>());
            SetMaterial(g, color);
            return g;
        }

        private static GameObject CreateVisual(string label, PrimitiveType shape, Transform parent,
            Vector3 localPos, Vector3 scale, Color color)
        {
            GameObject g = GameObject.CreatePrimitive(shape);
            g.name = label;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            Object.DestroyImmediate(g.GetComponent<Collider>()); // No compound colliders on visuals.
            SetMaterial(g, color);
            return g;
        }

        private static void SetMaterial(GameObject target, Color color)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color);
            Material material;
            if (!palette.TryGetValue(key, out material))
            {
                string dir = "Assets/SkaterSeoul/Materials";
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder("Assets/SkaterSeoul", "Materials");
                string path = dir + "/SS_" + key + ".mat";
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Standard");
                    material = new Material(shader);
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    else material.color = color;
                    AssetDatabase.CreateAsset(material, path);
                }
                palette.Add(key, material);
            }
            target.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
#endif
