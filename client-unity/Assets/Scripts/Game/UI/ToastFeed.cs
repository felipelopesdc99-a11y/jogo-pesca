using System.Collections.Generic;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    public enum ToastKind
    {
        Info,
        Catch,
        Important,
        Coins,
        LevelUp,
        Warning,
    }

    public sealed class Toast
    {
        public string Text;
        public ToastKind Kind;
        public float CreatedAt;
        public float Duration;
        public Texture2D Icon;
    }

    /// <summary>
    /// Non-blocking notifications (GDD section 39). A small bounded list: old entries simply drop
    /// off, nothing is persisted. Built from authoritative results the client already has; no
    /// extra request per notification.
    /// </summary>
    public sealed class ToastFeed
    {
        private const int MaxVisible = 5;
        private readonly List<Toast> _items = new List<Toast>();

        public IReadOnlyList<Toast> Items => _items;

        public void Push(string text, ToastKind kind, Texture2D icon = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _items.Add(new Toast
            {
                Text = text,
                Kind = kind,
                CreatedAt = Time.unscaledTime,
                Duration = kind == ToastKind.Warning ? 8f : kind == ToastKind.Important || kind == ToastKind.LevelUp ? 6f : 4f,
                Icon = icon,
            });

            while (_items.Count > MaxVisible)
            {
                _items.RemoveAt(0);
            }
        }

        /// <summary>Drops expired toasts. Called once per frame by the HUD.</summary>
        public void Prune()
        {
            _items.RemoveAll(t => Time.unscaledTime - t.CreatedAt > t.Duration);
        }
    }
}
