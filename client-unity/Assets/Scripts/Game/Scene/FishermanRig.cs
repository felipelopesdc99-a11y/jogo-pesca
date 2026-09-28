using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.GameService.Fishing;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.Scene
{
    /// <summary>
    /// The fisherman, his rod, line and bobber, and the moment a fish comes out of the water.
    /// </summary>
    /// <remarks>
    /// Pure presentation, synchronised to the game service's cycle (GDD section 8): the cast,
    /// the wait, the bite and the reel are timed from <see cref="FishingStatus"/>, and a fish
    /// appears only when the service has actually produced it. The animation never decides or
    /// predicts what is caught.
    /// </remarks>
    public sealed class FishermanRig : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            Casting,
            Waiting,
            Bite,
            Reeling,
            Showing,
        }

        private const int LineSegments = 18;
        private const float RodLength = 2.7f;
        private const float ShowSeconds = 2.2f;
        private const float StoreSeconds = 0.6f;

        private static readonly Vector3 WaterTarget = new Vector3(3.3f, -2.5f, 0f);
        private static readonly Vector3 BoxTarget = new Vector3(8.6f, -4.7f, 0f);

        private GameRoot _root;
        private Transform _world;
        private Transform _body;
        private Transform _rodPivot;
        private Transform _rodTip;
        private Transform _bobber;
        private readonly List<Transform> _line = new List<Transform>();
        private SpriteRenderer _fish;
        private SpriteRenderer _fishGlow;

        private Phase _phase = Phase.Idle;
        private float _phaseStartedAt;
        private Vector3 _castTarget;
        private Vector3 _bobberFrom;
        private float _rodAngle = 55f;
        private float _nextBiteRippleAt;
        private CatchView _shown;
        private Vector3 _fishFrom;

        /// <summary>What the fisherman is doing, in PT-BR, for the HUD.</summary>
        public string PhaseText
        {
            get
            {
                switch (_phase)
                {
                    case Phase.Casting: return GameTexts.Fishing.Casting;
                    case Phase.Waiting: return GameTexts.Fishing.Waiting;
                    case Phase.Bite: return GameTexts.Fishing.Bite;
                    case Phase.Reeling: return GameTexts.Fishing.Reeling;
                    case Phase.Showing: return GameTexts.Fishing.Caught;
                    default: return GameTexts.Fishing.Idle;
                }
            }
        }

        public void Build(GameRoot root, Transform boat, Transform world)
        {
            _root = root;
            _world = world;

            _body = new GameObject("Pescador").transform;
            _body.SetParent(boat, false);
            _body.localPosition = new Vector3(-0.7f, 0.36f, 0f);

            var shirt = new Color(0.25f, 0.45f, 0.62f);
            var pants = new Color(0.22f, 0.24f, 0.30f);
            var skin = new Color(0.87f, 0.66f, 0.50f);
            var o = FishingScene.OrderFisherman;

            FishingScene.Sprite(_body, "Pernas", Art.RoundedBox, new Vector3(0.22f, 0.12f, 0f), new Vector3(0.62f, 0.2f, 1f), o, pants);
            FishingScene.Sprite(_body, "Tronco", Art.RoundedBox, new Vector3(0f, 0.46f, 0f), new Vector3(0.44f, 0.66f, 1f), o, shirt);
            FishingScene.Sprite(_body, "Cabeça", Art.Circle, new Vector3(0.04f, 0.93f, 0f), Vector3.one * 0.36f, o, skin);
            FishingScene.Sprite(_body, "Chapéu", Art.Hat, new Vector3(0.04f, 0.99f, 0f), Vector3.one * 0.72f, o + 1);

            var arm = FishingScene.Sprite(_body, "Braço", Art.PixelLeft, new Vector3(0.08f, 0.66f, 0f), new Vector3(0.4f, 0.11f, 1f), o + 1, shirt * 0.9f);
            arm.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

            _rodPivot = new GameObject("Vara").transform;
            _rodPivot.SetParent(_body, false);
            _rodPivot.localPosition = new Vector3(0.44f, 0.54f, 0f);
            FishingScene.Sprite(_rodPivot, "Cabo", Art.PixelLeft, new Vector3(-0.15f, 0f, 0f), new Vector3(0.55f, 0.08f, 1f), FishingScene.OrderRod, new Color(0.72f, 0.56f, 0.36f));
            FishingScene.Sprite(_rodPivot, "Haste", Art.PixelLeft, Vector3.zero, new Vector3(RodLength, 0.04f, 1f), FishingScene.OrderRod, new Color(0.26f, 0.18f, 0.12f));
            FishingScene.Sprite(_body, "Mão", Art.Circle, new Vector3(0.44f, 0.54f, 0f), Vector3.one * 0.12f, FishingScene.OrderRod + 1, skin);

            _rodTip = new GameObject("Ponta").transform;
            _rodTip.SetParent(_rodPivot, false);
            _rodTip.localPosition = new Vector3(RodLength, 0f, 0f);

            var lineRoot = new GameObject("Linha").transform;
            lineRoot.SetParent(world, false);
            for (var i = 0; i < LineSegments; i++)
            {
                var segment = FishingScene.Sprite(lineRoot, "Segmento", Art.PixelLeft, Vector3.zero, Vector3.one, FishingScene.OrderRod, new Color(0.92f, 0.92f, 0.88f, 0.75f));
                _line.Add(segment.transform);
            }

            _bobber = new GameObject("Boia").transform;
            _bobber.SetParent(world, false);
            FishingScene.Sprite(_bobber, "Boia", Art.Circle, Vector3.zero, Vector3.one * 0.17f, FishingScene.OrderBobber, new Color(0.88f, 0.22f, 0.18f));
            FishingScene.Sprite(_bobber, "Topo", Art.Circle, new Vector3(0f, 0.05f, 0f), Vector3.one * 0.08f, FishingScene.OrderBobber + 1, new Color(0.98f, 0.97f, 0.92f));

            _fishGlow = FishingScene.Sprite(world, "Aura da captura", Art.Glow, Vector3.zero, Vector3.one, FishingScene.OrderCatchGlow);
            _fish = FishingScene.Sprite(world, "Captura", Art.Pixel, Vector3.zero, Vector3.one, FishingScene.OrderCatch);
            _fish.enabled = false;
            _fishGlow.enabled = false;

            _root.CatchesArrived += OnCatchesArrived;
            Enter(Phase.Idle);
        }

        private void OnDestroy()
        {
            if (_root != null)
            {
                _root.CatchesArrived -= OnCatchesArrived;
            }
        }

        private void OnCatchesArrived(FishingUpdate update)
        {
            if (update.NewCatches.Count == 0)
            {
                return;
            }

            // Several catches at once (rare): show the most remarkable one.
            _shown = update.NewCatches
                .OrderByDescending(c => c.IsImportant)
                .ThenByDescending(c => c.SalePriceCoins)
                .First();

            _fish.sprite = Art.Fish(_shown.SpeciesId);
            _fishFrom = _phase == Phase.Idle ? WaterTarget : _bobber.localPosition;
            Enter(Phase.Showing);
        }

        private void Update()
        {
            var status = _root.Status;
            var fishing = status != null && status.IsFishing;
            var now = Time.time - _phaseStartedAt;

            if (!fishing && _phase != Phase.Idle && _phase != Phase.Showing)
            {
                Enter(Phase.Idle);
            }

            switch (_phase)
            {
                case Phase.Idle:
                    UpdateIdle();
                    if (fishing)
                    {
                        Enter(Phase.Casting);
                    }

                    break;

                case Phase.Casting:
                    UpdateCasting(now, CastSeconds(status));
                    break;

                case Phase.Waiting:
                    UpdateWaiting();
                    if (SecondsToCatch(status) <= BiteSeconds(status))
                    {
                        Enter(Phase.Bite);
                    }

                    break;

                case Phase.Bite:
                    UpdateBite();
                    if (SecondsToCatch(status) <= ReelSeconds(status))
                    {
                        Enter(Phase.Reeling);
                    }

                    break;

                case Phase.Reeling:
                    UpdateReeling(now, ReelSeconds(status));
                    if (SecondsToCatch(status) > BiteSeconds(status) + 1f)
                    {
                        // The service restarted the cycle (e.g. after the PC slept): cast again.
                        Enter(Phase.Casting);
                    }

                    break;

                case Phase.Showing:
                    UpdateShowing(now, fishing);
                    break;
            }

            DrawLine();
        }

        // ------------------------------------------------------------------ phases

        private void Enter(Phase phase)
        {
            _phase = phase;
            _phaseStartedAt = Time.time;

            switch (phase)
            {
                case Phase.Casting:
                    _castTarget = WaterTarget + new Vector3(Random.Range(-0.5f, 0.6f), Random.Range(-0.25f, 0.2f), 0f);
                    break;
                case Phase.Reeling:
                    _bobberFrom = _bobber.localPosition;
                    break;
                case Phase.Showing:
                    _fish.enabled = true;
                    _fishGlow.enabled = _shown.IsImportant;
                    _fishGlow.color = GlowColor(_shown);
                    RippleEffect.Spawn(_world, _fishFrom, FishingScene.OrderWaterDetail, 1.4f, 0.8f);
                    break;
                case Phase.Idle:
                    _fish.enabled = false;
                    _fishGlow.enabled = false;
                    break;
            }
        }

        private void UpdateIdle()
        {
            _rodAngle = Mathf.Lerp(_rodAngle, 58f + Mathf.Sin(Time.time * 0.9f) * 1.5f, Time.deltaTime * 3f);
            ApplyRod();
            var swing = Mathf.Sin(Time.time * 1.7f) * 0.06f;
            _bobber.localPosition = TipPosition() + new Vector3(swing, -0.75f, 0f);
        }

        private void UpdateCasting(float t, float duration)
        {
            var k = Mathf.Clamp01(t / duration);
            if (k < 0.4f)
            {
                // Rod back over the shoulder, bobber still hanging.
                _rodAngle = Mathf.Lerp(58f, 105f, Ease(k / 0.4f));
                ApplyRod();
                _bobber.localPosition = TipPosition() + new Vector3(0f, -0.6f, 0f);
                _bobberFrom = _bobber.localPosition;
            }
            else
            {
                // Whip forward and send the bobber flying along an arc.
                var f = (k - 0.4f) / 0.6f;
                _rodAngle = Mathf.Lerp(105f, 26f, Ease(Mathf.Min(1f, f * 1.8f)));
                ApplyRod();
                var flat = Vector3.Lerp(_bobberFrom, _castTarget, f);
                _bobber.localPosition = flat + new Vector3(0f, Mathf.Sin(f * Mathf.PI) * 1.8f, 0f);
            }

            if (k >= 1f)
            {
                RippleEffect.Spawn(_world, _castTarget, FishingScene.OrderWaterDetail, 1f);
                Enter(Phase.Waiting);
            }
        }

        private void UpdateWaiting()
        {
            _rodAngle = Mathf.Lerp(_rodAngle, 27f + Mathf.Sin(Time.time * 0.7f) * 1.2f, Time.deltaTime * 4f);
            ApplyRod();
            _bobber.localPosition = _castTarget + new Vector3(0f, Mathf.Sin(Time.time * 2.1f) * 0.035f, 0f);
        }

        private void UpdateBite()
        {
            // Sharp dips and a nervous rod tip.
            var dip = Mathf.Max(0f, Mathf.Sin(Time.time * 13f)) * 0.12f + Mathf.Sin(Time.time * 7f) * 0.03f;
            _bobber.localPosition = _castTarget + new Vector3(Mathf.Sin(Time.time * 9f) * 0.03f, -dip, 0f);
            _rodAngle = 25f + Mathf.Sin(Time.time * 17f) * 2.5f;
            ApplyRod();

            if (Time.time >= _nextBiteRippleAt)
            {
                _nextBiteRippleAt = Time.time + 0.45f;
                RippleEffect.Spawn(_world, _castTarget, FishingScene.OrderWaterDetail, 0.7f, 0.6f);
            }
        }

        private void UpdateReeling(float t, float duration)
        {
            var k = Mathf.Clamp01(t / Mathf.Max(0.2f, duration));
            _rodAngle = Mathf.Lerp(26f, 62f, Ease(k)) + Mathf.Sin(Time.time * 22f) * 1.5f;
            ApplyRod();
            var near = TipPosition() + new Vector3(0.2f, -1.1f, 0f);
            near.y = Mathf.Min(near.y, _castTarget.y + 0.1f);
            _bobber.localPosition = Vector3.Lerp(_bobberFrom, near, Ease(k)) + new Vector3(0f, Mathf.Sin(Time.time * 15f) * 0.02f, 0f);
        }

        private void UpdateShowing(float t, bool fishing)
        {
            var size = FishDisplaySize(_shown);
            var hold = TipPosition() + new Vector3(-0.8f, 0.2f, 0f);

            if (t < 0.6f)
            {
                // Out of the water and up, in an arc.
                var k = Ease(t / 0.6f);
                _fish.transform.localPosition = Vector3.Lerp(_fishFrom, hold, k) + new Vector3(0f, Mathf.Sin(k * Mathf.PI) * 0.8f, 0f);
                _fish.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(60f, 0f, k));
                _fish.transform.localScale = Vector3.one * size * Mathf.Lerp(0.5f, 1f, k);
                _rodAngle = Mathf.Lerp(_rodAngle, 70f, Time.deltaTime * 6f);
            }
            else if (t < ShowSeconds)
            {
                // Held up for a moment, wriggling.
                var wiggle = Mathf.Sin(Time.time * 11f) * 6f * Mathf.Clamp01(1.4f - t);
                _fish.transform.localPosition = hold + new Vector3(0f, Mathf.Sin(Time.time * 3f) * 0.05f, 0f);
                _fish.transform.localRotation = Quaternion.Euler(0f, 0f, wiggle);
                _fish.transform.localScale = Vector3.one * size;
            }
            else
            {
                // Into the Fishing Box (bottom-right corner, where its button is).
                var k = Mathf.Clamp01((t - ShowSeconds) / StoreSeconds);
                _fish.transform.localPosition = Vector3.Lerp(hold, BoxTarget, Ease(k));
                _fish.transform.localScale = Vector3.one * size * Mathf.Lerp(1f, 0.15f, k);
                if (k >= 1f)
                {
                    _fish.enabled = false;
                    _fishGlow.enabled = false;
                    Enter(fishing ? Phase.Casting : Phase.Idle);
                    return;
                }
            }

            ApplyRod();
            _bobber.localPosition = _fish.transform.localPosition + new Vector3(0.1f, 0.05f, 0f);

            if (_fishGlow.enabled)
            {
                var pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
                _fishGlow.transform.localPosition = _fish.transform.localPosition;
                _fishGlow.transform.localScale = Vector3.one * size * 2.6f * pulse;
            }
        }

        // ------------------------------------------------------------------ helpers

        private void ApplyRod()
        {
            _rodPivot.localRotation = Quaternion.Euler(0f, 0f, _rodAngle);
        }

        private Vector3 TipPosition()
        {
            return _world.InverseTransformPoint(_rodTip.position);
        }

        /// <summary>Draws the line from the rod tip to the bobber, sagging when slack.</summary>
        private void DrawLine()
        {
            var from = TipPosition();
            var to = _bobber.localPosition;
            var slack = _phase == Phase.Waiting ? 0.45f : _phase == Phase.Casting ? 0.2f : 0.05f;
            var control = (from + to) * 0.5f + new Vector3(0f, -slack, 0f);

            var previous = from;
            for (var i = 0; i < LineSegments; i++)
            {
                var t = (i + 1f) / LineSegments;
                var point = (1 - t) * (1 - t) * from + 2 * (1 - t) * t * control + t * t * to;
                var delta = point - previous;
                var segment = _line[i];
                segment.localPosition = previous;
                segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                segment.localScale = new Vector3(delta.magnitude + 0.005f, 0.022f, 1f);
                previous = point;
            }
        }

        private static float SecondsToCatch(FishingStatus status)
        {
            return status == null ? float.MaxValue : (status.NextCatchAtMs - status.NowMs) / 1000f;
        }

        // Presentation timings scale down with short cycles (e.g. when testing with a 5 s cycle).
        private static float CastSeconds(FishingStatus s) => Mathf.Min(1.3f, (float)(s?.CycleSeconds ?? 30) * 0.15f);
        private static float BiteSeconds(FishingStatus s) => Mathf.Min(2.6f, (float)(s?.CycleSeconds ?? 30) * 0.25f);
        private static float ReelSeconds(FishingStatus s) => Mathf.Min(1.2f, (float)(s?.CycleSeconds ?? 30) * 0.12f);

        /// <summary>On-screen length of the caught fish: bigger species and specimens look bigger.</summary>
        private static float FishDisplaySize(CatchView c)
        {
            return Mathf.Clamp(0.35f + (float)c.SizeCm / 90f, 0.4f, 1.6f);
        }

        private static Color GlowColor(CatchView c)
        {
            if (c.RarityId != null && c.RarityId != "common")
            {
                return new Color(0.75f, 0.5f, 1f, 0.9f);
            }

            if (c.SizeCategoryId == "exceptional")
            {
                return new Color(1f, 0.82f, 0.3f, 0.9f);
            }

            return new Color(0.55f, 0.95f, 1f, 0.75f); // new species
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
