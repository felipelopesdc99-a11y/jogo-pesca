using System;
using UnityEngine;

namespace FishingIdle.Game.Scene
{
    // The pixel-art fishing scene (ART_BIBLE_PIXEL, M25-T05 to T08 and T12). Pure presentation: palettes, where each
    // layer sits, the boats' seats and the fisherman's hands live in Resources/Visual/cena_pixel.json, written by
    // tools/Arte/cena_pixel.py, which also draws the previews from the same numbers. Pixels are counted from the
    // top-left corner of each file; band offsets are grid pixels against the horizon (positive = up).

    /// <summary>The shape of Resources/Visual/cena_pixel.json (read with JsonUtility).</summary>
    [Serializable]
    public sealed class PixelSceneFile
    {
        public int grid_height = 216;
        public int horizon_row = 104;
        public PixelMapLook[] maps;
        public PixelFisherLook fisherman;
        public PixelBoatLook[] boats;
        public string starter_boat;
        public int box_x;
        public int box_y;
        public int bobber_waterline = 11;
        public PixelRodLook[] rods;
    }

    /// <summary>One map: sky and water bands (drawn by the code), clouds, glints, reflections and the layer offsets.</summary>
    [Serializable]
    public sealed class PixelMapLook
    {
        public string id;
        public string folder;
        public string prefix;
        public string[] sky;
        public string[] water;
        public int[] sky_rows;
        public int[] water_rows;
        public string cloud;
        public float cloud_alpha = 0.9f;
        public int clouds = 4;
        public string glint;
        public string reflect;
        public float reflect_alpha = 0.5f;
        public bool reflect_bands = true;
        public int far_dy;
        public int mid_dy;
        public int near_dy;
        public int side_dy;
        public int stars;
        public bool moon;
        public float moon_x;
        public float moon_y;
        public int moon_r;
        public string moon_color;
    }

    /// <summary>The fisherman's frames: 64 × 64, feet on (feet_x, feet_y), and where his hands hold the rod in each frame.</summary>
    [Serializable]
    public sealed class PixelFisherLook
    {
        public int frame = 64;
        public int feet_x = 40;
        public int feet_y = 63;
        public int rod_px = 60;
        public PixelFrameLook[] frames;
    }

    [Serializable]
    public sealed class PixelFrameLook
    {
        public string name;
        public int hand_x;
        public int hand_y;
    }

    /// <summary>A pixel boat: its picture, the near side cut by the tool, the water line (pivot) and the fisherman's feet.</summary>
    [Serializable]
    public sealed class PixelBoatLook
    {
        public string id;
        public string art;
        public string front;
        public int pivot_x;
        public int pivot_y;
        public int seat_x;
        public int seat_y;
    }

    /// <summary>The colours of a rod drawn by the code (its shaft and its grip), taken from the rod's pixel picture.</summary>
    [Serializable]
    public sealed class PixelRodLook
    {
        public string id;
        public string shaft;
        public string grip;
    }

    public static class PixelSceneConfig
    {
        private const string ResourcePath = "Visual/cena_pixel";
        private static PixelSceneFile _file;
        private static bool _loaded;

        public static PixelSceneFile File
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    _file = Load();
                }

                return _file;
            }
        }

        /// <summary>The look of a map by its art folder (e.g. "LagoSereno"), or null.</summary>
        public static PixelMapLook Map(string folder)
        {
            var maps = File?.maps;
            if (maps == null)
            {
                return null;
            }

            foreach (var m in maps)
            {
                if (m != null && m.folder == folder && m.sky != null && m.sky.Length > 0 && m.water != null && m.water.Length > 0)
                {
                    return m;
                }
            }

            return null;
        }

        /// <summary>The pixel boat of a boat id, or the starter boat; null when the file has none.</summary>
        public static PixelBoatLook Boat(string id)
        {
            var boats = File?.boats;
            if (boats == null)
            {
                return null;
            }

            PixelBoatLook starter = null;
            foreach (var b in boats)
            {
                if (b == null)
                {
                    continue;
                }

                if (b.id == id)
                {
                    return b;
                }

                if (b.id == File.starter_boat)
                {
                    starter = b;
                }
            }

            return starter;
        }

        public static PixelRodLook Rod(string id)
        {
            var rods = File?.rods;
            if (rods == null)
            {
                return null;
            }

            PixelRodLook first = null;
            foreach (var r in rods)
            {
                if (r == null)
                {
                    continue;
                }

                if (r.id == id)
                {
                    return r;
                }

                first = first ?? r;
            }

            return first;
        }

        /// <summary>A colour written as "#RRGGBB", or <paramref name="fallback"/>.</summary>
        public static Color Hex(string hex, Color fallback)
        {
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        }

        private static PixelSceneFile Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[FishingIdle] Pixel scene file not found at Resources/" + ResourcePath + ".json; the scene keeps the old painted art.");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<PixelSceneFile>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FishingIdle] Pixel scene file could not be read; the scene keeps the old painted art. " + e.Message);
                return null;
            }
        }
    }
}
