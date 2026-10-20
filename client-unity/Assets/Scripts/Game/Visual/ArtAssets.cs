using System.Collections.Generic;
using UnityEngine;

namespace FishingIdle.Game.Visual
{
    /// <summary>
    /// Loads the replaceable art files by name: the pixel art in Assets/Resources/ArtePixel first and, while a
    /// file is not there yet, the old art in Assets/Resources/Arte (TD-059, M25-T04). Since M25-T17 the old folder
    /// holds only the 6 Pantanal fish still waiting for their pixel drawing (pedido L4-03a).
    /// </summary>
    /// <remarks>
    /// Every image the game shows is a file that can be swapped keeping its name; nothing is wired into scene
    /// logic. A missing file returns null, and each caller falls back to the art generated from code, so the
    /// game never breaks because an asset is absent. Pixel art is remembered as such (<see cref="IsPixel(Texture)"/>),
    /// so the scene gives it 20 pixels per unit and the interface draws it at a whole scale (<see cref="PixelDraw"/>).
    /// </remarks>
    public static class ArtAssets
    {
        /// <summary>Folder of the pixel art (Estilo ARC, ART_BIBLE_PIXEL).</summary>
        public const string PixelRoot = "ArtePixel/";

        /// <summary>Folder of the old art, used while a pixel file is missing (since M25-T17 only 6 fish, pedido L4-03a).</summary>
        public const string LegacyRoot = "Arte/";

        /// <summary>Pixels of scene art per world unit (216-pixel scene grid on a 10.8-unit camera, TD-059).</summary>
        public const float PixelsPerUnit = 20f;

        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly HashSet<Texture> PixelTextures = new HashSet<Texture>();
        private static readonly Dictionary<Texture, string> PathOf = new Dictionary<Texture, string>();
        private static readonly Dictionary<Texture, Texture2D> SmallOf = new Dictionary<Texture, Texture2D>();

        /// <summary>A texture (path without extension, e.g. "Peixes/fish_lambari_master"): the pixel file, or the old one, or null.</summary>
        public static Texture2D Texture(string path)
        {
            // A missing file is remembered as null, so it is looked up only once.
            if (Textures.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var tex = Resources.Load<Texture2D>(PixelRoot + path);
            if (tex != null)
            {
                // Pixel art: never smoothed (the importer already sets Point; this also covers files added at runtime).
                tex.filterMode = FilterMode.Point;
                PixelTextures.Add(tex);
            }
            else
            {
                tex = Resources.Load<Texture2D>(LegacyRoot + path);
            }

            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                PathOf[tex] = path;
            }

            Textures[path] = tex;
            return tex;
        }

        /// <summary>True when the file at <paramref name="path"/> exists in ArtePixel.</summary>
        public static bool IsPixel(string path)
        {
            var tex = Texture(path);
            return tex != null && PixelTextures.Contains(tex);
        }

        /// <summary>True when <paramref name="tex"/> was loaded from ArtePixel.</summary>
        public static bool IsPixel(Texture tex) => tex != null && PixelTextures.Contains(tex);

        /// <summary>
        /// The small version of a pixel drawing (the same file with "_p", half size, made by pixel_folhas.py for the
        /// fish), or null. Looked up once per texture.
        /// </summary>
        public static Texture2D Small(Texture tex)
        {
            if (tex == null || !PixelTextures.Contains(tex))
            {
                return null;
            }

            if (SmallOf.TryGetValue(tex, out var small))
            {
                return small;
            }

            small = PathOf.TryGetValue(tex, out var path) ? Texture(path + "_p") : null;
            if (small != null && !PixelTextures.Contains(small))
            {
                small = null; // only a pixel "_p" counts
            }

            SmallOf[tex] = small;
            return small;
        }

        /// <summary>A sprite made from a texture, <paramref name="unitsWide"/> world units across.</summary>
        public static Sprite Sprite(string path, float unitsWide, Vector2 pivot)
        {
            var key = path + "|" + unitsWide + "|" + pivot;
            if (Sprites.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var tex = Texture(path);
            if (tex == null)
            {
                return null;
            }

            return Cache(key, path, tex, pivot, tex.width / unitsWide);
        }

        /// <summary>A sprite made from a texture, <paramref name="unitsTall"/> world units high (the width follows the picture).</summary>
        public static Sprite SpriteByHeight(string path, float unitsTall, Vector2 pivot)
        {
            var tex = Texture(path);
            return tex == null ? null : Sprite(path, unitsTall * tex.width / tex.height, pivot);
        }

        /// <summary>
        /// A sprite of a pixel file at the scene grid: one pixel of art = one pixel of the 216-pixel grid
        /// (<see cref="PixelsPerUnit"/>). The pivot is rounded to a whole pixel of the picture so the drawing lands on the grid.
        /// Null when the file is not pixel art.
        /// </summary>
        public static Sprite PixelSprite(string path, Vector2 pivot)
        {
            var tex = Texture(path);
            if (tex == null || !PixelTextures.Contains(tex))
            {
                return null;
            }

            var snapped = new Vector2(Mathf.Round(pivot.x * tex.width) / tex.width, Mathf.Round(pivot.y * tex.height) / tex.height);
            var key = path + "|px|" + snapped;
            if (Sprites.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            return Cache(key, path, tex, snapped, PixelsPerUnit);
        }

        /// <summary>A pixel sprite with its pivot given in pixels of the picture, from the bottom-left corner.</summary>
        public static Sprite PixelSpriteAt(string path, int pivotX, int pivotY)
        {
            var tex = Texture(path);
            return tex == null ? null : PixelSprite(path, new Vector2(pivotX / (float)tex.width, pivotY / (float)tex.height));
        }

        private static Sprite Cache(string key, string path, Texture2D tex, Vector2 pivot, float pixelsPerUnit)
        {
            var sprite = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = path;
            sprite.hideFlags = HideFlags.DontSave;
            Sprites[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Forgets every texture and sprite under <paramref name="prefix"/> (e.g. the previous map's
        /// layers after a trip) and asks Unity to free what nothing uses any more (M22-T13). Anything
        /// still needed is simply loaded again on its next use.
        /// </summary>
        public static void Release(string prefix)
        {
            var removed = false;
            foreach (var key in new List<string>(Sprites.Keys))
            {
                if (key.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    if (Sprites[key] != null)
                    {
                        Object.Destroy(Sprites[key]);
                    }

                    Sprites.Remove(key);
                    removed = true;
                }
            }

            removed |= Forget(Textures, prefix);

            if (removed)
            {
                Resources.UnloadUnusedAssets();
            }
        }

        private static bool Forget(Dictionary<string, Texture2D> textures, string prefix)
        {
            var removed = false;
            foreach (var key in new List<string>(textures.Keys))
            {
                if (key.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    var tex = textures[key];
                    if (tex != null)
                    {
                        PixelTextures.Remove(tex);
                        PathOf.Remove(tex);
                        SmallOf.Remove(tex);
                    }

                    textures.Remove(key);
                    removed = true;
                }
            }

            return removed;
        }

        /// <summary>An interface icon (Icones/ico_&lt;name&gt;). Use the names in <see cref="Icons"/>.</summary>
        public static Texture2D Icon(string name) => Texture("Icones/ico_" + name);
    }

    /// <summary>Names of the icon files (ArtePixel/Icones/ico_&lt;name&gt;).</summary>
    public static class Icons
    {
        public const string Fishing = "pesca", Map = "mapa", Aquarium = "aquario", Arena = "arena", Expedition = "expedicao",
            Market = "mercado", Shop = "loja", Profile = "perfil", Bell = "avisos", Settings = "opcoes", Close = "fechar",
            Play = "iniciar", Stop = "parar", Star = "estrela", Rod = "vara", Fish = "peixe", Book = "especies",
            Pin = "local", Clock = "relogio", Lock = "cadeado", Check = "confirmar", Energy = "energia", Honor = "honra",
            Chevron = "seta", Arrow = "seta_longa", Add = "adicionar", Box = "caixa", Sell = "vender", Attack = "atacar",
            Swap = "trocar", Ranking = "ranking", Info = "info", Warning = "aviso", Hourglass = "ampulheta", Level = "nivel",
            Buy = "comprar", Sort = "ordenar", Feed = "alimentar", Home = "inicio", Waves = "ondas", Compact = "compacto",
            Coin = "moeda", Shell = "concha", Pause = "pausa", Search = "buscar", Boat = "barco", Bait = "isca", Dollar = "dolar";

        /// <summary>The Eventos menu icon (M24-T39). ASSET_PENDENTE: ico_eventos.png; until it exists the menu shows the clock.</summary>
        public const string Events = "eventos";

        /// <summary>The Leilão icon (ico_leilao, received 13/10/2026, A-179).</summary>
        public const string Auction = "leilao";

        private static string _auctionIcon;

        /// <summary>The Leilão icon when its file exists, otherwise the hourglass (looked up once).</summary>
        public static string AuctionOrFallback => _auctionIcon ?? (_auctionIcon = ArtAssets.Icon(Auction) != null ? Auction : Hourglass);

        /// <summary>
        /// The 12 one-colour symbols (ART_BIBLE_PIXEL 3.3, "c"): white glyphs the code tints. Every other pixel icon is
        /// already coloured and is never tinted (only its transparency follows the colour asked for).
        /// </summary>
        public static bool IsSymbol(string name)
        {
            switch (name)
            {
                case Add: case Close: case Check: case Chevron: case Arrow: case Play: case Pause: case Stop:
                case Sort: case Compact: case Info: case Warning:
                    return true;
                default:
                    return false;
            }
        }
    }
}
