using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Session_Stopwatch.Patches;

/// <summary>Local HUD + Ctrl+F1 free-cursor capture (InputGuard-shaped; no InputGuard dependency).</summary>
internal static class HudCapture
{
  private static readonly FieldInfo? MouseCaptureField =
    AccessTools.Field(typeof(GameCamera), "m_mouseCapture");

  /// <summary>
  /// Ctrl+F1 toggles <see cref="GameCamera"/> m_mouseCapture.
  /// When false, cursor is free and gameplay must not see mouse look/attack anywhere.
  /// </summary>
  internal static bool FreeCursorMode
  {
    get
    {
      if (Player.m_localPlayer == null || Menu.IsVisible() || Hud.IsUserHidden())
      {
        return false;
      }

      var cam = GameCamera.instance;
      if (cam == null || MouseCaptureField == null)
      {
        return false;
      }

      return MouseCaptureField.GetValue(cam) is false;
    }
  }

  internal static bool Active =>
    ClockGui.PointerOver || ClockGui.Dragging || FreeCursorMode;
}
