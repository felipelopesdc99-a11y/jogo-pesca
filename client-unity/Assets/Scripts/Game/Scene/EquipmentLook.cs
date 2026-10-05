using System;
using UnityEngine;

namespace FishingIdle.Game.Scene
{
    // The equipped boat, rod and bait drawn in the fishing scene (addendum A-096). Pure presentation:
    // the ids come from the game service (GearView); how each one looks lives in
    // Resources/Visual/equipamento_cena.json, written by tools/Arte/equipamento_na_cena.py.

    /// <summary>The shape of Resources/Visual/equipamento_cena.json (read with JsonUtility).</summary>
    [Serializable]
    public sealed class EquipmentLookFile
    {
        public EquipmentFisherLook fisherman;
        public EquipmentBoatLook[] boats;
        public EquipmentRodLook[] rods;
        public EquipmentBaitLook[] baits;
    }

    /// <summary>
    /// The painted fisherman: his picture, its height in scene units, where he sits (the pivot) and the
    /// point between his fists where the rod turns (0–1, from the bottom-left), plus the fists alone,
    /// drawn over the rod. A height of 0 means the entry is missing (JsonUtility fills absent objects).
    /// </summary>
    [Serializable]
    public sealed class EquipmentFisherLook
    {
        public string art;
        public string hands_art;
        public float height;
        public float pivot_u;
        public float pivot_v;
        public float hands_u;
        public float hands_v;
    }

    /// <summary>A Shop boat in the scene: the whole picture behind the fisherman and its near side in front.</summary>
    [Serializable]
    public sealed class EquipmentBoatLook
    {
        public string id;
        public string art;
        public string front;
        public float width;
        public float pivot_y;
        public float seat_x;
        public float seat_y;
    }

    /// <summary>A painted rod: its butt and tip in the picture (0–1, from the bottom-left) and where the hands hold it.</summary>
    [Serializable]
    public sealed class EquipmentRodLook
    {
        public string id;
        public string art;
        public float butt_u;
        public float butt_v;
        public float tip_u;
        public float tip_v;
        public float grip;
    }

    /// <summary>The bait on the hook: its picture (Resources/Arte/Iscas) or, without it, a placeholder colour.</summary>
    [Serializable]
    public sealed class EquipmentBaitLook
    {
        public string id;
        public string art;
        public float width = 0.22f;
        public string color;
    }

    public static class EquipmentLook
    {
        private const string ResourcePath = "Visual/equipamento_cena";
        private static EquipmentLookFile _file;
        private static bool _loaded;

        /// <summary>The painted fisherman, or null when the file has no valid entry.</summary>
        public static EquipmentFisherLook Fisherman()
        {
            var look = File?.fisherman;
            return look != null && look.height > 0f && !string.IsNullOrEmpty(look.art) ? look : null;
        }

        public static EquipmentBoatLook Boat(string id) => Find(File?.boats, b => b.id == id);

        public static EquipmentRodLook Rod(string id) => Find(File?.rods, r => r.id == id);

        public static EquipmentBaitLook Bait(string id) => Find(File?.baits, b => b.id == id);

        /// <summary>The placeholder colour of a bait, or null when it has none.</summary>
        public static Color? BaitColor(EquipmentBaitLook bait)
        {
            return bait != null && ColorUtility.TryParseHtmlString(bait.color, out var color) ? color : (Color?)null;
        }

        private static EquipmentLookFile File
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

        private static T Find<T>(T[] items, Func<T, bool> match) where T : class
        {
            if (items == null)
            {
                return null;
            }

            foreach (var item in items)
            {
                if (item != null && match(item))
                {
                    return item;
                }
            }

            return null;
        }

        private static EquipmentLookFile Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[FishingIdle] Equipment look file not found at Resources/" + ResourcePath + ".json; the scene keeps the starter boat and rod.");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<EquipmentLookFile>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FishingIdle] Equipment look file could not be read; the scene keeps the starter boat and rod. " + e.Message);
                return null;
            }
        }
    }
}
