using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Ranking;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Ranking menu (addendum A-100): Nível, Moedas, Conchas and Peixes pescados, real players only.
    /// It shows what the ranking service returns; in the local MVP that is just the player on this PC.
    /// </summary>
    /// <remarks>
    /// Look (addendum A-146, the owner's art, a mix of "Palco" and "Placar de campeonato"): big tabs with the
    /// category emblem and "Você #N", a pier stage with the top 3 on pedestals (crown on the 1st), the 4th
    /// onwards as a scoreboard with numbered shields, and a fixed "Sua posição" bar. No up/down arrows (owner's
    /// decision). Every art file falls back to a plain drawing when it is missing.
    /// </remarks>
    public sealed class RankingWindow
    {
        // The owner's art, in pixel since M25-T15 (ArtePixel/UI; the shields are ui_rk_escudo_*). The category emblems
        // are the coloured pixel icons (moeda, concha, peixe, nível): the painted ui_rk_emblema_* are no longer read.
        private const string StageArt = "UI/ui_rk_palco", CrownArt = "UI/ui_rk_coroa";

        /// <summary>The tallest the pixel pier may be drawn (canvas px): its whole scale is the largest that fits it.</summary>
        private const float PierMaxHeight = 160f;

        /// <summary>The middle of the pixel pier's deck (fraction of its height, from the top): the pedestals stand there.</summary>
        private const float PixelDeckLine = 0.5f;

        private static readonly RankingCategory[] Categories =
        {
            RankingCategory.Level, RankingCategory.Coins, RankingCategory.Shells, RankingCategory.FishCaught,
        };

        private static readonly Color Silver = new Color(0.78f, 0.82f, 0.88f);
        private static readonly Color Bronze = new Color(0.80f, 0.52f, 0.30f);

        private const float StageHeight = 300f, RowHeight = 50f, FooterHeight = 58f, Gap = 12f;

        /// <summary>Where the pedestals stand on the pier art (fraction of its height, from the top).</summary>
        private const float DeckLine = 0.6f;

        private readonly GameRoot _root;
        private readonly RankingView[] _views = new RankingView[4];
        private RankingCategory _category = RankingCategory.Level;
        private string _avatarId;
        private float _nextRefresh;
        private float _openedAt;
        private Vector2 _scroll;
        private GUIStyle _shieldDark, _shieldLight;

        public RankingWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            _nextRefresh = 0f;
            _openedAt = Time.unscaledTime;
        }

        public void Close() => IsOpen = false;

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen)
            {
                return;
            }

            // The four lists (each tab shows the player's position in it) and the player's own avatar.
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 1f;
                for (var i = 0; i < Categories.Length; i++)
                {
                    _views[i] = _root.GetRanking(Categories[i]);
                }

                _avatarId = _root.GetProfile()?.AvatarId;
            }

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Ranking.Title, GameTexts.Ranking.Subtitle, out var closed, 1200f, 840f, Icons.Ranking, bookmarks: true);
            if (closed)
            {
                Close();
                return;
            }

            // The trophy beside the title left in A-190 (the window icon already says Ranking).
            DrawBookmarks(skin);

            var view = _views[System.Array.IndexOf(Categories, _category)];
            if (view == null)
            {
                return;
            }

            var stage = new Rect(area.x, area.y, area.width, StageHeight);
            // "Sua posição" is the sheet's footer, under a dashed line (A-170).
            var footer = WindowFrame.Footer(skin, area, FooterHeight);
            var list = new Rect(area.x, stage.yMax + Gap, area.width, footer.y - Gap - stage.yMax - Gap);
            DrawStage(skin, stage, view);
            DrawList(skin, list, view);
            DrawFooter(skin, footer, view.Entries.Find(e => e.IsYou));
        }

        // ------------------------------------------------------------------ header and tabs

        /// <summary>The four lists as bookmarks on the side of the diary (A-170), each with "Você #N".</summary>
        private void DrawBookmarks(UiSkin skin)
        {
            var labels = new string[Categories.Length];
            var notes = new string[Categories.Length];
            var icons = new string[Categories.Length];
            var active = 0;
            for (var i = 0; i < Categories.Length; i++)
            {
                labels[i] = TabName(Categories[i]);
                var mine = _views[i]?.Entries.Find(e => e.IsYou);
                notes[i] = mine != null ? GameTexts.Ranking.YouAt(mine.Position) : GameTexts.Ranking.NotListed;
                icons[i] = TabIcon(Categories[i]);
                if (Categories[i] == _category)
                {
                    active = i;
                }
            }

            var clicked = WindowFrame.Bookmarks(skin, labels, icons, active, notes);
            if (clicked >= 0)
            {
                _category = Categories[clicked];
                _scroll = Vector2.zero;
            }
        }

        // ------------------------------------------------------------------ the stage: top 3 on the pier

        private void DrawStage(UiSkin skin, Rect stage, RankingView view)
        {
            // The pier on a sunken card, with no frame of its own (A-190: at most two levels of box).
            skin.ArcFill(stage, UiSkin.CardColor, 0f);

            // The pier; without its art, a plain floor line.
            var pier = ArtAssets.Texture(StageArt);
            var pw = Mathf.Min(stage.width - 40f, 1000f);
            float floor;
            if (pier != null && ArtAssets.IsPixel(pier))
            {
                // Pixel pier (M25-T15): the largest whole scale under PierMaxHeight that fits the width, on screen pixels.
                var unit = PixelDraw.ScreenScale();
                var pierScale = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min((stage.width - 40f) * unit / pier.width, PierMaxHeight * unit / pier.height) + 0.001f));
                var drawnW = pier.width * pierScale / unit;
                var drawnH = pier.height * pierScale / unit;
                var pr = new Rect(Mathf.Round((stage.center.x - drawnW / 2f) * unit) / unit, Mathf.Round((stage.yMax - drawnH - 4f) * unit) / unit, drawnW, drawnH);
                GUI.DrawTexture(pr, pier, ScaleMode.StretchToFill, true);
                pw = drawnW;
                floor = pr.y + drawnH * PixelDeckLine;
            }
            else if (pier != null)
            {
                var ph = pw * pier.height / pier.width;
                var pr = new Rect(stage.center.x - pw / 2f, stage.yMax - ph - 4f, pw, ph);
                PixelDraw.Fit(pr, pier);
                floor = pr.y + ph * DeckLine;
            }
            else
            {
                floor = stage.yMax - 40f;
                skin.ArcFill(new Rect(stage.center.x - pw / 2f, floor, pw, UiSkin.ArcPixel), UiSkin.Border, 0f);
            }

            // Opening: the three rise and fade in once (presentation only).
            var t = Mathf.Clamp01((Time.unscaledTime - _openedAt) / 0.45f);
            var ease = 1f - (1f - t) * (1f - t) * (1f - t);
            var previous = GUI.color;
            GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * ease);

            const float gap = 28f;
            var slotW = Mathf.Min(220f, (pw - gap * 2f) / 3f);
            var x0 = stage.center.x - (slotW * 3f + gap * 2f) / 2f;
            var order = new[] { 2, 1, 3 };
            for (var k = 0; k < order.Length; k++)
            {
                var rank = order[k];
                DrawPodiumSlot(skin, x0 + k * (slotW + gap), slotW, floor + (1f - ease) * 24f, rank, view.Entries.Find(e => e.Position == rank));
            }

            GUI.color = previous;
        }

        /// <summary>One pedestal: avatar with its shield (and the crown on the 1st), name and value on the front.</summary>
        private void DrawPodiumSlot(UiSkin skin, float x, float w, float floor, int rank, RankingEntryView entry)
        {
            var color = MedalColor(rank);
            var cx = x + w / 2f;
            var pedestalH = rank == 1 ? 84f : rank == 2 ? 68f : 58f;
            var size = rank == 1 ? 84f : 68f;
            var pedestal = new Rect(x, floor - pedestalH, w, pedestalH);
            var avatar = new Rect(cx - size / 2f, pedestal.y - 8f - size, size, size);

            if (entry == null)
            {
                // Vacant place: a discreet outline, "—" and "Vago".
                var dim = new Color(UiSkin.Muted.r, UiSkin.Muted.g, UiSkin.Muted.b, 0.5f);
                skin.ArcFill(pedestal, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.55f), 10);
                skin.DashedFrame(pedestal, dim, 6f, 4f, UiSkin.ArcPixel);
                skin.LineBox(avatar, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.5f), dim, 1);
                GUI.Label(avatar, GameTexts.Ranking.VacantMark, skin.SmallMutedCenter);
                var top = pedestal.y + (pedestalH - 40f) / 2f;
                var content = GUI.contentColor;
                GUI.contentColor = new Color(color.r, color.g, color.b, 0.7f);
                GUI.Label(new Rect(pedestal.x, top, w, 20f), GameTexts.Ranking.PlaceShort(rank), skin.CenterBold);
                GUI.contentColor = content;
                GUI.Label(new Rect(pedestal.x, top + 20f, w, 20f), GameTexts.Ranking.Vacant, skin.SmallMutedCenter);
                return;
            }

            // Spotlight behind the 1st.
            if (rank == 1)
            {
                skin.DrawGlow(avatar, UiSkin.Gold, 0.45f);
            }

            // Pedestal: dark block with a medal-coloured rim.
            skin.ArcFill(pedestal, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.94f), 10);
            skin.ArcLine(pedestal, new Color(color.r, color.g, color.b, 0.8f), 10);
            skin.ArcFill(new Rect(pedestal.x + 10f, pedestal.y + UiSkin.ArcPixel, pedestal.width - 20f, UiSkin.ArcPixel), color, 0f);

            DrawAvatar(skin, avatar, entry, entry.IsYou ? UiSkin.Accent : color);
            DrawShield(skin, new Rect(avatar.xMax - 30f, avatar.yMax - 34f, 50f, 46f), rank);

            if (rank == 1)
            {
                var crown = ArtAssets.Texture(CrownArt);
                var crownRect = new Rect(cx - 26f, avatar.y - 36f, 52f, 52f);
                if (crown != null)
                {
                    PixelDraw.Fit(crownRect, crown);
                }
                else
                {
                    skin.DrawIcon(new Rect(cx - 14f, avatar.y - 30f, 28f, 28f), Icons.Star, UiSkin.Gold);
                }
            }

            // Name and value on the front of the pedestal.
            var faceTop = pedestal.y + (pedestalH - 46f) / 2f + 2f;
            var name = entry.IsYou ? entry.PlayerName + "  ·  " + GameTexts.Ranking.You : entry.PlayerName;
            GUI.Label(new Rect(pedestal.x + 8f, faceTop, w - 16f, 24f), FishCard.Fit(name, skin.CenterBold, w - 16f), skin.CenterBold);
            DrawValueCentred(skin, cx, faceTop + 24f, w - 16f, entry.Value);
        }

        // ------------------------------------------------------------------ the scoreboard: 4th onwards

        private void DrawList(UiSkin skin, Rect list, RankingView view)
        {
            var below = 0;
            foreach (var entry in view.Entries)
            {
                if (entry.Position > 3)
                {
                    below++;
                }
            }

            // Nobody past the podium (the local game today): the note takes the list's place.
            if (below == 0)
            {
                GUI.Box(list, GUIContent.none, skin.Card);
                if (view.LocalOnly)
                {
                    DrawNote(skin, new Rect(list.x + 24f, list.y, list.width - 48f, list.height));
                }

                return;
            }

            var noteH = view.LocalOnly ? 48f : 0f;
            var box = new Rect(list.x, list.y, list.width, list.height - noteH);
            var scrolls = below * RowHeight > box.height;
            var content = new Rect(0, 0, box.width - (scrolls ? 20f : 0f), below * RowHeight);
            _scroll = GUI.BeginScrollView(box, _scroll, content);
            var i = 0;
            foreach (var entry in view.Entries)
            {
                if (entry.Position <= 3)
                {
                    continue;
                }

                DrawRow(skin, new Rect(0, i * RowHeight, content.width, RowHeight - 4f), entry, i % 2 == 1);
                i++;
            }

            GUI.EndScrollView();

            if (noteH > 0f)
            {
                DrawNote(skin, new Rect(list.x, box.yMax + 4f, list.width, noteH - 4f));
            }
        }

        private void DrawRow(UiSkin skin, Rect row, RankingEntryView entry, bool odd)
        {
            if (entry.IsYou)
            {
                skin.ArcFill(row, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.18f), 10);
                skin.ArcLine(row, UiSkin.Accent, 10);
            }
            else
            {
                // On the ARC panel (M25-T15): the card blue and the field, alternating.
                var fill = odd ? new Color(UiSkin.CardColor.r, UiSkin.CardColor.g, UiSkin.CardColor.b, 0.9f) : new Color(UiSkin.FieldColor.r, UiSkin.FieldColor.g, UiSkin.FieldColor.b, 0.6f);
                skin.ArcFill(row, fill, 10);
            }

            // The pixel shield at 1× (50 × 46).
            DrawShield(skin, new Rect(row.x + 8f, row.center.y - 23f, 50f, 46f), entry.Position);
            DrawAvatar(skin, new Rect(row.x + 66f, row.center.y - 20f, 40f, 40f), entry, entry.IsYou ? UiSkin.Accent : UiSkin.Border);

            var nameX = row.x + 114f;
            var nameW = row.xMax - 340f - nameX;
            var name = entry.IsYou ? entry.PlayerName + "  ·  " + GameTexts.Ranking.You : entry.PlayerName;
            GUI.Label(new Rect(nameX, row.center.y - 12f, nameW, 24f), FishCard.Fit(name, skin.BodyBold, nameW), skin.BodyBold);
            if (_category != RankingCategory.Level)
            {
                // In the Nível list the value on the right is the level already (A-190: never twice on a line).
                GUI.Label(new Rect(row.xMax - 330f, row.center.y - 10f, 90f, 20f), GameTexts.Player.LevelShort + " " + entry.FisherLevel, skin.SmallMuted);
            }
            DrawValueRight(skin, new Rect(row.xMax - 230f, row.center.y - 13f, 214f, 26f), entry.Value);
        }

        private static void DrawNote(UiSkin skin, Rect rect)
        {
            var content = new GUIContent(GameTexts.Ranking.LocalNote);
            var textW = rect.width - 30f;
            var h = Mathf.Min(rect.height, skin.SmallMuted.CalcHeight(content, textW));
            var y = rect.center.y - h / 2f;
            skin.DrawIcon(new Rect(rect.x, y, 18f, 18f), Icons.Info, UiSkin.Muted);
            GUI.Label(new Rect(rect.x + 30f, y, textW, h), content, skin.SmallMuted);
        }

        // ------------------------------------------------------------------ "Sua posição" bar

        private void DrawFooter(UiSkin skin, Rect footer, RankingEntryView you)
        {
            skin.ArcFill(footer, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.16f), 14);
            skin.ArcLine(footer, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.7f), 14);

            DrawShield(skin, new Rect(footer.x + 10f, footer.center.y - 23f, 50f, 46f), you != null ? you.Position : 0);
            var avatar = new Rect(footer.x + 68f, footer.center.y - 20f, 40f, 40f);
            AvatarArt.Draw(skin, avatar, _avatarId, 0f);

            var textX = avatar.xMax + 14f;
            var textW = footer.xMax - 260f - textX;
            var label = you != null ? GameTexts.Ranking.YourPosition(you.Position) : GameTexts.Ranking.YourPositionNotListed;
            GUI.Label(new Rect(textX, footer.center.y - 13f, textW, 28f), FishCard.Fit(label, skin.Heading, textW), skin.Heading);

            if (you != null)
            {
                GUI.Label(new Rect(footer.xMax - 240f, footer.y + 5f, 224f, UiSkin.SmallLine), TabName(_category), skin.SmallMutedRightLine);
                DrawValueRight(skin, new Rect(footer.xMax - 240f, footer.y + 24f, 224f, 26f), you.Value);
            }
        }

        // ------------------------------------------------------------------ pieces

        /// <summary>A round avatar with a coloured ring: the player's own avatar, the default portrait for others.</summary>
        private void DrawAvatar(UiSkin skin, Rect rect, RankingEntryView entry, Color ring)
        {
            // The pixel portrait in a square with a one-pixel line (A-190: the smooth circles read as empty rings).
            AvatarArt.Draw(skin, rect, entry.IsYou ? _avatarId : null, 0f);
            skin.LineBox(rect, Color.clear, ring, 1);
        }

        /// <summary>The position shield (gold, silver, bronze, then blue) with the number written over it; 0 = "—".</summary>
        private void DrawShield(UiSkin skin, Rect rect, int position)
        {
            var art = position == 1 ? "UI/ui_rk_escudo_ouro" : position == 2 ? "UI/ui_rk_escudo_prata" : position == 3 ? "UI/ui_rk_escudo_bronze" : "UI/ui_rk_escudo_azul";
            var tex = ArtAssets.Texture(art);
            if (tex != null)
            {
                PixelDraw.Fit(rect, tex);
            }
            else
            {
                var fill = position >= 1 && position <= 3 ? MedalColor(position) : new Color(0.16f, 0.32f, 0.62f);
                skin.ArcFill(rect, fill, rect.width * 0.25f);
            }

            if (_shieldDark == null)
            {
                _shieldDark = new GUIStyle(skin.MedalText);
                _shieldLight = new GUIStyle(skin.MedalText);
                _shieldLight.normal.textColor = Color.white;
            }

            var text = position > 0 ? Format.Number(position) : GameTexts.Ranking.VacantMark;
            var style = position == 1 || position == 2 ? _shieldDark : _shieldLight;
            var fontSize = Mathf.RoundToInt(Mathf.Clamp(rect.height * 0.58f, UiSkin.ChipSize, UiSkin.BodySize));
            // Never under the small size (A-191): a three-digit place keeps the minimum instead of shrinking under it.
            style.fontSize = text.Length > 2 ? Mathf.Max(UiSkin.ChipSize, fontSize - 4) : fontSize;
            GUI.Label(new Rect(rect.x, rect.y - rect.height * 0.06f, rect.width, rect.height), text, style);
        }

        /// <summary>The category emblem (owner's art) or, without it, the plain icon.</summary>
        private static void DrawEmblem(UiSkin skin, Rect rect, RankingCategory category)
        {
            // The coloured pixel icon of the category (M25-T15; the painted emblems left in M25-T17).
            if (ArtAssets.Icon(TabIcon(category)) != null)
            {
                skin.DrawIconExact(rect, TabIcon(category), Color.white);
                return;
            }

            var pad = rect.width * 0.12f;
            var inner = new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2f, rect.height - pad * 2f);
            switch (category)
            {
                case RankingCategory.Coins: skin.DrawIcon(inner, Icons.Coin, Color.white); break;
                case RankingCategory.Shells: skin.DrawIcon(inner, Icons.Shell, Color.white); break;
                case RankingCategory.FishCaught: skin.DrawIcon(inner, Icons.Fish, UiSkin.Accent); break;
                default: skin.DrawIcon(inner, Icons.Level, UiSkin.Gold); break;
            }
        }

        private static string TabIcon(RankingCategory category)
        {
            switch (category)
            {
                case RankingCategory.Coins: return Icons.Coin;
                case RankingCategory.Shells: return Icons.Shell;
                case RankingCategory.FishCaught: return Icons.Fish;
                default: return Icons.Level;
            }
        }

        private string ValueText(long value) => _category == RankingCategory.Level ? GameTexts.Player.LevelShort + " " + value : Format.Number(value);

        /// <summary>The value right-aligned in <paramref name="rect"/>, the small category emblem before it.</summary>
        private void DrawValueRight(UiSkin skin, Rect rect, long value)
        {
            var text = ValueText(value);
            GUI.Label(rect, text, skin.NumberRight);
            var w = skin.NumberRight.CalcSize(new GUIContent(text)).x;
            DrawEmblem(skin, new Rect(rect.xMax - w - 30f, rect.y + 1f, 24f, 24f), _category);
        }

        /// <summary>Emblem and value centred on <paramref name="cx"/>, on one line.</summary>
        private void DrawValueCentred(UiSkin skin, float cx, float y, float maxWidth, long value)
        {
            var text = FishCard.Fit(ValueText(value), skin.SmallBold, maxWidth - 30f);
            var w = skin.SmallBold.CalcSize(new GUIContent(text)).x;
            var x = cx - (w + 30f) / 2f;
            DrawEmblem(skin, new Rect(x, y - 2f, 24f, 24f), _category);
            GUI.Label(new Rect(x + 30f, y + 1f, w + 4f, 20f), text, skin.SmallBold);
        }

        private static Color MedalColor(int rank) => rank == 1 ? UiSkin.Gold : rank == 2 ? Silver : Bronze;

        private static string TabName(RankingCategory category)
        {
            switch (category)
            {
                case RankingCategory.Coins: return GameTexts.Ranking.TabCoins;
                case RankingCategory.Shells: return GameTexts.Ranking.TabShells;
                case RankingCategory.FishCaught: return GameTexts.Ranking.TabFish;
                default: return GameTexts.Ranking.TabLevel;
            }
        }
    }
}
