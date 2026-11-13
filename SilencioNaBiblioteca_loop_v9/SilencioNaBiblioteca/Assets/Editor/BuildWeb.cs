using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SilencioBiblioteca.EditorTools
{
    // Build WebGL pronto para o itch.io.
    // Uso: Unity -batchmode -quit -projectPath . -buildTarget WebGL \
    //        -executeMethod SilencioBiblioteca.EditorTools.BuildWeb.Perform
    public static class BuildWeb
    {
        const string Scene = "Assets/Scenes/Main.unity";

        public static void Perform()
        {
            // pasta de saída: <raiz do repositório>/SilencioNaBibliotecaWeb
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outDir = Path.GetFullPath(Path.Combine(projectRoot, "..", "..", "SilencioNaBibliotecaWeb"));
            Directory.CreateDirectory(outDir);

            PlayerSettings.WebGL.template = "PROJECT:Itch";
            // Gzip: o itch.io detecta o conteúdo comprimido e envia Content-Encoding: gzip.
            // O fallback garante que o jogo também abra em servidores que não configuram o header.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = true;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log("WEB BUILD " + summary.result + " · " + (summary.totalSize / (1024 * 1024)) + " MB · " + outDir);

            if (summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
