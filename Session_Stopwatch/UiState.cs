namespace Session_Stopwatch;

/// <summary>
/// Session-only fold. Starts expanded each world load so Ctrl+F1 hint is visible;
/// park X/Y still come from cfg.
/// </summary>
internal static class UiState
{
  internal static bool Folded { get; set; }
}
