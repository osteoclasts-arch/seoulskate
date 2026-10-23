#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkaterSeoul.Editor
{
    public static class SkaterSeoulVisualCapture
    {
        private const string ScenePath = "Assets/SkaterSeoul/Scenes/SkaterSeoul_VisualSlice.unity";

        [MenuItem("Tools/SKATER SEOUL/3 - Capture Scene Camera")]
        public static void CaptureAfter()
        {
            Capture(ScenePath, "visual_overhaul/after_scene_camera.png", 1920, 1080);
        }

        public static void Capture(string scenePath, string outputRelativePath, int width, int height)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Camera camera = Object.FindFirstObjectByType<Camera>();
            if (camera == null) throw new System.InvalidOperationException("The selected scene has no camera.");
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/" + outputRelativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            RenderTexture oldTarget = camera.targetTexture;
            float oldAspect = camera.aspect;
            RenderTexture oldActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.aspect = width / (float)height;
                camera.Render();
                RenderTexture.active = target;
                Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(output, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                Debug.Log("SKATER SEOUL camera capture: " + output + " (" + width + "x" + height + ")");
            }
            finally
            {
                camera.targetTexture = oldTarget;
                camera.aspect = oldAspect;
                RenderTexture.active = oldActive;
                target.Release();
                Object.DestroyImmediate(target);
            }
            if (scene.isDirty) EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
