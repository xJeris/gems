# Erenshor Gems

A match-3 falling-block puzzle game for [Erenshor](https://store.steampowered.com/app/2382520/Erenshor/), inspired by EverQuest's classic `/gems` mini-game. Built as a BepInEx mod using Unity IMGUI.

## Features

- **12 gem types** with blue (positive) and red (negative) special effects
- **Game icons** pulled directly from Erenshor's spell, skill, and item assets
- **Chain combos** with gravity and cascading matches
- **Wave progression** with increasing speed
- **EverQuest-style UI** with beveled stone textures and gold trim
- **Draggable window** with persistent position

## Installation

1. Install [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases) for Erenshor
2. Download `ErenshorGems.dll` from [Releases](../../releases)
3. Copy to `Erenshor Playtest/BepInEx/plugins/Gems/`
4. Launch Erenshor and type `/icons` in chat to open the game

## How to Play

- **Left/Right arrows** to move the falling gem
- **Down arrow** to fast-drop
- **Click the game area** to pause/unpause
- **Start** resumes a paused game or begins from the instruction screen
- **Reset** restarts the game and shows the instructions again
- **Done** pauses and closes the window
- **Match 3+** identical gems in any direction (horizontal, vertical, diagonal) to clear them

### Blue Gems (Positive)

Matching 3+ blue gems cancels all active red effects. Matching 4+ also triggers the gem's special:

| Gem | Icon | 4+ Effect |
|-----|------|-----------|
| BlueSword | Cleave | Clears an entire row |
| BlueShield | Ocean's Lull | Slows drop speed for 15s |
| BlueStar | Magic Bolt | Clears a 3x3 area |
| BlueArrow | Arcstorm | Clears the most populated column |
| BlueCrescent | Antidote | Removes all red gems from the field |
| BlueOrb | Double Attack | 2x score multiplier for 20s |

### Red Gems (Negative)

Matching 3+ red gems triggers a bad effect and cancels active blue effects:

| Gem | Icon | 3+ Effect |
|-----|------|-----------|
| RedWhirlwind | Vithean Breeze | Scrambles all placed gems |
| RedMirror | Dazzle | Reverses left/right controls |
| RedShadow | Lunar Madness | Hides the NEXT gem preview |
| RedChaos | Brax's Fury | Adds 5 random gems to the field |
| RedHaste | Affinity for Suffering | Doubles drop speed for 12s |
| RedVoid | Wasting | Removes 4 random blue gems |

### Scoring

- 100 points per gem cleared
- Size bonus: 4-match 1.5x, 5-match 2x, 6+ match 3x
- Chain combos multiply score by combo count
- Waves advance every 50 gems cleared, increasing speed

## Building from Source

Requires .NET Framework 4.7.2 SDK and the following installed:
- [Erenshor](https://store.steampowered.com/app/2382520/Erenshor/) (for game DLLs)
- [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases) installed in the game directory

```bash
dotnet build
```

The game path is configured in `ErenshorGems.csproj`. Update the `GameDir` property if your install location differs from the default.

## License

[MIT](LICENSE)
