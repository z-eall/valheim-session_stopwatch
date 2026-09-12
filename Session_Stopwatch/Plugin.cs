using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace Session_Stopwatch;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class SessionStopwatchPlugin : BaseUnityPlugin
{
  internal const string ModName = "Session_Stopwatch";
  internal const string ModVersion = "0.2.3";
  /// <summary>Jere-style snake_case GUID. Thunderstore author when published: Zeall.</summary>
  internal const string ModGUID = "session_stopwatch";

  internal static SessionStopwatchPlugin Instance { get; private set; } = null!;
  internal static ManualLogSource Log { get; private set; } = null!;

  private readonly Harmony _harmony = new(ModGUID);
  private bool _loaded;

  internal static bool Allows(LogLevel level)
  {
    return Settings.LogLevels == null || (Settings.LogLevels.Value & level) != LogLevel.None;
  }

  internal static void LogAt(LogLevel level, string message)
  {
    if (!Allows(level))
    {
      return;
    }

    Log.Log(level, message);
  }

  private void Awake()
  {
    Instance = this;
    Log = Logger;

    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
    {
      Log.LogWarning($"{ModName} is client-only and will not load in headless mode.");
      return;
    }

    Settings.Init(Config);
    Clock.Load();
    _harmony.PatchAll(Assembly.GetExecutingAssembly());
    _loaded = true;
    LogAt(LogLevel.Info, $"{ModName} v{ModVersion} loaded (GUID {ModGUID}).");
  }

  private void Update()
  {
    if (!_loaded)
    {
      return;
    }

    Clock.Tick(Time.unscaledDeltaTime);
  }

  private void OnGUI()
  {
    if (_loaded)
    {
      ClockGui.Draw();
    }
  }

  private void OnDestroy()
  {
    if (_loaded)
    {
      _harmony.UnpatchSelf();
    }
  }
}
