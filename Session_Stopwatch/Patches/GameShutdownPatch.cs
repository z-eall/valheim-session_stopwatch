using HarmonyLib;

namespace Session_Stopwatch.Patches;

[HarmonyPatch(typeof(Game), nameof(Game.Shutdown))]
internal static class Game_Shutdown_Patch
{
  private static void Prefix()
  {
    Clock.Stop();
  }
}
