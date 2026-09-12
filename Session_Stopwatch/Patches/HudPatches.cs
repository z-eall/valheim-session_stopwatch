using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Session_Stopwatch.Patches;

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
    // Block both look and Attack/controls — look-only left bomb throws through (clockTest4).
    if (HudCapture.Active)
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

[HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseButton))]
internal static class ZInput_GetMouseButton_Patch
{
  private static void Postfix(ref bool __result)
  {
    if (HudCapture.Active)
    {
      __result = false;
    }
  }
}

[HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseButtonDown))]
internal static class ZInput_GetMouseButtonDown_Patch
{
  private static void Postfix(ref bool __result)
  {
    if (HudCapture.Active)
    {
      __result = false;
    }
  }
}

[HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButton))]
internal static class ZInput_GetButton_Patch
{
  private static void Postfix(string name, ref bool __result)
  {
    if (HudCapture.Active && IsCombatOrPlace(name))
    {
      __result = false;
    }
  }

  private static bool IsCombatOrPlace(string name)
  {
    return name is "Attack" or "JoyAttack" or "SecondaryAttack" or "JoySecondaryAttack"
      or "Block" or "JoyBlock" or "JoyPlace" or "JoyAltPlace" or "AltPlace";
  }
}

[HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown))]
internal static class ZInput_GetButtonDown_Patch
{
  private static void Postfix(string name, ref bool __result)
  {
    if (HudCapture.Active && IsCombatOrPlace(name))
    {
      __result = false;
    }
  }

  private static bool IsCombatOrPlace(string name)
  {
    return name is "Attack" or "JoyAttack" or "SecondaryAttack" or "JoySecondaryAttack"
      or "Block" or "JoyBlock" or "JoyPlace" or "JoyAltPlace" or "AltPlace";
  }
}

/// <summary>
/// IMGUI draws on top but EventSystem still hits Skills/inventory under the clock.
/// Explicit arg types — bare Raycast name is ambiguous (clockTest5 AmbiguousMatchException killed PatchAll).
/// </summary>
[HarmonyPatch(typeof(GraphicRaycaster), nameof(GraphicRaycaster.Raycast),
  new[] { typeof(PointerEventData), typeof(List<RaycastResult>) })]
internal static class GraphicRaycaster_Raycast_Patch
{
  private static void Postfix(List<RaycastResult> resultAppendList)
  {
    if (HudCapture.Active && resultAppendList != null && resultAppendList.Count > 0)
    {
      resultAppendList.Clear();
    }
  }
}
