using UnityEngine;

namespace FishingIdle.Game.Scene
{
    /// <summary>
    /// Sky and water drawn by the code in the palette of each map (M25-T06, ART_BIBLE_PIXEL 6.2): flat bands, one
    /// checker row where the sky changes colour, 1-pixel stars and a small moon on the night maps. Made once per map
    /// as one Point texture on the 216-pixel grid (plus a margin for the camera's slow drift).
    /// </summary>
    public static class PixelSky
    {
        /// <summary>Width of the texture in grid pixels: enough for windows up to about 3:1.</summary>
        public const int Width = 640;

        /// <summary>Extra rows above and below the grid, so the camera's drift never shows an edge.</summary>
        public const int Margin = 4;

        public static Texture2D Build(PixelMapLook look, int gridHeight, int horizonRow)
        {
            var h = gridHeight + Margin * 2;
            var pixels = new Color32[Width * h];
            var sky = Colors(look.sky);
            var water = Colors(look.water);
            var skyRows = Rows(look.sky_rows, sky.Length, horizonRow);
            var waterRows = Rows(look.water_rows, water.Length, gridHeight - horizonRow);

            // Row index from the top of the grid (negative and beyond the grid = the margins, which repeat the edge colour).
            for (var y = -Margin; y < gridHeight + Margin; y++)
            {
                Color32 c, next;
                bool checker;
                if (y < horizonRow)
                {
                    Band(sky, skyRows, Mathf.Max(0, y), out c, out next, out checker);
                }
                else
                {
                    Band(water, waterRows, Mathf.Min(gridHeight - 1, y) - horizonRow, out c, out next, out checker);
                    checker = false; // the water changes clean; its glints break the bands up
                }

                var row = (gridHeight + Margin - 1 - y) * Width; // texture rows grow upwards
                for (var x = 0; x < Width; x++)
                {
                    pixels[row + x] = checker && ((x + y) & 1) == 0 ? next : c;
                }
            }

            if (look.stars > 0)
            {
                var rnd = new System.Random(look.id != null ? look.id.GetHashCode() : 9);
                var star = (Color32)PixelSceneConfig.Hex(look.moon_color, Color.white);
                for (var i = 0; i < look.stars * Width / 384; i++)
                {
                    var x = rnd.Next(Width);
                    var y = rnd.Next(Mathf.Max(1, horizonRow * 3 / 4));
                    var strength = 0.5f + 0.25f * rnd.Next(3);
                    var bg = pixels[(gridHeight + Margin - 1 - y) * Width + x];
                    pixels[(gridHeight + Margin - 1 - y) * Width + x] = Color32.Lerp(bg, star, strength);
                }
            }

            if (look.moon && look.moon_r > 0)
            {
                var moon = (Color32)PixelSceneConfig.Hex(look.moon_color, Color.white);
                var cx = Width / 2 + Mathf.RoundToInt(look.moon_x * PixelGrid.PixelsPerUnit);
                var cy = gridHeight / 2 - Mathf.RoundToInt(look.moon_y * PixelGrid.PixelsPerUnit);
                var r = look.moon_r;
                for (var dy = -r; dy <= r; dy++)
                {
                    for (var dx = -r; dx <= r; dx++)
                    {
                        var x = cx + dx;
                        var y = cy + dy;
                        if (dx * dx + dy * dy <= r * r + r * 0.6f && x >= 0 && x < Width && y >= 0 && y < horizonRow)
                        {
                            pixels[(gridHeight + Margin - 1 - y) * Width + x] = moon;
                        }
                    }
                }
            }

            var tex = new Texture2D(Width, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
                name = "Céu e água " + look.folder,
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static Sprite _dot;

        /// <summary>A white square of 1 world unit with its pivot on the bottom-left corner: scaled by 1/20 it is one grid pixel.</summary>
        public static Sprite Dot
        {
            get
            {
                if (_dot == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.DontSave };
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _dot = UnityEngine.Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero, 1f, 0, SpriteMeshType.FullRect);
                    _dot.hideFlags = HideFlags.DontSave;
                }

                return _dot;
            }
        }

        /// <summary>A sprite of the sky texture centred on the camera's centre (world origin), 20 pixels per unit.</summary>
        public static Sprite Sprite(Texture2D tex)
        {
            var sprite = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), PixelGrid.PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static Color32[] Colors(string[] hex)
        {
            var c = new Color32[hex.Length];
            for (var i = 0; i < hex.Length; i++)
            {
                c[i] = PixelSceneConfig.Hex(hex[i], Color.gray);
            }

            return c;
        }

        /// <summary>Row counts per band; when the file has none (or they do not add up), equal bands.</summary>
        private static int[] Rows(int[] rows, int count, int total)
        {
            if (rows != null && rows.Length == count)
            {
                var sum = 0;
                foreach (var r in rows)
                {
                    sum += r;
                }

                if (sum == total)
                {
                    return rows;
                }
            }

            var even = new int[count];
            for (var i = 0; i < count; i++)
            {
                even[i] = total / count + (i < total % count ? 1 : 0);
            }

            return even;
        }

        private static void Band(Color32[] colors, int[] rows, int y, out Color32 c, out Color32 next, out bool checker)
        {
            var start = 0;
            for (var i = 0; i < colors.Length; i++)
            {
                var end = start + rows[i];
                if (y < end || i == colors.Length - 1)
                {
                    c = colors[i];
                    var last = y == end - 1 && i + 1 < colors.Length;
                    next = last ? colors[i + 1] : c;
                    checker = last;
                    return;
                }

                start = end;
            }

            c = next = colors[colors.Length - 1];
            checker = false;
        }
    }

    /// <summary>
    /// Glints on the water (M25-T06): short dashes of light, one grid pixel high, longer near the viewer, that jump to
    /// another place every 0.25–0.5 s and, on rivers and the open sea, slide with the current one pixel at a time.
    /// All renderers are made once; nothing is allocated per frame.
    /// </summary>
    public sealed class PixelGlints : MonoBehaviour
    {
        public int Count = 40;
        public Color Tint = Color.white;
        public float Horizon = FishingScene.Horizon;
        public float Bottom = -5.4f;
        public float HalfWidth = 10f;
        public float Current;
        public int SortingOrder = FishingScene.OrderWaterDetail;

        private SpriteRenderer[] _dashes;
        private float[] _nextAt;

        private void Start()
        {
            _dashes = new SpriteRenderer[Count];
            _nextAt = new float[Count];
            for (var i = 0; i < Count; i++)
            {
                _dashes[i] = FishingScene.Sprite(transform, "Brilho", PixelSky.Dot, Vector3.zero, Vector3.one, SortingOrder, Tint);
                Place(i);
                _nextAt[i] = Time.time + Random.Range(0f, 0.5f);
            }
        }

        private void Update()
        {
            if (_dashes == null)
            {
                return;
            }

            var now = Time.time;
            for (var i = 0; i < _dashes.Length; i++)
            {
                if (now < _nextAt[i])
                {
                    continue;
                }

                _nextAt[i] = now + Random.Range(0.25f, 0.5f);
                if (Current > 0f && Random.value < 0.7f)
                {
                    // Downstream by one grid pixel, wrapping around the view.
                    var p = _dashes[i].transform.localPosition;
                    p.x += PixelGrid.Unit * Mathf.Ceil(Current * 3f);
                    if (p.x > HalfWidth)
                    {
                        p.x = -HalfWidth;
                    }

                    _dashes[i].transform.localPosition = p;
                }
                else
                {
                    Place(i);
                }
            }
        }

        private void Place(int i)
        {
            var px = Mathf.Round((Horizon - Bottom) * PixelGrid.PixelsPerUnit);
            var row = Random.Range(3, (int)px - 2);
            var depth = row / px;
            var length = 1 + Mathf.RoundToInt(depth * 5f);
            var t = _dashes[i].transform;
            t.localPosition = new Vector3(PixelGrid.Snap(Random.Range(-HalfWidth, HalfWidth)), PixelGrid.Snap(Horizon) - row * PixelGrid.Unit, 0f);
            t.localScale = new Vector3(length * PixelGrid.Unit, PixelGrid.Unit, 1f);
            var c = Tint;
            c.a = 0.35f + 0.35f * depth;
            _dashes[i].color = c;
        }
    }
}
