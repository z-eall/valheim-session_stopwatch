using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;

namespace Session_Stopwatch;

internal static class SittingLog
{
  private static readonly string Dir = Path.Combine(Paths.ConfigPath, "session_stopwatch");
  private static readonly string FilePath = Path.Combine(Dir, "session_stopwatch_log.yaml");

  private const string Sep = " · ";
  private const int EntryRows = 3;

  internal static string PathFile => FilePath;

  internal static List<Sitting> LoadHistory()
  {
    var list = new List<Sitting>();
    if (!File.Exists(FilePath))
    {
      return list;
    }

    Sitting? current = null;
    var row = EntryRows;
    foreach (var raw in File.ReadAllLines(FilePath))
    {
      var line = raw.TrimEnd();
      if (line.Length == 0 || line[0] == '#')
      {
        continue;
      }

      if (line.StartsWith("- date:", StringComparison.Ordinal))
      {
        Flush(list, ref current);
        current = new Sitting();
        Apply(current, line.Substring(2));
        row = EntryRows;
        continue;
      }

      if (line.StartsWith("  ", StringComparison.Ordinal))
      {
        if (current != null)
        {
          Apply(current, line.TrimStart());
        }

        continue;
      }

      if (current == null || row >= EntryRows)
      {
        Flush(list, ref current);
        current = new Sitting();
        ParseHeader(current, line);
        row = 1;
        continue;
      }

      row++;
      if (row == 2)
      {
        ParseStartRow(current, line);
      }
      else
      {
        ParseTimeRow(current, line);
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

    File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
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
    sb.AppendLine($"{sitting.Character} ({sitting.World})");
    sb.AppendLine($"{sitting.DateStamp} {sitting.TimeStamp}{Sep}AFK after {FormatAfk(sitting.AfkSeconds)}");
    sb.AppendLine($"Active {FormatDuration(sitting.ActiveSeconds)}{Sep}Inactive {FormatDuration(sitting.InactiveSeconds)}{Sep}Total {FormatDuration(sitting.TotalSeconds)}");
  }

  private static void ParseHeader(Sitting sitting, string line)
  {
    var open = line.EndsWith(")", StringComparison.Ordinal) ? line.LastIndexOf(" (", StringComparison.Ordinal) : -1;
    if (open < 0)
    {
      sitting.Character = line;
      return;
    }

    sitting.Character = line.Substring(0, open);
    sitting.World = line.Substring(open + 2, line.Length - open - 3);
  }

  private static void ParseStartRow(Sitting sitting, string line)
  {
    var parts = line.Split(new[] { Sep }, StringSplitOptions.None);
    var start = parts[0].Split(' ');
    MergeStart(sitting, "date", start[0]);
    if (start.Length > 1)
    {
      MergeStart(sitting, "time", start[1]);
    }

    if (parts.Length > 1)
    {
      sitting.AfkSeconds = ParseAfk(parts[1].Substring(parts[1].LastIndexOf(' ') + 1));
    }
  }

  private static void ParseTimeRow(Sitting sitting, string line)
  {
    foreach (var part in line.Split(new[] { Sep }, StringSplitOptions.None))
    {
      var space = part.IndexOf(' ');
      if (space < 0)
      {
        continue;
      }

      var val = ParseDuration(part.Substring(space + 1));
      if (part.StartsWith("Active", StringComparison.Ordinal))
      {
        sitting.ActiveSeconds = val;
      }
      else if (part.StartsWith("Inactive", StringComparison.Ordinal))
      {
        sitting.InactiveSeconds = val;
      }
    }
  }

  private static string FormatAfk(int seconds)
  {
    return seconds <= 0 ? "Off" : $"{seconds / 60:00}:{seconds % 60:00}";
  }

  private static int ParseAfk(string val)
  {
    var parts = val.Split(':');
    return parts.Length == 2 && int.TryParse(parts[0], out var m) && int.TryParse(parts[1], out var s) ? (m * 60) + s : 0;
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

    if (key == "time")
    {
      var hm = val;
      var sp = val.IndexOf(' ');
      if (sp > 0)
      {
        hm = val.Substring(0, sp);
      }

      if (DateTime.TryParseExact(hm, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var clock))
      {
        t = clock.TimeOfDay;
      }
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

  private static string Unquote(string s)
  {
    if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
    {
      return s.Substring(1, s.Length - 2).Replace("\\\"", "\"");
    }

    return s;
  }
}
