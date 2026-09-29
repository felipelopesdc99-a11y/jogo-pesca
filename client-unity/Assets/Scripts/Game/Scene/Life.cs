using System.Collections.Generic;
using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.Scene
{
    // Small, quiet signs of life in the scenery (Art Bible, sections 9 and 15, level 1 "Calmo"):
    // dust in the warm light, dragonflies near the reeds, fish shadows under the surface, sun rays,
    // drifting mist, ripples around the boat, glints on the water and splashes at a catch. All of it
    // is pure presentation, uses UnityEngine.Random, stays at low opacity and scales with
    // "ambient_life" in tema_visual.json (0 turns it off).

    internal static class LifeSettings
    {
        /// <summary>0 = off, 1 = as designed. Read from the visual theme.</summary>
        public static float Intensity => VisualTheme.Current.AmbientLife;

        public static bool Off => Intensity <= 0.01f;
    }

    /// <summary>Specks of dust or pollen floating slowly in the warm light.</summary>
    public sealed class GoldenMotes : MonoBehaviour
    {
        public int Count = 14;
        public Rect Area = new Rect(-9f, -3f, 18f, 6f);
        public Color Color = new Color(1f, 0.88f, 0.6f, 1f);
        public int SortingOrder = 15;

        private readonly List<SpriteRenderer> _motes = new List<SpriteRenderer>();
        private readonly List<Vector3> _seed = new List<Vector3>();

        private void Start()
        {
            var count = Mathf.RoundToInt(Count * LifeSettings.Intensity);
            for (var i = 0; i < count; i++)
            {
                var r = FishingScene.Sprite(transform, "Poeira", Art.Glow,
                    new Vector3(Random.Range(Area.xMin, Area.xMax), Random.Range(Area.yMin, Area.yMax), 0f),
                    Vector3.one * Random.Range(0.06f, 0.13f), SortingOrder, new Color(Color.r, Color.g, Color.b, 0f));
                _motes.Add(r);
                _seed.Add(new Vector3(Random.value * 10f, Random.Range(0.05f, 0.14f), Random.Range(3f, 7f)));
            }
        }

        private void Update()
        {
            for (var i = 0; i < _motes.Count; i++)
            {
                var m = _motes[i];
                var s = _seed[i];
                var t = Time.time + s.x;
                var p = m.transform.localPosition;
                p.x += (s.y + Mathf.Sin(t * 0.4f) * 0.06f) * Time.deltaTime;
                p.y += Mathf.Sin(t * 0.7f) * 0.05f * Time.deltaTime;
                if (p.x > Area.xMax)
                {
                    p.x = Area.xMin;
                    p.y = Random.Range(Area.yMin, Area.yMax);
                }

                m.transform.localPosition = p;
                var a = Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 2f / s.z)) * 0.55f;
                m.color = new Color(Color.r, Color.g, Color.b, a);
            }
        }
    }

    /// <summary>Now and then a dragonfly darts in near the reeds, hovers a little, and leaves.</summary>
    public sealed class Dragonflies : MonoBehaviour
    {
        public float MinDelay = 25f;
        public float MaxDelay = 55f;
        public int SortingOrder = 32;
        public float HalfWidth = 9.6f;

        private float _nextAt;

        private void Start()
        {
            _nextAt = Time.time + Random.Range(8f, 18f);
        }

        private void Update()
        {
            if (LifeSettings.Off || Time.time < _nextAt)
            {
                return;
            }

            _nextAt = Time.time + Random.Range(MinDelay, MaxDelay) / LifeSettings.Intensity;
            var fromLeft = Random.value > 0.5f;
            var go = new GameObject("Libélula");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3((fromLeft ? -1f : 1f) * (HalfWidth + 1f), Random.Range(-3.6f, -2.2f), 0f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = Art.Dragonfly;
            r.sortingOrder = SortingOrder;
            var fly = go.AddComponent<Dragonfly>();
            fly.FromLeft = fromLeft;
            fly.HalfWidth = HalfWidth;
        }
    }

    public sealed class Dragonfly : MonoBehaviour
    {
        public bool FromLeft;
        public float HalfWidth = 9.6f;

        private Vector3 _from;
        private Vector3 _to;
        private float _legStarted;
        private float _legSeconds;
        private float _hoverUntil;
        private int _legs;
        private SpriteRenderer _renderer;

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
            NextLeg();
        }

        private void NextLeg()
        {
            _from = transform.localPosition;
            _legs++;
            var leaving = _legs > 4;
            var side = FromLeft ? -1f : 1f;
            _to = leaving
                ? new Vector3(-side * (HalfWidth + 2f), _from.y + Random.Range(0.5f, 1.5f), 0f)
                : new Vector3(side * Random.Range(HalfWidth - 4.5f, HalfWidth - 1.2f), Random.Range(-3.8f, -1.8f), 0f);
            _legSeconds = leaving ? 2.2f : Random.Range(0.35f, 0.7f);
            _legStarted = Time.time;
            _hoverUntil = 0f;
        }

        private void Update()
        {
            // Wings flicker; the body leans the way it flies.
            var flicker = 0.75f + 0.25f * Mathf.Sin(Time.time * 60f);
            var dir = Mathf.Sign(_to.x - _from.x);
            transform.localScale = new Vector3(0.36f * (dir == 0f ? 1f : dir), 0.36f * flicker, 1f);

            if (_hoverUntil > 0f)
            {
                transform.localPosition = _to + new Vector3(Mathf.Sin(Time.time * 7f) * 0.03f, Mathf.Sin(Time.time * 9f) * 0.04f, 0f);
                if (Time.time > _hoverUntil)
                {
                    NextLeg();
                }

                return;
            }

            var k = Mathf.Clamp01((Time.time - _legStarted) / _legSeconds);
            var eased = 1f - (1f - k) * (1f - k);
            transform.localPosition = Vector3.Lerp(_from, _to, eased) + new Vector3(0f, Mathf.Sin(k * Mathf.PI) * 0.25f, 0f);
            if (k >= 1f)
            {
                if (_legs > 4)
                {
                    Destroy(gameObject);
                    return;
                }

                _hoverUntil = Time.time + Random.Range(0.8f, 2.2f);
            }

            if (_renderer != null)
            {
                _renderer.color = new Color(1f, 1f, 1f, 0.9f);
            }
        }
    }

    /// <summary>Now and then the dark shape of a fish glides slowly under the surface.</summary>
    public sealed class FishShadows : MonoBehaviour
    {
        public float MinDelay = 12f;
        public float MaxDelay = 26f;
        public int SortingOrder = 8;
        public Color Tint = new Color(0.02f, 0.08f, 0.14f, 1f);

        private float _nextAt;

        private void Start()
        {
            _nextAt = Time.time + Random.Range(4f, 10f);
        }

        private void Update()
        {
            if (LifeSettings.Off || Time.time < _nextAt)
            {
                return;
            }

            _nextAt = Time.time + Random.Range(MinDelay, MaxDelay) / LifeSettings.Intensity;
            var go = new GameObject("Sombra de peixe");
            go.transform.SetParent(transform, false);
            var toRight = Random.value > 0.5f;
            var y = Random.Range(-4.6f, -1.4f);
            go.transform.localPosition = new Vector3(toRight ? -11f : 11f, y, 0f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = Art.Fish(null);
            r.sortingOrder = SortingOrder;
            r.color = new Color(Tint.r, Tint.g, Tint.b, 0f);
            var glide = go.AddComponent<FishShadow>();
            glide.Speed = Random.Range(0.35f, 0.7f) * (toRight ? 1f : -1f);
            // Closer to the viewer (lower) means bigger.
            glide.Scale = Mathf.Lerp(0.55f, 0.3f, Mathf.InverseLerp(-4.6f, -1.4f, y));
            glide.Tint = Tint;
        }
    }

    public sealed class FishShadow : MonoBehaviour
    {
        public float Speed = 0.5f;
        public float Scale = 0.4f;
        public Color Tint;

        private SpriteRenderer _renderer;
        private float _age;

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var p = transform.localPosition;
            p.x += Speed * Time.deltaTime;
            p.y += Mathf.Sin(_age * 0.8f) * 0.02f * Time.deltaTime;
            transform.localPosition = p;
            var wiggle = Mathf.Sin(_age * 3.2f) * 4f;
            transform.localRotation = Quaternion.Euler(0f, 0f, wiggle);
            transform.localScale = new Vector3(Mathf.Sign(Speed) * Scale, Scale * 0.8f, 1f);
            var fade = Mathf.Clamp01(_age / 2f) * Mathf.Clamp01((12f - Mathf.Abs(p.x)) / 2f);
            _renderer.color = new Color(Tint.r, Tint.g, Tint.b, 0.2f * fade);
            if (Mathf.Abs(p.x) > 11.5f && _age > 3f)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>A few soft beams of light fanning from the low sun, breathing very slowly.</summary>
    public sealed class SunRays : MonoBehaviour
    {
        public Vector2 Sun;
        public Color Color = new Color(1f, 0.86f, 0.6f, 1f);
        public int SortingOrder = 8;

        private readonly List<SpriteRenderer> _beams = new List<SpriteRenderer>();
        private readonly List<float> _phase = new List<float>();

        private void Start()
        {
            if (LifeSettings.Off)
            {
                return;
            }

            var angles = new[] { 18f, 32f, 47f, 63f };
            foreach (var angle in angles)
            {
                var beam = FishingScene.Sprite(transform, "Raio de sol", Art.Beam, new Vector3(Sun.x, Sun.y, 0f),
                    new Vector3(Random.Range(0.9f, 1.6f), Random.Range(2.2f, 3.2f), 1f), SortingOrder, new Color(Color.r, Color.g, Color.b, 0f));
                beam.transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
                _beams.Add(beam);
                _phase.Add(Random.value * 10f);
            }
        }

        private void Update()
        {
            for (var i = 0; i < _beams.Count; i++)
            {
                var k = 0.5f + 0.5f * Mathf.Sin((Time.time + _phase[i]) * 0.35f);
                _beams[i].color = new Color(Color.r, Color.g, Color.b, Mathf.Lerp(0.015f, 0.06f, k) * LifeSettings.Intensity);
            }
        }
    }

    /// <summary>Wide, faint banks of mist drifting along the horizon.</summary>
    public sealed class HorizonMist : MonoBehaviour
    {
        public float Y = 0.3f;
        public Color Color = new Color(1f, 0.92f, 0.85f, 1f);
        public float MaxAlpha = 0.22f;
        public int SortingOrder = 8;

        private void Start()
        {
            if (LifeSettings.Off)
            {
                return;
            }

            for (var i = 0; i < 4; i++)
            {
                var mist = FishingScene.Sprite(transform, "Névoa", Art.Glow, new Vector3(-12f + i * 7f + Random.Range(-1f, 1f), Y + Random.Range(-0.05f, 0.1f), 0f),
                    new Vector3(Random.Range(6f, 9f), Random.Range(0.45f, 0.8f), 1f), SortingOrder, new Color(Color.r, Color.g, Color.b, MaxAlpha * LifeSettings.Intensity));
                var drift = mist.gameObject.AddComponent<Drift>();
                drift.Speed = Random.Range(0.03f, 0.07f);
                drift.WrapHalfWidth = 15f;
                var breathe = mist.gameObject.AddComponent<Breathe>();
                breathe.MinAlpha = MaxAlpha * 0.4f * LifeSettings.Intensity;
                breathe.MaxAlpha = MaxAlpha * LifeSettings.Intensity;
                breathe.Period = Random.Range(9f, 14f);
            }
        }
    }

    /// <summary>Soft rings spreading from the hull now and then, so the boat sits in the water.</summary>
    public sealed class BoatRipples : MonoBehaviour
    {
        public Transform Boat;
        public int SortingOrder = 8;
        public float HalfLength = 1.6f;

        private float _nextAt;

        private void Update()
        {
            if (LifeSettings.Off || Boat == null || Time.time < _nextAt)
            {
                return;
            }

            _nextAt = Time.time + Random.Range(2.2f, 4f) / LifeSettings.Intensity;
            var local = transform.InverseTransformPoint(Boat.position);
            var at = new Vector3(local.x + Random.Range(-HalfLength, HalfLength), local.y - 0.22f, 0f);
            RippleEffect.Spawn(transform, at, SortingOrder, Random.Range(1.4f, 2.2f), 0.18f);
        }
    }

    /// <summary>Little four-pointed glints popping on the sun's reflection.</summary>
    public sealed class SunGlints : MonoBehaviour
    {
        public float X;
        public int SortingOrder = 8;

        private float _nextAt;

        private void Update()
        {
            if (LifeSettings.Off || Time.time < _nextAt)
            {
                return;
            }

            _nextAt = Time.time + Random.Range(0.5f, 1.3f) / LifeSettings.Intensity;
            var y = Random.Range(-4.6f, 0.05f);
            var spread = 0.3f + Mathf.InverseLerp(0.2f, -5f, y) * 1.4f;
            var go = new GameObject("Brilho");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(X + Random.Range(-spread, spread), y, 0f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = Art.Sparkle;
            r.sortingOrder = SortingOrder;
            var pop = go.AddComponent<Pop>();
            pop.Size = Mathf.Lerp(0.18f, 0.4f, Mathf.InverseLerp(0.2f, -5f, y));
        }
    }

    /// <summary>Grows and shrinks once, then goes away.</summary>
    public sealed class Pop : MonoBehaviour
    {
        public float Size = 0.3f;
        public float Duration = 0.6f;

        private SpriteRenderer _renderer;
        private float _age;

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var k = Mathf.Clamp01(_age / Duration);
            var s = Mathf.Sin(k * Mathf.PI);
            transform.localScale = Vector3.one * Size * s;
            _renderer.color = new Color(1f, 0.96f, 0.85f, 0.85f * s);
            if (k >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>The slow wobble of a reflection on the water.</summary>
    public sealed class Shimmer : MonoBehaviour
    {
        private Vector3 _origin;
        private Vector3 _scale;
        private float _phase;

        private void Start()
        {
            _origin = transform.localPosition;
            _scale = transform.localScale;
            _phase = Random.value * 10f;
        }

        private void Update()
        {
            var t = Time.time + _phase;
            transform.localPosition = _origin + new Vector3(Mathf.Sin(t * 0.5f) * 0.03f, 0f, 0f);
            transform.localScale = new Vector3(_scale.x, _scale.y * (1f + Mathf.Sin(t * 0.8f) * 0.02f), 1f);
        }
    }

    /// <summary>Drops of water thrown up by a catch, falling back with gravity.</summary>
    public sealed class Droplet : MonoBehaviour
    {
        private Vector3 _velocity;
        private float _life;
        private float _age;
        private SpriteRenderer _renderer;

        public static void Splash(Transform parent, Vector3 position, int sortingOrder, int count, float power = 1f)
        {
            if (LifeSettings.Off)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var go = new GameObject("Gota");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = position + new Vector3(Random.Range(-0.2f, 0.2f), 0f, 0f);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = Art.Circle;
                r.sortingOrder = sortingOrder;
                r.color = new Color(0.85f, 0.95f, 1f, 0.85f);
                go.transform.localScale = Vector3.one * Random.Range(0.04f, 0.08f);
                var d = go.AddComponent<Droplet>();
                d._renderer = r;
                d._velocity = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(1.2f, 2.6f), 0f) * power;
                d._life = Random.Range(0.6f, 1f);
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            _velocity += new Vector3(0f, -6.5f, 0f) * Time.deltaTime;
            transform.localPosition += _velocity * Time.deltaTime;
            var a = Mathf.Clamp01(1f - _age / _life);
            _renderer.color = new Color(0.85f, 0.95f, 1f, 0.85f * a);
            if (_age >= _life)
            {
                Destroy(gameObject);
            }
        }
    }
}
