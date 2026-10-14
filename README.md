# Session_Stopwatch

[![Find me here](https://img.shields.io/badge/Find_me_here-Discord-5865F2?logo=discord&logoColor=white&style=flat)](https://discord.gg/VFRJcPwUdm)
[![Support](https://img.shields.io/badge/Support-Ko--fi-FF5E5B?logo=ko-fi&logoColor=white&style=flat)](https://ko-fi.com/zeall)

Adds a small Timer on screen so you can time how long you sat down to build. Start and stop it yourself, or let it start when you spawn.

Install on your client only.

## Features

- Timer under the minimap. Drag to move it. Click it to switch between Expanded and Collapsed.
- Options: Start, Pause, Stop, Auto start (**AS**) and an **AFK** button. Hover one to see what it does.
- Opening the game menu with Esc expands the panel. It goes back when you resume.
- Press **Ctrl+F1** to show the cursor. Hover or drag the panel so world clicks do not go through it.

## Configuration

File: `BepInEx/config/session_stopwatch.cfg`.

- Auto start: starts the Timer when your character first spawns after a world load. The **AS** button sets the same value.
- Default view: `Expanded` shows the Options when a world loads. `Collapsed` shows only the Timer.
- AFK timeout seconds: seconds with no input before time counts as AFK. `0` turns it off, max `600`. Scroll the mouse wheel over the **AFK** button to change it by 30 s.
- Panel X / Panel Y: where the panel sits. Dragging saves these. Set either to `-1` to put the panel back under the minimap.

Log file: `BepInEx/config/session_stopwatch/session_stopwatch_log.yaml`. Open sessions update it every 10 minutes. Stop, logout, and exit save right away.

## Credits

Source: [<img src="https://cdn.simpleicons.org/github/181717" width="16" height="16" alt="" /> GitHub](https://github.com/z-eall/valheim-session_stopwatch)
