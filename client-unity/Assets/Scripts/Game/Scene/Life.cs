using System.Collections.Generic;
using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.Scene
{
    // Small, quiet signs of life in the scenery: fish shadows under the surface, ripples around the
    // boat and splashes at a catch. All of it is pure presentation, uses UnityEngine.Random, stays at
    // low opacity and scales with "ambient_life" in tema_visual.json (0 turns it off). The pieces that
    // only the old painted scenery used (dust, dragonflies, sun rays, mist, wave strips, sliced
    // reflections) were removed in M25-T17.

    internal static class LifeSettings
    {
        /// <summary>0 = off, 1 = as designed. Read from the visual theme.</summary>
        public static float Intensity => VisualTheme.Current.AmbientLife;

        public static bool Off => Intensity <= 0.01f;
    }

    /// <summary>Now and then the dark shape of a fish glides slowly under the surface.</summary>
    public sealed class FishShadows : MonoBehaviour
    {
        public float MinDelay = 12f;
        public float MaxDelay = 26f;
        public int SortingOrder = FishingScene.OrderWaterDetail;
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

            // The scene director keeps small effects apart and holds them while a window is open.
            if (!SceneDirector.AllowSmall())
            {
                _nextAt = Time.time + Random.Range(2f, 5f);
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

    /// <summary>Soft rings spreading from the hull now and then, so the boat sits in the water.</summary>
    public sealed class BoatRipples : MonoBehaviour
    {
        public Transform Boat;
        public int SortingOrder = FishingScene.OrderWaterDetail;
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
