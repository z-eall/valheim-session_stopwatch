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
  internal string TimeStamp => StartedLocal.ToString("HH:mm");
}
