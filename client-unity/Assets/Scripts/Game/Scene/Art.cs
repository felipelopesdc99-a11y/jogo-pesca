using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishingIdle.Game.Scene
{
    /// <summary>
    /// Placeholder art generated from code: gradients, silhouettes, a boat, fish, clouds.
    /// </summary>
    /// <remarks>
    /// Everything visual in the MVP comes from here so the project runs without any imported asset.
    /// Replacing the placeholder art later means swapping what these methods return for real
    /// sprites; nothing that uses them needs to change. Shapes are drawn with a one-pixel soft edge
    /// so they stay smooth when scaled.
    /// </remarks>
    public static class Art
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        // ------------------------------------------------------------------ basic shapes

        /// <summary>A white 1×1 unit square. Scale and tint it for bars, lines and rods.</summary>
        public static Sprite Pixel => Cached("pixel", () =>
        {
            var tex = NewTexture(4, 4);
            Fill(tex, Color.white);
            return ToSprite(tex, 4, new Vector2(0.5f, 0.5f));
        });

        /// <summary>A white square pivoted on its left edge, for lines drawn from a point.</summary>
        public static Sprite PixelLeft => Cached("pixel-left", () =>
        {
            var tex = NewTexture(4, 4);
            Fill(tex, Color.white);
            return ToSprite(tex, 4, new Vector2(0f, 0.5f));
        });

        /// <summary>A white disc, 1 unit across.</summary>
        public static Sprite Circle => Cached("circle", () =>
        {
            const int size = 64;
            var tex = NewTexture(size, size);
            var r = size / 2f;
            Paint(tex, (x, y) =>
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                return new Color(1, 1, 1, Edge(r - d));
            });
            return ToSprite(tex, size, new Vector2(0.5f, 0.5f));
        });

        /// <summary>A soft radial glow, 1 unit across, bright in the middle.</summary>
        public static Sprite Glow => Cached("glow", () =>
        {
            const int size = 128;
            var tex = NewTexture(size, size);
            var r = size / 2f;
            Paint(tex, (x, y) =>
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                var a = Mathf.Clamp01(1f - d);
                return new Color(1, 1, 1, a * a);
            });
            return ToSprite(tex, size, new Vector2(0.5f, 0.5f));
        });

        /// <summary>A thin elliptical ring, 1 unit wide, for ripples on the water.</summary>
        public static Sprite Ripple => Cached("ripple", () =>
        {
            const int w = 128, h = 48;
            var tex = NewTexture(w, h);
            Paint(tex, (x, y) =>
            {
                var nx = (x + 0.5f - w / 2f) / (w / 2f);
                var ny = (y + 0.5f - h / 2f) / (h / 2f);
                var d = Mathf.Sqrt(nx * nx + ny * ny);
                var ring = 1f - Mathf.Abs(d - 0.85f) / 0.1f;
                return new Color(1, 1, 1, Mathf.Clamp01(ring));
            });
            return ToSprite(tex, w, new Vector2(0.5f, 0.5f));
        });

        /// <summary>A horizontal streak of light for the water surface.</summary>
        public static Sprite Streak => Cached("streak", () =>
        {
            const int w = 128, h = 8;
            var tex = NewTexture(w, h);
            Paint(tex, (x, y) =>
            {
                var nx = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                var ny = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                var a = Mathf.Clamp01(1f - nx * nx) * Mathf.Clamp01(1f - ny);
                return new Color(1, 1, 1, a);
            });
            return ToSprite(tex, w, new Vector2(0.5f, 0.5f));
        });

        /// <summary>A vertical gradient, 1×1 unit, top colour first.</summary>
        public static Sprite VerticalGradient(string key, params Color[] topToBottom)
        {
            return Cached("vgrad-" + key, () =>
            {
                const int h = 256;
                var tex = NewTexture(h, h);
                Paint(tex, (x, y) => SampleGradient(topToBottom, 1f - (y + 0.5f) / h));
                return ToSprite(tex, h, new Vector2(0.5f, 0.5f));
            });
        }

        // ------------------------------------------------------------------ landscape

        /// <summary>A rolling hill silhouette, 1 unit tall per 0.25 width units (pivot bottom-centre).</summary>
        public static Sprite Hills(string key, int seed, float roughness, Color top, Color bottom)
        {
            return Cached("hills-" + key, () =>
            {
                const int w = 1024, h = 256;
                var tex = NewTexture(w, h);
                var rng = new System.Random(seed);
                var phases = new float[4];
                for (var i = 0; i < phases.Length; i++)
                {
                    phases[i] = (float)rng.NextDouble() * 10f;
                }

                Paint(tex, (x, y) =>
                {
                    var u = (x + 0.5f) / w;
                    var ridge = 0.55f
                        + 0.22f * Mathf.Sin(u * 5.1f + phases[0])
                        + 0.12f * roughness * Mathf.Sin(u * 13.7f + phases[1])
                        + 0.06f * roughness * Mathf.Sin(u * 31.3f + phases[2])
                        + 0.03f * roughness * Mathf.Sin(u * 71.9f + phases[3]);
                    var v = (y + 0.5f) / h;
                    var alpha = Edge((ridge - v) * h);
                    var c = Color.Lerp(bottom, top, v / Mathf.Max(0.01f, ridge));
                    c.a = alpha;
                    return c;
                });
                return ToSprite(tex, 256, new Vector2(0.5f, 0f));
            });
        }

        /// <summary>A far shore tree line: overlapping rounded crowns (pivot bottom-centre).</summary>
        public static Sprite TreeLine(string key, int seed, Color crown, Color shade)
        {
            return Cached("trees-" + key, () =>
            {
                const int w = 1024, h = 192;
                var tex = NewTexture(w, h);
                var rng = new System.Random(seed);
                var crowns = new List<Vector3>();
                for (var x = -20f; x < w + 20f; x += 14f + (float)rng.NextDouble() * 22f)
                {
                    var radius = 18f + (float)rng.NextDouble() * 34f;
                    var height = 30f + (float)rng.NextDouble() * 90f;
                    crowns.Add(new Vector3(x, height, radius));
                }

                Paint(tex, (x, y) =>
                {
                    var best = 0f;
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    foreach (var c in crowns)
                    {
                        if (y < c.y)
                        {
                            // The trunk area under a crown is solid down to the ground.
                            if (Mathf.Abs(p.x - c.x) < c.z * 0.9f)
                            {
                                best = 1f;
                                break;
                            }

                            continue;
                        }

                        var d = Vector2.Distance(p, new Vector2(c.x, c.y));
                        best = Mathf.Max(best, Edge(c.z - d));
                    }

                    var col = Color.Lerp(shade, crown, Mathf.Clamp01((y + 0.5f) / h * 1.3f));
                    col.a = best;
                    return col;
                });
                return ToSprite(tex, 256, new Vector2(0.5f, 0f));
            });
        }

        /// <summary>A fluffy cloud, about 2 units wide.</summary>
        public static Sprite Cloud(int variant)
        {
            return Cached("cloud-" + variant, () =>
            {
                const int w = 256, h = 128;
                var tex = NewTexture(w, h);
                var rng = new System.Random(100 + variant);
                var puffs = new List<Vector3>();
                for (var i = 0; i < 6; i++)
                {
                    var x = 50f + (float)rng.NextDouble() * 156f;
                    var r = 22f + (float)rng.NextDouble() * 26f;
                    puffs.Add(new Vector3(x, 40f + r * 0.6f, r));
                }

                Paint(tex, (x, y) =>
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var a = 0f;
                    foreach (var c in puffs)
                    {
                        a = Mathf.Max(a, Edge((c.z - Vector2.Distance(p, new Vector2(c.x, c.y))) * 0.5f));
                    }

                    // Flat-ish base, like a fair-weather cumulus.
                    a *= Mathf.Clamp01((y - 30f) / 8f);
                    var shade = Mathf.Lerp(0.86f, 1f, Mathf.Clamp01((y - 30f) / 60f));
                    return new Color(shade, shade, Mathf.Min(1f, shade + 0.03f), a);
                });
                return ToSprite(tex, 128, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>A reed blade, slightly curved, pivot at its base.</summary>
        public static Sprite Reed(bool withCattail)
        {
            return Cached("reed-" + withCattail, () =>
            {
                const int w = 32, h = 256;
                var tex = NewTexture(w, h);
                Paint(tex, (x, y) =>
                {
                    var v = (y + 0.5f) / h;
                    var centre = w * 0.5f + Mathf.Sin(v * 1.6f) * 6f;
                    var half = Mathf.Lerp(3.2f, 0.6f, v);
                    var a = Edge(half - Mathf.Abs(x + 0.5f - centre));
                    var col = Color.Lerp(new Color(0.20f, 0.36f, 0.16f), new Color(0.46f, 0.62f, 0.28f), v);

                    if (withCattail && v > 0.62f && v < 0.84f)
                    {
                        var ca = Edge(4.2f - Mathf.Abs(x + 0.5f - centre));
                        if (ca > a)
                        {
                            col = new Color(0.40f, 0.25f, 0.13f);
                            a = ca;
                        }
                    }

                    col.a = a;
                    return col;
                });
                return ToSprite(tex, 128, new Vector2(0.5f, 0f));
            });
        }

        /// <summary>A small bird in flight (a soft "m" shape), pivot centre.</summary>
        public static Sprite Bird => Cached("bird", () =>
        {
            const int w = 64, h = 24;
            var tex = NewTexture(w, h);
            Paint(tex, (x, y) =>
            {
                var u = (x + 0.5f) / w * 2f - 1f;
                var curve = h * (0.35f + 0.45f * (1f - Mathf.Abs(Mathf.Abs(u) * 2f - 1f)));
                var thickness = Mathf.Lerp(2.4f, 0.8f, Mathf.Abs(u));
                var a = Edge(thickness - Mathf.Abs(y + 0.5f - curve));
                return new Color(0.16f, 0.18f, 0.22f, a);
            });
            return ToSprite(tex, 64, new Vector2(0.5f, 0.5f));
        });

        // ------------------------------------------------------------------ boat and fisherman

        /// <summary>A small wooden rowing boat seen from the side, about 3.6 units long.</summary>
        public static Sprite Boat => Cached("boat", () =>
        {
            const int w = 512, h = 128;
            var tex = NewTexture(w, h);
            var wood = new Color(0.55f, 0.33f, 0.18f);
            var woodDark = new Color(0.38f, 0.21f, 0.11f);
            var rim = new Color(0.78f, 0.58f, 0.34f);
            Paint(tex, (x, y) =>
            {
                var u = (x + 0.5f) / w * 2f - 1f;
                var v = (y + 0.5f) / h;
                var bottom = 0.08f + 0.62f * Mathf.Pow(Mathf.Abs(u), 3.2f);
                var top = 0.80f + 0.14f * Mathf.Pow(Mathf.Abs(u), 4f);
                var a = Mathf.Min(Edge((v - bottom) * h), Edge((top - v) * h));
                a = Mathf.Min(a, Edge((1f - Mathf.Abs(u)) * w * 0.5f));
                var plank = Mathf.Repeat((v - bottom) * 9f, 1f) < 0.12f ? 0.82f : 1f;
                var col = Color.Lerp(woodDark, wood, Mathf.Clamp01((v - bottom) / 0.6f)) * plank;
                if (top - v < 0.08f)
                {
                    col = rim;
                }

                col.a = a;
                return col;
            });
            return ToSprite(tex, 142, new Vector2(0.5f, 0.3f));
        });

        /// <summary>A straw hat: brim + crown with a band, about 0.9 units wide.</summary>
        public static Sprite Hat => Cached("hat", () =>
        {
            const int w = 128, h = 64;
            var tex = NewTexture(w, h);
            var straw = new Color(0.93f, 0.80f, 0.49f);
            var band = new Color(0.62f, 0.20f, 0.16f);
            Paint(tex, (x, y) =>
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var brim = Edge((1f - EllipseDistance(p, new Vector2(64, 14), new Vector2(62, 9))) * 9f);
                var crown = Edge((1f - EllipseDistance(p, new Vector2(64, 26), new Vector2(30, 28))) * 28f) * Edge(p.y - 14f);
                var a = Mathf.Max(brim, crown);
                var col = straw * Mathf.Lerp(0.85f, 1.05f, p.y / h);
                if (crown > 0.5f && p.y > 17f && p.y < 25f)
                {
                    col = band;
                }

                col.a = a;
                return col;
            });
            return ToSprite(tex, 142, new Vector2(0.5f, 0.2f));
        });

        /// <summary>A rounded rectangle, 1×1 unit, for bodies and limbs.</summary>
        public static Sprite RoundedBox => Cached("rounded-box", () =>
        {
            const int size = 64;
            var tex = NewTexture(size, size);
            Paint(tex, (x, y) =>
            {
                var d = RoundedRectDistance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f), new Vector2(size / 2f, size / 2f), 18f);
                return new Color(1, 1, 1, Edge(-d));
            });
            return ToSprite(tex, size, new Vector2(0.5f, 0.5f));
        });

        // ------------------------------------------------------------------ fish

        /// <summary>The placeholder fish for a species, facing right, about 2 units long.</summary>
        public static Sprite Fish(string speciesId) => Cached("fish-" + speciesId, () => ToSprite(FishTexture(speciesId), 128, new Vector2(0.5f, 0.5f)));

        /// <summary>The same fish as a texture, for the interface.</summary>
        public static Texture2D FishTexture(string speciesId)
        {
            if (FishTextures.TryGetValue(speciesId ?? string.Empty, out var cached))
            {
                return cached;
            }

            var look = FishLooks.For(speciesId);
            const int w = 256, h = 128;
            var tex = NewTexture(w, h);
            var body = new Vector2(w * 0.54f, h * 0.5f);
            var radii = new Vector2(w * 0.36f, h * 0.30f * Mathf.Min(look.Chubbiness, 1.25f));
            var outline = look.Back * 0.55f;
            outline.a = 1;

            Paint(tex, (x, y) =>
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);

                // Body: an ellipse, slightly pointed at the head.
                var bodyD = EllipseDistance(p, body, radii);
                var bodyA = Edge((1f - bodyD) * radii.y);

                // Tail: a forked fan behind the body.
                var tx = (w * 0.22f - p.x) / (w * 0.2f);
                var spread = Mathf.Lerp(0.08f, 0.42f, Mathf.Clamp01(tx)) * h;
                var fork = Mathf.Clamp01(tx) * h * 0.14f;
                var dy = Mathf.Abs(p.y - h * 0.5f);
                var tailA = tx > -0.1f && tx < 1f ? Edge(spread - dy) * Edge((dy - fork + h * 0.06f) * 0.8f + 2f) : 0f;

                // Dorsal fin: a soft triangle on the back.
                var fx = (p.x - w * 0.42f) / (w * 0.22f);
                var finTop = body.y + radii.y + (1f - Mathf.Abs(fx * 2f - 1f)) * h * 0.13f * look.FinSize;
                var finA = fx > 0f && fx < 1f && p.y > body.y ? Edge(finTop - p.y) : 0f;

                var alpha = Mathf.Max(bodyA, Mathf.Max(tailA, finA));
                if (alpha <= 0f)
                {
                    return Color.clear;
                }

                // Countershading: dark back, pale belly.
                var t = Mathf.Clamp01((p.y - (body.y - radii.y)) / (2f * radii.y));
                var col = Color.Lerp(look.Belly, look.Back, Mathf.SmoothStep(0.15f, 0.85f, t));

                if (bodyA < 0.5f)
                {
                    col = Color.Lerp(look.Back, look.Fin, 0.6f); // fins and tail
                }
                else
                {
                    col = ApplyPattern(look, col, p, body, radii);

                    // Gill line and eye.
                    var gill = Mathf.Abs(Vector2.Distance(p, new Vector2(w * 0.86f, body.y)) - h * 0.24f);
                    if (gill < 1.4f && p.x < w * 0.76f && p.x > w * 0.70f)
                    {
                        col = Color.Lerp(col, outline, 0.5f);
                    }

                    var eye = Vector2.Distance(p, new Vector2(w * 0.80f, body.y + h * 0.05f));
                    if (eye < h * 0.065f)
                    {
                        col = eye < h * 0.035f ? new Color(0.05f, 0.05f, 0.07f) : new Color(0.97f, 0.96f, 0.9f);
                    }
                }

                // Darker rim so the fish reads against any background.
                if (bodyA > 0.5f && bodyD > 0.9f)
                {
                    col = Color.Lerp(col, outline, 0.55f);
                }

                col.a = alpha;
                return col;
            });

            FishTextures[speciesId ?? string.Empty] = tex;
            return tex;
        }

        private static readonly Dictionary<string, Texture2D> FishTextures = new Dictionary<string, Texture2D>();

        private static Color ApplyPattern(FishLook look, Color col, Vector2 p, Vector2 body, Vector2 radii)
        {
            var u = (p.x - (body.x - radii.x)) / (2f * radii.x);
            var v = (p.y - (body.y - radii.y)) / (2f * radii.y);
            switch (look.Pattern)
            {
                case FishPattern.Bars:
                    if (Mathf.Repeat(u * 5f, 1f) < 0.22f && u > 0.12f && u < 0.8f && v > 0.25f)
                    {
                        return Color.Lerp(col, look.Mark, 0.65f);
                    }

                    break;
                case FishPattern.Spots:
                    var cell = new Vector2(Mathf.Floor(u * 12f), Mathf.Floor(v * 6f));
                    var jitter = Hash(cell);
                    var centre = new Vector2((cell.x + 0.3f + jitter * 0.4f) / 12f, (cell.y + 0.5f) / 6f);
                    if (Vector2.Distance(new Vector2(u, v * 0.5f), new Vector2(centre.x, centre.y * 0.5f)) < 0.022f && v > 0.35f)
                    {
                        return Color.Lerp(col, look.Mark, 0.8f);
                    }

                    break;
                case FishPattern.Stripe:
                    if (Mathf.Abs(v - 0.52f) < 0.06f && u > 0.1f && u < 0.85f)
                    {
                        return Color.Lerp(col, look.Mark, 0.6f);
                    }

                    break;
                case FishPattern.Scales:
                    var s = Mathf.Repeat(u * 14f + Mathf.Floor(v * 7f) * 0.5f, 1f);
                    if (s < 0.1f && v > 0.2f && v < 0.9f)
                    {
                        return Color.Lerp(col, look.Mark, 0.35f);
                    }

                    break;
            }

            return col;
        }

        // ------------------------------------------------------------------ interface textures

        /// <summary>A rounded rectangle texture for the IMGUI skin (use with a matching border).</summary>
        public static Texture2D RoundedRectTexture(Color fill, Color border, int radius, float borderWidth)
        {
            var size = radius * 2 + 4;
            var tex = NewTexture(size, size);
            var half = new Vector2(size / 2f, size / 2f);
            Paint(tex, (x, y) =>
            {
                var d = RoundedRectDistance(new Vector2(x + 0.5f, y + 0.5f), half, half, radius);
                var a = Edge(-d);
                var col = borderWidth > 0f && d > -borderWidth ? border : fill;
                col.a *= a;
                return col;
            });
            return tex;
        }

        /// <summary>A plain 1×1 texture of one colour.</summary>
        public static Texture2D SolidTexture(Color color)
        {
            var tex = NewTexture(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ helpers

        private static Sprite Cached(string key, Func<Sprite> create)
        {
            if (!Cache.TryGetValue(key, out var sprite) || sprite == null)
            {
                sprite = create();
                sprite.name = key;
                Cache[key] = sprite;
            }

            return sprite;
        }

        private static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
        }

        private static Sprite ToSprite(Texture2D tex, float pixelsPerUnit, Vector2 pivot)
        {
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static void Fill(Texture2D tex, Color color)
        {
            var pixels = new Color[tex.width * tex.height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            tex.SetPixels(pixels);
            tex.Apply();
        }

        private static void Paint(Texture2D tex, Func<int, int, Color> shader)
        {
            var w = tex.width;
            var h = tex.height;
            var pixels = new Color[w * h];
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    pixels[y * w + x] = shader(x, y);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
        }

        /// <summary>Coverage of a pixel given its signed distance (in pixels) inside a shape.</summary>
        private static float Edge(float insideDistance) => Mathf.Clamp01(insideDistance + 0.5f);

        private static float EllipseDistance(Vector2 p, Vector2 centre, Vector2 radii)
        {
            var d = new Vector2((p.x - centre.x) / radii.x, (p.y - centre.y) / radii.y);
            return d.magnitude;
        }

        private static float RoundedRectDistance(Vector2 p, Vector2 centre, Vector2 half, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x - centre.x), Mathf.Abs(p.y - centre.y)) - half + new Vector2(radius, radius);
            var outside = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude;
            return outside + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
        }

        private static Color SampleGradient(Color[] stops, float t)
        {
            if (stops.Length == 1)
            {
                return stops[0];
            }

            var scaled = Mathf.Clamp01(t) * (stops.Length - 1);
            var index = Mathf.Min(stops.Length - 2, Mathf.FloorToInt(scaled));
            return Color.Lerp(stops[index], stops[index + 1], scaled - index);
        }

        private static float Hash(Vector2 cell)
        {
            var h = Mathf.Sin(cell.x * 127.1f + cell.y * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }
    }
}
