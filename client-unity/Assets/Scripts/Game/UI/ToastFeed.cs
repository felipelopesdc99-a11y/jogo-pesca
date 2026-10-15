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
    /// relevant ones for the Notification Center. The important ones (A-127) are kept between sessions,
    /// on this PC, in the client's own preferences (never in the save). Built from authoritative
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

        // A-127: the important notices survive closing the game (the 20 most recent, without icons).
        private const string PrefsKey = "fishingidle.avisos";
        private const int KeptLimit = 20;
        private bool _loaded;

        [Serializable]
        private sealed class Kept
        {
            public List<KeptEntry> entries = new List<KeptEntry>();
        }

        [Serializable]
        private sealed class KeptEntry
        {
            public string text;
            public int kind;
            public long at;
        }

        private static bool IsImportant(ToastKind kind) => kind == ToastKind.Important || kind == ToastKind.LevelUp;

        /// <summary>Brings back the important notices of earlier sessions. Called once the game has started.</summary>
        public void LoadKept()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            try
            {
                var json = PlayerPrefs.GetString(PrefsKey, string.Empty);
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }

                var kept = JsonUtility.FromJson<Kept>(json);
                foreach (var e in kept?.entries ?? new List<KeptEntry>())
                {
                    if (!string.IsNullOrEmpty(e.text))
                    {
                        _history.Add(new NotificationEntry { Text = e.text, Kind = (ToastKind)e.kind, At = DateTime.FromBinary(e.at) });
                    }
                }
            }
            catch (Exception)
            {
                // A damaged preference only loses old notices; the game goes on.
                PlayerPrefs.DeleteKey(PrefsKey);
            }
        }

        private void SaveKept()
        {
            if (!_loaded)
            {
                return;
            }

            var kept = new Kept();
            foreach (var e in _history)
            {
                if (IsImportant(e.Kind) && kept.entries.Count < KeptLimit)
                {
                    kept.entries.Add(new KeptEntry { text = e.Text, kind = (int)e.Kind, at = e.At.ToBinary() });
                }
            }

            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(kept));
        }

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
                if (IsImportant(kind))
                {
                    SaveKept();
                }
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
            SaveKept();
        }

        /// <summary>Drops expired toasts. Called once per frame by the HUD.</summary>
        public void Prune()
        {
            _items.RemoveAll(t => Time.unscaledTime - t.CreatedAt > t.Duration);
        }
    }
}
