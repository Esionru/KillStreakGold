using System.Collections.Generic;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace KillStreakGold
{
    public class KillStreakGoldSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);
        public StreakSettings Streak { get; set; } = new StreakSettings();
        public KillsSettings Kills { get; set; } = new KillsSettings();
        public GoldSettings Gold { get; set; } = new GoldSettings();
        public PanelSettings Panel { get; set; } = new PanelSettings();
        public HistorySettings History { get; set; } = new HistorySettings();
        public StyleSettings Style { get; set; } = new StyleSettings();
        public SessionSettings Session { get; set; } = new SessionSettings();
    }

    [Submenu(CollapsedByDefault = false)]
    public class StreakSettings
    {
        [Menu("Streak Window (seconds to keep combo)")]
        public RangeNode<int> WindowSeconds { get; set; } = new RangeNode<int>(5, 2, 60);

        [Menu("Show Streak Timer Bar")]
        public ToggleNode ShowTimerBar { get; set; } = new ToggleNode(true);

        [Menu("Streak Timer Bar Color")]
        public ColorNode TimerBarColor { get; set; } = new ColorNode(Color.Orange);

        [Menu("Chat Notify On Unique Kill")]
        public ToggleNode NotifyOnUnique { get; set; } = new ToggleNode(true);
    }

    [Submenu(CollapsedByDefault = false)]
    public class KillsSettings
    {
        [Menu("Show Area Kills Section")]
        public ToggleNode ShowAreaKills { get; set; } = new ToggleNode(true);

        [Menu("Show Session Kills Section")]
        public ToggleNode ShowSessionKills { get; set; } = new ToggleNode(true);

        [Menu("Normal Monster Color")]
        public ColorNode NormalColor { get; set; } = new ColorNode(Color.White);

        [Menu("Magic Monster Color")]
        public ColorNode MagicColor { get; set; } = new ColorNode(Color.DodgerBlue);

        [Menu("Rare Monster Color")]
        public ColorNode RareColor { get; set; } = new ColorNode(Color.Gold);

        [Menu("Unique Monster Color")]
        public ColorNode UniqueColor { get; set; } = new ColorNode(Color.Orange);
    }

    [Submenu(CollapsedByDefault = false)]
    public class GoldSettings
    {
        [Menu("Show Gold Section")]
        public ToggleNode ShowGold { get; set; } = new ToggleNode(true);

        [Menu("Gold Text Color")]
        public ColorNode GoldColor { get; set; } = new ColorNode(new Color(255, 215, 0, 255));
    }

    [Submenu(CollapsedByDefault = true)]
    public class PanelSettings
    {
        [Menu("Position X")]
        public RangeNode<int> X { get; set; } = new RangeNode<int>(10, 0, 5000);

        [Menu("Position Y")]
        public RangeNode<int> Y { get; set; } = new RangeNode<int>(250, 0, 5000);

        [Menu("Show Only In Combat Areas")]
        public ToggleNode OnlyInCombatAreas { get; set; } = new ToggleNode(false);

        [Menu("Hide While Game UI Open (inventory/stash/etc.)")]
        public ToggleNode HideWhenGameUiOpen { get; set; } = new ToggleNode(true);
    }

    [Submenu(CollapsedByDefault = true)]
    public class HistorySettings
    {
        [Menu("Show Recent Areas Section")]
        public ToggleNode ShowHistory { get; set; } = new ToggleNode(true);

        [Menu("Recent Areas To Keep")]
        public RangeNode<int> MaxEntries { get; set; } = new RangeNode<int>(5, 1, 15);
    }

    [Submenu]
    public class StyleSettings
    {
        [Menu("Content Text")]
        public ColorNode TextColor { get; set; } = new ColorNode(Color.White);

        [Menu("Section Header")]
        public ColorNode HeaderColor { get; set; } = new ColorNode(new Color(0, 157, 255, 255));

        [Menu("Title Text")]
        public ColorNode TitleTextColor { get; set; } = new ColorNode(Color.White);

        [Menu("Title Bar")]
        public ColorNode TitleBarColor { get; set; } = new ColorNode(new Color(0, 157, 255, 80));

        [Menu("Background")]
        public ColorNode BackgroundColor { get; set; } = new ColorNode(new Color(0, 0, 0, 150));
    }

    [Submenu]
    public class SessionSettings
    {
        [Menu("Reset Session Stats")]
        public ButtonNode ResetSessionButton { get; set; } = new ButtonNode();
    }
}
