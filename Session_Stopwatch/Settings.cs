using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace Session_Stopwatch;

/// <summary>
/// Player prefs in <c>session_stopwatch.cfg</c> (HUD writes the same entries).
/// Advanced Log levels + Park schema only for LogOutput / internals — AFK + park XY are player-facing.
/// </summary>
internal static class Settings
{
  internal const string SectionGeneral = "1 - General";
  internal const string SectionInternal = "9 - Internal";
  internal const string SectionLogging = "10. Logging";

  internal const int AfkStepSeconds = 30;
  internal const int AfkMaxSeconds = 1800;
  internal const int DefaultAfkSeconds = 120;

  /// <summary>Screen-space top-left of the pebble. Negative = optional snap under minimap.</summary>
  internal const float DefaultHudX = -1f;
  internal const float DefaultHudY = -1f;

  internal const LogLevel DefaultLogLevels =
    LogLevel.Fatal | LogLevel.Error | LogLevel.Warning | LogLevel.Message | LogLevel.Info;

  internal static ConfigEntry<LogLevel>? LogLevels { get; private set; }

  private static ConfigEntry<int> _afkTimeoutSeconds = null!;
  private static ConfigEntry<float> _hudX = null!;
  private static ConfigEntry<float> _hudY = null!;
  private static ConfigEntry<int> _parkSchema = null!;

  /// <summary>Bumped when default park math changes — forces one under-minimap snap (no manual -1).</summary>
  private const int CurrentParkSchema = 2;

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

  /// <summary>Optional cfg reset: either axis negative means “snap under minimap”.</summary>
  internal static bool WantsMinimapSnap => _hudX.Value < 0f || _hudY.Value < 0f;

  internal static void Init(ConfigFile config)
  {
    _afkTimeoutSeconds = config.Bind(
      SectionGeneral,
      "AFK timeout seconds",
      DefaultAfkSeconds,
      new ConfigDescription(
        "Seconds with no input before time counts as AFK while a session is running.\n" +
        "0 turns AFK tracking off.\n" +
        "Max 1800. The panel steps this by 30.",
        new AcceptableValueRange<int>(0, AfkMaxSeconds)));
    _hudX = config.Bind(SectionGeneral, "HUD X", DefaultHudX,
      "Where the clock sits on screen (from the left).\n" +
      "Drag the clock to change this.\n" +
      "Set HUD X or HUD Y to -1 to put the clock back under the minimap.");
    _hudY = config.Bind(SectionGeneral, "HUD Y", DefaultHudY,
      "Where the clock sits on screen (from the top).\n" +
      "Drag the clock to change this.\n" +
      "Set HUD X or HUD Y to -1 to put the clock back under the minimap.");
    // Section 9 + Advanced (not player-facing). Default = current so a section move does not re-snap.
    _parkSchema = config.Bind(SectionInternal, "Park schema", CurrentParkSchema,
      new ConfigDescription(
        "Internal. Raised when the default under-minimap position changes; moves the clock there once.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true } }));

    if (_parkSchema.Value < CurrentParkSchema)
    {
      _hudX.Value = DefaultHudX;
      _hudY.Value = DefaultHudY;
      _parkSchema.Value = CurrentParkSchema;
      SessionStopwatchPlugin.LogAt(LogLevel.Info,
        $"Park schema → {CurrentParkSchema}: forcing under-minimap snap (no manual -1 needed).");
    }

    LogLevels = config.Bind(
      SectionLogging,
      "Log levels",
      DefaultLogLevels,
      new ConfigDescription(
        "Same flags as BepInEx Logging.Disk / Logging.Console. Sitting and AFK traces are Debug; they only reach LogOutput.log when Debug is checked here and in BepInEx.cfg.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true } }));

    config.SettingChanged += OnSettingChanged;
    config.ConfigReloaded += (_, _) => SessionStopwatchPlugin.LogAt(LogLevel.Info, "Config reloaded.");

    LogLoaded(LogLevels);
    LogLoaded(_afkTimeoutSeconds);
    LogLoaded(_hudX);
    LogLoaded(_hudY);
    LogLoaded(_parkSchema);
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
