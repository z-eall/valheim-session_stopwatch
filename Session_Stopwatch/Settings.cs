using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace Session_Stopwatch;

/// <summary>
/// Player prefs in <c>session_stopwatch.cfg</c> (HUD writes the same entries).
/// Advanced Log levels only for LogOutput / debug — AFK + park are also on the HUD.
/// </summary>
internal static class Settings
{
  internal const string SectionGeneral = "1 - General";
  internal const string SectionLogging = "10. Logging";

  internal const int AfkStepSeconds = 30;
  internal const int AfkMaxSeconds = 1800;
  internal const int DefaultAfkSeconds = 120;

  /// <summary>Screen-space top-left of the pebble. Negative X = snap below minimap once.</summary>
  internal const float DefaultHudX = -1f;
  internal const float DefaultHudY = -1f;

  internal const LogLevel DefaultLogLevels =
    LogLevel.Fatal | LogLevel.Error | LogLevel.Warning | LogLevel.Message | LogLevel.Info;

  internal static ConfigEntry<LogLevel>? LogLevels { get; private set; }

  private static ConfigEntry<int> _afkTimeoutSeconds = null!;
  private static ConfigEntry<float> _hudX = null!;
  private static ConfigEntry<float> _hudY = null!;

  internal static int AfkTimeoutSeconds
  {
    get => Mathf.Clamp(_afkTimeoutSeconds.Value, 0, AfkMaxSeconds);
    set => _afkTimeoutSeconds.Value = Mathf.Clamp(value, 0, AfkMaxSeconds);
  }

  internal static Vector2 HudPosition
  {
    get => new(_hudX.Value, _hudY.Value);
    set
    {
      _hudX.Value = value.x;
      _hudY.Value = value.y;
    }
  }

  /// <summary>True until the player has a saved park (both axes non-negative).</summary>
  internal static bool NeedsDefaultPark => _hudX.Value < 0f || _hudY.Value < 0f;

  internal static void Init(ConfigFile config)
  {
    _afkTimeoutSeconds = config.Bind(
      SectionGeneral,
      "AFK timeout seconds",
      DefaultAfkSeconds,
      new ConfigDescription(
        "No qualifying input for this many seconds counts as AFK (0 = off, max 1800). HUD steps by 30.",
        new AcceptableValueRange<int>(0, AfkMaxSeconds)));
    _hudX = config.Bind(SectionGeneral, "HUD X", DefaultHudX,
      "Pebble X (pixels from left). Negative = snap below minimap on first load.");
    _hudY = config.Bind(SectionGeneral, "HUD Y", DefaultHudY,
      "Pebble Y (pixels from top). Negative = snap below minimap on first load.");

    LogLevels = config.Bind(
      SectionLogging,
      "Log levels",
      DefaultLogLevels,
      new ConfigDescription(
        "Same flags as BepInEx Logging.Disk / Logging.Console. Tick Debug for sitting and AFK traces.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true } }));

    config.SettingChanged += OnSettingChanged;
    config.ConfigReloaded += (_, _) => SessionStopwatchPlugin.LogAt(LogLevel.Info, "Config reloaded.");

    LogLoaded(LogLevels);
    LogLoaded(_afkTimeoutSeconds);
    LogLoaded(_hudX);
    LogLoaded(_hudY);
  }

  private static void OnSettingChanged(object sender, SettingChangedEventArgs args)
  {
    var entry = args.ChangedSetting;
    if (entry == null)
    {
      return;
    }

    SessionStopwatchPlugin.LogAt(LogLevel.Info, $"{entry.Definition.Key} set to {entry.BoxedValue}.");
  }

  private static void LogLoaded(ConfigEntryBase entry)
  {
    SessionStopwatchPlugin.LogAt(LogLevel.Info, $"{entry.Definition.Key}: {entry.BoxedValue}.");
  }
}

/// <summary>Dummy type Configuration Manager looks for on <see cref="ConfigDescription.Tags"/>.</summary>
internal sealed class ConfigurationManagerAttributes
{
  public int? Order = null;
  public bool? ShowRangeAsPercent = null;
  public bool? IsAdvanced = null;
}
