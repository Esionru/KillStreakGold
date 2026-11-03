# Kill Streak Gold

Kill Streak Gold is an ExileAPI (ExileCore) plugin for Path of Exile 1 that tracks your kill
streaks, counts kills by monster rarity (normal / magic / rare / unique) and shows gold gained
per area and per session — in one compact on-screen panel.

## Features

- **Kill streaks** — current combo, best streak of the session and of the current area,
  with a configurable time window between kills (default: 5 seconds)
- **Streak timer bar** — shows how long you have left before the combo breaks
- **Kills by rarity** — separate counters for Normal / Magic / Rare / Unique monsters,
  for the current area and for the whole session
- **Gold tracking** — gold gained in the current area and in the session, plus gold/hour;
  gold spent is never counted as gained
- **Recent areas history** — last areas you cleared with kill counts and gold earned
- **Unique kill notification** — optional log message when a unique monster is slain
- **Session reset button** — in plugin settings
- Fully customizable: panel position, colors per rarity, section visibility, streak window

## How it works

A kill is registered when a hostile monster that was seen alive becomes dead. Monsters that
are hidden or not yet spawned are ignored, so the counters only reflect monsters you actually
fight. Area stats reset on zone change; session stats persist until you reset them.

Gold is tracked as positive deltas of your total gold, so spending currency in your hideout
never inflates the counter.

## Installation

1. Put the `KillStreakGold` folder into `Plugins\Source\` of your ExileCore (PoE Helper) install:

   ```
   Plugins
   |
   ->Source
     |
     ->KillStreakGold
       ->KillStreakGold.csproj
       ->KillStreakGold.cs
       ->KillStreakGoldSettings.cs
   ```

2. Launch the HUD — the plugin is compiled automatically from source.
3. Open the menu (F12), find **Kill Streak Gold** and make sure it is enabled.

## Settings

All options live in the plugin menu (F12 → Kill Streak Gold):

| Section | Options |
|---------|---------|
| Streak | Time window, timer bar on/off and color, unique kill notification |
| Kills | Area/session section visibility, colors per rarity |
| Gold | Section visibility, text color |
| Panel | Position X/Y, hide in peaceful areas, hide when game UI is open |
| History | Recent areas on/off, how many entries to keep |
| Style | Text, header, title bar and background colors |

## Building manually (for development)

```
dotnet build KillStreakGold.csproj -p:exapiPackage=C:\Path\To\Your\PoeHelper
```

`exapiPackage` must point to the folder containing `ExileCore.dll`
(or set the `exapiPackage` environment variable for your IDE).

## Requirements

- [ExileCore](https://github.com/ExileTools/ExileCore) (PoE Helper) for Path of Exile 1
- .NET 10 SDK (only needed for manual builds)
