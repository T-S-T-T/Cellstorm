using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Cellstorm.EditorTools
{
    /// <summary>Menu items to prepare and make a standalone build.</summary>
    public static class BuildTools
    {
        static readonly string[] ScenePaths =
        {
            "Assets/Scenes/Boot.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Game.unity"
        };

        [MenuItem("Cellstorm/Build/1 - Prepare project (scenes, shaders, window)")]
        public static void Prepare()
        {
            string missing = "";
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var path in ScenePaths)
            {
                if (!File.Exists(path))
                {
                    missing += path + "\n";
                    continue;
                }
                list.Add(new EditorBuildSettingsScene(path, true));
            }
            if (missing.Length > 0)
            {
                EditorUtility.DisplayDialog("Cellstorm", "These scenes were not found:\n\n" + missing +
                    "\nSave the Boot, MainMenu and Game scenes in Assets/Scenes with these exact names.", "OK");
                return;
            }
            EditorBuildSettings.scenes = list.ToArray();

            bool shader = EnsureAlwaysIncluded("Sprites/Default");

            PlayerSettings.productName = "Cellstorm";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;

            Debug.Log("Cellstorm build prepared: 3 scenes in Build Settings, Sprites/Default " +
                      (shader ? "is always included" : "could not be added automatically (add it under Project Settings > Graphics)") +
                      ", windowed 1280x720.");
        }

        [MenuItem("Cellstorm/Build/2 - Build for the current platform")]
        public static void Build()
        {
            Prepare();
            if (EditorBuildSettings.scenes.Length != ScenePaths.Length) return;

            var target = EditorUserBuildSettings.activeBuildTarget;
            string folder = "Builds/" + target;
            string exe;
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneWindows:
                    exe = "Cellstorm.exe";
                    break;
                case BuildTarget.StandaloneOSX:
                    exe = "Cellstorm.app";
                    break;
                case BuildTarget.StandaloneLinux64:
                    exe = "Cellstorm.x86_64";
                    break;
                default:
                    exe = "Cellstorm";
                    break;
            }

            Directory.CreateDirectory(folder);
            var options = new BuildPlayerOptions
            {
                scenes = ScenePaths,
                locationPathName = Path.Combine(folder, exe),
                target = target,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Cellstorm build succeeded: " + options.locationPathName + " (" +
                          (report.summary.totalSize / (1024 * 1024)) + " MB)");
                EditorUtility.RevealInFinder(options.locationPathName);
            }
            else
            {
                Debug.LogError("Cellstorm build failed: " + report.summary.result);
            }
        }

        static bool EnsureAlwaysIncluded(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) return false;

            var graphics = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (graphics == null) return false;

            var so = new SerializedObject(graphics);
            var array = so.FindProperty("m_AlwaysIncludedShaders");
            if (array == null) return false;

            for (int i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == shader) return true;

            array.arraySize++;
            array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = shader;
            so.ApplyModifiedProperties();
            return true;
        }
    }
}
