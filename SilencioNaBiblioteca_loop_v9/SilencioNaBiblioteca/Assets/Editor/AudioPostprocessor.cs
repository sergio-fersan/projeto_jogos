using UnityEditor;
using UnityEngine;

namespace SilencioBibliotecaEditor
{
    // Músicas longas em Resources/Audio são lidas em streaming (economiza memória).
    public class AudioPostprocessor : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            s.loadType = assetPath.Contains("alarme") ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.Streaming;
            ai.defaultSampleSettings = s;
        }
    }
}
