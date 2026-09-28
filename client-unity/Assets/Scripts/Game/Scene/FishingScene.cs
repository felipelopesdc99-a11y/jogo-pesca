using FishingIdle.Game.Bootstrap;
using UnityEngine;

namespace FishingIdle.Game.Scene
{
    /// <summary>Colours of one map's scenery. Map 2 (Milestone 4) adds its own theme.</summary>
    public sealed class SceneTheme
    {
        public Color SkyTop, SkyHorizon, Sun, FarHillTop, FarHillBottom, MidHillTop, MidHillBottom;
        public Color TreeCrown, TreeShade, WaterHorizon, WaterMid, WaterDeep, Glint;

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
                Glint = new Color(1f, 0.96f, 0.85f),
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
        public const int OrderSky = 0, OrderSun = 1, OrderClouds = 2, OrderBirds = 3, OrderFarHills = 4,
            OrderMidHills = 5, OrderTrees = 6, OrderWater = 7, OrderWaterDetail = 8, OrderDistantFish = 9,
            OrderBobber = 10, OrderBoat = 12, OrderFisherman = 13, OrderRod = 14, OrderCatchGlow = 20,
            OrderCatch = 21, OrderForeground = 30;

        private Transform _world;

        public Camera Camera { get; private set; }

        public FishermanRig Fisherman { get; private set; }

        public void Build(GameRoot root)
        {
            var theme = SceneTheme.LagoSereno();

            _world = new GameObject("Cenário").transform;
            _world.SetParent(transform, false);

            BuildCamera(theme);
            BuildSky(theme);
            BuildLand(theme);
            BuildWater(theme);
            BuildForeground();

            var boat = BuildBoat();
            Fisherman = boat.gameObject.AddComponent<FishermanRig>();
            Fisherman.Build(root, boat, _world);
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
            Sprite(far, "Morros", Art.Hills("far", 3, 0.6f, theme.FarHillTop, theme.FarHillBottom),
                new Vector3(0f, Horizon - 0.05f, 0f), new Vector3(9f, 1.9f, 1f), OrderFarHills);

            var mid = Layer("Morros próximos", 0.55f);
            Sprite(mid, "Morros", Art.Hills("mid", 11, 1f, theme.MidHillTop, theme.MidHillBottom),
                new Vector3(2f, Horizon - 0.05f, 0f), new Vector3(8f, 1.05f, 1f), OrderMidHills);

            var trees = Layer("Mata da margem", 0.35f);
            Sprite(trees, "Árvores", Art.TreeLine("shore", 21, theme.TreeCrown, theme.TreeShade),
                new Vector3(0f, Horizon - 0.08f, 0f), new Vector3(8f, 1.1f, 1f), OrderTrees);
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
            }

            var jumps = new GameObject("Peixes saltando ao longe");
            jumps.transform.SetParent(water, false);
            var jumper = jumps.AddComponent<DistantFishJumps>();
            jumper.SortingOrder = OrderDistantFish;
            jumper.Area = new Rect(-9f, -1.5f, 18f, 1.4f);
        }

        private void BuildForeground()
        {
            var fg = Layer("Primeiro plano", -0.25f);

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
            var boat = new GameObject("Barco").transform;
            boat.SetParent(layer, false);
            boat.localPosition = new Vector3(-1.2f, -2.1f, 0f);
            boat.gameObject.AddComponent<Bobbing>();

            Sprite(boat, "Casco", Art.Boat, Vector3.zero, Vector3.one, OrderBoat);
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
        private void LateUpdate()
        {
            var t = Time.time;
            transform.localPosition = new Vector3(Mathf.Sin(t * 0.05f) * 0.35f, Mathf.Sin(t * 0.037f) * 0.06f, -10f);
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
