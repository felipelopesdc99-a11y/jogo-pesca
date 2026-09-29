using UnityEditor;
using UnityEngine;

namespace FishingIdle.Editor
{
    /// <summary>
    /// Import settings for the game's sounds (Assets/Resources/Sons): the long ambience loops are
    /// compressed and stay compressed in memory; the short effects are kept uncompressed so they
    /// start instantly. Applies automatically when a file is added or replaced.
    /// </summary>
    public sealed class AudioImportSettings : AssetPostprocessor
    {
        private const string Folder = "Assets/Resources/Sons/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Folder, System.StringComparison.Ordinal))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            var ambience = System.IO.Path.GetFileName(assetPath).StartsWith("ambiente", System.StringComparison.Ordinal);
            if (ambience)
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
            }

            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
        }
    }
}
