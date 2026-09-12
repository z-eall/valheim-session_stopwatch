# Session_Stopwatch

Client QoL: an in-world **session clock** for capturing how long you sat down to build. You press Play; nothing auto-detects “building.”

## Identity

| Layer | Value |
|---|---|
| Thunderstore | **Zeall/Session_Stopwatch** |
| Plugin title | **Session_Stopwatch** |
| BepInEx GUID | `session_stopwatch` |

## How to use

1. Client-only — do **not** install on dedicated headless (it no-ops there).
2. `dotnet build -c Release` (from this folder or the nested project) writes `Session_Stopwatch.dll` to `plugins\`. Copy that folder (or the DLL) into `Valheim\BepInEx\plugins\Session_Stopwatch\` when you want to test.
3. Load a world. A floating **S** pebble parks below the minimap (drag it). Click to open/close the panel beside it. Ctrl+F3 hides it with the vanilla HUD.
4. **►** starts a sitting (green while ticking). **||** pauses (yellow while inactive). **■** stops and writes the yaml sitting.
5. **AFK timer** row: label + `−` / value (`HH:MM:SS`) / `+` (30 s steps; `00:00:00` = off). Tab/Esc or **Ctrl+F1** for the cursor; hover/drag on the clock blocks gameplay input bleed.

## Player commands

None — HUD only.

## Admin commands

None.

## Config

`BepInEx/config/session_stopwatch.cfg`

- **AFK timeout seconds** — also on the HUD (±30 s, 0 = off)
- **HUD X / HUD Y** — park position (drag also writes these)
- **Log levels** — advanced; Debug for sitting/AFK traces

Log: `BepInEx/config/session_stopwatch/session_stopwatch_log.yaml`. An open sitting is written only on Stop / logout / clean quit (no periodic rewrite).

Thunderstore author when published: **Zeall**.
