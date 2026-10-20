using UnityEditor;
using UnityEngine;

namespace FishingIdle.Editor
{
    /// <summary>
    /// Import settings for the game's art (Assets/Resources/ArtePixel, and the few old files still in
    /// Assets/Resources/Arte) and fonts (Assets/Resources/Fontes), applied automatically when a file is added or
    /// replaced, so swapping in final art never needs a manual step in the Inspector.
    /// </summary>
    /// <remarks>
    /// The pixel art keeps every pixel exact (TD-059). Since M25-T17 the old painted folder holds only the 6 Pantanal
    /// fish still waiting for their pixel drawing (pedido L4-03a): they keep the old fish rules (mipmaps, 512 px cap,
    /// high-quality compression). Fonts fall back to a system font for the rare symbol they lack.
    /// </remarks>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/Resources/Arte/";
        private const string FontFolder = "Assets/Resources/Fontes/";

        private const string PixelFolder = "Assets/Resources/ArtePixel/";

        // Raise this when the rules below change, so Unity re-imports the art with them.
        public override uint GetVersion() => 6;

        /// <summary>
        /// TD-059: the pixel art (Estilo ARC) keeps every pixel sharp and exact: Point filter, no compression,
        /// no mipmaps, no size cap (the largest file is 384 × 256), clamped, never rescaled to a power of two.
        /// </summary>
        internal static void ApplyPixel(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.maxTextureSize = 8192;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
        }

        /// <summary>
        /// The old painted fish still in Resources/Arte (TD-031): drawn small in cards, so they ship at 512 px with
        /// mipmaps (Kaiser filter) and high-quality compression.
        /// </summary>
        internal static void ApplyOldFish(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = 512;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = false;
        }

        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(PixelFolder, System.StringComparison.Ordinal))
            {
                ApplyPixel((TextureImporter)assetImporter);
            }
            else if (assetPath.StartsWith(ArtFolder, System.StringComparison.Ordinal))
            {
                ApplyOldFish((TextureImporter)assetImporter);
            }
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

            // TD-059 / TD-061: the two pixel fonts of the ARC skin are rasterised without smoothing, so their square
            // pixels stay sharp.
            var file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (file == "PressStart2P-Regular" || file == "VT323-Regular")
            {
                font.fontRenderingMode = FontRenderingMode.HintedRaster;
            }
        }
    }
}
