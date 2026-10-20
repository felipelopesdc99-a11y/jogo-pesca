using System.Collections.Generic;
using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.Scene
{
    /// <summary>The scene's pixel grid (ART_BIBLE_PIXEL 3.1, TD-059): 216 pixels high, 20 pixels per world unit.</summary>
    public static class PixelGrid
    {
        /// <summary>Height of the scene grid in pixels (5× on a 1080p screen).</summary>
        public const int Height = 216;

        /// <summary>Grid pixels per world unit (216 ÷ 10.8).</summary>
        public const float PixelsPerUnit = ArtAssets.PixelsPerUnit;

        /// <summary>One grid pixel in world units.</summary>
        public const float Unit = 1f / PixelsPerUnit;

        /// <summary>True while the scene is drawn on the pixel grid (a map with pixel layers is on screen).</summary>
        public static bool Active { get; internal set; }

        /// <summary>A world coordinate rounded to the grid.</summary>
        public static float Snap(float v) => Mathf.Round(v * PixelsPerUnit) / PixelsPerUnit;

        /// <summary>A position rounded to the grid (z kept).</summary>
        public static Vector3 Snap(Vector3 p) => new Vector3(Snap(p.x), Snap(p.y), p.z);
    }

    /// <summary>
    /// The pixel camera of the scene (M25-T05, TD-059), without the 2D Pixel Perfect package: a second camera draws
    /// the world into a 216-pixel-high texture (Point filter) and the main camera shows it on screen at the largest
    /// whole scale, or — when the window height is not a multiple of 216 — at the next whole scale reduced with a
    /// bilinear filter ("nítido-suave"), with no black bars. The HUD (IMGUI) is drawn on top at screen resolution.
    /// </summary>
    /// <remarks>
    /// The main camera keeps its position, size and tag, so everything that converts world to screen points
    /// (the escape message over the bobber) keeps working; it just renders nothing itself. Right before the pixel
    /// camera draws, every unrotated sprite of the scene is put on a whole grid pixel and put back right after, so
    /// all that moves stops on whole pixels without touching the motion code. Nothing is allocated per frame: the
    /// textures are made when the window size changes and the sprite list is reused.
    /// </remarks>
    public sealed class PixelCamera : MonoBehaviour
    {
        private Camera _main;
        private Camera _pixel;
        private Transform _sceneRoot;
        private RenderTexture _grid;
        private RenderTexture _upscaled;
        private int _screenW, _screenH;
        private int _defaultMask;
        private bool _on;

        private readonly List<SpriteRenderer> _sprites = new List<SpriteRenderer>(512);
        private readonly List<Transform> _moved = new List<Transform>(512);
        private readonly List<Vector3> _saved = new List<Vector3>(512);

        /// <summary>Width of the grid now (384 in 16:9, 432 in 2:1).</summary>
        public int GridWidth { get; private set; } = 384;

        /// <summary>Sets up the pixel camera beside the main camera; it starts off.</summary>
        public static PixelCamera Attach(Camera main, Transform sceneRoot)
        {
            var pc = main.gameObject.GetComponent<PixelCamera>();
            if (pc == null)
            {
                pc = main.gameObject.AddComponent<PixelCamera>();
            }

            pc._main = main;
            pc._sceneRoot = sceneRoot;
            pc._defaultMask = main.cullingMask;
            if (pc._pixel == null)
            {
                var go = new GameObject("Câmera pixel");
                go.transform.SetParent(main.transform.parent, false);
                pc._pixel = go.AddComponent<Camera>();
                pc._pixel.enabled = false;
                go.AddComponent<PixelSnapPass>().Owner = pc;
            }

            return pc;
        }

        /// <summary>Turns the pixel grid on (a map with pixel layers) or off (the old painted scenery).</summary>
        public void SetOn(bool on)
        {
            _on = on;
            PixelGrid.Active = on;
            if (_main == null || _pixel == null)
            {
                return;
            }

            _main.cullingMask = on ? 0 : _defaultMask;
            _pixel.enabled = on;
            if (on)
            {
                Sync();
            }
        }

        private void OnDestroy()
        {
            PixelGrid.Active = false;
            Free();
            if (_pixel != null)
            {
                Destroy(_pixel.gameObject);
            }
        }

        private void LateUpdate()
        {
            if (_on)
            {
                Sync();
            }
        }

        /// <summary>The pixel camera follows the main one, on whole grid pixels, and its texture follows the window.</summary>
        private void Sync()
        {
            if (Screen.width != _screenW || Screen.height != _screenH || _grid == null)
            {
                Rebuild();
            }

            _pixel.orthographic = true;
            _pixel.orthographicSize = _main.orthographicSize;
            _pixel.clearFlags = CameraClearFlags.SolidColor;
            _pixel.backgroundColor = _main.backgroundColor;
            _pixel.cullingMask = _defaultMask;
            _pixel.depth = _main.depth - 1;
            _pixel.targetTexture = _grid;
            var p = _main.transform.position;
            _pixel.transform.position = new Vector3(PixelGrid.Snap(p.x), PixelGrid.Snap(p.y), p.z);
            _pixel.transform.rotation = _main.transform.rotation;
        }

        private void Rebuild()
        {
            Free();
            _screenW = Mathf.Max(1, Screen.width);
            _screenH = Mathf.Max(1, Screen.height);
            var aspect = _screenW / (float)_screenH;
            GridWidth = Mathf.Max(2, Mathf.RoundToInt(PixelGrid.Height * aspect / 2f) * 2);

            _grid = new RenderTexture(GridWidth, PixelGrid.Height, 16, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                name = "Grade da cena",
            };
            _grid.Create();

            // Not a whole multiple: enlarge by the next whole scale (sharp), then reduce smoothly to the window.
            if (_screenH % PixelGrid.Height != 0)
            {
                var k = Mathf.CeilToInt(_screenH / (float)PixelGrid.Height);
                _upscaled = new RenderTexture(GridWidth * k, PixelGrid.Height * k, 0, RenderTextureFormat.ARGB32)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    name = "Grade ampliada",
                };
                _upscaled.Create();
            }

            if (_pixel != null)
            {
                _pixel.targetTexture = _grid;
            }
        }

        private void Free()
        {
            if (_pixel != null)
            {
                _pixel.targetTexture = null;
            }

            if (_grid != null)
            {
                _grid.Release();
                Destroy(_grid);
                _grid = null;
            }

            if (_upscaled != null)
            {
                _upscaled.Release();
                Destroy(_upscaled);
                _upscaled = null;
            }
        }

        // The main camera draws nothing of its own while the grid is on; this puts the grid on the screen.
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!_on || _grid == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (_upscaled == null)
            {
                Graphics.Blit(_grid, destination);
                return;
            }

            Graphics.Blit(_grid, _upscaled);
            Graphics.Blit(_upscaled, destination);
        }

        /// <summary>Puts every unrotated sprite of the scene on a whole grid pixel (undone by <see cref="AfterRender"/>).</summary>
        internal void BeforeRender()
        {
            _moved.Clear();
            _saved.Clear();
            if (_sceneRoot == null)
            {
                return;
            }

            _sceneRoot.GetComponentsInChildren(false, _sprites);
            for (var i = 0; i < _sprites.Count; i++)
            {
                var t = _sprites[i].transform;
                if (t.rotation != Quaternion.identity)
                {
                    continue;
                }

                var world = t.position;
                var snapped = new Vector3(PixelGrid.Snap(world.x), PixelGrid.Snap(world.y), world.z);
                if (snapped == world)
                {
                    continue;
                }

                _moved.Add(t);
                _saved.Add(t.localPosition);
                t.position = snapped;
            }
        }

        /// <summary>Puts every sprite moved by <see cref="BeforeRender"/> back where the motion code left it.</summary>
        internal void AfterRender()
        {
            for (var i = _moved.Count - 1; i >= 0; i--)
            {
                if (_moved[i] != null)
                {
                    _moved[i].localPosition = _saved[i];
                }
            }

            _moved.Clear();
            _saved.Clear();
        }
    }

    /// <summary>Runs the grid snap around the pixel camera's own rendering (it lives on that camera).</summary>
    public sealed class PixelSnapPass : MonoBehaviour
    {
        internal PixelCamera Owner;

        private void OnPreCull()
        {
            if (Owner != null)
            {
                Owner.BeforeRender();
            }
        }

        private void OnPostRender()
        {
            if (Owner != null)
            {
                Owner.AfterRender();
            }
        }
    }
}
