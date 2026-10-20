using System;
using UnityEngine;

namespace FishingIdle.Game.Scene
{
    // The bait on the hook in the fishing scene (addendum A-096). Pure presentation: the id comes from the game
    // service (GearView); its colour lives in Resources/Visual/equipamento_cena.json. The boat and the rod of the
    // pixel scene come from Resources/Visual/cena_pixel.json (PixelSceneConfig); the painted fisherman, boats and
    // rods that this file also described were removed with the old art in M25-T17.

    /// <summary>The shape of Resources/Visual/equipamento_cena.json (read with JsonUtility).</summary>
    [Serializable]
    public sealed class EquipmentLookFile
    {
        public EquipmentBaitLook[] baits;
    }

    /// <summary>The bait on the hook: the colour of its dot in the scene.</summary>
    [Serializable]
    public sealed class EquipmentBaitLook
    {
        public string id;
        public string color;
    }

    public static class EquipmentLook
    {
        private const string ResourcePath = "Visual/equipamento_cena";
        private static EquipmentLookFile _file;
        private static bool _loaded;

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
                Debug.LogWarning("[FishingIdle] Equipment look file not found at Resources/" + ResourcePath + ".json; the bait is not drawn.");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<EquipmentLookFile>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FishingIdle] Equipment look file could not be read; the bait is not drawn. " + e.Message);
                return null;
            }
        }
    }
}
