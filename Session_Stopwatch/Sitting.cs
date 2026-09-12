using System;

namespace Session_Stopwatch;

internal sealed class Sitting
{
  internal DateTime StartedLocal { get; set; }
  internal string Character { get; set; } = "";
  internal string World { get; set; } = "";
  internal float ActiveSeconds { get; set; }
  internal float InactiveSeconds { get; set; }

  internal float TotalSeconds => ActiveSeconds + InactiveSeconds;

  internal string DateStamp => StartedLocal.ToString("yyyy-MM-dd");

  /// <summary>Local wall clock at Start, with UTC offset e.g. <c>00:26 (UTC+8)</c>.</summary>
  internal string TimeStamp
  {
    get
    {
      var offset = TimeZoneInfo.Local.GetUtcOffset(StartedLocal);
      var sign = offset < TimeSpan.Zero ? "-" : "+";
      var abs = offset.Duration();
      var tz = abs.Minutes == 0
        ? $"UTC{sign}{(int)abs.TotalHours}"
        : $"UTC{sign}{(int)abs.TotalHours}:{abs.Minutes:D2}";
      return $"{StartedLocal:HH:mm} ({tz})";
    }
  }
}
