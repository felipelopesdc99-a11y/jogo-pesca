using System;
using System.Collections.Generic;
using FishingIdle.Game.Audio;
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

    /// <summary>An entry of the Notification Center (the bell).</summary>
    public sealed class NotificationEntry
    {
        public string Text;
        public ToastKind Kind;
        public DateTime At;
        public Texture2D Icon;
    }

    /// <summary>
    /// Non-blocking notifications (GDD section 39): short-lived toasts, plus a bounded list of the
    /// relevant ones for the Notification Center. Nothing is persisted. Built from authoritative
    /// results the client already has; no extra request per notification.
    /// </summary>
    public sealed class ToastFeed
    {
        private const int MaxVisible = 5;

        private static Visual.VisualTheme Theme => Visual.VisualTheme.Current;
        private const int HistoryLimit = 50;
        private readonly List<Toast> _items = new List<Toast>();
        private readonly List<NotificationEntry> _history = new List<NotificationEntry>();

        public IReadOnlyList<Toast> Items => _items;

        /// <summary>Most recent first.</summary>
        public IReadOnlyList<NotificationEntry> History => _history;

        public int Unread { get; private set; }

        /// <summary>Raised for every toast shown, with the sound it should make (the audio listens to it).</summary>
        public event Action<SoundCue> Pushed;

        /// <summary>
        /// Shows a toast. <paramref name="notify"/> also keeps it in the Notification Center.
        /// <paramref name="sound"/> picks a specific sound; by default only a level up makes one
        /// (addendum A-080: sound is kept for level up, Excepcional and new species).
        /// </summary>
        public void Push(string text, ToastKind kind, Texture2D icon = null, bool notify = false, SoundCue? sound = null)
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
                Duration = kind == ToastKind.Warning ? Theme.ToastWarningSeconds
                    : kind == ToastKind.Important || kind == ToastKind.LevelUp ? Theme.ToastImportantSeconds : Theme.ToastSeconds,
                Icon = icon,
            });

            while (_items.Count > MaxVisible)
            {
                _items.RemoveAt(0);
            }

            if (notify)
            {
                _history.Insert(0, new NotificationEntry { Text = text, Kind = kind, At = DateTime.Now, Icon = icon });
                if (_history.Count > HistoryLimit)
                {
                    _history.RemoveRange(HistoryLimit, _history.Count - HistoryLimit);
                }

                Unread++;
            }

            Pushed?.Invoke(sound ?? DefaultSound(kind));
        }

        private static SoundCue DefaultSound(ToastKind kind)
        {
            // The owner found a sound on every notice tiring: the other kinds are silent.
            return kind == ToastKind.LevelUp ? SoundCue.LevelUp : SoundCue.None;
        }

        public void MarkAllRead() => Unread = 0;

        public void ClearHistory()
        {
            _history.Clear();
            Unread = 0;
        }

        /// <summary>Drops expired toasts. Called once per frame by the HUD.</summary>
        public void Prune()
        {
            _items.RemoveAll(t => Time.unscaledTime - t.CreatedAt > t.Duration);
        }
    }
}
