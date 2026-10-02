using System.Collections.Generic;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.Scene
{
    /// <summary>Colours of one map's scenery. Map 2 (Milestone 4) adds its own theme.</summary>
    public sealed class SceneTheme
    {
        public Color SkyTop, SkyHorizon, Sun, FarHillTop, FarHillBottom, MidHillTop, MidHillBottom;
        public Color TreeCrown, TreeShade, WaterHorizon, WaterMid, WaterDeep, Glint;
        public int HillSeed = 3, TreeSeed = 21;
        public float HillHeight = 1.9f, TreeHeight = 1.1f;

        /// <summary>Speed of the light streaks across the water: 0 for a lake, positive for a river current.</summary>
        public float Current;
        public bool Rocks;
        public bool Waterfall;

        /// <summary>Folder and file prefix of the painted layers (Resources/Arte/Mapas), e.g. "LagoSereno" and "map_lago_sereno".</summary>
        public string ArtFolder, ArtPrefix;

        /// <summary>Where the sun is painted in the sky, so its reflection lines up on the water.</summary>
        public Vector2 SunPosition = new Vector2(4.6f, 3.1f);

        /// <summary>True when the sun is low enough to draw a column of light on the water.</summary>
        public bool SunColumn = true;

        /// <summary>Heights in world units of the shore groups and the foreground corners (painted art, Resources/Arte/Mapas).</summary>
        public float NearLeftHeight = 3f, NearRightHeight = 2.6f, CornerHeight = 4.4f;

        public string ArtPath(string layer) => "Mapas/" + ArtFolder + "/" + ArtPrefix + "_" + layer;

        /// <summary>The scenery for a map id. Unknown maps fall back to the lake.</summary>
        public static SceneTheme For(string mapId)
        {
            switch (mapId)
            {
                case "map_02": return RioSelvagem();
                case "map_03": return PantanalDourado();
                case "map_04": return EstuarioDasMares();
                default: return LagoSereno();
            }
        }

        /// <summary>Pantanal Dourado: wide flooded plain at dawn, calm golden water, palms and capões (docs/PROGRESSAO_MAPAS_3_4.md).</summary>
        public static SceneTheme PantanalDourado()
        {
            return new SceneTheme
            {
                SkyTop = new Color(0.44f, 0.58f, 0.77f),
                SkyHorizon = new Color(1f, 0.89f, 0.65f),
                Sun = new Color(1f, 0.96f, 0.80f),
                FarHillTop = new Color(0.56f, 0.65f, 0.72f),
                FarHillBottom = new Color(0.86f, 0.80f, 0.66f),
                MidHillTop = new Color(0.34f, 0.44f, 0.24f),
                MidHillBottom = new Color(0.49f, 0.56f, 0.28f),
                TreeCrown = new Color(0.24f, 0.35f, 0.17f),
                TreeShade = new Color(0.15f, 0.24f, 0.12f),
                WaterHorizon = new Color(0.96f, 0.84f, 0.60f),
                WaterMid = new Color(0.31f, 0.49f, 0.52f),
                WaterDeep = new Color(0.14f, 0.27f, 0.31f),
                Glint = new Color(1f, 0.90f, 0.66f),
                HillSeed = 13,
                TreeSeed = 57,
                HillHeight = 0.9f,
                TreeHeight = 0.9f,
                ArtFolder = "PantanalDourado",
                ArtPrefix = "map_pantanal_dourado",
                SunPosition = new Vector2(-5.0f, 1.5f),
                NearLeftHeight = 2.6f,
                NearRightHeight = 2.6f,
            };
        }

        /// <summary>Estuário das Marés: mangrove channel at a warm coastal sunset, darker brackish water, a slow tide.</summary>
        public static SceneTheme EstuarioDasMares()
        {
            return new SceneTheme
            {
                SkyTop = new Color(0.24f, 0.25f, 0.49f),
                SkyHorizon = new Color(1f, 0.70f, 0.42f),
                Sun = new Color(1f, 0.94f, 0.75f),
                FarHillTop = new Color(0.42f, 0.35f, 0.53f),
                FarHillBottom = new Color(0.84f, 0.60f, 0.53f),
                MidHillTop = new Color(0.12f, 0.23f, 0.19f),
                MidHillBottom = new Color(0.20f, 0.31f, 0.23f),
                TreeCrown = new Color(0.14f, 0.29f, 0.19f),
                TreeShade = new Color(0.05f, 0.14f, 0.09f),
                WaterHorizon = new Color(0.95f, 0.66f, 0.44f),
                WaterMid = new Color(0.12f, 0.29f, 0.31f),
                WaterDeep = new Color(0.05f, 0.14f, 0.16f),
                Glint = new Color(1f, 0.76f, 0.48f),
                HillSeed = 17,
                TreeSeed = 63,
                HillHeight = 1.2f,
                TreeHeight = 1.4f,
                Current = 0.25f,
                ArtFolder = "EstuarioDasMares",
                ArtPrefix = "map_estuario_das_mares",
                SunPosition = new Vector2(2.4f, 1.8f),
                NearLeftHeight = 3f,
                NearRightHeight = 2.8f,
            };
        }

        /// <summary>Rio Selvagem: bigger river, current, rocks, dense forest, waterfall and mist, cooler light.</summary>
        public static SceneTheme RioSelvagem()
        {
            return new SceneTheme
            {
                SkyTop = new Color(0.30f, 0.50f, 0.68f),
                SkyHorizon = new Color(0.82f, 0.86f, 0.82f),
                Sun = new Color(1f, 0.96f, 0.86f),
                FarHillTop = new Color(0.36f, 0.48f, 0.52f),
                FarHillBottom = new Color(0.48f, 0.58f, 0.62f),
                MidHillTop = new Color(0.18f, 0.36f, 0.26f),
                MidHillBottom = new Color(0.28f, 0.44f, 0.34f),
                TreeCrown = new Color(0.12f, 0.30f, 0.18f),
                TreeShade = new Color(0.06f, 0.18f, 0.12f),
                WaterHorizon = new Color(0.52f, 0.68f, 0.66f),
                WaterMid = new Color(0.16f, 0.40f, 0.42f),
                WaterDeep = new Color(0.04f, 0.17f, 0.22f),
                Glint = new Color(0.92f, 0.97f, 1f),
                HillSeed = 8,
                TreeSeed = 44,
                HillHeight = 2.6f,
                TreeHeight = 1.5f,
                Current = 0.6f,
                Rocks = true,
                Waterfall = true,
                ArtFolder = "RioSelvagem",
                ArtPrefix = "map_rio_selvagem",
                SunPosition = new Vector2(-4.5f, 3.9f),
                SunColumn = false,
                NearLeftHeight = 3f,
                NearRightHeight = 2.8f,
            };
        }

        /// <summary>Lago Sereno: calm lake, warm late-afternoon light (GDD section 18).</summary>
        public static SceneTheme LagoSereno()
        {
            return new SceneTheme
            {
                SkyTop = new Color(0.36f, 0.62f, 0.86f),
                SkyHorizon = new Color(0.99f, 0.86f, 0.66f),
                Sun = new Color(1f, 0.93f, 0.72f),
                FarHillTop = new Color(0.56f, 0.64f, 0.76f),
                FarHillBottom = new Color(0.66f, 0.72f, 0.80f),
                MidHillTop = new Color(0.38f, 0.55f, 0.46f),
                MidHillBottom = new Color(0.50f, 0.62f, 0.56f),
                TreeCrown = new Color(0.20f, 0.40f, 0.25f),
                TreeShade = new Color(0.12f, 0.26f, 0.18f),
                WaterHorizon = new Color(0.72f, 0.84f, 0.84f),
                WaterMid = new Color(0.24f, 0.55f, 0.62f),
                WaterDeep = new Color(0.07f, 0.26f, 0.36f),
                Glint = new Color(1f, 0.90f, 0.66f),
                ArtFolder = "LagoSereno",
                ArtPrefix = "map_lago_sereno",
                SunPosition = new Vector2(5.3f, 1.95f),
                NearLeftHeight = 3f,
                NearRightHeight = 2.5f,
            };
        }
    }

    /// <summary>
    /// Builds the living fishing scene from code: sky, hills, trees, water, boat, fisherman and
    /// all the small ambient motion. Presentation only.
    /// </summary>
    /// <remarks>
    /// World units: the camera shows 10.8 units of height; the horizon sits at y = 0.2 and the
    /// water fills everything below it. Sorting orders go back to front; see the constants.
    /// </remarks>
    public sealed class FishingScene : MonoBehaviour
    {
        public const float Horizon = 0.2f;

        // Sorting orders, back to front.
        public const int OrderSky = 0, OrderSun = 10, OrderClouds = 20, OrderBirds = 30, OrderFarHills = 40,
            OrderMidHills = 50, OrderSkyLife = 55, OrderTrees = 60, OrderShorePlants = 62, OrderShoreLife = 65, OrderWater = 70,
            OrderReflection = 80, OrderWaterDetail = 90, OrderWaterLife = 95, OrderDistantFish = 100,
            OrderBobber = 110, OrderBoat = 120, OrderFisherman = 130, OrderBoatFront = 140, OrderRod = 150, OrderCatchGlow = 200,
            OrderCatch = 210, OrderCornerBack = 295, OrderForeground = 300, OrderCornerFront = 305;

        private Transform _world;
        private Transform _boatLayer;
        private GameRoot _root;
        private SceneTheme _theme;
        private float _arrivedAt = -100f;

        // The equipped boat (A-096): the starter hull pieces, the Shop boat pieces and what is shown now.
        private readonly System.Collections.Generic.List<SpriteRenderer> _starterHull = new System.Collections.Generic.List<SpriteRenderer>();
        private SpriteRenderer _gearHullBack;
        private SpriteRenderer _gearHullFront;
        private string _shownBoat, _shownRod, _shownBait;
        private bool _gearShown;

        /// <summary>0–1 darkness the HUD draws over the scene while the trip ends and the new map appears.</summary>
        public float TravelFade
        {
            get
            {
                var travel = _root != null ? _root.Travel : null;
                if (travel != null && travel.Active)
                {
                    return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 1f, (float)travel.Progress));
                }

                return 1f - Mathf.Clamp01((Time.time - _arrivedAt) / 1.2f);
            }
        }

        public Camera Camera { get; private set; }

        public FishermanRig Fisherman { get; private set; }

        public void Build(GameRoot root)
        {
            _root = root;
            _theme = SceneTheme.For(root.Player?.MapId);
            BuildCamera(_theme);
            BuildWorld(_theme);
            root.MapChanged += OnMapChanged;
        }

        private void OnDestroy()
        {
            if (_root != null)
            {
                _root.MapChanged -= OnMapChanged;
            }
        }

        /// <summary>Arriving on another map swaps the whole scenery; the boat glides in from the left.</summary>
        private void OnMapChanged(string mapId)
        {
            _theme = SceneTheme.For(mapId);
            if (_world != null)
            {
                Destroy(_world.gameObject);
            }

            Camera.backgroundColor = _theme.SkyTop;
            BuildWorld(_theme);
            _arrivedAt = Time.time;
        }

        private void BuildWorld(SceneTheme theme)
        {
            _world = new GameObject("Cenário").transform;
            _world.SetParent(transform, false);

            // Painted layers (Resources/Arte/Mapas) when present; the scenery drawn from code otherwise.
            if (ArtAssets.Texture(theme.ArtPath("bg_sky")) != null)
            {
                BuildPainted(theme);
            }
            else
            {
                BuildSky(theme);
                BuildLand(theme);
                BuildWater(theme);
                BuildForeground(theme);
            }

            var boat = BuildBoat();
            Fisherman = boat.gameObject.AddComponent<FishermanRig>();
            Fisherman.Build(_root, boat, _world);
            _gearShown = false;
            ShowGear();
        }

        /// <summary>Puts the boat, rod and bait in use in the scene, when they change (A-096).</summary>
        private void ShowGear()
        {
            var gear = _root != null ? _root.Gear : null;
            if (gear == null || Fisherman == null)
            {
                return;
            }

            if (_gearShown && gear.BoatId == _shownBoat && gear.RodId == _shownRod && gear.BaitId == _shownBait)
            {
                return;
            }

            _gearShown = true;
            _shownBoat = gear.BoatId;
            _shownRod = gear.RodId;
            _shownBait = gear.BaitId;

            // A Shop boat: its picture behind the fisherman and its near side in front of him. The
            // starter boat (or a boat without pictures) keeps the scene's own hull.
            var look = EquipmentLook.Boat(gear.BoatId);
            var back = look != null ? ArtAssets.Sprite(look.art, look.width, new Vector2(0.5f, look.pivot_y)) : null;
            var front = look != null ? ArtAssets.Sprite(look.front, look.width, new Vector2(0.5f, look.pivot_y)) : null;
            var shopBoat = back != null && front != null;
            foreach (var piece in _starterHull)
            {
                piece.enabled = !shopBoat;
            }

            _gearHullBack.sprite = back;
            _gearHullFront.sprite = front;
            _gearHullBack.enabled = _gearHullFront.enabled = shopBoat;
            Fisherman.SitAt(shopBoat ? new Vector3(look.seat_x, look.seat_y, 0f) : (Vector3?)null);
            Fisherman.UseRod(gear.RodId);
            Fisherman.UseBait(gear.BaitId);
        }

        private void Update()
        {
            if (_boatLayer == null || _root == null)
            {
                return;
            }

            // Travel is presentation of the service's timer: the boat sails out to the right…
            var travel = _root.Travel;
            var x = 0f;
            if (travel != null && travel.Active)
            {
                var p = (float)travel.Progress;
                x = p * p * 14f;
            }
            else
            {
                // …and glides in from the left on the new map.
                var t = Mathf.Clamp01((Time.time - _arrivedAt) / 2.5f);
                x = -9f * (1f - t) * (1f - t);
            }

            _boatLayer.localPosition = new Vector3(x, 0f, 0f);
            ShowGear();
        }

        private void BuildCamera(SceneTheme theme)
        {
            Camera = Camera.main;
            if (Camera == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                Camera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            Camera.transform.SetParent(transform, false);
            Camera.transform.position = new Vector3(0f, 0f, -10f);
            Camera.transform.rotation = Quaternion.identity;
            Camera.orthographic = true;
            Camera.orthographicSize = 5.4f;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = theme.SkyTop;
            Camera.gameObject.AddComponent<CameraSway>();
        }

        /// <summary>
        /// The painted scenery (Art Bible, sections 9–10): layers back to front with parallax, plus the
        /// motion that keeps the scene alive — drifting clouds, a breathing sun, glints on the water,
        /// birds, distant fish and reeds swaying in the corners.
        /// </summary>
        private void BuildPainted(SceneTheme theme)
        {
            const float width = 26f;
            var bottom = new Vector2(0.5f, 0f);

            var sky = Layer("Céu", 0.95f);
            Painted(sky, theme.ArtPath("bg_sky"), width, bottom, new Vector3(0f, Horizon - 0.2f, 0f), OrderSky);
            var sunGlow = Sprite(sky, "Brilho do sol", Art.Glow, new Vector3(theme.SunPosition.x, theme.SunPosition.y, 0f), Vector3.one * 3.2f, OrderSun,
                new Color(1f, 0.93f, 0.75f, 0.35f));
            var breathe = sunGlow.gameObject.AddComponent<Breathe>();
            breathe.MinAlpha = 0.18f;
            breathe.MaxAlpha = 0.38f;

            var clouds = Layer("Nuvens", 0.85f);
            for (var i = 0; i < 5; i++)
            {
                var cloudSprite = ArtAssets.Sprite(theme.ArtPath("cloud_" + (i % 3 + 1).ToString("00")), 5.2f, new Vector2(0.5f, 0.5f));
                if (cloudSprite == null)
                {
                    continue;
                }

                var scale = Random.Range(0.55f, 1.05f);
                var cloud = Sprite(clouds, "Nuvem", cloudSprite, new Vector3(-12f + i * 5.2f + Random.Range(-1f, 1f), Random.Range(2.6f, 4.7f), 0f),
                    Vector3.one * scale, OrderClouds, new Color(1f, 1f, 1f, Random.Range(0.8f, 0.95f)));
                cloud.gameObject.AddComponent<Drift>().Speed = Random.Range(0.04f, 0.1f);
            }

            var halfWidth = Camera != null ? Camera.orthographicSize * Camera.aspect : 9.6f;
            var living = LivingConfig.For(theme.ArtFolder);
            if (living == null)
            {
                // Without the living landscape, the simple drawn flock keeps the sky alive.
                var birds = new GameObject("Pássaros");
                birds.transform.SetParent(clouds, false);
                birds.AddComponent<BirdFlock>().SortingOrder = OrderBirds;
            }

            // Land layers, each with its reflection on the water (drawn from the art itself, so it
            // always matches whatever painting is in the file).
            Reflect(Painted(Layer("Montanhas distantes", 0.75f), theme.ArtPath("bg_far"), width, bottom, new Vector3(0f, Horizon - 0.1f, 0f), OrderFarHills), 0.6f, 0.35f);
            Reflect(Painted(Layer("Morros próximos", 0.55f), theme.ArtPath("bg_mid"), width, bottom, new Vector3(0f, Horizon - 0.1f, 0f), OrderMidHills), 0.6f, 0.4f);
            var shore = Layer("Mata da margem", 0.35f);
            var left = ArtAssets.SpriteByHeight(theme.ArtPath("near_left"), theme.NearLeftHeight, new Vector2(0f, 0f));
            var right = ArtAssets.SpriteByHeight(theme.ArtPath("near_right"), theme.NearRightHeight, new Vector2(1f, 0f));
            if (left != null && right != null)
            {
                // The two shore groups are pinned to the edges of the view, leaving the lake open in the middle.
                Reflect(Sprite(shore, "Margem esquerda", left, new Vector3(-halfWidth - 0.3f, Horizon - 0.08f, 0f), Vector3.one, OrderTrees), 0.7f, 0.5f);
                Reflect(Sprite(shore, "Margem direita", right, new Vector3(halfWidth + 0.3f, Horizon - 0.08f, 0f), Vector3.one, OrderTrees), 0.7f, 0.5f);
            }
            else
            {
                Painted(shore, theme.ArtPath("bg_near"), width, bottom, new Vector3(0f, Horizon - 0.15f, 0f), OrderTrees);
            }

            var water = Layer("Água", 0f);
            Painted(water, theme.ArtPath("water"), width, new Vector2(0.5f, 1f), new Vector3(0f, Horizon, 0f), OrderWater);
            Sprite(water, "Linha do horizonte", Art.Pixel, new Vector3(0f, Horizon, 0f), new Vector3(44f, 0.03f, 1f), OrderWaterDetail, new Color(1f, 0.95f, 0.85f, 0.35f));

            // The surface itself moves: rows of wave glints in perspective.
            var waves = new GameObject("Ondas").AddComponent<WaterWaves>();
            waves.transform.SetParent(water, false);
            waves.Horizon = Horizon;
            waves.Current = theme.Current;
            waves.Tint = theme.SunColumn ? new Color(1f, 0.93f, 0.82f) : new Color(0.88f, 0.97f, 1f);
            waves.SortingOrder = OrderWaterDetail;

            if (theme.SunColumn)
            {
                // Glints that shimmer over the painted reflection of the sun.
                for (var i = 0; i < 12; i++)
                {
                    var y = Horizon - 0.2f - i * 0.36f;
                    var glint = Sprite(water, "Reflexo do sol", Art.Streak,
                        new Vector3(theme.SunPosition.x + Random.Range(-0.35f, 0.35f) * (1f + i * 0.15f), y, 0f),
                        new Vector3(Random.Range(0.6f, 1.4f) * (1f + i * 0.14f), 1.1f, 1f), OrderWaterDetail, theme.Glint);
                    var twinkle = glint.gameObject.AddComponent<Twinkle>();
                    twinkle.MaxAlpha = Mathf.Lerp(0.85f, 0.3f, i / 12f);
                    twinkle.Period = Random.Range(1.6f, 3.2f);
                }
            }

            for (var i = 0; i < 26; i++)
            {
                var y = Random.Range(-5.2f, Horizon - 0.3f);
                var depth = Mathf.InverseLerp(Horizon, -5.4f, y);
                var streak = Sprite(water, "Brilho na água", Art.Streak, new Vector3(Random.Range(-11f, 11f), y, 0f),
                    new Vector3(Mathf.Lerp(0.5f, 2.4f, depth), Mathf.Lerp(0.6f, 1.4f, depth), 1f), OrderWaterDetail, Color.white);
                var twinkle = streak.gameObject.AddComponent<Twinkle>();
                twinkle.MaxAlpha = Random.Range(0.08f, 0.22f);
                twinkle.Period = Random.Range(2.5f, 5f);
                if (theme.Current > 0f)
                {
                    var drift = streak.gameObject.AddComponent<Drift>();
                    drift.Speed = theme.Current * Mathf.Lerp(0.5f, 1.4f, depth);
                    drift.WrapHalfWidth = 12f;
                }
            }

            var jumps = new GameObject("Peixes saltando ao longe");
            jumps.transform.SetParent(water, false);
            var jumper = jumps.AddComponent<DistantFishJumps>();
            jumper.SortingOrder = OrderDistantFish;
            jumper.Area = new Rect(-9f, -1.5f, 18f, 1.4f);

            BuildLife(theme, water);

            // Foreground corners, anchored to the edges of the view whatever the screen shape.
            var fg = Layer("Primeiro plano", -0.25f);
            Corner(fg, theme.ArtPath("fg_left"), new Vector2(0f, 0f), new Vector3(-halfWidth - 0.4f, -5.7f, 0f), theme.CornerHeight);
            Corner(fg, theme.ArtPath("fg_right"), new Vector2(1f, 0f), new Vector3(halfWidth + 0.4f, -5.7f, 0f), theme.CornerHeight);

            if (living != null)
            {
                BuildLiving(theme, living, halfWidth, clouds, shore, water, fg);
            }
        }

        /// <summary>
        /// The living landscape (addendum A-078, Resources/Visual/paisagem_viva.json): loose plants
        /// that bend in the gusts, lily pads, and the scene director that brings the animals.
        /// </summary>
        private void BuildLiving(SceneTheme theme, LivingSceneConfig living, float halfWidth, Transform sky, Transform shore, Transform water, Transform corner)
        {
            var pads = new List<Transform>();
            if (living.plants != null)
            {
                foreach (var p in living.plants)
                {
                    if (p == null || string.IsNullOrEmpty(p.art) || p.height <= 0f)
                    {
                        continue;
                    }

                    var floating = p.layer == "water";
                    var sprite = ArtAssets.SpriteByHeight("Vivos/Plantas/" + p.art, p.height, floating ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f));
                    if (sprite == null)
                    {
                        continue;
                    }

                    var position = LivingConfig.Place(p.side, p.x, p.y, halfWidth);
                    switch (p.layer)
                    {
                        case "water":
                            var pad = Sprite(water, "Vitória-régia", sprite, position, Vector3.one, OrderWaterLife);
                            pad.gameObject.AddComponent<Floating>();
                            pads.Add(pad.transform);
                            break;
                        case "corner_back":
                        case "corner_front":
                            var reed = Sprite(corner, "Planta", sprite, position, Vector3.one, p.layer == "corner_back" ? OrderCornerBack : OrderCornerFront);
                            var reedSway = reed.gameObject.AddComponent<WindSway>();
                            reedSway.Bend = p.bend;
                            reedSway.Stiffness = 1.3f;
                            break;
                        default:
                            // A touch of the distance haze, so a loose tree matches the painted shore behind it.
                            var tree = Sprite(shore, "Árvore", sprite, position, Vector3.one, OrderShorePlants, new Color(0.9f, 0.9f, 0.95f));
                            var treeSway = tree.gameObject.AddComponent<WindSway>();
                            treeSway.Bend = p.bend;
                            treeSway.Stiffness = 0.7f;
                            Reflect(tree, 0.7f, 0.5f);
                            break;
                    }
                }
            }

            var rhythm = LivingConfig.File;
            var director = new GameObject("Diretor de cenário").AddComponent<SceneDirector>();
            director.transform.SetParent(_world, false);
            director.Setup(_root, living, rhythm, new LivingAnimals(living, halfWidth, theme.Current, sky, shore, water, corner, pads));
        }

        /// <summary>The small, quiet signs of life (Life.cs), tuned per map; all of them fade with "ambient_life".</summary>
        private void BuildLife(SceneTheme theme, Transform water)
        {
            var lake = theme.SunColumn;
            var life = new GameObject("Vida").transform;
            life.SetParent(_world, false);

            var shadows = life.gameObject.AddComponent<FishShadows>();
            shadows.SortingOrder = OrderWaterDetail;
            shadows.Tint = lake ? new Color(0.03f, 0.08f, 0.16f) : new Color(0.02f, 0.1f, 0.1f);

            var rays = new GameObject("Raios de sol").AddComponent<SunRays>();
            rays.transform.SetParent(life, false);
            rays.Sun = theme.SunPosition;
            rays.Color = lake ? new Color(1f, 0.84f, 0.58f) : new Color(0.95f, 1f, 0.9f);
            rays.SortingOrder = OrderWaterDetail;

            var mist = new GameObject("Névoa do horizonte").AddComponent<HorizonMist>();
            mist.transform.SetParent(life, false);
            mist.Y = Horizon + 0.12f;
            mist.Color = lake ? new Color(1f, 0.86f, 0.74f) : new Color(0.95f, 0.98f, 1f);
            mist.MaxAlpha = lake ? 0.14f : 0.26f;
            mist.SortingOrder = OrderWaterDetail;

            var motes = new GameObject("Poeira no ar").AddComponent<GoldenMotes>();
            motes.transform.SetParent(life, false);
            motes.Color = lake ? new Color(1f, 0.88f, 0.6f) : new Color(0.92f, 1f, 0.9f);
            motes.Count = lake ? 14 : 9;
            motes.SortingOrder = OrderRod + 1;

            var flies = new GameObject("Libélulas").AddComponent<Dragonflies>();
            flies.transform.SetParent(life, false);
            flies.SortingOrder = OrderForeground + 2;
            flies.HalfWidth = Camera != null ? Camera.orthographicSize * Camera.aspect : 9.6f;

            if (lake)
            {
                var glints = new GameObject("Brilhos do sol").AddComponent<SunGlints>();
                glints.transform.SetParent(water, false);
                glints.X = theme.SunPosition.x;
                glints.SortingOrder = OrderWaterDetail;
            }
        }

        private void Corner(Transform parent, string path, Vector2 pivot, Vector3 position, float height)
        {
            var sprite = ArtAssets.SpriteByHeight(path, height, pivot);
            if (sprite == null)
            {
                return;
            }

            var corner = Sprite(parent, "Juncos", sprite, position, Vector3.one, OrderForeground);
            var sway = corner.gameObject.AddComponent<Sway>();
            sway.Degrees = 0.7f;
            sway.Period = 6f;
        }

        private static SpriteRenderer Painted(Transform parent, string path, float width, Vector2 pivot, Vector3 position, int order)
        {
            var sprite = ArtAssets.Sprite(path, width, pivot);
            return sprite != null ? Sprite(parent, path.Substring(path.LastIndexOf('/') + 1), sprite, position, Vector3.one, order) : null;
        }

        /// <summary>
        /// A darker, squashed mirror image of a land layer below the horizon, cut into horizontal
        /// slices that sway out of step, like a reflection on moving water. Slices further from the
        /// horizon sway more and fade a little.
        /// </summary>
        private static void Reflect(SpriteRenderer land, float squash, float alpha)
        {
            if (land == null)
            {
                return;
            }

            const int slices = 12;
            var source = land.sprite;
            var tex = source.texture;
            var rect = source.rect;
            var ppu = source.pixelsPerUnit;
            var pivotX = source.pivot.x / rect.width;
            var p = land.transform.localPosition;
            var root = new GameObject(land.name + " (reflexo)").transform;
            root.SetParent(land.transform.parent, false);
            for (var i = 0; i < slices; i++)
            {
                var y0 = rect.y + rect.height * i / slices;
                var h = rect.height / slices;
                var slice = UnityEngine.Sprite.Create(tex, new Rect(rect.x, y0, rect.width, h), new Vector2(pivotX, 0f), ppu, 0, SpriteMeshType.FullRect);
                slice.hideFlags = HideFlags.DontSave;
                var depth = (y0 - rect.y) / ppu * squash;
                var k = i / (float)slices;
                var r = Sprite(root, "Faixa", slice, new Vector3(p.x, Horizon - depth, 0f), new Vector3(1f, -squash, 1f),
                    OrderReflection, new Color(0.5f, 0.58f, 0.72f, alpha * (1f - 0.45f * k)));
                var sway = r.gameObject.AddComponent<ReflectionSlice>();
                sway.Amplitude = Mathf.Lerp(0.012f, 0.07f, k);
                sway.Phase = i * 0.8f;
            }
        }

        private void BuildSky(SceneTheme theme)
        {
            var sky = Layer("Céu", 0.95f);
            Sprite(sky, "Gradiente do céu", Art.VerticalGradient("sky", theme.SkyTop, Color.Lerp(theme.SkyTop, theme.SkyHorizon, 0.55f), theme.SkyHorizon),
                new Vector3(0f, Horizon + 3f, 0f), new Vector3(44f, 6.4f, 1f), OrderSky);

            var sun = Sprite(sky, "Brilho do sol", Art.Glow, new Vector3(4.6f, 3.1f, 0f), Vector3.one * 6f, OrderSun, new Color(theme.Sun.r, theme.Sun.g, theme.Sun.b, 0.55f));
            sun.gameObject.AddComponent<Twinkle>().MaxAlpha = 0.55f;
            Sprite(sky, "Sol", Art.Circle, new Vector3(4.6f, 3.1f, 0f), Vector3.one * 1.05f, OrderSun, theme.Sun);

            var clouds = Layer("Nuvens", 0.85f);
            for (var i = 0; i < 6; i++)
            {
                var scale = Random.Range(1.1f, 2.2f);
                var cloud = Sprite(clouds, "Nuvem", Art.Cloud(i % 4),
                    new Vector3(-11f + i * 4.2f + Random.Range(-1f, 1f), Random.Range(2.3f, 4.6f), 0f),
                    new Vector3(scale * 1.4f, scale, 1f), OrderClouds, new Color(1f, 1f, 1f, Random.Range(0.75f, 0.95f)));
                var drift = cloud.gameObject.AddComponent<Drift>();
                drift.Speed = Random.Range(0.05f, 0.14f);
            }

            var birds = new GameObject("Pássaros");
            birds.transform.SetParent(clouds, false);
            birds.AddComponent<BirdFlock>().SortingOrder = OrderBirds;
        }

        private void BuildLand(SceneTheme theme)
        {
            var far = Layer("Morros distantes", 0.75f);
            Sprite(far, "Morros", Art.Hills("far" + theme.HillSeed, theme.HillSeed, 0.6f, theme.FarHillTop, theme.FarHillBottom),
                new Vector3(0f, Horizon - 0.05f, 0f), new Vector3(9f, theme.HillHeight, 1f), OrderFarHills);

            var mid = Layer("Morros próximos", 0.55f);
            Sprite(mid, "Morros", Art.Hills("mid" + theme.HillSeed, theme.HillSeed + 8, 1f, theme.MidHillTop, theme.MidHillBottom),
                new Vector3(2f, Horizon - 0.05f, 0f), new Vector3(8f, 1.05f, 1f), OrderMidHills);

            var trees = Layer("Mata da margem", 0.35f);
            Sprite(trees, "Árvores", Art.TreeLine("shore" + theme.TreeSeed, theme.TreeSeed, theme.TreeCrown, theme.TreeShade),
                new Vector3(0f, Horizon - 0.08f, 0f), new Vector3(8f, theme.TreeHeight, 1f), OrderTrees);

            if (theme.Waterfall)
            {
                // A distant waterfall between the hills, with mist at its foot.
                var falls = Sprite(trees, "Cachoeira", Art.VerticalGradient("falls", new Color(0.86f, 0.93f, 0.96f), new Color(0.72f, 0.84f, 0.9f)),
                    new Vector3(-5.6f, Horizon + 0.75f, 0f), new Vector3(0.45f, 1.5f, 1f), OrderTrees + 1, new Color(1f, 1f, 1f, 0.9f));
                falls.gameObject.AddComponent<Bobbing>().Amplitude = 0.01f;
                Sprite(trees, "Névoa", Art.Glow, new Vector3(-5.6f, Horizon + 0.05f, 0f), new Vector3(2.6f, 0.9f, 1f), OrderWaterDetail, new Color(1f, 1f, 1f, 0.45f));
            }
        }

        private void BuildWater(SceneTheme theme)
        {
            var water = Layer("Água", 0f);
            Sprite(water, "Gradiente da água", Art.VerticalGradient("water", theme.WaterHorizon, theme.WaterMid, theme.WaterDeep),
                new Vector3(0f, Horizon - 3.2f, 0f), new Vector3(44f, 6.4f, 1f), OrderWater);

            Sprite(water, "Linha do horizonte", Art.Pixel, new Vector3(0f, Horizon, 0f), new Vector3(44f, 0.035f, 1f),
                OrderWaterDetail, new Color(1f, 1f, 1f, 0.35f));

            // Reflection of the sun: a column of glints under it.
            for (var i = 0; i < 9; i++)
            {
                var y = Horizon - 0.25f - i * 0.42f;
                var glint = Sprite(water, "Reflexo do sol", Art.Streak,
                    new Vector3(4.6f + Random.Range(-0.3f, 0.3f), y, 0f),
                    new Vector3(Random.Range(0.8f, 1.8f) * (1f + i * 0.12f), 1.2f, 1f), OrderWaterDetail, theme.Glint);
                var twinkle = glint.gameObject.AddComponent<Twinkle>();
                twinkle.MaxAlpha = Mathf.Lerp(0.8f, 0.35f, i / 9f);
                twinkle.Period = Random.Range(1.8f, 3.2f);
            }

            // Scattered light on the surface, larger closer to the viewer.
            for (var i = 0; i < 34; i++)
            {
                var y = Random.Range(-5.2f, Horizon - 0.2f);
                var depth = Mathf.InverseLerp(Horizon, -5.4f, y);
                var streak = Sprite(water, "Brilho na água", Art.Streak,
                    new Vector3(Random.Range(-11f, 11f), y, 0f),
                    new Vector3(Mathf.Lerp(0.5f, 2.6f, depth), Mathf.Lerp(0.6f, 1.6f, depth), 1f), OrderWaterDetail, Color.white);
                var twinkle = streak.gameObject.AddComponent<Twinkle>();
                twinkle.MaxAlpha = Random.Range(0.12f, 0.3f);
                twinkle.Period = Random.Range(2.5f, 5f);
                if (theme.Current > 0f)
                {
                    // River current: the light moves downstream, faster closer to the viewer.
                    var drift = streak.gameObject.AddComponent<Drift>();
                    drift.Speed = theme.Current * Mathf.Lerp(0.5f, 1.4f, depth);
                    drift.WrapHalfWidth = 12f;
                }
            }

            var jumps = new GameObject("Peixes saltando ao longe");
            jumps.transform.SetParent(water, false);
            var jumper = jumps.AddComponent<DistantFishJumps>();
            jumper.SortingOrder = OrderDistantFish;
            jumper.Area = new Rect(-9f, -1.5f, 18f, 1.4f);
        }

        private void BuildForeground(SceneTheme theme)
        {
            var fg = Layer("Primeiro plano", -0.25f);

            if (theme.Rocks)
            {
                var rockColor = new Color(0.30f, 0.33f, 0.34f);
                var rocks = new[] { new Vector3(-8.4f, -3.9f, 1.6f), new Vector3(-6.9f, -4.6f, 1.1f), new Vector3(7.6f, -4.2f, 1.8f), new Vector3(5.2f, -1.1f, 0.7f), new Vector3(-3.4f, -0.6f, 0.5f) };
                foreach (var r in rocks)
                {
                    Sprite(fg, "Pedra", Art.RoundedBox, new Vector3(r.x, r.y, 0f), new Vector3(r.z, r.z * 0.55f, 1f), OrderWaterDetail + 1, rockColor);
                    Sprite(fg, "Espuma", Art.Streak, new Vector3(r.x, r.y - r.z * 0.25f, 0f), new Vector3(r.z * 1.4f, 1.4f, 1f), OrderWaterDetail + 1, new Color(1f, 1f, 1f, 0.45f));
                }
            }

            // Lily pads near the edges.
            for (var i = 0; i < 7; i++)
            {
                var left = i % 2 == 0;
                var x = left ? Random.Range(-9.6f, -6.2f) : Random.Range(6.4f, 9.8f);
                var y = Random.Range(-5f, -3.2f);
                var size = Random.Range(0.5f, 0.95f);
                Sprite(fg, "Vitória-régia", Art.Circle, new Vector3(x, y, 0f), new Vector3(size, size * 0.34f, 1f),
                    OrderWaterDetail, new Color(0.24f, 0.45f, 0.22f, 0.95f));
            }

            // Reed clusters in both bottom corners.
            PlaceReeds(fg, -10.5f, -6.8f, 16);
            PlaceReeds(fg, 7.2f, 10.8f, 14);
        }

        private void PlaceReeds(Transform parent, float fromX, float toX, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var reed = Sprite(parent, "Junco", Art.Reed(Random.value > 0.6f),
                    new Vector3(Random.Range(fromX, toX), -5.6f + Random.Range(0f, 0.5f), 0f),
                    new Vector3(Random.Range(0.8f, 1.2f), Random.Range(0.9f, 1.55f), 1f),
                    OrderForeground + i % 3);
                reed.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-8f, 8f));
                var sway = reed.gameObject.AddComponent<Sway>();
                sway.Degrees = Random.Range(2f, 4.5f);
                sway.Period = Random.Range(3.5f, 5.5f);
            }
        }

        private Transform BuildBoat()
        {
            var layer = Layer("Barco", 0f);
            _boatLayer = layer;
            var boat = new GameObject("Barco").transform;
            boat.SetParent(layer, false);
            boat.localPosition = new Vector3(-1.2f, -2.1f, 0f);
            boat.gameObject.AddComponent<Bobbing>();
            var ripples = layer.gameObject.AddComponent<BoatRipples>();
            ripples.Boat = boat;
            ripples.SortingOrder = OrderWaterDetail;

            // The hull in two layers (Cena/barco_fundo + barco_frente, cut by processar_pedidos.py): the far
            // gunwale and the inside behind the fisherman, the near side in front of him, so he sits
            // inside the boat. Without them, the single picture (or the one drawn from code).
            var hullBack = ArtAssets.Sprite("Cena/barco_fundo", 3.6f, new Vector2(0.5f, 0.3f));
            var hullFront = ArtAssets.Sprite("Cena/barco_frente", 3.6f, new Vector2(0.5f, 0.3f));
            var tackle = ArtAssets.Sprite("Cena/caixa_de_pesca", 0.5f, new Vector2(0.5f, 0f));
            _starterHull.Clear();
            if (hullBack != null && hullFront != null)
            {
                _starterHull.Add(Sprite(boat, "Casco (fundo)", hullBack, Vector3.zero, Vector3.one, OrderBoat));
                _starterHull.Add(Sprite(boat, "Casco (frente)", hullFront, Vector3.zero, Vector3.one, OrderBoatFront));
                if (tackle != null)
                {
                    _starterHull.Add(Sprite(boat, "Caixa de pesca", tackle, new Vector3(0.95f, 0.22f, 0f), Vector3.one, OrderFisherman));
                }
            }
            else
            {
                _starterHull.Add(Sprite(boat, "Casco", ArtAssets.Sprite("Cena/barco", 3.6f, new Vector2(0.5f, 0.3f)) ?? Art.Boat, Vector3.zero, Vector3.one, OrderBoat));
                if (tackle != null)
                {
                    _starterHull.Add(Sprite(boat, "Caixa de pesca", tackle, new Vector3(0.55f, 0.32f, 0f), Vector3.one, OrderBoat - 1));
                }
            }

            // The equipped Shop boat, shown instead of the hull above (ShowGear).
            _gearHullBack = Sprite(boat, "Barco equipado", null, Vector3.zero, Vector3.one, OrderBoat);
            _gearHullFront = Sprite(boat, "Barco equipado (frente)", null, Vector3.zero, Vector3.one, OrderBoatFront);
            _gearHullBack.enabled = _gearHullFront.enabled = false;

            // Soft shadow on the water under the hull.
            Sprite(layer, "Sombra do barco", Art.Circle, new Vector3(-1.2f, -2.35f, 0f), new Vector3(4.4f, 0.45f, 1f),
                OrderWaterDetail, new Color(0.02f, 0.12f, 0.18f, 0.35f));
            return boat;
        }

        // ------------------------------------------------------------------ helpers

        private Transform Layer(string name, float parallax)
        {
            var layer = new GameObject(name).transform;
            layer.SetParent(_world, false);
            if (!Mathf.Approximately(parallax, 0f))
            {
                layer.gameObject.AddComponent<Parallax>().Follow = parallax;
            }

            return layer;
        }

        public static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Vector3 position, Vector3 scale, int order, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.color = color ?? Color.white;
            return renderer;
        }
    }

    /// <summary>Very slow camera drift, so the layered scenery reveals its depth.</summary>
    public sealed class CameraSway : MonoBehaviour
    {
        // Zoom (A-102): from the whole scene to a little closer to the boat. Kept small on purpose, so
        // the painted layers never show their edges.
        private const float WideSize = 5.4f;
        private const float CloseSize = 4.3f;
        private static readonly Vector2 CloseFocus = new Vector2(-0.6f, -0.75f);

        private Camera _camera;
        private float _zoom = -1f;

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            var target = Bootstrap.GameSettings.Zoom;
            _zoom = _zoom < 0f ? target : Mathf.Lerp(_zoom, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 6f));
            if (_camera != null)
            {
                _camera.orthographicSize = Mathf.Lerp(WideSize, CloseSize, _zoom);
            }

            var t = Time.time;
            var focus = CloseFocus * _zoom;
            transform.localPosition = new Vector3(Mathf.Sin(t * 0.05f) * 0.35f * (1f - 0.5f * _zoom) + focus.x, Mathf.Sin(t * 0.037f) * 0.06f + focus.y, -10f);
        }
    }

    /// <summary>Moves a layer with the camera by a fraction: far layers follow more, so they seem slower.</summary>
    public sealed class Parallax : MonoBehaviour
    {
        public float Follow;

        private Transform _camera;
        private Vector3 _origin;

        private void Start()
        {
            _camera = Camera.main != null ? Camera.main.transform : null;
            _origin = transform.localPosition;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                return;
            }

            var cam = _camera.localPosition;
            transform.localPosition = _origin + new Vector3(cam.x * Follow, cam.y * Follow, 0f);
        }
    }
}
