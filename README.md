# Session_Stopwatch

[![Find me here](https://img.shields.io/badge/Find_me_here-Discord-5865F2?logo=discord&logoColor=white&style=flat)](https://discord.gg/VFRJcPwUdm)
[![Support](https://img.shields.io/badge/Support-Ko--fi-FF5E5B?logo=ko-fi&logoColor=white&style=flat)](https://ko-fi.com/zeall)

Adds a small **session clock** on screen so you can time how long you sat down to build. You start and stop it yourself.

Install on your client only.

## Features

- Clock under the minimap (drag to move; click to open or close the panel).
- **►** starts timing. **||** pauses. **■** stops and saves.
- AFK timer on the panel (±30 s; `00:00:00` = off).
- Tab, Esc, or **Ctrl+F1** for the cursor. Hover or drag the clock so world clicks do not go through it.
- Open sessions update the log every 10 minutes; Stop, logout, or exit save right away.

## How to use

1. Install the plugin on your client.
2. Load a world — the clock appears under the minimap.
3. Click the clock to open the panel. Press **►** when you want to start timing.
4. Use **||** to pause, **■** to stop and save. Change **AFK timeout** on the panel if you like (±30 s).
5. Ctrl+F3 hides it with the vanilla HUD.

## Configuration

File: `BepInEx/config/session_stopwatch.cfg`.

- AFK timeout seconds: seconds with no input before time counts as AFK (0 = off, max 1800). Also on the panel (±30 s).
- HUD X / HUD Y: where the clock sits. Drag also saves these. Set either to `-1` to put the clock back under the minimap.

Log file: `BepInEx/config/session_stopwatch/session_stopwatch_log.yaml` (start time with timezone, e.g. `00:26 (UTC+8)`).

## Credits

Source: [<img src="https://cdn.simpleicons.org/github/181717" width="16" height="16" alt="" /> GitHub](https://github.com/z-eall/valheim-session_stopwatch)
