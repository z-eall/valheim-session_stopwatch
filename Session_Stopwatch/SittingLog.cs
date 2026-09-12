using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;

namespace Session_Stopwatch;

/// <summary>Hand-rolled list-of-maps yaml (spec shape). No YamlDotNet.</summary>
internal static class SittingLog
{
  private static readonly string Dir = Path.Combine(Paths.ConfigPath, "session_stopwatch");
  private static readonly string PathFile = Path.Combine(Dir, "session_stopwatch_log.yaml");

  internal static List<Sitting> LoadHistory()
  {
    var list = new List<Sitting>();
    if (!File.Exists(PathFile))
    {
      return list;
    }

    Sitting? current = null;
    foreach (var raw in File.ReadAllLines(PathFile))
    {
      var line = raw.TrimEnd();
      if (line.Length == 0 || line[0] == '#')
      {
        continue;
      }

      if (line.StartsWith("- ", StringComparison.Ordinal))
      {
        Flush(list, ref current);
        current = new Sitting();
        Apply(current, line.Substring(2));
        continue;
      }

      if (current != null && line.Length >= 2 && line[0] == ' ' && line[1] == ' ')
      {
        Apply(current, line.TrimStart());
      }
    }

    Flush(list, ref current);
    return list;
  }

  internal static void Write(IReadOnlyList<Sitting> history, Sitting? open)
  {
    Directory.CreateDirectory(Dir);
    var sb = new StringBuilder();
    sb.AppendLine("# Session_Stopwatch sittings. One entry per sitting.");
    sb.AppendLine();

    var first = true;
    foreach (var sitting in history)
    {
      if (!first)
      {
        sb.AppendLine();
      }

      first = false;
      Append(sb, sitting);
    }

    if (open != null)
    {
      if (!first)
      {
        sb.AppendLine();
      }

      Append(sb, open);
    }

    File.WriteAllText(PathFile, sb.ToString(), Encoding.UTF8);
  }

  private static void Flush(List<Sitting> list, ref Sitting? current)
  {
    if (current != null)
    {
      list.Add(current);
      current = null;
    }
  }

  private static void Append(StringBuilder sb, Sitting sitting)
  {
    sb.AppendLine($"- date: {sitting.DateStamp}");
    sb.AppendLine($"  time: {sitting.TimeStamp}");
    sb.AppendLine($"  character: {Quote(sitting.Character)}");
    sb.AppendLine($"  world: {Quote(sitting.World)}");
    sb.AppendLine($"  active: {FormatDuration(sitting.ActiveSeconds)}");
    sb.AppendLine($"  inactive: {FormatDuration(sitting.InactiveSeconds)}");
    sb.AppendLine($"  total: {FormatDuration(sitting.TotalSeconds)}");
  }

  private static void Apply(Sitting sitting, string body)
  {
    var colon = body.IndexOf(':');
    if (colon < 0)
    {
      return;
    }

    var key = body.Substring(0, colon).Trim();
    var val = Unquote(body.Substring(colon + 1).Trim());
    switch (key)
    {
      case "date":
      case "time":
        MergeStart(sitting, key, val);
        break;
      case "character":
        sitting.Character = val;
        break;
      case "world":
        sitting.World = val;
        break;
      case "active":
        sitting.ActiveSeconds = ParseDuration(val);
        break;
      case "inactive":
        sitting.InactiveSeconds = ParseDuration(val);
        break;
    }
  }

  private static void MergeStart(Sitting sitting, string key, string val)
  {
    var d = sitting.StartedLocal == default ? DateTime.Now.Date : sitting.StartedLocal.Date;
    var t = sitting.StartedLocal == default ? TimeSpan.Zero : sitting.StartedLocal.TimeOfDay;
    if (key == "date" && DateTime.TryParseExact(val, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
    {
      d = day.Date;
    }

    if (key == "time" && DateTime.TryParseExact(val, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var clock))
    {
      t = clock.TimeOfDay;
    }

    sitting.StartedLocal = d + t;
  }

  internal static string FormatDuration(float seconds)
  {
    var sec = Math.Max(0, (int)seconds);
    var h = sec / 3600;
    var m = (sec % 3600) / 60;
    var s = sec % 60;
    return $"{h:D2}:{m:D2}:{s:D2}";
  }

  private static float ParseDuration(string val)
  {
    var parts = val.Split(':');
    if (parts.Length != 3)
    {
      return 0f;
    }

    if (!int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m) || !int.TryParse(parts[2], out var s))
    {
      return 0f;
    }

    return (h * 3600) + (m * 60) + s;
  }

  private static string Quote(string s)
  {
    if (string.IsNullOrEmpty(s))
    {
      return "\"\"";
    }

    if (s.IndexOfAny(new[] { ':', '#', '"', '\n' }) >= 0 || s.StartsWith(" ") || s.EndsWith(" "))
    {
      return "\"" + s.Replace("\"", "\\\"") + "\"";
    }

    return s;
  }

  private static string Unquote(string s)
  {
    if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
    {
      return s.Substring(1, s.Length - 2).Replace("\\\"", "\"");
    }

    return s;
  }
}
