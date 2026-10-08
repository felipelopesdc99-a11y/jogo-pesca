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
    /// Images keep their proportions (no power-of-two rescale), are clamped, and get mipmaps so they
    /// stay smooth when drawn small in cards. Each kind of picture is capped at the size the camera
    /// can show (TD-031): the files keep the owner's full resolution, the game ships a smaller copy.
    /// Small files (icons) stay uncompressed; the rest use high-quality compression. Fonts fall back to
    /// a system font for the rare symbol they lack.
    /// </remarks>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/Resources/Arte/";
        private const string FontFolder = "Assets/Resources/Fontes/";

        // Raise this when the rules below change, so Unity re-imports the art with them.
        public override uint GetVersion() => 3;

        /// <summary>
        /// Largest side, in pixels, each picture is imported at: about what a 1440p screen shows of it
        /// (the camera is 10,8 units tall). Sprites are built from the texture size, so a smaller import
        /// keeps the same size on screen.
        /// </summary>
        internal static int MaxSizeFor(string path)
        {
            var file = System.IO.Path.GetFileNameWithoutExtension(path);
            if (path.StartsWith(ArtFolder + "Peixes/", System.StringComparison.Ordinal)
                || path.StartsWith(ArtFolder + "Expedicoes/", System.StringComparison.Ordinal)
                || (path.StartsWith(ArtFolder + "Cena/", System.StringComparison.Ordinal) && file.StartsWith("pescador", System.StringComparison.Ordinal)))
            {
                return 512;
            }

            if (path.StartsWith(ArtFolder + "Mapas/", System.StringComparison.Ordinal)
                && (file.Contains("_fg_") || file.Contains("_near_") || file.EndsWith("_thumb", System.StringComparison.Ordinal)))
            {
                return 1024;
            }

            if (path.StartsWith(ArtFolder + "Cena/barco", System.StringComparison.Ordinal) || path.StartsWith(ArtFolder + "Barcos/", System.StringComparison.Ordinal))
            {
                return 1024;
            }

            if (path.StartsWith(ArtFolder + "Mapas/", System.StringComparison.Ordinal) && file.EndsWith("_bg_sky", System.StringComparison.Ordinal))
            {
                return 2048;
            }

            return 4096;
        }

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
            importer.maxTextureSize = MaxSizeFor(assetPath);
            importer.isReadable = false;

            var small = assetPath.StartsWith(ArtFolder + "Icones/", System.StringComparison.Ordinal);
            importer.textureCompression = small ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;

            // TD-035: the scenery, living animals, boats and rods ship crunched (much smaller on disk, almost
            // the same on screen). Skies keep the high-quality format, where gradients would band, and so do
            // the fish, seen up close in every card.
            if (Crunched(assetPath))
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.crunchedCompression = true;
                importer.compressionQuality = 90;
            }
            else
            {
                importer.crunchedCompression = false;
            }

            // Art drawn much smaller than its file (cards, icons, the Shop) stays sharper with the Kaiser
            // filter for its smaller copies.
            if (DrawnSmall(assetPath))
            {
                importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            }

            // Interface pieces (Arte/UI): uncompressed and bilinear, so edges and 9-slice borders stay crisp. The
            // 9-slice frames are drawn near their own size and skip mipmaps (a stretched middle would pick a blurry
            // level); the logo, the avatar frame and the currencies are drawn much smaller and keep them.
            if (assetPath.StartsWith(ArtFolder + "UI/", System.StringComparison.Ordinal))
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.crunchedCompression = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = !NineSlice(assetPath);
                importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            }
        }

        /// <summary>The interface frames drawn as 9-slice (UiSkin.NineSlice).</summary>
        internal static bool NineSlice(string path)
        {
            var file = System.IO.Path.GetFileNameWithoutExtension(path);
            return file.StartsWith("ui_nav_button", System.StringComparison.Ordinal)
                || file == "ui_topbar_frame"
                || file == "ui_wallet_inset";
        }

        internal static bool Crunched(string path)
        {
            var file = System.IO.Path.GetFileNameWithoutExtension(path);
            if (path.StartsWith(ArtFolder + "Mapas/", System.StringComparison.Ordinal))
            {
                return !file.EndsWith("_bg_sky", System.StringComparison.Ordinal);
            }

            return path.StartsWith(ArtFolder + "Vivos/", System.StringComparison.Ordinal)
                || path.StartsWith(ArtFolder + "Barcos/", System.StringComparison.Ordinal)
                || path.StartsWith(ArtFolder + "Varas/", System.StringComparison.Ordinal)
                || path.StartsWith(ArtFolder + "Cena/barco", System.StringComparison.Ordinal);
        }

        internal static bool DrawnSmall(string path)
        {
            return path.StartsWith(ArtFolder + "Peixes/", System.StringComparison.Ordinal)
                || path.StartsWith(ArtFolder + "Icones/", System.StringComparison.Ordinal)
                || path.StartsWith(ArtFolder + "Varas/", System.StringComparison.Ordinal)
                || path.StartsWith(ArtFolder + "Barcos/", System.StringComparison.Ordinal)
                || path.StartsWith(ArtFolder + "Expedicoes/", System.StringComparison.Ordinal)
                || path.StartsWith(ArtFolder + "Iscas/", System.StringComparison.Ordinal);
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
