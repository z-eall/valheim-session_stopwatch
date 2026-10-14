using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Session_Stopwatch.Patches;

internal static class HudCapture
{
  private static readonly FieldInfo? MouseCaptureField =
    AccessTools.Field(typeof(GameCamera), "m_mouseCapture");

  private static int _cacheFrame = -1;
  private static bool _cacheFree;

  internal static bool FreeCursorMode
  {
    get
    {
      var frame = Time.frameCount;
      if (frame != _cacheFrame)
      {
        _cacheFrame = frame;
        _cacheFree = ComputeFreeCursorMode();
      }

      return _cacheFree;
    }
  }

  private static bool ComputeFreeCursorMode()
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

  internal static bool Active =>
    ClockGui.PointerOver || ClockGui.Dragging || FreeCursorMode;
}
