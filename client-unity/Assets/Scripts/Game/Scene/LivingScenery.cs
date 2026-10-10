using System;
using System.Collections;
using System.Collections.Generic;
using FishingIdle.Game.Bootstrap;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FishingIdle.Game.Scene
{
    // The living landscape (addendum A-078): loose plants that bend in gusts of wind and animals that
    // show up now and then, painted by the owner (Resources/Arte/Vivos). Pure presentation: no rule,
    // reward or mechanic. Placement, weights and rhythm live in Resources/Visual/paisagem_viva.json.

    /// <summary>The shape of Resources/Visual/paisagem_viva.json (read with JsonUtility).</summary>
    [Serializable]
    public sealed class LivingFile
    {
        public float first_event_min = 12f;
        public float first_event_max = 25f;
        public float rest_min = 25f;
        public float rest_max = 60f;
        public float small_gap = 6f;
        public float gust_min = 8f;
        public float gust_max = 20f;
        public LivingSceneConfig[] scenes;
    }

    [Serializable]
    public sealed class LivingSceneConfig
    {
        public string scene;
        public LivingPlant[] plants;
        public LivingSpot[] spots;
        public LivingAnimal[] animals;
    }

    [Serializable]
    public sealed class LivingPlant
    {
        public string art;
        public string side;
        public float x;
        public float y;
        public float height;
        public float bend;
        public string layer;
    }

    [Serializable]
    public sealed class LivingSpot
    {
        public string id;
        public string side;
        public float x;
        public float y;
    }

    [Serializable]
    public sealed class LivingAnimal
    {
        public string id;
        public float weight;
    }

    public static class LivingConfig
    {
        private const string ResourcePath = "Visual/paisagem_viva";
        private static LivingFile _file;
        private static bool _loaded;

        /// <summary>The whole file, or null when it is missing or broken (the scene then has no extra life).</summary>
        public static LivingFile File
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

        /// <summary>The plants, spots and animals of one scene (SceneTheme.ArtFolder), or null.</summary>
        public static LivingSceneConfig For(string scene)
        {
            var file = File;
            if (file?.scenes == null)
            {
                return null;
            }

            foreach (var s in file.scenes)
            {
                if (s != null && s.scene == scene)
                {
                    return s;
                }
            }

            return null;
        }

        /// <summary>World position of a placement measured from a screen edge ("left"/"right") or the centre.</summary>
        public static Vector3 Place(string side, float x, float y, float halfWidth)
        {
            switch (side)
            {
                case "left": return new Vector3(-halfWidth + x, y, 0f);
                case "right": return new Vector3(halfWidth - x, y, 0f);
                default: return new Vector3(x, y, 0f);
            }
        }

        private static LivingFile Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[FishingIdle] Living landscape file not found at Resources/" + ResourcePath + ".json; no extra plants or animals.");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<LivingFile>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FishingIdle] Living landscape file could not be read; no extra plants or animals. " + e.Message);
                return null;
            }
        }
    }

    /// <summary>
    /// Gusts of wind that cross the screen from left to right every few seconds (random, never on a
    /// beat). Between gusts the air is almost still, so plants barely move.
    /// </summary>
    public static class Wind
    {
        private const float StartX = -15f;
        private static float _gustStart = -100f;
        private static float _speed = 4f;
        private static float _strength;
        private static float _nextGust;
        private static int _frame = -1;

        public static float MinGap = 8f;
        public static float MaxGap = 20f;

        /// <summary>How strong the wind is at world x right now: 0 = still, 1 = a full gust.</summary>
        public static float At(float x)
        {
            Tick();
            var front = StartX + (Time.time - _gustStart) * _speed;
            var d = x - front;
            // The gust arrives quickly and dies down slowly behind its front.
            var k = d > 0f ? Mathf.Exp(-(d * d) / 1.6f) : Mathf.Exp(-(d * d) / 14f);
            return _strength * k;
        }

        private static void Tick()
        {
            if (_frame == Time.frameCount)
            {
                return;
            }

            _frame = Time.frameCount;
            if (Time.time < _nextGust)
            {
                return;
            }

            _gustStart = Time.time;
            _speed = Random.Range(3f, 5f);
            _strength = Random.Range(0.5f, 1f);
            _nextGust = Time.time + Random.Range(MinGap, MaxGap) / Mathf.Max(0.3f, LifeSettings.Intensity);
        }
    }

    /// <summary>
    /// A plant rooted at its pivot that bends with the wind: a spring pulls it towards the bend the
    /// gust asks for, so it leans, overshoots a little and settles, like a real stem.
    /// </summary>
    public sealed class WindSway : MonoBehaviour
    {
        /// <summary>Degrees of bend in a full gust.</summary>
        public float Bend = 2f;

        /// <summary>How fast the stem swings back (about once per second at 1).</summary>
        public float Stiffness = 1f;

        private float _angle;
        private float _velocity;
        private float _phase;
        private float _base;

        private void Start()
        {
            _phase = Random.value * 20f;
            _base = transform.localEulerAngles.z;
        }

        private void Update()
        {
            var dt = Mathf.Min(Time.deltaTime, 0.05f);
            var target = 0f;
            if (!LifeSettings.Off)
            {
                var t = Time.time + _phase;
                var gust = Wind.At(transform.position.x);
                var breeze = 0.06f + 0.05f * Mathf.Sin(t * 0.7f) + 0.03f * Mathf.Sin(t * 1.9f);
                var flutter = Mathf.Sin(t * 8.5f) * 0.12f * gust;
                // The wind blows to the right, so the tops lean right (negative z).
                target = -(breeze + gust + flutter) * Bend;
            }

            var w = Mathf.PI * 2f * Stiffness;
            _velocity += (w * w * (target - _angle) - 2f * 0.35f * w * _velocity) * dt;
            _angle += _velocity * dt;
            transform.localRotation = Quaternion.Euler(0f, 0f, _base + _angle);
        }
    }

    /// <summary>Something floating on calm water (a lily pad): a slow bob and a tiny turn.</summary>
    public sealed class Floating : MonoBehaviour
    {
        private float _phase;
        private Vector3 _origin;

        private void Start()
        {
            _phase = Random.value * 10f;
            _origin = transform.localPosition;
        }

        private void Update()
        {
            var t = Time.time + _phase;
            transform.localPosition = _origin + new Vector3(Mathf.Sin(t * 0.45f) * 0.012f, Mathf.Sin(t * 1.2f) * 0.008f, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.8f) * 1.2f);
        }
    }

    /// <summary>A dim, squashed mirror image of a sprite standing on the water line (its reflection).</summary>
    public sealed class Mirror : MonoBehaviour
    {
        private SpriteRenderer _source;
        private SpriteRenderer _renderer;
        private float _alpha;

        public static void Attach(SpriteRenderer source, float squash, float alpha)
        {
            var go = new GameObject("Reflexo");
            go.transform.SetParent(source.transform, false);
            go.transform.localScale = new Vector3(1f, -squash, 1f);
            var mirror = go.AddComponent<Mirror>();
            mirror._source = source;
            mirror._alpha = alpha;
            mirror._renderer = go.AddComponent<SpriteRenderer>();
            mirror._renderer.sortingOrder = FishingScene.OrderReflection + 1;
            mirror.LateUpdate();
        }

        private void LateUpdate()
        {
            if (_source == null)
            {
                return;
            }

            _renderer.sprite = _source.sprite;
            _renderer.flipX = _source.flipX;
            var a = _source.color.a * _alpha;
            _renderer.color = new Color(0.5f, 0.58f, 0.72f, a);
        }
    }

    /// <summary>Plays a row of frames in a loop (wings flapping).</summary>
    public sealed class Flipbook : MonoBehaviour
    {
        public Sprite[] Frames;
        public float Fps = 8f;
        public bool Playing = true;

        private SpriteRenderer _renderer;
        private float _t;

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _t = Random.value * 4f;
        }

        private void Update()
        {
            if (!Playing || Frames == null || Frames.Length == 0 || _renderer == null)
            {
                return;
            }

            _t += Time.deltaTime * Fps;
            _renderer.sprite = Frames[(int)_t % Frames.Length];
        }

        /// <summary>Stops on one frame (a glide).</summary>
        public void Hold(int frame)
        {
            Playing = false;
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }

            _renderer.sprite = Frames[Mathf.Clamp(frame, 0, Frames.Length - 1)];
        }
    }

    /// <summary>
    /// The "scene director" (addendum A-078): decides when the next animal shows up so the scene
    /// never feels spammy. At most one animal at a time; after it leaves, a random rest; the next one
    /// is drawn by weight and is never the one that just left; nothing new starts while a window is
    /// open, during a celebration or on a trip. The small effects (dragonflies, fish shadows and
    /// jumps) ask <see cref="AllowSmall"/> before appearing, so they keep a gap from each other too.
    /// </summary>
    public sealed class SceneDirector : MonoBehaviour
    {
        private GameRoot _root;
        private LivingSceneConfig _scene;
        private LivingFile _rhythm;
        private LivingAnimals _animals;
        private float _nextAt;
        private string _last;
        private bool _busy;
        private float _lastSmall = -100f;
        private float _eventStartedAt = -100f;

        public static SceneDirector Current { get; private set; }

        public void Setup(GameRoot root, LivingSceneConfig scene, LivingFile rhythm, LivingAnimals animals)
        {
            _root = root;
            _scene = scene;
            _rhythm = rhythm;
            _animals = animals;
            Wind.MinGap = Mathf.Max(2f, rhythm.gust_min);
            Wind.MaxGap = Mathf.Max(Wind.MinGap, rhythm.gust_max);
            _nextAt = Time.time + Random.Range(rhythm.first_event_min, rhythm.first_event_max) / Pace;
        }

        private static float Pace => Mathf.Max(0.2f, LifeSettings.Intensity);

        /// <summary>True while nothing new should appear: a window, a celebration or a trip.</summary>
        public bool Quiet => _root != null && (_root.WindowOpen || _root.Celebrations.Active || (_root.Travel != null && _root.Travel.Active));

        /// <summary>
        /// Asked by a small effect before it appears. Says no while the scene is quiet, right after an
        /// animal arrived, or too soon after the last small effect; a yes books the slot.
        /// </summary>
        public static bool AllowSmall()
        {
            var d = Current;
            if (d == null || d._rhythm == null)
            {
                return true;
            }

            if (d.Quiet || Time.time - d._lastSmall < d._rhythm.small_gap || Time.time - d._eventStartedAt < 5f)
            {
                return false;
            }

            d._lastSmall = Time.time;
            return true;
        }

        private void Awake()
        {
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        private void Update()
        {
            if (_animals == null || LifeSettings.Off || _busy || Time.time < _nextAt)
            {
                return;
            }

            if (Quiet)
            {
                _nextAt = Time.time + 3f;
                return;
            }

            var pick = Pick();
            if (pick == null)
            {
                _nextAt = Time.time + 10f;
                return;
            }

            _busy = true;
            _last = pick;
            _eventStartedAt = Time.time;
            StartCoroutine(Run(pick));
        }

        private IEnumerator Run(string id)
        {
            yield return StartCoroutine(_animals.Play(id));
            _busy = false;
            _nextAt = Time.time + Random.Range(_rhythm.rest_min, Mathf.Max(_rhythm.rest_min, _rhythm.rest_max)) / Pace;
        }

        private string Pick()
        {
            if (_scene.animals == null)
            {
                return null;
            }

            var options = new List<LivingAnimal>();
            var total = 0f;
            foreach (var a in _scene.animals)
            {
                if (a != null && a.weight > 0f && a.id != _last && _animals.CanPlay(a.id))
                {
                    options.Add(a);
                    total += a.weight;
                }
            }

            var roll = Random.value * total;
            foreach (var a in options)
            {
                roll -= a.weight;
                if (roll <= 0f)
                {
                    return a.id;
                }
            }

            return options.Count > 0 ? options[options.Count - 1].id : null;
        }
    }
}
