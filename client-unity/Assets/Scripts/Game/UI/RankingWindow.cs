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
        // The owner's art (Resources/Arte/UI, made by tools/Arte/processar_ranking.py).
        private const string StageArt = "UI/ui_rk_palco", CrownArt = "UI/ui_rk_coroa", TrophyArt = "UI/ui_rk_trofeu";

        private static readonly RankingCategory[] Categories =
        {
            RankingCategory.Level, RankingCategory.Coins, RankingCategory.Shells, RankingCategory.FishCaught,
        };

        private static readonly Color Silver = new Color(0.78f, 0.82f, 0.88f);
        private static readonly Color Bronze = new Color(0.80f, 0.52f, 0.30f);

        private const float TabHeight = 60f, StageHeight = 300f, RowHeight = 50f, FooterHeight = 58f, Gap = 12f;

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

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Ranking.Title, GameTexts.Ranking.Subtitle, out var closed, 1200f, 840f, Icons.Ranking);
            if (closed)
            {
                Close();
                return;
            }

            DrawTrophy(skin, area);
            DrawTabs(skin, new Rect(area.x, area.y, area.width, TabHeight));

            var view = _views[System.Array.IndexOf(Categories, _category)];
            if (view == null)
            {
                return;
            }

            var stage = new Rect(area.x, area.y + TabHeight + Gap, area.width, StageHeight);
            var footer = new Rect(area.x, area.yMax - FooterHeight, area.width, FooterHeight);
            var list = new Rect(area.x, stage.yMax + Gap, area.width, footer.y - Gap - stage.yMax - Gap);
            DrawStage(skin, stage, view);
            DrawList(skin, list, view);
            DrawFooter(skin, footer, view.Entries.Find(e => e.IsYou));
        }

        // ------------------------------------------------------------------ header and tabs

        /// <summary>The trophy beside the window title (decoration only; nothing when the file is missing).</summary>
        private static void DrawTrophy(UiSkin skin, Rect area)
        {
            var tex = ArtAssets.Texture(TrophyArt);
            if (tex == null)
            {
                return;
            }

            // Next to the header sign (A-148), in the strip WindowFrame leaves free.
            var side = WindowFrame.TakeSide(44f);
            if (side.width >= 44f)
            {
                GUI.DrawTexture(new Rect(side.x, side.center.y - 22f, 44f, 44f), tex, ScaleMode.ScaleToFit, true);
            }
        }

        private void DrawTabs(UiSkin skin, Rect rect)
        {
            const float gap = 10f;
            var w = (rect.width - gap * (Categories.Length - 1)) / Categories.Length;
            for (var i = 0; i < Categories.Length; i++)
            {
                var category = Categories[i];
                var active = _category == category;
                var tab = new Rect(rect.x + i * (w + gap), rect.y, w, rect.height);
                if (active)
                {
                    skin.DrawGlow(tab, UiSkin.Accent, 0.25f);
                }

                if (GUI.Button(tab, GUIContent.none, active ? skin.ChipActive : skin.Chip) && !active)
                {
                    _category = category;
                    _scroll = Vector2.zero;
                }

                DrawEmblem(skin, new Rect(tab.x + 12f, tab.center.y - 20f, 40f, 40f), category);
                var tx = tab.x + 62f;
                var tw = tab.xMax - 12f - tx;
                GUI.Label(new Rect(tx, tab.y + 8f, tw, 24f), FishCard.Fit(TabName(category), skin.BodyBold, tw), skin.BodyBold);
                var mine = _views[i]?.Entries.Find(e => e.IsYou);
                var line = mine != null ? GameTexts.Ranking.YouAt(mine.Position) : GameTexts.Ranking.NotListed;
                var lineStyle = active ? skin.SmallBold : skin.SmallMuted;
                GUI.Label(new Rect(tx, tab.y + 32f, tw, 20f), FishCard.Fit(line, lineStyle, tw), lineStyle);
            }
        }

        // ------------------------------------------------------------------ the stage: top 3 on the pier

        private void DrawStage(UiSkin skin, Rect stage, RankingView view)
        {
            GUI.DrawTexture(stage, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.45f), 0, 14);

            // The pier; without its art, a plain floor line.
            var pier = ArtAssets.Texture(StageArt);
            var pw = Mathf.Min(stage.width - 40f, 1000f);
            float floor;
            if (pier != null)
            {
                var ph = pw * pier.height / pier.width;
                var pr = new Rect(stage.center.x - pw / 2f, stage.yMax - ph - 4f, pw, ph);
                GUI.DrawTexture(pr, pier, ScaleMode.ScaleToFit, true);
                floor = pr.y + ph * DeckLine;
            }
            else
            {
                floor = stage.yMax - 40f;
                GUI.DrawTexture(new Rect(stage.center.x - pw / 2f, floor, pw, 3f), skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Border, 0, 1.5f);
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
                GUI.DrawTexture(pedestal, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.55f), 0, 10);
                skin.DashedFrame(pedestal, dim);
                GUI.DrawTexture(avatar, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.5f), 0, size / 2f);
                GUI.DrawTexture(avatar, skin.White, ScaleMode.StretchToFill, true, 0, dim, 1.5f, size / 2f);
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
            GUI.DrawTexture(pedestal, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.94f), 0, 10);
            GUI.DrawTexture(pedestal, skin.White, ScaleMode.StretchToFill, true, 0, new Color(color.r, color.g, color.b, 0.8f), 2f, 10);
            GUI.DrawTexture(new Rect(pedestal.x + 10f, pedestal.y + 2f, pedestal.width - 20f, 3f), skin.White, ScaleMode.StretchToFill, true, 0, color, 0, 1.5f);

            DrawAvatar(skin, avatar, entry, entry.IsYou ? UiSkin.Accent : color);
            DrawShield(skin, new Rect(avatar.xMax - 26f, avatar.yMax - 30f, 32f, 36f), rank);

            if (rank == 1)
            {
                var crown = ArtAssets.Texture(CrownArt);
                var crownRect = new Rect(cx - 26f, avatar.y - 36f, 52f, 52f);
                if (crown != null)
                {
                    GUI.DrawTexture(crownRect, crown, ScaleMode.ScaleToFit, true);
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
                GUI.DrawTexture(row, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.18f), 0, 10);
                GUI.DrawTexture(row, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 1.5f, 10);
            }
            else
            {
                var fill = odd ? new Color(1f, 1f, 1f, 0.06f) : new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.45f);
                GUI.DrawTexture(row, skin.White, ScaleMode.StretchToFill, true, 0, fill, 0, 10);
            }

            DrawShield(skin, new Rect(row.x + 10f, row.center.y - 20f, 36f, 40f), entry.Position);
            DrawAvatar(skin, new Rect(row.x + 56f, row.center.y - 18f, 36f, 36f), entry, entry.IsYou ? UiSkin.Accent : UiSkin.Border);

            var nameX = row.x + 104f;
            var nameW = row.xMax - 340f - nameX;
            var name = entry.IsYou ? entry.PlayerName + "  ·  " + GameTexts.Ranking.You : entry.PlayerName;
            GUI.Label(new Rect(nameX, row.center.y - 12f, nameW, 24f), FishCard.Fit(name, skin.BodyBold, nameW), skin.BodyBold);
            GUI.Label(new Rect(row.xMax - 330f, row.center.y - 10f, 90f, 20f), GameTexts.Player.LevelShort + " " + entry.FisherLevel, skin.SmallMuted);
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
            GUI.DrawTexture(footer, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.16f), 0, 14);
            GUI.DrawTexture(footer, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.7f), 1.5f, 14);

            DrawShield(skin, new Rect(footer.x + 14f, footer.center.y - 21f, 38f, 42f), you != null ? you.Position : 0);
            var avatar = new Rect(footer.x + 62f, footer.center.y - 20f, 40f, 40f);
            GUI.DrawTexture(avatar, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Night, 0, 20f);
            AvatarArt.Draw(skin, new Rect(avatar.x + 3f, avatar.y + 3f, 34f, 34f), _avatarId, 17f);
            GUI.DrawTexture(avatar, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 2f, 20f);

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
            var r = rect.width / 2f;
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Night, 0, r);
            AvatarArt.Draw(skin, new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), entry.IsYou ? _avatarId : null, r - 3f);
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, ring, 2.5f, r);
        }

        /// <summary>The position shield (gold, silver, bronze, then blue) with the number written over it; 0 = "—".</summary>
        private void DrawShield(UiSkin skin, Rect rect, int position)
        {
            var art = position == 1 ? "UI/ui_rk_escudo_ouro" : position == 2 ? "UI/ui_rk_escudo_prata" : position == 3 ? "UI/ui_rk_escudo_bronze" : "UI/ui_rk_escudo_azul";
            var tex = ArtAssets.Texture(art);
            if (tex != null)
            {
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true);
            }
            else
            {
                var fill = position >= 1 && position <= 3 ? MedalColor(position) : new Color(0.16f, 0.32f, 0.62f);
                GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, fill, 0, rect.width * 0.25f);
            }

            if (_shieldDark == null)
            {
                _shieldDark = new GUIStyle(skin.MedalText);
                _shieldLight = new GUIStyle(skin.MedalText);
                _shieldLight.normal.textColor = Color.white;
            }

            var text = position > 0 ? Format.Number(position) : GameTexts.Ranking.VacantMark;
            var style = position == 1 || position == 2 ? _shieldDark : _shieldLight;
            var fontSize = Mathf.RoundToInt(Mathf.Clamp(rect.height * 0.42f, 12f, 20f));
            style.fontSize = text.Length > 2 ? fontSize - 3 : fontSize;
            GUI.Label(new Rect(rect.x, rect.y - rect.height * 0.06f, rect.width, rect.height), text, style);
        }

        /// <summary>The category emblem (owner's art) or, without it, the plain icon.</summary>
        private static void DrawEmblem(UiSkin skin, Rect rect, RankingCategory category)
        {
            var tex = ArtAssets.Texture(EmblemArt(category));
            if (tex != null)
            {
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true);
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
            var text = FishCard.Fit(ValueText(value), skin.SmallBold, maxWidth - 26f);
            var w = skin.SmallBold.CalcSize(new GUIContent(text)).x;
            var x = cx - (w + 26f) / 2f;
            DrawEmblem(skin, new Rect(x, y, 20f, 20f), _category);
            GUI.Label(new Rect(x + 26f, y + 1f, w + 4f, 20f), text, skin.SmallBold);
        }

        private static Color MedalColor(int rank) => rank == 1 ? UiSkin.Gold : rank == 2 ? Silver : Bronze;

        private static string EmblemArt(RankingCategory category)
        {
            switch (category)
            {
                case RankingCategory.Coins: return "UI/ui_rk_emblema_moedas";
                case RankingCategory.Shells: return "UI/ui_rk_emblema_conchas";
                case RankingCategory.FishCaught: return "UI/ui_rk_emblema_peixes";
                default: return "UI/ui_rk_emblema_nivel";
            }
        }

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
