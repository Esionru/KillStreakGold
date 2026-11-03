using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace KillStreakGold
{
    /// <summary>
    /// Kill streaks, kill counters by monster rarity (normal / magic / rare / unique)
    /// and gold gained per area / per session.
    /// </summary>
    public class KillStreakGold : BaseSettingsPlugin<KillStreakGoldSettings>
    {
        private enum RarityBucket
        {
            Normal,
            Magic,
            Rare,
            Unique
        }

        private sealed class AreaRecord
        {
            public string Name;
            public int TotalKills;
            public long Gold;
            public int BestStreak;
            public TimeSpan Elapsed;
        }

        // --- kill detection state ---
        private readonly HashSet<long> _seenAlive = new();    // monsters we have seen alive in this area
        private readonly Dictionary<RarityBucket, int> _areaKills = new();
        private readonly Dictionary<RarityBucket, int> _sessionKills = new();

        // --- streak state ---
        private int _currentStreak;
        private int _bestStreakSession;
        private int _bestStreakArea;
        private DateTime _lastKillTime = DateTime.MinValue;

        // --- gold state ---
        private long _previousGold = -1;
        private long _areaGold;
        private long _sessionGold;

        // --- time / area state ---
        private TimeSpan _sessionElapsed;
        private TimeSpan _areaElapsed;
        private string _currentAreaName;
        private readonly List<AreaRecord> _areaHistory = new();

        public override bool Initialise()
        {
            Name = "Kill Streak Gold";
            Settings.Session.ResetSessionButton.OnPressed = ResetSession;
            ResetRarityCounts(_areaKills);
            ResetRarityCounts(_sessionKills);
            return true;
        }

        public override Job Tick()
        {
            if (!GameController.InGame)
                return null;

            var area = GameController.Area.CurrentArea;
            if (area != null && !area.IsPeaceful)
            {
                var delta = TimeSpan.FromMilliseconds(GameController.DeltaTime);
                _sessionElapsed += delta;
                _areaElapsed += delta;
                TrackKills();
                TrackGold();
            }

            ExpireStreak();
            return null;
        }

        public override void AreaChange(AreaInstance area)
        {
            FinalizeArea();

            _currentAreaName = area?.Name;
            _seenAlive.Clear();
            ResetRarityCounts(_areaKills);
            _areaGold = 0;
            _bestStreakArea = 0;
            _currentStreak = 0;
            _lastKillTime = DateTime.MinValue;
            _previousGold = -1;
            _areaElapsed = TimeSpan.Zero;
        }

        #region Logic

        private static void ResetRarityCounts(Dictionary<RarityBucket, int> dict)
        {
            dict[RarityBucket.Normal] = 0;
            dict[RarityBucket.Magic] = 0;
            dict[RarityBucket.Rare] = 0;
            dict[RarityBucket.Unique] = 0;
        }

        private void TrackKills()
        {
            IEnumerable<Entity> monsters;
            try
            {
                monsters = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster];
            }
            catch
            {
                return;
            }

            if (monsters == null)
                return;

            foreach (var entity in monsters)
            {
                try
                {
                    if (entity == null || !entity.IsHostile)
                        continue;

                    var address = entity.Address;
                    if (entity.IsAlive)
                    {
                        // Only remember monsters that are actually rendered (hidden/unspawned ones are skipped)
                        if (entity.HasComponent<Render>())
                            _seenAlive.Add(address);
                        continue;
                    }

                    // Was alive before and is dead now -> a kill
                    if (_seenAlive.Remove(address))
                        CountKill(entity.Rarity);
                }
                catch
                {
                    // entity memory may become invalid mid-iteration; skip it
                }
            }
        }

        private void CountKill(MonsterRarity rarity)
        {
            var bucket = rarity switch
            {
                MonsterRarity.White => RarityBucket.Normal,
                MonsterRarity.Magic => RarityBucket.Magic,
                MonsterRarity.Rare => RarityBucket.Rare,
                MonsterRarity.Unique => RarityBucket.Unique,
                _ => (RarityBucket?)null
            };
            if (bucket == null)
                return;

            _areaKills[bucket.Value]++;
            _sessionKills[bucket.Value]++;

            var now = DateTime.UtcNow;
            var window = Settings.Streak.WindowSeconds.Value;
            var continuing = _currentStreak > 0 && (now - _lastKillTime).TotalSeconds <= window;
            _currentStreak = continuing ? _currentStreak + 1 : 1;
            _lastKillTime = now;

            if (_currentStreak > _bestStreakSession) _bestStreakSession = _currentStreak;
            if (_currentStreak > _bestStreakArea) _bestStreakArea = _currentStreak;

            if (bucket == RarityBucket.Unique && Settings.Streak.NotifyOnUnique.Value)
                LogMessage($"Unique monster slain! Kill streak: {_currentStreak}", 3);
        }

        private void ExpireStreak()
        {
            if (_currentStreak > 0 &&
                (DateTime.UtcNow - _lastKillTime).TotalSeconds > Settings.Streak.WindowSeconds.Value)
            {
                _currentStreak = 0;
            }
        }

        private void TrackGold()
        {
            long gold;
            try
            {
                gold = GameController.IngameState.ServerData.Gold;
            }
            catch
            {
                return;
            }

            if (_previousGold < 0)
            {
                _previousGold = gold;
                return;
            }

            if (gold > _previousGold)
            {
                var gained = gold - _previousGold;
                _areaGold += gained;
                _sessionGold += gained;
            }

            // follow the value down too, so spending gold doesn't register as gains later
            _previousGold = gold;
        }

        private void FinalizeArea()
        {
            var totalKills = _areaKills.Values.Sum();
            if (string.IsNullOrEmpty(_currentAreaName) || (totalKills == 0 && _areaGold == 0))
                return;

            _areaHistory.Insert(0, new AreaRecord
            {
                Name = _currentAreaName,
                TotalKills = totalKills,
                Gold = _areaGold,
                BestStreak = _bestStreakArea,
                Elapsed = _areaElapsed
            });

            while (_areaHistory.Count > Settings.History.MaxEntries.Value)
                _areaHistory.RemoveAt(_areaHistory.Count - 1);
        }

        private void ResetSession()
        {
            ResetRarityCounts(_sessionKills);
            _sessionGold = 0;
            _sessionElapsed = TimeSpan.Zero;
            _bestStreakSession = 0;
            _areaHistory.Clear();
            LogMessage("Kill Streak Gold: session stats reset.", 3);
        }

        #endregion

        #region Render

        public override void Render()
        {
            if (!Settings.Enable.Value || !GameController.InGame)
                return;

            if (Settings.Panel.HideWhenGameUiOpen.Value && IsAnyGameUIVisible())
                return;

            var area = GameController.Area.CurrentArea;
            if (Settings.Panel.OnlyInCombatAreas.Value && area.IsPeaceful)
                return;

            var pos = new Vector2(Settings.Panel.X.Value, Settings.Panel.Y.Value);
            DrawPanel(pos);
        }

        private void DrawPanel(Vector2 pos)
        {
            const int padding = 10;
            const int sectionSpacing = 8;
            const float barHeight = 6f;
            const float barSpacing = 4f;

            // ---- build all lines first, so the panel width is known ----
            var lines = new List<(string text, ColorNode color, bool header)> { };

            float streakFraction = 0f;
            bool showBar = false;

            // streak section
            lines.Add(("STREAK", Settings.Style.HeaderColor, true));
            var streakText = _currentStreak > 0
                ? $"Current: {_currentStreak}"
                : "Current: -";
            lines.Add((streakText, Settings.Style.TextColor, false));
            lines.Add(($"Best (session / area): {_bestStreakSession} / {_bestStreakArea}", Settings.Style.TextColor, false));
            if (Settings.Streak.ShowTimerBar.Value)
            {
                showBar = true;
                var window = Settings.Streak.WindowSeconds.Value;
                var remaining = Math.Max(0f, window - (float)(DateTime.UtcNow - _lastKillTime).TotalSeconds);
                streakFraction = _currentStreak > 0 ? remaining / window : 0f;
            }

            // area kills section
            var areaTotal = _areaKills.Values.Sum();
            if (Settings.Kills.ShowAreaKills.Value)
            {
                lines.Add(($"KILLS (AREA) - {areaTotal}", Settings.Style.HeaderColor, true));
                lines.Add(($"  Normal:  {_areaKills[RarityBucket.Normal]}", Settings.Kills.NormalColor, false));
                lines.Add(($"  Magic:   {_areaKills[RarityBucket.Magic]}", Settings.Kills.MagicColor, false));
                lines.Add(($"  Rare:    {_areaKills[RarityBucket.Rare]}", Settings.Kills.RareColor, false));
                lines.Add(($"  Unique:  {_areaKills[RarityBucket.Unique]}", Settings.Kills.UniqueColor, false));
            }

            // session kills section
            if (Settings.Kills.ShowSessionKills.Value)
            {
                var sessionTotal = _sessionKills.Values.Sum();
                lines.Add(($"KILLS (SESSION) - {sessionTotal}", Settings.Style.HeaderColor, true));
                lines.Add(($"  Normal:  {_sessionKills[RarityBucket.Normal]}", Settings.Kills.NormalColor, false));
                lines.Add(($"  Magic:   {_sessionKills[RarityBucket.Magic]}", Settings.Kills.MagicColor, false));
                lines.Add(($"  Rare:    {_sessionKills[RarityBucket.Rare]}", Settings.Kills.RareColor, false));
                lines.Add(($"  Unique:  {_sessionKills[RarityBucket.Unique]}", Settings.Kills.UniqueColor, false));
            }

            // gold section
            if (Settings.Gold.ShowGold.Value)
            {
                var sessionRate = _sessionElapsed.TotalHours > 0 ? _sessionGold / _sessionElapsed.TotalHours : 0;
                lines.Add(("GOLD", Settings.Style.HeaderColor, true));
                lines.Add(($"Area:   {FormatNumber(_areaGold)}", Settings.Gold.GoldColor, false));
                lines.Add(($"Session: {FormatNumber(_sessionGold)} ({FormatNumber(sessionRate)}/hr)", Settings.Gold.GoldColor, false));
            }

            // history section
            if (Settings.History.ShowHistory.Value && _areaHistory.Count > 0)
            {
                lines.Add(("RECENT AREAS", Settings.Style.HeaderColor, true));
                foreach (var record in _areaHistory)
                    lines.Add(($"{record.Name}: {record.TotalKills} kills, {FormatNumber(record.Gold)}g", Settings.Style.TextColor, false));
            }

            // ---- measure panel bounds ----
            var titleSize = Graphics.MeasureText(Name);
            float contentWidth = 0f;
            float contentHeight = 0f;
            foreach (var (text, _, _) in lines)
            {
                var s = Graphics.MeasureText(text);
                contentWidth = Math.Max(contentWidth, s.X);
                contentHeight += s.Y + (showBar && text.StartsWith("Current:") ? barHeight + barSpacing : 0);
            }

            float panelWidth = Math.Max(contentWidth, titleSize.X) + padding * 2;
            float titleBarHeight = titleSize.Y + padding / 2f;
            float contentHeightWithSpacing = contentHeight + Math.Max(0, lines.Count(l => l.header) - 1) * sectionSpacing;
            float panelHeight = titleBarHeight + padding + contentHeightWithSpacing + padding;

            // ---- draw panel ----
            Graphics.DrawBox(new RectangleF(pos.X, pos.Y, panelWidth, titleBarHeight), Settings.Style.TitleBarColor);
            Graphics.DrawText(Name, new Vector2(pos.X + padding, pos.Y + titleBarHeight / 2 - titleSize.Y / 2), Settings.Style.TitleTextColor, FontAlign.Left);
            Graphics.DrawBox(new RectangleF(pos.X, pos.Y + titleBarHeight, panelWidth, panelHeight - titleBarHeight), Settings.Style.BackgroundColor);

            var cursor = new Vector2(pos.X + padding, pos.Y + titleBarHeight + padding);
            for (var i = 0; i < lines.Count; i++)
            {
                var (text, color, header) = lines[i];

                // small gap between sections
                if (header && i > 0)
                    cursor.Y += sectionSpacing;

                Graphics.DrawText(text, cursor, color, FontAlign.Left);
                var textSize = Graphics.MeasureText(text);

                if (showBar && text.StartsWith("Current:"))
                {
                    var barY = cursor.Y + textSize.Y + barSpacing / 2;
                    Graphics.DrawBox(new RectangleF(cursor.X, barY, contentWidth, barHeight), new Color(0, 0, 0, 120));
                    Graphics.DrawBox(new RectangleF(cursor.X, barY, contentWidth * Math.Clamp(streakFraction, 0f, 1f), barHeight), Settings.Streak.TimerBarColor);
                    cursor.Y += barHeight + barSpacing;
                }

                cursor.Y += textSize.Y;
            }
        }

        private static string FormatNumber(double num)
        {
            if (num >= 1000000000)
                return (num / 1000000000D).ToString("0.#") + "b";
            if (num >= 1000000)
                return (num / 1000000D).ToString("0.#") + "m";
            if (num >= 1000)
                return (num / 1000D).ToString("0.#") + "k";
            return num.ToString("N0");
        }

        private bool IsAnyGameUIVisible()
        {
            var ui = GameController.IngameState.IngameUi;
            return ui.InventoryPanel.IsVisible ||
                   ui.OpenLeftPanel.IsVisible ||
                   ui.TreePanel.IsVisible ||
                   ui.Atlas.IsVisible ||
                   ui.SyndicatePanel.IsVisible ||
                   ui.DelveWindow.IsVisible ||
                   ui.IncursionWindow.IsVisible ||
                   ui.HeistWindow.IsVisible ||
                   ui.ExpeditionWindow.IsVisible ||
                   ui.RitualWindow.IsVisible ||
                   ui.UltimatumPanel.IsVisible;
        }

        #endregion
    }
}
