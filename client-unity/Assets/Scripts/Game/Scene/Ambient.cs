using UnityEngine;

namespace FishingIdle.Game.Scene
{
    // Small visual-only behaviours that keep the scene alive when nothing is happening
    // (GDD section 8, "Environmental motion"). They use UnityEngine.Random freely: none of this
    // has gameplay value, so none of it goes through the game service.

    /// <summary>Gentle up-down and tilt motion, e.g. the boat on the water.</summary>
    public sealed class Bobbing : MonoBehaviour
    {
        public float Amplitude = 0.06f;
        public float Period = 3.2f;
        public float TiltDegrees = 1.4f;

        private Vector3 _origin;
        private float _phase;

        private void Start()
        {
            _origin = transform.localPosition;
            _phase = Random.value * 10f;
        }

        private void Update()
        {
            var t = (Time.time + _phase) * Mathf.PI * 2f / Period;
            transform.localPosition = _origin + new Vector3(0f, Mathf.Sin(t) * Amplitude, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.8f + 1.3f) * TiltDegrees);
        }
    }

    /// <summary>Slow horizontal drift that wraps around the view, e.g. clouds.</summary>
    public sealed class Drift : MonoBehaviour
    {
        public float Speed = 0.12f;
        public float WrapHalfWidth = 13f;

        private void Update()
        {
            var p = transform.localPosition;
            p.x += Speed * Time.deltaTime;
            if (p.x > WrapHalfWidth)
            {
                p.x = -WrapHalfWidth;
            }
            else if (p.x < -WrapHalfWidth)
            {
                p.x = WrapHalfWidth;
            }

            transform.localPosition = p;
        }
    }

    /// <summary>Wind sway around the pivot, e.g. reeds.</summary>
    public sealed class Sway : MonoBehaviour
    {
        public float Degrees = 4f;
        public float Period = 4f;

        private float _phase;
        private float _base;

        private void Start()
        {
            _phase = Random.value * 10f;
            _base = transform.localEulerAngles.z;
        }

        private void Update()
        {
            var t = (Time.time + _phase) * Mathf.PI * 2f / Period;
            // Two frequencies read as gusts rather than a metronome.
            var angle = Mathf.Sin(t) * Degrees + Mathf.Sin(t * 2.7f) * Degrees * 0.25f;
            transform.localRotation = Quaternion.Euler(0f, 0f, _base + angle);
        }
    }

    /// <summary>Alpha that breathes in and out, e.g. light glints on the water.</summary>
    public sealed class Twinkle : MonoBehaviour
    {
        public float Period = 3f;
        public float MaxAlpha = 0.5f;
        public float DriftSpeed = 0.05f;

        private SpriteRenderer _renderer;
        private float _phase;
        private Vector3 _origin;

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _phase = Random.value * 10f;
            _origin = transform.localPosition;
        }

        private void Update()
        {
            var t = (Time.time + _phase) * Mathf.PI * 2f / Period;
            var c = _renderer.color;
            c.a = Mathf.Max(0f, Mathf.Sin(t)) * MaxAlpha;
            _renderer.color = c;
            transform.localPosition = _origin + new Vector3(Mathf.Sin(t * 0.3f) * DriftSpeed * 10f, 0f, 0f);
        }
    }

    /// <summary>An expanding, fading ring; destroys itself when done.</summary>
    public sealed class RippleEffect : MonoBehaviour
    {
        public float Duration = 1.6f;
        public float StartScale = 0.2f;
        public float EndScale = 1.4f;
        public float StartAlpha = 0.7f;

        private SpriteRenderer _renderer;
        private float _age;

        public static void Spawn(Transform parent, Vector3 position, int sortingOrder, float size, float alpha = 0.7f)
        {
            var go = new GameObject("Ripple");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Art.Ripple;
            renderer.sortingOrder = sortingOrder;
            var ripple = go.AddComponent<RippleEffect>();
            ripple.StartScale = 0.2f * size;
            ripple.EndScale = 1.4f * size;
            ripple.StartAlpha = alpha;
            go.transform.localScale = Vector3.one * ripple.StartScale;
            renderer.color = new Color(1f, 1f, 1f, alpha);
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var t = Mathf.Clamp01(_age / Duration);
            transform.localScale = Vector3.one * Mathf.Lerp(StartScale, EndScale, 1f - (1f - t) * (1f - t));
            _renderer.color = new Color(1f, 1f, 1f, StartAlpha * (1f - t));
            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>A few soft sparks rising and fading around an important catch (GDD section 8).</summary>
    public sealed class SparkleEffect : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Vector3 _velocity;
        private Color _color;
        private float _life;
        private float _age;
        private float _size;

        public static void Burst(Transform parent, Vector3 position, int sortingOrder, Color color, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var go = new GameObject("Brilho");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = position + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.2f, 0.25f), 0f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = Art.Glow;
                renderer.sortingOrder = sortingOrder;
                var spark = go.AddComponent<SparkleEffect>();
                spark._renderer = renderer;
                spark._color = color;
                spark._velocity = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.4f, 1.0f), 0f);
                spark._life = Random.Range(0.9f, 1.6f);
                spark._size = Random.Range(0.12f, 0.24f);
                go.transform.localScale = Vector3.one * spark._size;
                renderer.color = new Color(color.r, color.g, color.b, 0f);
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var t = Mathf.Clamp01(_age / _life);
            transform.localPosition += _velocity * Time.deltaTime;
            _velocity *= 1f - Time.deltaTime * 0.8f;
            var alpha = Mathf.Sin(t * Mathf.PI) * _color.a;
            _renderer.color = new Color(_color.r, _color.g, _color.b, alpha);
            transform.localScale = Vector3.one * _size * (0.6f + 0.4f * Mathf.Sin(_age * 14f + _size * 50f));
            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>A small flock crossing the sky now and then.</summary>
    public sealed class BirdFlock : MonoBehaviour
    {
        public float MinDelay = 18f;
        public float MaxDelay = 40f;
        public int SortingOrder = 3;

        private float _nextAt;

        private void Start()
        {
            _nextAt = Time.time + Random.Range(4f, 10f);
        }

        private void Update()
        {
            if (Time.time < _nextAt)
            {
                return;
            }

            _nextAt = Time.time + Random.Range(MinDelay, MaxDelay);
            var count = Random.Range(2, 6);
            var leftToRight = Random.value > 0.5f;
            var y = Random.Range(2.2f, 4.2f);
            var speed = Random.Range(0.9f, 1.4f) * (leftToRight ? 1f : -1f);
            for (var i = 0; i < count; i++)
            {
                var go = new GameObject("Bird");
                go.transform.SetParent(transform, false);
                var offset = new Vector3(-i * 0.55f * Mathf.Sign(speed), (i % 2 == 0 ? 1 : -1) * i * 0.18f, 0f);
                go.transform.localPosition = new Vector3(leftToRight ? -12f : 12f, y, 0f) + offset;
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = Art.Bird;
                renderer.sortingOrder = SortingOrder;
                renderer.color = new Color(1f, 1f, 1f, 0.8f);
                var scale = Random.Range(0.28f, 0.4f);
                var bird = go.AddComponent<Bird>();
                bird.Velocity = new Vector3(speed, Random.Range(-0.05f, 0.05f), 0f);
                bird.BaseScale = scale;
            }
        }
    }

    public sealed class Bird : MonoBehaviour
    {
        public Vector3 Velocity;
        public float BaseScale = 0.3f;

        private float _phase;

        private void Start()
        {
            _phase = Random.value * 5f;
        }

        private void Update()
        {
            transform.localPosition += Velocity * Time.deltaTime;
            var flap = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin((Time.time + _phase) * 5.5f));
            transform.localScale = new Vector3(BaseScale, BaseScale * flap, 1f);
            if (Mathf.Abs(transform.localPosition.x) > 13f)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>Now and then a distant fish jumps out of the water. Pure scenery.</summary>
    public sealed class DistantFishJumps : MonoBehaviour
    {
        public float MinDelay = 7f;
        public float MaxDelay = 18f;
        public int SortingOrder = 8;
        public Rect Area = new Rect(-8f, -1.4f, 16f, 1.2f);

        private float _nextAt;

        private void Start()
        {
            _nextAt = Time.time + Random.Range(3f, 8f);
        }

        private void Update()
        {
            if (Time.time < _nextAt)
            {
                return;
            }

            _nextAt = Time.time + Random.Range(MinDelay, MaxDelay);
            var start = new Vector3(Random.Range(Area.xMin, Area.xMax), Random.Range(Area.yMin, Area.yMax), 0f);
            // Farther away (higher on screen) means smaller.
            var depth = Mathf.InverseLerp(Area.yMin, Area.yMax, start.y);
            var scale = Mathf.Lerp(0.3f, 0.14f, depth);

            var go = new GameObject("DistantFish");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = start;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Art.Fish(null);
            renderer.color = new Color(0.25f, 0.33f, 0.36f, 0.9f);
            renderer.sortingOrder = SortingOrder;
            var jump = go.AddComponent<FishJump>();
            jump.Origin = start;
            jump.Direction = Random.value > 0.5f ? 1f : -1f;
            jump.Scale = scale;
            jump.SortingOrder = SortingOrder;
            RippleEffect.Spawn(transform, start, SortingOrder - 1, scale * 3f, 0.5f);
        }
    }

    public sealed class FishJump : MonoBehaviour
    {
        public Vector3 Origin;
        public float Direction = 1f;
        public float Scale = 0.2f;
        public float Duration = 0.9f;
        public int SortingOrder = 8;

        private float _age;

        private void Update()
        {
            _age += Time.deltaTime;
            var t = Mathf.Clamp01(_age / Duration);
            var height = Mathf.Sin(t * Mathf.PI) * Scale * 3.2f;
            transform.localPosition = Origin + new Vector3(Direction * t * Scale * 4f, height, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(50f, -50f, t) * Direction);
            transform.localScale = new Vector3(Direction * Scale, Scale, 1f);
            if (t >= 1f)
            {
                RippleEffect.Spawn(transform.parent, transform.localPosition, SortingOrder - 1, Scale * 3f, 0.5f);
                Destroy(gameObject);
            }
        }
    }
}
