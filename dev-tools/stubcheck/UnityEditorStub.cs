// Stub mínimo do UnityEditor (só para a checagem fora do Unity).
using System;

namespace UnityEditor
{
    public class AssetImporter : UnityEngine.Object
    {
        public static AssetImporter GetAtPath(string path) { return null; }
        public void SaveAndReimport() { }
    }

    public class TextureImporter : AssetImporter
    {
        public UnityEngine.TextureImporterType textureType;
        public UnityEngine.SpriteImportMode spriteImportMode;
        public UnityEngine.FilterMode filterMode;
        public bool mipmapEnabled;
    }

    namespace SceneManagement
    {
        public class Scene { }
        public struct NewSceneSetup { public static NewSceneSetup EmptyScene; }
        public struct NewSceneMode { public static NewSceneMode Single; }
        public class EditorSceneManager
        {
            public static Scene NewScene(object setup, object mode) { return new Scene(); }
            public static bool SaveScene(Scene s, string path) { return true; }
        }
    }

    public class EditorBuildSettingsScene
    {
        public EditorBuildSettingsScene(string path, bool enabled) { }
    }

    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes;
    }

    public class MenuItemAttribute : UnityEngine.PropertyAttribute
    {
        public MenuItemAttribute(string item) { }
    }
}

namespace UnityEngine
{
    public class PropertyAttribute : Attribute { public int order; }

    public enum TextureImporterType { Default, Sprite }
    public enum SpriteImportMode { Single, Multiple }
}
