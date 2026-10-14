using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace Session_Stopwatch;

internal enum DefaultView
{
  Expanded,
  Collapsed
}

internal static class Settings
{
  internal const string SectionGeneral = "1 - General";
  internal const string SectionInternal = "9 - Internal";
  internal const string SectionLogging = "10. Logging";

  internal const int AfkStepSeconds = 30;
  internal const int AfkMaxSeconds = 1800;
  internal const int DefaultAfkSeconds = 120;

  internal const float DefaultHudX = -1f;
  internal const float DefaultHudY = -1f;

  internal const LogLevel DefaultLogLevels =
    LogLevel.Fatal | LogLevel.Error | LogLevel.Warning | LogLevel.Message | LogLevel.Info;

  internal static ConfigEntry<LogLevel>? LogLevels { get; private set; }

  private static ConfigEntry<bool> _autoStart = null!;
  private static ConfigEntry<DefaultView> _defaultView = null!;
  private static ConfigEntry<int> _afkTimeoutSeconds = null!;
  private static ConfigEntry<float> _hudX = null!;
  private static ConfigEntry<float> _hudY = null!;
  private static ConfigEntry<int> _parkSchema = null!;

  private const int CurrentParkSchema = 2;

  internal static int AfkTimeoutSeconds
  {
    get => Mathf.Clamp(_afkTimeoutSeconds.Value, 0, AfkMaxSeconds);
    set => _afkTimeoutSeconds.Value = Mathf.Clamp(value, 0, AfkMaxSeconds);
  }

  internal static bool AutoStart
  {
    get => _autoStart.Value;
    set => _autoStart.Value = value;
  }

  internal static bool StartCollapsed => _defaultView.Value == DefaultView.Collapsed;

  internal static Vector2 HudPosition
  {
    get => new(_hudX.Value, _hudY.Value);
    set
    {
      _hudX.Value = value.x;
      _hudY.Value = value.y;
    }
  }

  internal static bool WantsMinimapSnap => _hudX.Value < 0f || _hudY.Value < 0f;

  internal static void Init(ConfigFile config)
  {
    _autoStart = config.Bind(
      SectionGeneral,
      "Auto start",
      false,
      "Start the clock when your character first spawns after you load a world.\n" +
      "Stop and Start still work by hand.\n" +
      "The clock panel has the same switch.");
    _defaultView = config.Bind(
      SectionGeneral,
      "Default view",
      DefaultView.Expanded,
      "How the clock panel looks when you load a world.\n" +
      "Expanded shows the buttons. Collapsed shows only the small clock.");
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
    LogLoaded(_autoStart);
    LogLoaded(_defaultView);
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

internal sealed class ConfigurationManagerAttributes
{
  public int? Order = null;
  public bool? ShowRangeAsPercent = null;
  public bool? IsAdvanced = null;
}
