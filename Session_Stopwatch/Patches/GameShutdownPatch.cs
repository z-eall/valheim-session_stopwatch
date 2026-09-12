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

/// <summary>
/// Exit / Alt+F4: vanilla calls this before <see cref="Game.Shutdown"/>.
/// Prefer this over plugin OnApplicationQuit so the reason is <c>quit</c> before Shutdown clears the sitting.
/// </summary>
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
    // Fallback only — Logout / OnApplicationQuit already stopped when those paths ran.
    Clock.Stop("shutdown");
  }
}
