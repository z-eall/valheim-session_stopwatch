using HarmonyLib;
using UnityEngine;

namespace Session_Stopwatch.Patches;

/// <summary>Local HUD input capture (InputGuard-shaped; no InputGuard dependency).</summary>
internal static class HudCapture
{
  internal static bool Active => ClockGui.PointerOver || ClockGui.Dragging;
}

[HarmonyPatch(typeof(Player), nameof(Player.TakeInput))]
internal static class Player_TakeInput_Patch
{
  private static void Postfix(ref bool __result)
  {
    if (HudCapture.Active)
    {
      __result = false;
    }
  }
}

[HarmonyPatch(typeof(PlayerController), "TakeInput")]
internal static class PlayerController_TakeInput_Patch
{
  private static void Postfix(bool look, ref bool __result)
  {
    // Block look whenever the pointer is over the clock (Tab/Ctrl+F1 cursor bleed).
    if (HudCapture.Active && look)
    {
      __result = false;
    }
  }
}

[HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseDelta))]
internal static class ZInput_GetMouseDelta_Patch
{
  private static void Postfix(ref Vector2 __result)
  {
    if (HudCapture.Active)
    {
      __result = Vector2.zero;
    }
  }
}

[HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
internal static class ZInput_GetMouseScrollWheel_Patch
{
  private static void Postfix(ref float __result)
  {
    if (HudCapture.Active)
    {
      __result = 0f;
    }
  }
}
