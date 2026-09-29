using UnityEditor;
using UnityEngine;

namespace FishingIdle.Editor
{
    /// <summary>
    /// Import settings for the game's art (Assets/Resources/Arte) and fonts (Assets/Resources/Fontes),
    /// applied automatically when a file is added or replaced, so swapping in final art never needs
    /// a manual step in the Inspector.
    /// </summary>
    /// <remarks>
    /// Images keep their real size (no power-of-two rescale, up to 4096 px), are clamped, and get
    /// mipmaps so they stay smooth when drawn small in cards. Small files (icons) stay uncompressed;
    /// large ones use high-quality compression. Fonts fall back to a system font for the rare
    /// symbol they lack.
    /// </remarks>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/Resources/Arte/";
        private const string FontFolder = "Assets/Resources/Fontes/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder, System.StringComparison.Ordinal))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = 4096;
            importer.isReadable = false;

            var small = assetPath.StartsWith(ArtFolder + "Icones/", System.StringComparison.Ordinal);
            importer.textureCompression = small ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
        }

        private void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith(FontFolder, System.StringComparison.Ordinal) || !(assetImporter is TrueTypeFontImporter font))
            {
                return;
            }

            font.fontTextureCase = FontTextureCase.Dynamic;
            font.includeFontData = true;
            font.fontNames = new[] { "Segoe UI", "Arial" };
        }
    }
}
