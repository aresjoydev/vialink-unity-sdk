using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

namespace ViaLinkSample.Editor
{
    /// CI/Batch build entry point — Unity command line에서 호출되는 빌드 메서드.
    /// 사용 예: Unity -batchmode -quit -executeMethod ViaLinkSample.Editor.CommandLineBuild.BuildAndroid -buildOutput <path>
    public static class CommandLineBuild
    {
        public static void BuildAndroid()
        {
            string output = GetArg("-buildOutput") ?? "build/outputs/vialink-unity-sample.apk";
            string fullPath = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            string[] scenes = new[] { "Assets/Scenes/SampleScene.unity" };
            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        private static string GetArg(string name)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}
