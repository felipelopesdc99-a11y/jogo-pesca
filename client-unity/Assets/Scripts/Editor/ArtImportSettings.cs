using UnityEditor;
using UnityEngine;

namespace FishingIdle.Editor
{
    /// <summary>
    /// Import settings for the game's art (Assets/Resources/ArtePixel) and fonts (Assets/Resources/Fontes), applied automatically when a file is added or
    /// replaced, so swapping in final art never needs a manual step in the Inspector.
    /// </summary>
    /// <remarks>
    /// The pixel art keeps every pixel exact (TD-059); the old painted folder and its import rule are gone (TD-065).
    /// The text is Nunito since A-191 (TD-067): a smooth font, rasterised with anti-aliasing and filtered Bilinear (the
    /// Point rule is only for the sprites). Fonts fall back to a system font for the rare symbol they lack.
    /// </remarks>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        private const string FontFolder = "Assets/Resources/Fontes/";

        private const string PixelFolder = "Assets/Resources/ArtePixel/";

        // Raise this when the rules below change, so Unity re-imports the art with them.
        public override uint GetVersion() => 8;

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

        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(PixelFolder, System.StringComparison.Ordinal))
            {
                ApplyPixel((TextureImporter)assetImporter);
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

            // TD-067: Nunito is smooth; hinted smoothing keeps its stems on whole pixels at the small sizes of the game
            // (never HintedRaster, which is for pixel fonts). The glyph texture is set to Bilinear by UiSkin.LoadFont.
            font.fontRenderingMode = FontRenderingMode.HintedSmooth;
        }
    }
}
