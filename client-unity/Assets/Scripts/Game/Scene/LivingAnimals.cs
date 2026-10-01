using System.Collections;
using System.Collections.Generic;
using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.Scene
{
    /// <summary>
    /// The animals of the living landscape and how each one behaves (addendum A-078). The scene
    /// director picks which one comes next; every behaviour here runs as one coroutine and ends when
    /// the animal has left, so only one is ever on screen.
    /// </summary>
    /// <remarks>
    /// Behaviour follows the real animals: the egret flaps slowly (about twice a second) and stands
    /// still for a long time before a quick strike; the kingfisher waits on a perch and dives; macaws
    /// fly in pairs; the toucan flaps and glides; swallows skim low over the water; capybaras graze at
    /// the water's edge; turtles bask on rocks and slide back in; howler monkeys sit for a long time.
    /// </remarks>
    public sealed class LivingAnimals
    {
        private const string Folder = "Vivos/Animais/";

        private readonly Dictionary<string, Vector3> _spots = new Dictionary<string, Vector3>();
        private readonly Transform _sky;
        private readonly Transform _shore;
        private readonly Transform _water;
        private readonly Transform _corner;
        private readonly float _halfWidth;
        private readonly float _current;
        private readonly List<Transform> _pads;

        public LivingAnimals(LivingSceneConfig scene, float halfWidth, float current, Transform sky, Transform shore, Transform water, Transform corner, List<Transform> pads)
        {
            _halfWidth = halfWidth;
            _current = current;
            _sky = sky;
            _shore = shore;
            _water = water;
            _corner = corner;
            _pads = pads;
            if (scene.spots != null)
            {
                foreach (var s in scene.spots)
                {
                    if (s != null && !string.IsNullOrEmpty(s.id))
                    {
                        _spots[s.id] = LivingConfig.Place(s.side, s.x, s.y, halfWidth);
                    }
                }
            }

            // The kingfisher's twig stays in the scene: the bird lands on it and leaves it behind.
            if (_spots.TryGetValue("perch", out var perch))
            {
                var twig = ArtAssets.Sprite(Folder + "martim_pouso_galho", PerchWidth, new Vector2(0.5f, 0f));
                if (twig != null)
                {
                    var r = FishingScene.Sprite(_corner, "Galho do martim-pescador", twig, perch, Vector3.one, FishingScene.OrderCornerFront);
                    r.gameObject.AddComponent<WindSway>().Bend = 1.5f;
                }
            }
        }

        private const float PerchWidth = 0.55f;

        /// <summary>Whether the animal's art and its place in this scene exist.</summary>
        public bool CanPlay(string id)
        {
            switch (id)
            {
                case "ducks_flying": return Has("pato_voo_1");
                case "heron_flyover": return Has("garca_voo_1");
                case "swallows": return Has("andorinha_1");
                case "macaws": return Has("arara_1");
                case "toucan": return Has("tucano_1");
                case "ducks_swimming": return Has("pato_1") && _spots.ContainsKey("ducks");
                case "heron_wading": return Has("garca_1") && Has("garca_voo_1") && _spots.ContainsKey("heron");
                case "kingfisher": return Has("martim_voo_1") && Has("martim_pouso") && _spots.ContainsKey("perch");
                case "capybaras": return Has("capivara_1") && _spots.ContainsKey("capybara");
                case "turtle": return Has("tartaruga_1") && _spots.ContainsKey("turtle");
                case "frog": return Has("sapo_1") && Has("sapo_pulo") && _spots.ContainsKey("frog");
                case "caiman": return Has("jacare_nadando") && _spots.ContainsKey("caiman");
                case "caiman_bank": return Has("jacare_margem") && _spots.ContainsKey("caiman_bank");
                case "monkey": return Has("macaco") && _spots.ContainsKey("monkey");
                case "monkey_hanging": return Has("macaco_pendurado") && _spots.ContainsKey("monkey_hanging");
                case "howler": return Has("bugio") && _spots.ContainsKey("howler");
                // Maps 3 and 4 (docs/PROGRESSAO_MAPAS_3_4.md): the same behaviours with the new art.
                case "jabiru_flyover": return Has("tuiuiu_voo_1");
                case "jabiru_wading": return Has("tuiuiu_1") && Has("tuiuiu_voo_1") && _spots.ContainsKey("heron");
                case "hyacinth_macaws": return Has("arara_azul_1");
                case "spoonbills": return Has("colhereiro_voo_1");
                case "scarlet_ibis": return Has("guara_voo_1");
                case "terns": return Has("trinta_reis_1");
                case "fiddler_crab": return Has("caranguejo_1") && _spots.ContainsKey("crab");
                case "dolphin": return Has("boto_1") && _spots.ContainsKey("dolphin");
                default: return false;
            }
        }

        public IEnumerator Play(string id)
        {
            switch (id)
            {
                case "ducks_flying": return Flock("pato_voo", 4, 0.42f, Random.Range(3, 6), 2.3f, 14f, 2.3f, 3.9f);
                case "heron_flyover": return Flock("garca_voo", 4, 0.95f, 1, 1.4f, 8f, 2.6f, 4.0f);
                case "macaws": return Flock("arara", 4, 0.8f, 2, 2.2f, 10f, 2.8f, 4.2f);
                case "toucan": return Toucan();
                case "swallows": return Swallows();
                case "ducks_swimming": return DucksSwimming();
                case "heron_wading": return HeronWading("garca", "garca_voo", 1.5f, 1.9f, "Garça");
                case "kingfisher": return Kingfisher();
                case "capybaras": return Capybaras();
                case "turtle": return Turtle();
                case "frog": return Frog();
                case "caiman": return Caiman();
                case "caiman_bank": return Appear("jacare_margem", 0.95f, _spots["caiman_bank"], _shore, FishingScene.OrderShoreLife, true, 40f, 80f, new Vector3(0f, -0.1f, 0f), true);
                case "monkey": return Appear("macaco", 0.42f, _spots["monkey"], _shore, FishingScene.OrderShoreLife, false, 25f, 50f, new Vector3(0f, -0.05f, 0f), false);
                case "monkey_hanging": return HangingMonkey();
                case "howler": return Appear("bugio", 0.45f, _spots["howler"], _shore, FishingScene.OrderShoreLife, true, 40f, 80f, new Vector3(0f, -0.05f, 0f), false);
                // The jabiru flaps slowly and glides; spoonbills and scarlet ibises fly in small lines;
                // terns fly fast and low over the water.
                case "jabiru_flyover": return Flock("tuiuiu_voo", 4, 1.15f, 1, 1.2f, 6f, 2.6f, 4.0f);
                case "jabiru_wading": return HeronWading("tuiuiu", "tuiuiu_voo", 1.7f, 2.2f, "Tuiuiú");
                case "hyacinth_macaws": return Flock("arara_azul", 4, 0.8f, 2, 2.2f, 10f, 2.8f, 4.2f);
                case "spoonbills": return Flock("colhereiro_voo", 4, 0.75f, Random.Range(1, 4), 1.6f, 8f, 2.4f, 3.8f);
                case "scarlet_ibis": return Flock("guara_voo", 4, 0.6f, Random.Range(3, 7), 1.8f, 8f, 2.4f, 4.0f);
                case "terns": return Flock("trinta_reis", 4, 0.5f, Random.Range(2, 4), 2.6f, 9f, 1.4f, 3.0f);
                case "fiddler_crab": return FiddlerCrab();
                case "dolphin": return Dolphin();
                default: return Nothing();
            }
        }

        // ------------------------------------------------------------------ flying across

        /// <summary>A flock (or one bird) crossing the sky in a loose line, each bird flapping out of step.</summary>
        private IEnumerator Flock(string art, int frames, float width, int count, float speed, float fps, float yMin, float yMax)
        {
            var sprites = Frames(art, frames, width, new Vector2(0.5f, 0.5f));
            if (sprites == null)
            {
                yield break;
            }

            var dir = Random.value > 0.5f ? 1f : -1f;
            var y = Random.Range(yMin, yMax);
            speed *= Random.Range(0.9f, 1.1f);
            var birds = new List<Transform>();
            for (var i = 0; i < count; i++)
            {
                var r = Spawn(_sky, art, sprites[0], Vector3.zero, FishingScene.OrderSkyLife, dir < 0f);
                var book = r.gameObject.AddComponent<Flipbook>();
                book.Frames = sprites;
                book.Fps = fps * Random.Range(0.9f, 1.1f);
                // Behind the leader, a little lower or higher, like a real line of birds.
                var offset = new Vector3(-dir * i * width * 1.4f, (i % 2 == 0 ? -1f : 1f) * i * 0.12f + Random.Range(-0.05f, 0.05f), 0f);
                r.transform.localPosition = new Vector3(-dir * (_halfWidth + 1.5f), y, 0f) + offset;
                r.transform.localScale = Vector3.one * Random.Range(0.92f, 1.05f);
                birds.Add(r.transform);
            }

            var t = 0f;
            var distance = 2f * _halfWidth + 3f + count * width * 1.4f;
            while (t * speed < distance)
            {
                t += Time.deltaTime;
                foreach (var b in birds)
                {
                    if (b != null)
                    {
                        b.localPosition += new Vector3(dir * speed * Time.deltaTime, Mathf.Sin(t * 0.8f + b.GetInstanceID()) * 0.03f * Time.deltaTime, 0f);
                    }
                }

                yield return null;
            }

            Clear(birds);
        }

        /// <summary>The toucan flies in bounds: a few flaps climbing, then a glide with the beak slowly dipping.</summary>
        private IEnumerator Toucan()
        {
            var sprites = Frames("tucano", 4, 0.62f, new Vector2(0.5f, 0.5f));
            if (sprites == null)
            {
                yield break;
            }

            var dir = Random.value > 0.5f ? 1f : -1f;
            var r = Spawn(_sky, "Tucano", sprites[0], new Vector3(-dir * (_halfWidth + 1.2f), Random.Range(2.9f, 3.8f), 0f), FishingScene.OrderSkyLife, dir < 0f);
            var book = r.gameObject.AddComponent<Flipbook>();
            book.Frames = sprites;
            book.Fps = 12f;
            var speed = 2.1f;
            var t = 0f;
            var phase = 0f;
            while (Mathf.Abs(r.transform.localPosition.x) < _halfWidth + 1.6f || t < 1f)
            {
                t += Time.deltaTime;
                phase += Time.deltaTime;
                var flapping = phase < 0.7f;
                if (flapping && !book.Playing)
                {
                    book.Playing = true;
                }
                else if (!flapping && book.Playing)
                {
                    book.Hold(1);
                }

                if (phase > 1.8f)
                {
                    phase = 0f;
                }

                var climb = flapping ? 0.25f : -0.28f;
                r.transform.localPosition += new Vector3(dir * speed * Time.deltaTime, climb * Time.deltaTime, 0f);
                r.transform.localRotation = Quaternion.Euler(0f, 0f, dir * (flapping ? 3f : -5f));
                yield return null;
            }

            Object.Destroy(r.gameObject);
        }

        /// <summary>Two or three swallows skimming low over the water in quick swoops, flapping in bursts.</summary>
        private IEnumerator Swallows()
        {
            var sprites = Frames("andorinha", 3, 0.34f, new Vector2(0.5f, 0.5f));
            if (sprites == null)
            {
                yield break;
            }

            var count = Random.Range(2, 4);
            var dir = Random.value > 0.5f ? 1f : -1f;
            var birds = new List<SpriteRenderer>();
            var starts = new List<float>();
            var bases = new List<float>();
            var speeds = new List<float>();
            for (var i = 0; i < count; i++)
            {
                var r = Spawn(_water, "Andorinha", sprites[0], new Vector3(-dir * (_halfWidth + 1f), 0f, 0f), FishingScene.OrderWaterLife, dir < 0f);
                birds.Add(r);
                starts.Add(i * Random.Range(0.5f, 1.3f));
                bases.Add(Random.Range(-2.3f, -0.5f));
                speeds.Add(Random.Range(4f, 5.5f));
            }

            var t = 0f;
            var done = false;
            while (!done)
            {
                t += Time.deltaTime;
                done = true;
                for (var i = 0; i < count; i++)
                {
                    var r = birds[i];
                    var age = t - starts[i];
                    var x = -dir * (_halfWidth + 1f) + dir * speeds[i] * Mathf.Max(0f, age);
                    if (Mathf.Abs(x) < _halfWidth + 1.2f || age < 0f)
                    {
                        done = false;
                    }

                    var swoop = Mathf.Sin(age * 1.7f + i) * 0.35f;
                    var dy = Mathf.Cos(age * 1.7f + i) * 0.6f;
                    r.transform.localPosition = new Vector3(x, bases[i] + swoop, 0f);
                    r.transform.localRotation = Quaternion.Euler(0f, 0f, dir * dy * 12f);
                    // Flap bursts (a real swallow beats very fast) and short glides.
                    var burst = Mathf.Repeat(age + i * 0.3f, 0.9f) < 0.4f;
                    r.sprite = burst ? sprites[(int)(age * 22f) % sprites.Length] : sprites[1];
                }

                yield return null;
            }

            foreach (var r in birds)
            {
                Object.Destroy(r.gameObject);
            }
        }

        // ------------------------------------------------------------------ on the water

        /// <summary>A pair of ducks swimming across, now and then dipping the head or shaking the wings.</summary>
        private IEnumerator DucksSwimming()
        {
            var width = 0.55f;
            var swim = Frames("pato", 3, width, new Vector2(0f, 0f));
            if (swim == null)
            {
                yield break;
            }

            // On the river they drift with the current; on the lake they pick a side.
            var dir = _current > 0f ? 1f : (Random.value > 0.5f ? 1f : -1f);
            var speed = 0.33f + _current * 0.3f;
            var spot = _spots["ducks"];
            var ducks = new List<SpriteRenderer>();
            var nextMove = new List<float>();
            for (var i = 0; i < 2; i++)
            {
                var r = Spawn(_water, "Pato", swim[0], new Vector3(-dir * (_halfWidth + 1f + i * 0.8f), spot.y + i * 0.06f, 0f), FishingScene.OrderWaterLife, dir < 0f);
                r.transform.localScale = Vector3.one * (i == 0 ? 1f : 0.9f);
                Mirror.Attach(r, 0.5f, 0.28f);
                ducks.Add(r);
                nextMove.Add(Random.Range(4f, 10f));
            }

            var t = 0f;
            var ripple = 0f;
            var holdUntil = new float[2];
            while (Mathf.Abs(ducks[1].transform.localPosition.x) < _halfWidth + 1.5f || t < 3f)
            {
                t += Time.deltaTime;
                ripple -= Time.deltaTime;
                for (var i = 0; i < ducks.Count; i++)
                {
                    var r = ducks[i];
                    var p = r.transform.localPosition;
                    r.transform.localPosition = new Vector3(p.x + dir * speed * Time.deltaTime, spot.y + i * 0.06f + Mathf.Sin(t * 1.6f + i) * 0.006f, 0f);
                    if (t > nextMove[i])
                    {
                        var shake = Random.value < 0.15f;
                        r.sprite = swim[shake ? 2 : 1];
                        holdUntil[i] = t + (shake ? 0.8f : Random.Range(1.2f, 2f));
                        nextMove[i] = t + Random.Range(7f, 15f);
                    }
                    else if (t > holdUntil[i])
                    {
                        r.sprite = swim[0];
                    }
                }

                if (ripple <= 0f)
                {
                    ripple = 1f;
                    foreach (var r in ducks)
                    {
                        RippleEffect.Spawn(_water, r.transform.localPosition + new Vector3(dir * 0.1f, 0.02f, 0f), FishingScene.OrderWaterDetail + 1, 0.35f, 0.5f);
                    }
                }

                yield return null;
            }

            foreach (var r in ducks)
            {
                Object.Destroy(r.gameObject);
            }
        }

        /// <summary>The caiman surfaces, drifts along with only the eyes and snout showing, then sinks.</summary>
        private IEnumerator Caiman()
        {
            var sprite = One("jacare_nadando", 0.95f, new Vector2(0.5f, 0f));
            if (sprite == null)
            {
                yield break;
            }

            var spot = _spots["caiman"];
            var start = new Vector3(Random.Range(-_halfWidth + 3f, _halfWidth - 7f), spot.y + Random.Range(-0.2f, 0.2f), 0f);
            var r = Spawn(_water, "Jacaré", sprite, start + new Vector3(0f, -0.05f, 0f), FishingScene.OrderWaterLife, false);
            var speed = 0.18f + _current * 0.35f;
            var stay = Random.Range(18f, 30f);
            var t = 0f;
            var ripple = 0f;
            while (t < stay)
            {
                t += Time.deltaTime;
                ripple -= Time.deltaTime;
                var surface = Mathf.Clamp01(t / 2.5f) * Mathf.Clamp01((stay - t) / 2f);
                SetAlpha(r, surface);
                r.transform.localPosition = start + new Vector3(speed * t, -0.05f * (1f - surface) + Mathf.Sin(t * 1.1f) * 0.005f, 0f);
                if (ripple <= 0f && surface > 0.5f)
                {
                    ripple = 1.2f;
                    RippleEffect.Spawn(_water, r.transform.localPosition + new Vector3(0.4f, 0.03f, 0f), FishingScene.OrderWaterDetail + 1, 0.45f, 0.5f);
                }

                yield return null;
            }

            Object.Destroy(r.gameObject);
        }

        // ------------------------------------------------------------------ near the viewer

        /// <summary>
        /// A great egret glides down into the shallows by the reeds, stands very still, stalks and now
        /// and then strikes at the water, then flies off.
        /// </summary>
        /// <summary>A wading bird (egret, jabiru) lands on its spot, stalks and strikes, then flies off.</summary>
        private IEnumerator HeronWading(string standArt, string flyArt, float standHeight, float flyWidth, string label)
        {
            var stand = FramesByHeight(standArt, 3, standHeight, new Vector2(0.5f, 0f));
            var fly = Frames(flyArt, 4, flyWidth, new Vector2(0.5f, 0.5f));
            if (stand == null || fly == null)
            {
                yield break;
            }

            var spot = _spots["heron"];
            var above = spot + new Vector3(0.15f, 0.75f, 0f);

            // Arrive from the right, facing left, slowing down to land.
            var bird = Spawn(_corner, label, fly[0], new Vector3(_halfWidth + 2.5f, spot.y + 4f, 0f), FishingScene.OrderCornerBack, true);
            var book = bird.gameObject.AddComponent<Flipbook>();
            book.Frames = fly;
            book.Fps = 8f;
            var from = bird.transform.localPosition;
            yield return Glide(bird.transform, from, above, 4.5f, book, 1);

            Object.Destroy(book);
            bird.sprite = stand[0];
            bird.transform.localPosition = spot;
            Mirror.Attach(bird, 0.45f, 0.3f);
            RippleEffect.Spawn(_corner, spot, FishingScene.OrderCornerBack - 1, 0.8f, 0.5f);

            var stay = Random.Range(45f, 90f);
            var t = 0f;
            while (t < stay)
            {
                var still = Random.Range(6f, 14f);
                bird.sprite = stand[0];
                yield return Wait(still);
                t += still;
                if (Random.value < 0.55f)
                {
                    // Stalking: neck stretched forward, very slow.
                    var stalk = Random.Range(3f, 6f);
                    bird.sprite = stand[1];
                    yield return Wait(stalk);
                    t += stalk;
                    if (Random.value < 0.5f)
                    {
                        bird.sprite = stand[2];
                        RippleEffect.Spawn(_corner, spot + new Vector3(-0.55f, 0.02f, 0f), FishingScene.OrderCornerBack - 1, 0.45f, 0.5f);
                        yield return Wait(0.4f);
                        bird.sprite = stand[1];
                        yield return Wait(1f);
                        t += 1.4f;
                    }
                }
            }

            // Take off to the upper right.
            Object.Destroy(bird.gameObject);
            var leaving = Spawn(_corner, label, fly[0], above, FishingScene.OrderCornerBack, false);
            var wings = leaving.gameObject.AddComponent<Flipbook>();
            wings.Frames = fly;
            wings.Fps = 9f;
            yield return Glide(leaving.transform, above, new Vector3(_halfWidth + 3f, spot.y + 5f, 0f), 4f, null, 0, true);
            Object.Destroy(leaving.gameObject);
        }

        /// <summary>The kingfisher flies to its twig, watches the water, dives with a splash and flies off.</summary>
        private IEnumerator Kingfisher()
        {
            var fly = Frames("martim_voo", 2, 0.5f, new Vector2(0.5f, 0.5f));
            var perched = One("martim_pouso", PerchWidth, new Vector2(0.5f, 0f));
            var dive = One("martim_mergulho", 0.26f, new Vector2(0.5f, 0.5f));
            if (fly == null || perched == null)
            {
                yield break;
            }

            var perch = _spots["perch"];
            var above = perch + new Vector3(0f, 0.3f, 0f);
            var bird = Spawn(_corner, "Martim-pescador", fly[0], new Vector3(-_halfWidth - 1f, perch.y + 1.2f, 0f), FishingScene.OrderCornerFront + 1, false);
            var book = bird.gameObject.AddComponent<Flipbook>();
            book.Frames = fly;
            book.Fps = 14f;
            yield return Glide(bird.transform, bird.transform.localPosition, above, 2.4f, book, 0);
            Object.Destroy(book);
            bird.sprite = perched;
            bird.transform.localPosition = perch;
            yield return Wait(Random.Range(10f, 25f));

            if (dive != null && Random.value < 0.7f)
            {
                var target = perch + new Vector3(2.6f, -0.9f, 0f);
                bird.sprite = dive;
                bird.transform.localPosition = perch + new Vector3(0.1f, 0.25f, 0f);
                var start = bird.transform.localPosition;
                var delta = target - start;
                bird.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.x, -delta.y) * Mathf.Rad2Deg);
                yield return Glide(bird.transform, start, target, 0.45f, null, 0, true);
                bird.enabled = false;
                Droplet.Splash(_corner, target, FishingScene.OrderCornerFront + 2, 10, 0.7f);
                RippleEffect.Spawn(_corner, target, FishingScene.OrderCornerBack - 1, 0.8f, 0.5f);
                yield return Wait(1.1f);
                bird.enabled = true;
                bird.transform.localRotation = Quaternion.identity;
                bird.transform.localPosition = target;
                Droplet.Splash(_corner, target, FishingScene.OrderCornerFront + 2, 5, 0.5f);
            }

            var away = bird.gameObject.AddComponent<Flipbook>();
            away.Frames = fly;
            away.Fps = 16f;
            var from = bird.transform.localPosition;
            yield return Glide(bird.transform, from, new Vector3(_halfWidth + 1.5f, from.y + 2.2f, 0f), 3f, null, 0, true);
            Object.Destroy(bird.gameObject);
        }

        /// <summary>A turtle climbs onto its rock, basks, lifts its head now and then, and slides back in.</summary>
        private IEnumerator Turtle()
        {
            var frames = Frames("tartaruga", 3, 0.8f, new Vector2(0f, 0f));
            if (frames == null)
            {
                yield break;
            }

            // It faces the lake, to the left; the pivot is its back end.
            var spot = _spots["turtle"] + new Vector3(0.4f, 0f, 0f);
            var water = new Vector3(-0.3f, -0.22f, 0f);
            var r = Spawn(_corner, "Tartaruga", frames[2], spot + water, FishingScene.OrderCornerFront, true);
            RippleEffect.Spawn(_corner, spot + water + new Vector3(-0.35f, 0f, 0f), FishingScene.OrderCornerBack - 1, 0.5f, 0.5f);
            var t = 0f;
            while (t < 2.5f)
            {
                t += Time.deltaTime;
                var k = Mathf.SmoothStep(0f, 1f, t / 2.5f);
                SetAlpha(r, Mathf.Clamp01(t / 1.2f));
                r.transform.localPosition = spot + water * (1f - k);
                yield return null;
            }

            r.sprite = frames[0];
            var stay = Random.Range(35f, 75f);
            t = 0f;
            while (t < stay)
            {
                var rest = Random.Range(9f, 18f);
                r.sprite = frames[0];
                yield return Wait(rest);
                t += rest;
                var look = Random.Range(3f, 6f);
                r.sprite = frames[1];
                yield return Wait(look);
                t += look;
            }

            r.sprite = frames[2];
            t = 0f;
            while (t < 1.6f)
            {
                t += Time.deltaTime;
                var k = t / 1.6f;
                r.transform.localPosition = spot + water * k;
                SetAlpha(r, 1f - Mathf.Clamp01((t - 0.8f) / 0.8f));
                yield return null;
            }

            RippleEffect.Spawn(_corner, spot + water + new Vector3(-0.35f, 0f, 0f), FishingScene.OrderCornerBack - 1, 0.6f, 0.5f);
            Object.Destroy(r.gameObject);
        }

        /// <summary>A frog hops onto a lily pad, croaks in short bouts, and jumps into the water.</summary>
        private IEnumerator Frog()
        {
            var sit = Frames("sapo", 2, 0.32f, new Vector2(0.5f, 0f));
            var jump = One("sapo_pulo", 0.4f, new Vector2(0.5f, 0.3f));
            if (sit == null || jump == null)
            {
                yield break;
            }

            // It sits on the nearest floating pad, riding its bob.
            var spot = _spots["frog"];
            var parent = _water;
            var local = spot;
            foreach (var pad in _pads)
            {
                if (pad != null && Vector3.Distance(pad.localPosition, spot) < 0.6f)
                {
                    parent = pad;
                    local = new Vector3(0f, 0.02f, 0f);
                    break;
                }
            }

            var worldSpot = parent.TransformPoint(local);
            var r = Spawn(_water, "Sapo", jump, spot + new Vector3(-1.1f, 0.3f, 0f), FishingScene.OrderWaterLife + 2, false);
            yield return Hop(r.transform, r.transform.localPosition, _water.InverseTransformPoint(worldSpot), 0.6f, 0.45f);
            r.transform.SetParent(parent, false);
            r.transform.localPosition = local;
            r.sprite = sit[0];
            RippleEffect.Spawn(_water, spot, FishingScene.OrderWaterDetail + 1, 0.4f, 0.5f);

            var stay = Random.Range(20f, 45f);
            var t = 0f;
            while (t < stay)
            {
                var pause = Random.Range(5f, 10f);
                yield return Wait(pause);
                t += pause;
                var croaks = Random.Range(2, 5);
                for (var i = 0; i < croaks; i++)
                {
                    r.sprite = sit[1];
                    yield return Wait(0.35f);
                    r.sprite = sit[0];
                    yield return Wait(0.3f);
                    t += 0.65f;
                }
            }

            var from = _water.InverseTransformPoint(r.transform.position);
            r.transform.SetParent(_water, false);
            r.transform.localPosition = from;
            r.sprite = jump;
            var into = from + new Vector3(1.2f, -0.35f, 0f);
            yield return Hop(r.transform, from, into, 0.6f, 0.4f);
            Droplet.Splash(_water, into, FishingScene.OrderWaterLife + 2, 6, 0.5f);
            RippleEffect.Spawn(_water, into, FishingScene.OrderWaterDetail + 1, 0.5f, 0.5f);
            Object.Destroy(r.gameObject);
        }

        // ------------------------------------------------------------------ on the shore

        /// <summary>A capybara (sometimes with its young) walks along the bank, grazes for a while and walks back.</summary>
        private IEnumerator Capybaras()
        {
            var adult = Frames("capivara", 2, 0.62f, new Vector2(0.5f, 0f));
            if (adult == null)
            {
                yield break;
            }

            var young = Random.value < 0.6f ? One("capivara_filhote", 0.36f, new Vector2(0.5f, 0f)) : null;
            var spot = _spots["capybara"] + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);
            var start = new Vector3(-_halfWidth - 1f, spot.y, 0f);
            var a = Spawn(_shore, "Capivara", adult[0], start, FishingScene.OrderShoreLife + 1, false);
            Mirror.Attach(a, 0.6f, 0.3f);
            SpriteRenderer y = null;
            if (young != null)
            {
                y = Spawn(_shore, "Filhote de capivara", young, start + new Vector3(-0.6f, 0f, 0f), FishingScene.OrderShoreLife, false);
                Mirror.Attach(y, 0.6f, 0.3f);
            }

            yield return Walk(a, y, start, spot, 0.22f);

            var stay = Random.Range(35f, 65f);
            var t = 0f;
            while (t < stay)
            {
                var graze = Random.Range(6f, 15f);
                a.sprite = adult[1];
                yield return Wait(graze);
                var look = Random.Range(3f, 6f);
                a.sprite = adult[0];
                yield return Wait(look);
                t += graze + look;
            }

            a.sprite = adult[0];
            a.flipX = true;
            if (y != null)
            {
                y.flipX = true;
            }

            yield return Walk(a, y, spot, start - new Vector3(0.8f, 0f, 0f), 0.22f);
            Object.Destroy(a.gameObject);
            if (y != null)
            {
                Object.Destroy(y.gameObject);
            }
        }

        /// <summary>The capuchin hanging by its tail swings gently among the trees on the bank.</summary>
        private IEnumerator HangingMonkey()
        {
            var sprite = SpriteTall("macaco_pendurado", 0.6f, new Vector2(0.5f, 1f));
            if (sprite == null)
            {
                yield break;
            }

            var spot = _spots["monkey_hanging"];
            var r = Spawn(_shore, "Macaco", sprite, spot, FishingScene.OrderShoreLife, true);
            var stay = Random.Range(20f, 40f);
            var t = 0f;
            while (t < stay)
            {
                t += Time.deltaTime;
                SetAlpha(r, Mathf.Clamp01(t / 2f) * Mathf.Clamp01((stay - t) / 2f));
                var swing = Mathf.Lerp(6f, 2f, Mathf.Clamp01(t / 8f));
                r.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.6f) * swing);
                yield return null;
            }

            Object.Destroy(r.gameObject);
        }

        /// <summary>
        /// An animal that comes out of cover (the leaves, or the water for the caiman on the bank),
        /// stays put for a while and goes back.
        /// </summary>
        /// <summary>
        /// A fiddler crab on the mud: it comes out, waves its big claw a few times, scuttles sideways
        /// and goes back into its hole.
        /// </summary>
        private IEnumerator FiddlerCrab()
        {
            var frames = FramesByHeight("caranguejo", 3, 0.28f, new Vector2(0.5f, 0f));
            if (frames == null)
            {
                yield break;
            }

            var spot = _spots["crab"];
            var r = Spawn(_corner, "Caranguejo", frames[0], spot, FishingScene.OrderCornerFront, false);
            for (var t = 0f; t < 1f; t += Time.deltaTime)
            {
                SetAlpha(r, t);
                yield return null;
            }

            SetAlpha(r, 1f);
            var waves = Random.Range(2, 5);
            for (var i = 0; i < waves; i++)
            {
                yield return Wait(Random.Range(1.5f, 4f));
                r.sprite = frames[1];
                yield return Wait(0.5f);
                r.sprite = frames[0];
            }

            // Sideways to the hole, legs moving.
            var dir = Random.value > 0.5f ? 1f : -1f;
            var from = r.transform.localPosition;
            for (var t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                r.sprite = frames[(int)(t * 8f) % 2 == 0 ? 2 : 0];
                r.transform.localPosition = from + new Vector3(dir * 0.35f * t, 0f, 0f);
                SetAlpha(r, Mathf.Clamp01((1.6f - t) / 0.5f));
                yield return null;
            }

            Object.Destroy(r.gameObject);
        }

        /// <summary>
        /// A guiana dolphin far out in the channel: two or three surfacings in a row, the back and fin
        /// showing, sometimes a leap, then the tail going down.
        /// </summary>
        private IEnumerator Dolphin()
        {
            var frames = Frames("boto", 3, 0.9f, new Vector2(0.5f, 0f));
            if (frames == null)
            {
                yield break;
            }

            var spot = _spots["dolphin"];
            var dir = Random.value > 0.5f ? 1f : -1f;
            var x = dir > 0 ? Random.Range(-_halfWidth + 2f, -2f) : Random.Range(2f, _halfWidth - 2f);
            var r = Spawn(_water, "Boto", frames[0], new Vector3(x, spot.y, 0f), FishingScene.OrderWaterLife, dir < 0);
            SetAlpha(r, 0f);
            var surfacings = Random.Range(2, 4);
            for (var i = 0; i < surfacings; i++)
            {
                var leap = Random.value < 0.35f;
                var at = new Vector3(x, spot.y, 0f);
                RippleEffect.Spawn(_water, at, FishingScene.OrderWaterDetail + 1, 0.7f, 0.5f);
                // Back and fin roll through the surface.
                r.sprite = leap ? frames[1] : frames[0];
                for (var t = 0f; t < 1.4f; t += Time.deltaTime)
                {
                    var k = t / 1.4f;
                    SetAlpha(r, Mathf.Sin(k * Mathf.PI));
                    var rise = leap ? Mathf.Sin(k * Mathf.PI) * 0.45f : Mathf.Sin(k * Mathf.PI) * 0.06f - 0.06f;
                    r.transform.localPosition = at + new Vector3(dir * 0.5f * k, rise, 0f);
                    yield return null;
                }

                if (leap)
                {
                    Droplet.Splash(_water, at + new Vector3(dir * 0.5f, 0f, 0f), FishingScene.OrderWaterLife + 1, 8, 0.6f);
                }

                // The tail goes down.
                r.sprite = frames[2];
                var tailAt = at + new Vector3(dir * 0.55f, -0.02f, 0f);
                for (var t = 0f; t < 0.9f; t += Time.deltaTime)
                {
                    SetAlpha(r, 1f - t / 0.9f);
                    r.transform.localPosition = tailAt + new Vector3(0f, -0.12f * t, 0f);
                    yield return null;
                }

                SetAlpha(r, 0f);
                x += dir * Random.Range(1.6f, 2.6f);
                yield return Wait(Random.Range(2.5f, 5f));
            }

            Object.Destroy(r.gameObject);
        }

        private IEnumerator Appear(string art, float height, Vector3 spot, Transform parent, int order, bool faceLeft, float minStay, float maxStay, Vector3 from, bool reflect)
        {
            var sprite = SpriteTall(art, height, new Vector2(0.5f, 0f));
            if (sprite == null)
            {
                yield break;
            }

            var r = Spawn(parent, art, sprite, spot + from, order, faceLeft);
            if (reflect)
            {
                Mirror.Attach(r, 0.6f, 0.3f);
            }

            var stay = Random.Range(minStay, maxStay);
            var t = 0f;
            while (t < stay)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / 2.5f) * Mathf.Clamp01((stay - t) / 2.5f);
                SetAlpha(r, k);
                r.transform.localPosition = spot + from * (1f - Mathf.SmoothStep(0f, 1f, k));
                yield return null;
            }

            Object.Destroy(r.gameObject);
        }

        // ------------------------------------------------------------------ helpers

        private IEnumerator Walk(SpriteRenderer a, SpriteRenderer young, Vector3 from, Vector3 to, float speed)
        {
            var dist = Vector3.Distance(from, to);
            var dir = Mathf.Sign(to.x - from.x);
            var t = 0f;
            while (t * speed < dist)
            {
                t += Time.deltaTime;
                var p = Vector3.MoveTowards(from, to, t * speed);
                var step = Mathf.Abs(Mathf.Sin(t * 5.5f)) * 0.012f;
                a.transform.localPosition = p + new Vector3(0f, step, 0f);
                if (young != null)
                {
                    young.transform.localPosition = p + new Vector3(-dir * 0.6f, Mathf.Abs(Mathf.Sin(t * 7f + 1f)) * 0.01f, 0f);
                }

                yield return null;
            }
        }

        /// <summary>Moves along a gentle curve; slows down at the end unless <paramref name="accelerate"/>.</summary>
        private static IEnumerator Glide(Transform t, Vector3 from, Vector3 to, float seconds, Flipbook book, int glideFrame, bool accelerate = false)
        {
            var age = 0f;
            while (age < seconds && t != null)
            {
                age += Time.deltaTime;
                var k = Mathf.Clamp01(age / seconds);
                var eased = accelerate ? k * k : 1f - (1f - k) * (1f - k);
                var arc = Mathf.Sin(k * Mathf.PI) * 0.25f;
                t.localPosition = Vector3.Lerp(from, to, eased) + new Vector3(0f, arc, 0f);
                if (book != null && k > 0.75f && book.Playing)
                {
                    book.Hold(glideFrame);
                }

                yield return null;
            }
        }

        private static IEnumerator Hop(Transform t, Vector3 from, Vector3 to, float seconds, float height)
        {
            var age = 0f;
            while (age < seconds)
            {
                age += Time.deltaTime;
                var k = Mathf.Clamp01(age / seconds);
                t.localPosition = Vector3.Lerp(from, to, k) + new Vector3(0f, Mathf.Sin(k * Mathf.PI) * height, 0f);
                yield return null;
            }
        }

        private static IEnumerator Wait(float seconds)
        {
            var end = Time.time + seconds;
            while (Time.time < end)
            {
                yield return null;
            }
        }

        private static IEnumerator Nothing()
        {
            yield break;
        }

        private static void SetAlpha(SpriteRenderer r, float a)
        {
            var c = r.color;
            r.color = new Color(c.r, c.g, c.b, a);
        }

        private static void Clear(List<Transform> items)
        {
            foreach (var i in items)
            {
                if (i != null)
                {
                    Object.Destroy(i.gameObject);
                }
            }
        }

        private static SpriteRenderer Spawn(Transform parent, string name, Sprite sprite, Vector3 position, int order, bool faceLeft)
        {
            var r = FishingScene.Sprite(parent, name, sprite, position, Vector3.one, order);
            // The art faces right; facing left is a mirror image.
            r.flipX = faceLeft;
            return r;
        }

        private static bool Has(string art) => ArtAssets.Texture(Folder + art) != null;

        private static Sprite One(string art, float unitsWide, Vector2 pivot) => ArtAssets.Sprite(Folder + art, unitsWide, pivot);

        private static Sprite SpriteTall(string art, float unitsTall, Vector2 pivot) => ArtAssets.SpriteByHeight(Folder + art, unitsTall, pivot);

        private static Sprite[] Frames(string art, int count, float unitsWide, Vector2 pivot)
        {
            var frames = new Sprite[count];
            for (var i = 0; i < count; i++)
            {
                frames[i] = ArtAssets.Sprite(Folder + art + "_" + (i + 1), unitsWide, pivot);
                if (frames[i] == null)
                {
                    return null;
                }
            }

            return frames;
        }

        private static Sprite[] FramesByHeight(string art, int count, float unitsTall, Vector2 pivot)
        {
            var frames = new Sprite[count];
            for (var i = 0; i < count; i++)
            {
                frames[i] = ArtAssets.SpriteByHeight(Folder + art + "_" + (i + 1), unitsTall, pivot);
                if (frames[i] == null)
                {
                    return null;
                }
            }

            return frames;
        }
    }
}
