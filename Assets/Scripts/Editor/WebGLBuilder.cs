using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Warashibe.Editor
{
    /// <summary>
    /// WebGL build for Phase 0 distribution (docs/13 §5): Brotli 圧縮 / Code Stripping High /
    /// エンジンコード strip / 2D 最小構成。出力は <c>Builds/WebGL</c>（Firebase Hosting の public）。
    ///
    /// 使い方:
    ///   • エディタ: メニュー <b>Warashibe ▸ Build WebGL</b>
    ///   • CI / ローカルCLI (T-U11 で GameCI 化):
    ///       Unity -batchmode -quit -projectPath . -executeMethod Warashibe.Editor.WebGLBuilder.BuildFromCommandLine
    ///
    /// High stripping で Newtonsoft のリフレクション型が落ちないよう <c>Assets/link.xml</c> で保護。
    /// </summary>
    public static class WebGLBuilder
    {
        public const string OutputDir = "Builds/WebGL";

        [MenuItem("Warashibe/Build WebGL")]
        public static void BuildFromMenu()
        {
            var report = Build();
            if (report.summary.result != BuildResult.Succeeded)
                Debug.LogError("[WebGLBuilder] build failed: " + report.summary.result);
        }

        /// <summary>Batchmode entry (-executeMethod). Exits non-zero on failure so CI can gate.</summary>
        public static void BuildFromCommandLine()
        {
            var report = Build();
            if (report.summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        public static BuildReport Build()
        {
            ApplyWebGLSettings();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            Debug.Log("[WebGLBuilder] building " + scenes.Length + " scene(s) → " + OutputDir);
            var report = BuildPipeline.BuildPlayer(options);
            LogResult(report);
            return report;
        }

        /// <summary>docs/13 §5 の WebGL 設定を適用。ProjectSettings に永続化されるので一度走らせて
        /// コミットしておけば CI でも同じ設定になる。</summary>
        public static void ApplyWebGLSettings()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
        }

        static void LogResult(BuildReport report)
        {
            var s = report.summary;
            long br = 0;
            var buildFolder = Path.Combine(OutputDir, "Build");
            if (Directory.Exists(buildFolder))
                br = Directory.GetFiles(buildFolder, "*.br", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
            Debug.Log($"[WebGLBuilder] {s.result} in {s.totalTime} — compressed(.br)={br / (1024f * 1024f):F1} MB " +
                      $"(docs/13 §5 目標 ≤30MB), scenes={s.totalSize / (1024f * 1024f):F1} MB uncompressed");
        }
    }
}
