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
            Escaping,
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
        private float _nextIdleRippleAt;
        private float _nextDripAt;
        private CatchView _shown;
        private Vector3 _fishFrom;

        // The fish that got away (docs/SISTEMA_SUCESSO_PESCA.md): only its rarity is known here.
        private SpriteRenderer _shadow;
        private Vector3 _seat;
        private SpriteRenderer[] _codeRod;
        private SpriteRenderer _paintedRod;
        private SpriteRenderer _bait;
        private SpriteRenderer _baitLeader;
        private Color _baitColor;
        private int _escapeStrength;
        private Vector3 _escapeFrom;
        private Vector3 _shadowDir;
        private bool _snapped;

        /// <summary>When the last fish broke free (Time.time), for the message near the bobber; below 0 = never.</summary>
        public float LastEscapeAt { get; private set; } = -100f;

        /// <summary>Rarity of the last fish that broke free, and where (world position of the bobber then).</summary>
        public string LastEscapeRarityId { get; private set; }

        /// <summary>The name of that rarity, so the message never relies on the colour alone (M22-T12).</summary>
        public string LastEscapeRarityName { get; private set; }
        public Vector3 LastEscapePoint { get; private set; }

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
                    case Phase.Escaping: return GameTexts.Fishing.Escaped;
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

            // The painted fisherman (Resources/Arte/Cena/pescador.png) sits behind the rim of the hull
            // with his fists closed around the rod; without the file, the figure is built from simple
            // shapes with an arm holding the rod. His size, seat (pivot) and the point between his
            // fists come from Visual/equipamento_cena.json (tools/Arte/equipamento_na_cena.py); the
            // old picture's numbers are the fallback.
            var fisher = EquipmentLook.Fisherman();
            var paintedHeight = fisher != null ? fisher.height : 1.7f;
            var pivot = fisher != null ? new Vector2(fisher.pivot_u, fisher.pivot_v) : new Vector2(0.27f, 0.12f);
            var grip = fisher != null ? new Vector2(fisher.hands_u, fisher.hands_v) : new Vector2(0.97f, 0.23f);
            var painted = Visual.ArtAssets.SpriteByHeight(fisher != null ? fisher.art : "Cena/pescador", paintedHeight, pivot);
            Vector3 hands;
            if (painted != null)
            {
                // Inside a two-layer hull he sits on the bench, between its back and front; with a
                // single hull picture he stays behind it.
                var insideHull = Visual.ArtAssets.Texture("Cena/barco_frente") != null;
                _body.localPosition = insideHull ? new Vector3(-0.35f, 0.2f, 0f) : _body.localPosition;
                FishingScene.Sprite(_body, "Pescador", painted, Vector3.zero, Vector3.one, insideHull ? FishingScene.OrderFisherman : FishingScene.OrderBoat - 1);
                var width = paintedHeight * painted.rect.width / painted.rect.height;
                hands = new Vector3((grip.x - pivot.x) * width, (grip.y - pivot.y) * paintedHeight, 0f);

                // His fists alone, drawn over the rod so it sits inside them.
                var fists = insideHull && fisher != null && !string.IsNullOrEmpty(fisher.hands_art)
                    ? Visual.ArtAssets.SpriteByHeight(fisher.hands_art, paintedHeight, pivot)
                    : null;
                if (fists != null)
                {
                    FishingScene.Sprite(_body, "Mãos", fists, Vector3.zero, Vector3.one, FishingScene.OrderRod + 1);
                }
            }
            else
            {
                FishingScene.Sprite(_body, "Pernas", Art.RoundedBox, new Vector3(0.22f, 0.12f, 0f), new Vector3(0.62f, 0.2f, 1f), o, pants);
                FishingScene.Sprite(_body, "Tronco", Art.RoundedBox, new Vector3(0f, 0.46f, 0f), new Vector3(0.44f, 0.66f, 1f), o, shirt);
                FishingScene.Sprite(_body, "Cabeça", Art.Circle, new Vector3(0.04f, 0.93f, 0f), Vector3.one * 0.36f, o, skin);
                FishingScene.Sprite(_body, "Chapéu", Art.Hat, new Vector3(0.04f, 0.99f, 0f), Vector3.one * 0.72f, o + 1);
                var arm = FishingScene.Sprite(_body, "Braço", Art.PixelLeft, new Vector3(0.08f, 0.66f, 0f), new Vector3(0.4f, 0.11f, 1f), o + 1, shirt * 0.9f);
                arm.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
                hands = new Vector3(0.44f, 0.54f, 0f);
            }

            _rodPivot = new GameObject("Vara").transform;
            _rodPivot.SetParent(_body, false);
            _rodPivot.localPosition = hands;
            _codeRod = new[]
            {
                FishingScene.Sprite(_rodPivot, "Cabo", Art.PixelLeft, new Vector3(-0.15f, 0f, 0f), new Vector3(0.55f, 0.08f, 1f), FishingScene.OrderRod, new Color(0.72f, 0.56f, 0.36f)),
                FishingScene.Sprite(_rodPivot, "Haste", Art.PixelLeft, Vector3.zero, new Vector3(RodLength, 0.04f, 1f), FishingScene.OrderRod, new Color(0.26f, 0.18f, 0.12f)),
            };
            _paintedRod = FishingScene.Sprite(_rodPivot, "Vara pintada", null, Vector3.zero, Vector3.one, FishingScene.OrderRod);
            _paintedRod.enabled = false;
            if (painted == null)
            {
                FishingScene.Sprite(_body, "Mão", Art.Circle, hands, Vector3.one * 0.12f, FishingScene.OrderRod + 1, skin);
            }

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

            // The bait in use hangs on the hook a little below the bobber (placeholder shape, A-096).
            _baitLeader = FishingScene.Sprite(_bobber, "Linha da isca", Art.PixelLeft, new Vector3(0f, -0.07f, 0f), new Vector3(0.24f, 0.015f, 1f), FishingScene.OrderBobber - 1, new Color(0.92f, 0.92f, 0.88f, 0.75f));
            _baitLeader.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            _bait = FishingScene.Sprite(_bobber, "Isca", Art.RoundedBox, new Vector3(0f, -0.33f, 0f), new Vector3(0.16f, 0.07f, 1f), FishingScene.OrderBobber - 1);
            _baitLeader.enabled = _bait.enabled = false;

            _fishGlow = FishingScene.Sprite(world, "Aura da captura", Art.Glow, Vector3.zero, Vector3.one, FishingScene.OrderCatchGlow);
            _fish = FishingScene.Sprite(world, "Captura", Art.Pixel, Vector3.zero, Vector3.one, FishingScene.OrderCatch);
            _fish.enabled = false;
            _fishGlow.enabled = false;
            _shadow = FishingScene.Sprite(world, "Sombra do peixe", Art.Glow, Vector3.zero, Vector3.one, FishingScene.OrderWaterDetail + 1, new Color(0.02f, 0.06f, 0.08f, 0f));
            _shadow.enabled = false;

            _seat = _body.localPosition;
            _root.CatchesArrived += OnCatchesArrived;
            Enter(Phase.Idle);
        }

        // ------------------------------------------------------------------ equipment (A-096)

        /// <summary>Where he sits in the boat; null = the seat of the starter boat.</summary>
        public void SitAt(Vector3? seat)
        {
            _body.localPosition = seat ?? _seat;
        }

        /// <summary>Shows the painted picture of the rod in use along the animated rod (the simple drawn rod without it).</summary>
        public void UseRod(string rodId)
        {
            var look = EquipmentLook.Rod(rodId);
            var tex = look != null ? Visual.ArtAssets.Texture(look.art) : null;
            var painted = false;
            if (tex != null)
            {
                // Lay the picture so its butt-to-tip line runs along the rod, the hands at "grip" and the tip at RodLength.
                var du = (look.tip_u - look.butt_u) * tex.width;
                var dv = (look.tip_v - look.butt_v) * tex.height;
                var lengthPx = Mathf.Sqrt(du * du + dv * dv);
                var grip = Mathf.Clamp(look.grip, 0f, 0.6f);
                if (lengthPx > 1f)
                {
                    var total = RodLength / (1f - grip);
                    _paintedRod.sprite = Visual.ArtAssets.Sprite(look.art, total * tex.width / lengthPx, new Vector2(look.butt_u, look.butt_v));
                    _paintedRod.transform.localPosition = new Vector3(-grip * total, 0f, 0f);
                    _paintedRod.transform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(dv, du) * Mathf.Rad2Deg);
                    painted = _paintedRod.sprite != null;
                }
            }

            _paintedRod.enabled = painted;
            foreach (var part in _codeRod)
            {
                part.enabled = !painted;
            }
        }

        /// <summary>Hangs the bait in use on the hook (its picture, or a coloured placeholder); nothing when no bait is in use.</summary>
        public void UseBait(string baitId)
        {
            var look = baitId != null ? EquipmentLook.Bait(baitId) : null;
            var art = look != null && !string.IsNullOrEmpty(look.art) ? Visual.ArtAssets.Sprite(look.art, look.width, new Vector2(0.5f, 0.85f)) : null;
            if (art != null)
            {
                _bait.sprite = art;
                _bait.transform.localScale = Vector3.one;
                _bait.transform.localPosition = new Vector3(0f, -0.3f, 0f);
                _baitColor = Color.white;
            }
            else
            {
                _bait.sprite = Art.RoundedBox;
                _bait.transform.localScale = new Vector3(0.16f, 0.07f, 1f);
                _bait.transform.localPosition = new Vector3(0f, -0.33f, 0f);
                _baitColor = EquipmentLook.BaitColor(look) ?? Color.clear;
            }

            _bait.enabled = _baitLeader.enabled = _baitColor.a > 0f;
        }

        /// <summary>Under the water the bait is seen faintly; on a fish's catch it is hidden (the fish took it).</summary>
        private void UpdateBait()
        {
            if (_baitColor.a <= 0f)
            {
                return;
            }

            var showing = _phase == Phase.Showing;
            _bait.enabled = _baitLeader.enabled = !showing;
            var underwater = _phase == Phase.Waiting || _phase == Phase.Bite;
            var c = _baitColor;
            c.a = underwater ? 0.45f : 1f;
            _bait.color = c;
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
                // Only escapes: the fight is lost. A catch being shown is never interrupted.
                if (update.Escapes.Count > 0 && _phase != Phase.Showing)
                {
                    var notable = update.Escapes.OrderByDescending(e => Strength(e.RarityId)).First();
                    LastEscapeRarityId = notable.RarityId;
                    LastEscapeRarityName = notable.RarityName;
                    _escapeStrength = Strength(notable.RarityId);
                    _escapeFrom = _phase == Phase.Idle ? WaterTarget : _bobber.localPosition;
                    Enter(Phase.Escaping);
                }

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

            if (!fishing && _phase != Phase.Idle && _phase != Phase.Showing && _phase != Phase.Escaping)
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

                case Phase.Escaping:
                    UpdateEscaping(now, fishing);
                    break;
            }

            DrawLine();
            UpdateBait();
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
                    Droplet.Splash(_world, _fishFrom, FishingScene.OrderCatch + 1, _shown.IsImportant ? 16 : 9, _shown.IsImportant ? 1.2f : 0.9f);
                    if (_shown.IsImportant)
                    {
                        // Stands out before any text is read: glow, pulse and a few rising sparks.
                        SparkleEffect.Burst(_world, TipPosition() + new Vector3(-0.8f, 0.2f, 0f), FishingScene.OrderCatchGlow + 1, GlowColor(_shown), 14);
                    }
                    break;
                case Phase.Escaping:
                    _snapped = false;
                    _shadow.enabled = true;
                    _shadowDir = new Vector3(Random.value < 0.5f ? -1f : 1f, -0.25f, 0f);
                    break;
                case Phase.Idle:
                    _fish.enabled = false;
                    _fishGlow.enabled = false;
                    _shadow.enabled = false;
                    break;
            }

            if (phase != Phase.Escaping)
            {
                _shadow.enabled = false;
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

            // The bobber sits in the water: a faint ring now and then.
            if (Time.time >= _nextIdleRippleAt)
            {
                _nextIdleRippleAt = Time.time + Random.Range(2.4f, 3.6f);
                RippleEffect.Spawn(_world, _castTarget, FishingScene.OrderWaterDetail, 0.5f, 0.25f);
            }
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
                // Held up for a moment, wriggling and dripping.
                var wiggle = Mathf.Sin(Time.time * 11f) * 6f * Mathf.Clamp01(1.4f - t);
                if (Time.time >= _nextDripAt)
                {
                    _nextDripAt = Time.time + Random.Range(0.18f, 0.35f);
                    Droplet.Splash(_world, hold + new Vector3(Random.Range(-0.4f, 0.4f) * size, -0.15f * size, 0f), FishingScene.OrderCatch + 1, 1, 0.15f);
                }

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

        /// <summary>
        /// A fish that bit and got away, in a few seconds (docs/SISTEMA_SUCESSO_PESCA.md, section 14):
        /// the rod fights, the bobber is dragged under and a dark shape moves in the water; then the
        /// line goes slack with a short splash, the shape darts off and the fisherman casts again.
        /// Rarer fish fight longer and harder (section 15).
        /// </summary>
        private void UpdateEscaping(float t, bool fishing)
        {
            var s = _escapeStrength;
            var fight = 0.45f + 0.35f * s;
            var total = fight + 1.2f + 0.2f * s;
            var shadowAlpha = 0.28f + 0.1f * s;
            var shadowSize = new Vector3(0.9f + 0.35f * s, 0.32f + 0.1f * s, 1f);

            if (t < fight)
            {
                var shake = 3f + 2.5f * s;
                _rodAngle = 22f + Mathf.Sin(Time.time * (20f + 4f * s)) * shake - 4f * s * Mathf.Clamp01(t / fight);
                ApplyRod();
                var pull = Mathf.Clamp01(t / 0.25f) * (0.16f + 0.06f * s);
                _bobber.localPosition = _escapeFrom + new Vector3(Mathf.Sin(Time.time * 17f) * 0.05f, -pull, 0f);

                // The fish almost shows itself: a dark shape pulling under the bobber.
                _shadow.transform.localPosition = _escapeFrom + new Vector3(Mathf.Sin(Time.time * 5f) * 0.25f, -0.35f, 0f);
                _shadow.transform.localScale = shadowSize;
                _shadow.color = new Color(0.02f, 0.06f, 0.08f, shadowAlpha * Mathf.Clamp01(t / 0.2f));
                if (Time.time >= _nextBiteRippleAt)
                {
                    _nextBiteRippleAt = Time.time + 0.3f;
                    RippleEffect.Spawn(_world, _bobber.localPosition, FishingScene.OrderWaterDetail, 0.7f + 0.2f * s, 0.5f);
                }
            }
            else
            {
                if (!_snapped)
                {
                    // The line loses tension: a short splash where the fish broke free.
                    _snapped = true;
                    LastEscapeAt = Time.time;
                    LastEscapePoint = _world.TransformPoint(_bobber.localPosition);
                    RippleEffect.Spawn(_world, _bobber.localPosition, FishingScene.OrderWaterDetail, 1.1f + 0.4f * s, 0.5f);
                    Droplet.Splash(_world, _bobber.localPosition, FishingScene.OrderCatch + 1, 7 + 5 * s, 0.8f + 0.2f * s);
                }

                var k = Mathf.Clamp01((t - fight) / 0.5f);
                // The rod springs back past rest and settles; the bobber floats up on a slack line.
                _rodAngle = Mathf.Lerp(72f, 58f, Ease(Mathf.Clamp01((t - fight) / 0.8f))) + Mathf.Sin((t - fight) * 18f) * 4f * (1f - k);
                ApplyRod();
                var rest = TipPosition() + new Vector3(0.35f, -0.95f, 0f);
                _bobber.localPosition = Vector3.Lerp(_escapeFrom, rest, Ease(k)) + new Vector3(0f, Mathf.Sin(k * Mathf.PI) * 0.25f, 0f);

                var gone = Mathf.Clamp01((t - fight) / 0.6f);
                _shadow.transform.localPosition = _escapeFrom + new Vector3(0f, -0.35f, 0f) + _shadowDir * (gone * (1.8f + 0.5f * s));
                _shadow.color = new Color(0.02f, 0.06f, 0.08f, shadowAlpha * (1f - gone));
            }

            if (t >= total)
            {
                Enter(fishing ? Phase.Casting : Phase.Idle);
            }
        }

        /// <summary>How hard a fish of this rarity fights: 0 common, 1 rare, 2 above.</summary>
        private static int Strength(string rarityId)
        {
            return rarityId == null || rarityId == "common" ? 0 : rarityId == "rare" ? 1 : 2;
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
            var slack = _phase == Phase.Waiting ? 0.45f : _phase == Phase.Casting ? 0.2f : _phase == Phase.Escaping && _snapped ? 0.65f : 0.05f;
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

        /// <summary>The aura of an important catch: rarity colour, gold for Excepcional, turquoise for a new species.</summary>
        private static Color GlowColor(CatchView c)
        {
            var theme = Visual.VisualTheme.Current;
            Color color;
            if (c.RarityId != null && c.RarityId != "common")
            {
                color = theme.Rarity(c.RarityId);
            }
            else if (Visual.VisualTheme.IsSpecialSize(c.SizeCategoryId))
            {
                color = theme.Size(c.SizeCategoryId);
            }
            else
            {
                color = theme.Action; // new species or personal record
            }

            color.a = 0.9f * theme.GlowIntensity + 0.1f;
            return color;
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
