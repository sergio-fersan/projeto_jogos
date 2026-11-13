using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SilencioBibliotecaEditor
{
    // Prepara a demo com um clique:
    //  1) ajusta as importações das artes (PNGs -> Sprite)
    //  2) cria e salva a cena Main (câmera, EventSystem, GameManager)
    //  3) adiciona a cena ao Build Settings
    public static class DemoSetup
    {
        [MenuItem("Silêncio na Biblioteca/1. Preparar projeto (artes + cena)")]
        public static void Prepare()
        {
            FixArtImports();
            BuildScene();
        }

        [MenuItem("Silêncio na Biblioteca/2. Corrigir importação das artes")]
        public static void FixArtOnly()
        {
            FixArtImports();
        }

        static void FixArtImports()
        {
            string dir = "Assets/Resources/Art";
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning("Pasta Assets/Resources/Art não encontrada.");
                return;
            }
            foreach (string file in Directory.GetFiles(dir, "*.png"))
            {
                var importer = AssetImporter.GetAtPath(file) as TextureImporter;
                if (importer == null) continue;
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    dirty = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }
                if (dirty)
                {
                    importer.SaveAndReimport();
                    Debug.Log("[DemoSetup] Importação ajustada: " + file);
                }
            }
        }

        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            var gameGo = new GameObject("Game");
            gameGo.AddComponent<SilencioBiblioteca.Game>();

            string dir = "Assets/Scenes";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = dir + "/Main.unity";
            EditorSceneManager.SaveScene(scene, path);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };

            Debug.Log("[DemoSetup] Cena criada em " + path + ". Pressione Play para jogar a demo.");
        }
    }
}
