using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Session_Stopwatch;

internal static class Clock
{
  private static readonly MethodInfo? JoyRightX =
    AccessTools.Method(typeof(ZInput), "GetJoyRightStickX", new[] { typeof(bool) });
  private static readonly MethodInfo? JoyRightY =
    AccessTools.Method(typeof(ZInput), "GetJoyRightStickY", new[] { typeof(bool) });

  private static readonly string[] Buttons =
  {
    "Forward", "Backward", "Left", "Right", "Jump", "Run", "Crouch", "Attack", "Block", "AutoRun"
  };

  /// <summary>Open-sitting yaml rewrite interval (10 minutes).</summary>
  private const float OpenRewriteIntervalSeconds = 600f;

  private static List<Sitting> _history = new();
  private static Sitting? _open;
  private static bool _manualPaused;
  private static float _secondsSinceInput;
  private static string _awayReason = "";
  private static bool _wasFocused = true;
  private static float _sinceOpenRewrite;

  internal static bool HasSitting => _open != null;
  internal static bool ManualPaused => _manualPaused;
  internal static bool IsTicking { get; private set; }
  internal static float ActiveSeconds => _open?.ActiveSeconds ?? 0f;
  internal static string ActiveDisplay => SittingLog.FormatDuration(ActiveSeconds);

  internal static void Load()
  {
    _history = SittingLog.LoadHistory();
    _open = null;
    _manualPaused = false;
    _secondsSinceInput = 0f;
    _awayReason = "";
    _wasFocused = true;
    _sinceOpenRewrite = 0f;
    IsTicking = false;
    SessionStopwatchPlugin.LogAt(LogLevel.Info, $"Loaded {_history.Count} sitting(s) from yaml.");
    SessionStopwatchPlugin.LogAt(LogLevel.Info,
      $"ZInput GetJoyRightStickX(bool)={(JoyRightX != null)} GetJoyRightStickY(bool)={(JoyRightY != null)}.");
    SessionStopwatchPlugin.LogAt(LogLevel.Info,
      $"Open-sitting rewrite interval={OpenRewriteIntervalSeconds:0}s.");
  }

  internal static void Start()
  {
    if (_open != null)
    {
      _manualPaused = false;
      _secondsSinceInput = 0f;
      _awayReason = "";
      SessionStopwatchPlugin.LogAt(LogLevel.Info, "Sitting resumed (manual pause cleared).");
      return;
    }

    if (Player.m_localPlayer == null || Game.instance == null || ZNet.instance == null)
    {
      SessionStopwatchPlugin.LogAt(LogLevel.Warning,
        $"Start ignored (localPlayer={Player.m_localPlayer != null}, Game={Game.instance != null}, ZNet={ZNet.instance != null}).");
      return;
    }

    var profile = Game.instance.GetPlayerProfile();
    _open = new Sitting
    {
      StartedLocal = DateTime.Now,
      Character = profile != null ? profile.GetName() : "",
      World = ZNet.instance.GetWorldName() ?? ""
    };
    _manualPaused = false;
    _secondsSinceInput = 0f;
    _awayReason = "";
    _sinceOpenRewrite = 0f;
    SessionStopwatchPlugin.LogAt(LogLevel.Info, $"Sitting started character={_open.Character} world={_open.World}.");
  }

  internal static void Pause()
  {
    if (_open == null)
    {
      return;
    }

    _manualPaused = true;
    SessionStopwatchPlugin.LogAt(LogLevel.Info, "Sitting paused (manual).");
  }

  internal static void Stop(string reason = "stop")
  {
    if (_open == null)
    {
      return;
    }

    var active = SittingLog.FormatDuration(_open.ActiveSeconds);
    var inactive = SittingLog.FormatDuration(_open.InactiveSeconds);
    var total = SittingLog.FormatDuration(_open.TotalSeconds);
    Flush();
    _history.Add(_open);
    _open = null;
    _manualPaused = false;
    _awayReason = "";
    _sinceOpenRewrite = 0f;
    IsTicking = false;
    SittingLog.Write(_history, null);
    SessionStopwatchPlugin.LogAt(LogLevel.Info,
      $"Sitting stopped reason={reason} active={active} inactive={inactive} total={total} history={_history.Count} path={SittingLog.PathFile}.");
  }

  internal static void Tick(float unscaledDt)
  {
    if (_open == null || unscaledDt <= 0f)
    {
      IsTicking = false;
      return;
    }

    if (Game.instance != null && Game.instance.IsShuttingDown())
    {
      return;
    }

    var focused = Application.isFocused;
    if (focused && !_wasFocused)
    {
      _secondsSinceInput = 0f;
      SessionStopwatchPlugin.LogAt(LogLevel.Debug, "Focus regained — AFK idle reset.");
    }

    _wasFocused = focused;

    var input = HasQualifyingInput();
    if (input)
    {
      _secondsSinceInput = 0f;
    }
    else if (focused)
    {
      _secondsSinceInput += unscaledDt;
    }

    var afkTimeout = Settings.AfkTimeoutSeconds;
    var afk = afkTimeout > 0 && focused && _secondsSinceInput >= afkTimeout;
    var reason = _manualPaused ? "manual" : !focused ? "unfocused" : afk ? "afk" : "";
    if (reason != _awayReason)
    {
      _awayReason = reason;
      SessionStopwatchPlugin.LogAt(LogLevel.Debug,
        reason.Length == 0
          ? "Sitting active."
          : $"Sitting inactive ({reason}).");
    }

    var inactive = reason.Length > 0;
    IsTicking = !inactive;
    if (inactive)
    {
      _open.InactiveSeconds += unscaledDt;
    }
    else
    {
      _open.ActiveSeconds += unscaledDt;
    }

    _sinceOpenRewrite += unscaledDt;
    if (_sinceOpenRewrite >= OpenRewriteIntervalSeconds)
    {
      _sinceOpenRewrite = 0f;
      Flush();
      SessionStopwatchPlugin.LogAt(LogLevel.Info,
        $"Open sitting rewritten ({OpenRewriteIntervalSeconds:0}s) active={SittingLog.FormatDuration(_open.ActiveSeconds)} inactive={SittingLog.FormatDuration(_open.InactiveSeconds)} total={SittingLog.FormatDuration(_open.TotalSeconds)}.");
    }
  }

  private static void Flush()
  {
    if (_open == null)
    {
      return;
    }

    SittingLog.Write(_history, _open);
  }

  private static bool HasQualifyingInput()
  {
    if (ClockGui.PointerOver || ClockGui.Dragging)
    {
      return true;
    }

    if (ZInput.instance == null)
    {
      return false;
    }

    if (ZInput.GetMouseDelta() != Vector2.zero || Mathf.Abs(ZInput.GetMouseScrollWheel()) > 0.01f)
    {
      return true;
    }

    if (ZInput.GetMouseButton(0) || ZInput.GetMouseButton(1) || ZInput.GetMouseButton(2))
    {
      return true;
    }

    if (Mathf.Abs(ZInput.GetJoyLeftStickX(false)) > 0.01f || Mathf.Abs(ZInput.GetJoyLeftStickY(true)) > 0.01f
        || Mathf.Abs(ReadStick(JoyRightX, true)) > 0.01f || Mathf.Abs(ReadStick(JoyRightY, true)) > 0.01f)
    {
      return true;
    }

    foreach (var name in Buttons)
    {
      if (ZInput.GetButton(name))
      {
        return true;
      }
    }

    var player = Player.m_localPlayer;
    return player != null && player.m_autoRun;
  }

  private static float ReadStick(MethodInfo? method, bool smooth)
  {
    if (method == null)
    {
      return 0f;
    }

    return (float)method.Invoke(null, new object[] { smooth });
  }
}
