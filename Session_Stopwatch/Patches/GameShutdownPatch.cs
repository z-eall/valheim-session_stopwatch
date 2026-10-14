using HarmonyLib;

namespace Session_Stopwatch.Patches;

[HarmonyPatch(typeof(Game), nameof(Game.Logout))]
internal static class Game_Logout_Patch
{
  private static void Prefix()
  {
    Clock.Stop("logout");
  }
}

[HarmonyPatch(typeof(Game), "OnApplicationQuit")]
internal static class Game_OnApplicationQuit_Patch
{
  private static void Prefix()
  {
    Clock.Stop("quit");
  }
}

[HarmonyPatch(typeof(Game), "Shutdown")]
internal static class Game_Shutdown_Patch
{
  private static void Prefix()
  {
    Clock.Stop("shutdown");
  }
}
