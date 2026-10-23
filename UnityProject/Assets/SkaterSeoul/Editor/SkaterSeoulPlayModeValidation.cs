#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkaterSeoul.Editor
{
    [InitializeOnLoad]
    public static class SkaterSeoulPlayModeValidation
    {
        private const string RunProbeKey = "SkaterSeoul.RunVisualSliceProbe";
        private const string ScenePath = "Assets/SkaterSeoul/Scenes/SkaterSeoul_VisualSlice.unity";

        static SkaterSeoulPlayModeValidation()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/SKATER SEOUL/4 - Run Play Mode Capture")]
        public static void StartCapture()
        {
            SkaterSeoulPlayModeProbe.CapturePlayerPrefsSnapshot();
            if (Application.isBatchMode) SkaterSeoulSceneBuilder.BuildBatch();
            SessionState.SetBool(RunProbeKey, true);
            if (Application.isBatchMode) SessionState.SetBool("SkaterSeoul.ExitAfterProbe", true);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int qualityLevel = QualitySettings.GetQualityLevel();
            string[] qualityNames = QualitySettings.names;
            string qualityName = qualityLevel >= 0 && qualityLevel < qualityNames.Length ? qualityNames[qualityLevel] : "Unknown";
            Camera camera = Object.FindFirstObjectByType<Camera>();
            Debug.Log("SKATER CAPTURE SETTINGS | target=" + EditorUserBuildSettings.activeBuildTarget + " | quality=" + qualityName +
                " | pipeline=" + (GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "Built-in") +
                " | AA=" + QualitySettings.antiAliasing + " | shadowDistance=" + QualitySettings.shadowDistance +
                " | camera=" + (camera != null ? (camera.orthographic ? "Orthographic" : "Perspective") + " HDR=" + camera.allowHDR : "Missing"));
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RunProbeKey, false))
            {
                SessionState.SetBool(RunProbeKey, false);
                EditorApplication.delayCall += AttachProbe;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SkaterSeoulPlayModeProbe.RestorePlayerPrefsAfterRun();
                if (Application.isBatchMode && SessionState.GetBool("SkaterSeoul.ExitAfterProbe", false))
                {
                    SessionState.SetBool("SkaterSeoul.ExitAfterProbe", false);
                    EditorApplication.Exit(0);
                }
            }
        }

        private static void AttachProbe()
        {
            SkaterSeoulPrototype game = Object.FindFirstObjectByType<SkaterSeoulPrototype>();
            if (game == null)
            {
                Debug.LogError("SKATER SEOUL QA FAIL: no prototype in the visual slice scene.");
                EditorApplication.isPlaying = false;
                return;
            }
            SkaterSeoulPlayModeProbe probe = game.gameObject.AddComponent<SkaterSeoulPlayModeProbe>();
            probe.StartCapture();
        }
    }
}
#endif
