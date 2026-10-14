using UnityEngine;

namespace Session_Stopwatch;

internal static class ClockGui
{
  private const int WindowId = 0x53535731;

  private const float W = 170f;
  private const float Pad = 5f;
  private const float TimerH = 28f;
  private const float Btn = 22f;
  private const float RowGap = 4f;
  private const float OptionsH = (2f * Btn) + (2f * RowGap);
  private const float CollapsedH = TimerH + (2f * Pad);
  private const float ExpandedH = CollapsedH + OptionsH;

  private const float MinimapGapY = 112f;

  private static readonly Color Bg = new(0.145f, 0.145f, 0.145f, 0.85f);
  private static readonly Color PlayOn = new(0.561f, 0.682f, 0.416f, 1f);
  private static readonly Color PauseOn = new(0.808f, 0.675f, 0.322f, 1f);
  private static readonly Color StopCol = new(0.647f, 0.278f, 0.306f, 1f);
  private static readonly Color AutoOn = new(0.302f, 0.439f, 0.773f, 1f);
  private static readonly Color Dim = new(0.360f, 0.360f, 0.360f, 1f);
  private static readonly Color TimerBg = new(0.360f, 0.360f, 0.360f, 1f);
  private static readonly Color TextCol = new(0.949f, 0.949f, 0.949f, 1f);
  private static readonly Color Text2Col = new(0.604f, 0.604f, 0.604f, 1f);
  private static readonly Color DarkOnGreen = new(0.11f, 0.14f, 0.06f, 1f);
  private static readonly Color DarkOnGold = new(0.11f, 0.086f, 0.031f, 1f);

  private static Rect _window;
  private static Vector2 _pos;
  private static bool _posReady;
  private static bool _flipUp;
  private static bool _stylesReady;
  private static GUIStyle _box = null!;
  private static GUIStyle _label = null!;
  private static GUIStyle _value = null!;
  private static GUIStyle _time = null!;
  private static GUIStyle _btn = null!;
  private static GUIStyle _btnPlay = null!;
  private static GUIStyle _btnPause = null!;
  private static GUIStyle _btnStop = null!;
  private static GUIStyle _btnAuto = null!;
  private static GUIStyle _btnAfk = null!;
  private static GUIStyle _btnAs = null!;
  private static GUIStyle _tipBox = null!;
  private static GUIStyle _tipText = null!;
  private static string _tipNext = "";
  private static GUIStyle _hint = null!;
  private static Texture2D? _texTriangle;
  private static Texture2D? _texBg;
  private static Texture2D? _texDim;
  private static Texture2D? _texPlay;
  private static Texture2D? _texPause;
  private static Texture2D? _texStop;
  private static Texture2D? _texAuto;
  private static Texture2D? _texTimerBg;
  private static bool _menuWasOpen;
  private static bool _foldBeforeMenu;
  private static bool _toggledInMenu;
  private static bool _dragged;
  private static Vector2 _posAtMouseDown;
  private static bool _minimapSnapDone;
  private static float _minimapSnapSince = -1f;

  internal static bool PointerOver { get; private set; }
  internal static bool Dragging { get; private set; }

  private static bool OffScreen(Vector2 pos)
  {
    return pos.x < -W || pos.y < -CollapsedH
      || pos.x > Screen.width || pos.y > Screen.height;
  }

  internal static void Draw()
  {
    if (Player.m_localPlayer == null || Hud.instance == null || Hud.IsUserHidden())
    {
      PointerOver = false;
      Dragging = false;
      _minimapSnapDone = false;
      _minimapSnapSince = -1f;
      _posReady = false;
      UiState.Folded = Settings.StartCollapsed;
      _menuWasOpen = false;
      _toggledInMenu = false;
      return;
    }

    TrackMenu();

    EnsureStyles();
    EnsurePark();

    if (!_posReady)
    {
      var saved = Settings.HudPosition;
      _pos = OffScreen(saved) || Settings.WantsMinimapSnap
        ? new Vector2(Mathf.Max(20f, Screen.width - W - 20f), 220f)
        : new Vector2(Mathf.Clamp(saved.x, 0f, Screen.width - W), saved.y);
      _posReady = true;
    }

    if (!Dragging)
    {
      _flipUp = _pos.y + ExpandedH > Screen.height - 8f && _pos.y - OptionsH >= 0f;
    }

    var up = !UiState.Folded && _flipUp;
    _window = new Rect(
      _pos.x,
      up ? _pos.y - OptionsH : _pos.y,
      W,
      UiState.Folded ? CollapsedH : ExpandedH);

    var tip = _tipNext;
    _tipNext = "";
    _window = GUI.Window(WindowId, _window, DrawWindow, GUIContent.none, _box);
    DrawTooltip(tip);

    _pos = new Vector2(_window.x, up ? _window.y + OptionsH : _window.y);
    if (!Dragging)
    {
      Persist(_pos);
    }

    var screenMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
    PointerOver = _window.Contains(screenMouse);
  }

  private static void TrackMenu()
  {
    var open = Menu.IsVisible();
    if (open == _menuWasOpen)
    {
      return;
    }

    if (open)
    {
      _foldBeforeMenu = UiState.Folded;
      _toggledInMenu = false;
      UiState.Folded = false;
    }
    else if (!_toggledInMenu)
    {
      UiState.Folded = _foldBeforeMenu;
    }

    _menuWasOpen = open;
    SessionStopwatchPlugin.LogAt(BepInEx.Logging.LogLevel.Debug,
      open ? "Esc menu opened: panel expanded." : "Esc menu closed.");
  }

  private static void EnsurePark()
  {
    if (_minimapSnapDone)
    {
      if (Settings.WantsMinimapSnap)
      {
        _minimapSnapDone = false;
        _minimapSnapSince = -1f;
      }
      else
      {
        return;
      }
    }

    var saved = Settings.HudPosition;
    var needSnap = Settings.WantsMinimapSnap || OffScreen(saved);
    if (!needSnap)
    {
      _minimapSnapDone = true;
      return;
    }

    if (_minimapSnapSince < 0f)
    {
      _minimapSnapSince = Time.unscaledTime;
    }

    var below = TryBelowMinimap();
    if (below.HasValue)
    {
      Settings.HudPosition = below.Value;
      _pos = below.Value;
      _posReady = true;
      _minimapSnapDone = true;
      return;
    }

    if (Time.unscaledTime - _minimapSnapSince < 5f)
    {
      return;
    }

    _minimapSnapDone = true;
    if (!_posReady)
    {
      var fallback = new Vector2(Mathf.Max(20f, Screen.width - W - 20f), 220f);
      Settings.HudPosition = fallback;
      _pos = fallback;
      _posReady = true;
      SessionStopwatchPlugin.LogAt(BepInEx.Logging.LogLevel.Info,
        "HUD park fallback (minimap not ready); drag the timer if needed.");
    }
  }

  private static Vector2? TryBelowMinimap()
  {
    if (Minimap.instance == null || Minimap.instance.m_mapImageSmall == null)
    {
      return null;
    }

    var map = Minimap.instance.m_mapImageSmall.rectTransform;
    if (!map.gameObject.activeInHierarchy)
    {
      return null;
    }

    var corners = new Vector3[4];
    map.GetWorldCorners(corners);
    var mapRightX = corners[3].x;
    var mapBottomGuiY = Screen.height - corners[0].y;
    var x = mapRightX - W;
    var y = mapBottomGuiY + MinimapGapY;
    var pos = new Vector2(
      Mathf.Clamp(x, 8f, Screen.width - W - 8f),
      Mathf.Clamp(y, 8f, Screen.height - CollapsedH - 8f));

    SessionStopwatchPlugin.LogAt(BepInEx.Logging.LogLevel.Info,
      $"HUD park from minimap: mapRightX={mapRightX:0.#} mapBottomGuiY={mapBottomGuiY:0.#} " +
      $"timerTL=({pos.x:0},{pos.y:0}) timerRight={pos.x + W:0} gapY={MinimapGapY} screen={Screen.width}x{Screen.height}.");
    return pos;
  }

  private static void Persist(Vector2 pos)
  {
    if (!_minimapSnapDone && (Settings.WantsMinimapSnap || OffScreen(Settings.HudPosition)))
    {
      return;
    }

    var cur = Settings.HudPosition;
    if (!Mathf.Approximately(cur.x, pos.x) || !Mathf.Approximately(cur.y, pos.y))
    {
      Settings.HudPosition = pos;
    }
  }

  private static void DrawWindow(int id)
  {
    var up = !UiState.Folded && _flipUp;
    var timerY = up ? Pad + OptionsH : Pad;

    var timerRect = new Rect(Pad, timerY, W - (2f * Pad), TimerH);
    GUI.DrawTexture(timerRect, _texTimerBg);
    GUI.Label(timerRect, Clock.ActiveDisplay, _time);

    if (!UiState.Folded)
    {
      DrawOptions(up ? Pad : Pad + TimerH + RowGap);
    }

    var whole = new Rect(0f, 0f, _window.width, _window.height);
    HandleMouse(whole);
    GUI.DragWindow(whole);
  }

  private static void HandleMouse(Rect area)
  {
    var e = Event.current;
    if (e == null || e.button != 0)
    {
      return;
    }

    if (e.type == EventType.MouseDown && area.Contains(e.mousePosition))
    {
      _posAtMouseDown = _window.position;
      _dragged = false;
      Dragging = true;
      return;
    }

    if (!Dragging)
    {
      return;
    }

    if (e.type == EventType.MouseDrag || e.type == EventType.MouseUp)
    {
      if ((_window.position - _posAtMouseDown).sqrMagnitude > 16f)
      {
        _dragged = true;
      }
    }

    if (e.type == EventType.MouseUp)
    {
      if (!_dragged)
      {
        UiState.Folded = !UiState.Folded;
        _toggledInMenu = _menuWasOpen;
        SessionStopwatchPlugin.LogAt(BepInEx.Logging.LogLevel.Debug,
          UiState.Folded ? "HUD folded." : "HUD unfolded.");
      }

      Dragging = false;
      e.Use();
    }
  }

  private static void DrawOptions(float top)
  {
    const float AfkW = 44f;
    var left = Pad;
    var right = W - Pad;
    var gap = (right - left - (4f * Btn) - AfkW) / 4f;
    var pitch = Btn + gap;

    var y = top;
    var ticking = Clock.IsTicking;
    var hasSitting = Clock.HasSitting;
    var paused = hasSitting && !ticking;

    var startRect = new Rect(left, y, Btn, Btn);
    var startTip = ticking ? "Timer is running" : paused ? "Resume the timer" : "Start the timer";
    if (GUI.Button(startRect, GUIContent.none, ticking ? _btnPlay : _btn))
    {
      if (!hasSitting || Clock.ManualPaused || !ticking)
      {
        Clock.Start();
      }
    }

    DrawGlyph(startRect, 0, ticking ? DarkOnGreen : TextCol);
    Hover(startRect, startTip);

    var pauseRect = new Rect(Mathf.Round(left + pitch), y, Btn, Btn);
    var pauseTip = ticking ? "Pause the timer" : paused ? "Timer is paused" : "Nothing to pause";
    if (GUI.Button(pauseRect, GUIContent.none, paused ? _btnPause : _btn))
    {
      Clock.Pause();
    }

    DrawGlyph(pauseRect, 1, paused ? DarkOnGold : TextCol);
    Hover(pauseRect, pauseTip);

    var stopRect = new Rect(Mathf.Round(left + (2f * pitch)), y, Btn, Btn);
    var stopTip = hasSitting ? "Stop and save this sitting" : "Nothing to stop";
    if (GUI.Button(stopRect, GUIContent.none, _btnStop))
    {
      Clock.Stop("stop");
    }

    DrawGlyph(stopRect, 2, TextCol);
    Hover(stopRect, stopTip);

    var autoStart = Settings.AutoStart;
    var autoRect = new Rect(Mathf.Round(left + (3f * pitch)), y, Btn, Btn);
    var autoTip = autoStart ? "Auto start is on. Click to turn off" : "Auto start is off. Click to turn on";
    if (GUI.Button(autoRect, "AS", autoStart ? _btnAuto : _btnAs))
    {
      Settings.AutoStart = !autoStart;
    }

    Hover(autoRect, autoTip);

    var afk = Settings.AfkTimeoutSeconds;
    var afkRect = new Rect(right - AfkW, y, AfkW, Btn);
    var afkTip = afk == 0
      ? "AFK check is off. Scroll to set the time"
      : $"AFK after {FormatAfk(afk)} idle. Scroll to change";
    GUI.Button(afkRect, FormatAfk(afk), _btnAfk);
    Hover(afkRect, afkTip);

    var e = Event.current;
    if (e != null && e.type == EventType.ScrollWheel && afkRect.Contains(e.mousePosition))
    {
      NudgeAfk(e.delta.y < 0f ? Settings.AfkStepSeconds : -Settings.AfkStepSeconds);
      e.Use();
    }

    y += Btn + RowGap;
    GUI.Label(new Rect(left, y, W - (2f * Pad), Btn), "Ctrl+F1 shows the cursor", _hint);
  }

  private static void Hover(Rect rect, string tip)
  {
    var e = Event.current;
    if (e != null && rect.Contains(e.mousePosition))
    {
      _tipNext = tip;
    }
  }

  private static void DrawGlyph(Rect btn, int kind, Color color)
  {
    var cx = btn.x + (Btn / 2f);
    var cy = btn.y + (Btn / 2f);
    var prev = GUI.color;
    GUI.color = color;
    if (kind == 0)
    {
      GUI.DrawTexture(new Rect(cx - 4f, cy - 6f, 10f, 12f), _texTriangle);
    }
    else if (kind == 1)
    {
      GUI.DrawTexture(new Rect(cx - 4f, cy - 4f, 3f, 8f), Texture2D.whiteTexture);
      GUI.DrawTexture(new Rect(cx + 1f, cy - 4f, 3f, 8f), Texture2D.whiteTexture);
    }
    else
    {
      GUI.DrawTexture(new Rect(cx - 5f, cy - 5f, 10f, 10f), Texture2D.whiteTexture);
    }

    GUI.color = prev;
  }

  private static void DrawTooltip(string tip)
  {
    var id = WindowId + 1;
    if (string.IsNullOrEmpty(tip) || Event.current == null)
    {
      return;
    }

    var mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
    var size = _tipText.CalcSize(new GUIContent(tip)) + new Vector2(12f, 6f);
    var x = mouse.x + 16f;
    if (x + size.x > Screen.width - 4f)
    {
      x = mouse.x - 12f - size.x;
    }

    var y = mouse.y + 20f;
    if (y + size.y > Screen.height - 4f)
    {
      y = mouse.y - 8f - size.y;
    }

    GUI.Window(id, new Rect(x, y, size.x, size.y), _ =>
    {
      GUI.Label(new Rect(6f, 3f, size.x - 12f, size.y - 6f), tip, _tipText);
    }, GUIContent.none, _tipBox);
    GUI.BringWindowToFront(id);
  }

  private static string FormatAfk(int seconds)
  {
    return seconds <= 0 ? "OFF" : $"{seconds / 60:00}:{seconds % 60:00}";
  }

  private static void NudgeAfk(int delta)
  {
    var next = Settings.AfkTimeoutSeconds + delta;
    next = Mathf.RoundToInt(next / (float)Settings.AfkStepSeconds) * Settings.AfkStepSeconds;
    Settings.AfkTimeoutSeconds = next;
  }

  private static void EnsureStyles()
  {
    if (_stylesReady)
    {
      return;
    }

    _texBg = Solid(Bg);
    _texDim = Solid(Dim);
    _texPlay = Solid(PlayOn);
    _texPause = Solid(PauseOn);
    _texStop = Solid(StopCol);
    _texAuto = Solid(AutoOn);
    _texTimerBg = Solid(TimerBg);
    _texTriangle = Triangle();

    _box = new GUIStyle(GUI.skin.box)
    {
      normal = { background = _texBg, textColor = Color.white },
      onNormal = { background = _texBg, textColor = Color.white },
      border = new RectOffset(1, 1, 1, 1),
      padding = new RectOffset(0, 0, 0, 0),
      margin = new RectOffset(0, 0, 0, 0),
      overflow = new RectOffset(0, 0, 0, 0)
    };

    _label = new GUIStyle(GUI.skin.label)
    {
      alignment = TextAnchor.MiddleLeft,
      fontSize = 12,
      padding = new RectOffset(0, 0, 0, 0),
      margin = new RectOffset(0, 0, 0, 0),
      normal = { textColor = TextGamma(TextCol) }
    };

    _value = new GUIStyle(_label) { alignment = TextAnchor.MiddleRight };

    _time = new GUIStyle(_label)
    {
      fontSize = 18,
      fontStyle = FontStyle.Bold,
      alignment = TextAnchor.MiddleCenter
    };

    _hint = new GUIStyle(_label)
    {
      fontSize = 11,
      alignment = TextAnchor.MiddleCenter,
      normal = { textColor = TextGamma(PauseOn) }
    };

    _tipBox = new GUIStyle(_box)
    {
      normal = { background = Solid(new Color(0f, 0f, 0f, 0.92f)), textColor = Color.white },
      onNormal = { background = Solid(new Color(0f, 0f, 0f, 0.92f)), textColor = Color.white }
    };

    _tipText = new GUIStyle(GUI.skin.label)
    {
      fontSize = 11,
      alignment = TextAnchor.MiddleLeft,
      wordWrap = false,
      padding = new RectOffset(0, 0, 0, 0),
      margin = new RectOffset(0, 0, 0, 0),
      normal = { textColor = TextGamma(Color.white) }
    };

    _btn = ButtonStyle(_texDim, TextCol);
    _btnPlay = ButtonStyle(_texPlay, DarkOnGreen);
    _btnPause = ButtonStyle(_texPause, DarkOnGold);
    _btnStop = ButtonStyle(_texStop, TextCol);
    _btnAuto = ButtonStyle(_texAuto, TextCol, 9);
    _btnAs = ButtonStyle(_texDim, TextCol, 9);
    _btnAfk = ButtonStyle(_texDim, PauseOn);
    _stylesReady = true;
  }

  private static Color TextGamma(Color c)
  {
    return c.gamma;
  }

  private static GUIStyle ButtonStyle(Texture2D? bg, Color text, int fontSize = 12)
  {
    text = TextGamma(text);
    return new GUIStyle(GUI.skin.button)
    {
      fontSize = fontSize,
      fontStyle = FontStyle.Bold,
      alignment = TextAnchor.MiddleCenter,
      normal = { background = bg, textColor = text },
      hover = { background = bg, textColor = text },
      active = { background = bg, textColor = text },
      onNormal = { background = bg, textColor = text },
      padding = new RectOffset(0, 0, 0, 0),
      margin = new RectOffset(0, 0, 0, 0)
    };
  }

  private static Texture2D Triangle()
  {
    const int N = 32;
    var t = new Texture2D(N, N, TextureFormat.ARGB32, false) { filterMode = FilterMode.Bilinear };
    var px = new Color[N * N];
    for (var yy = 0; yy < N; yy++)
    {
      var limit = N * (1f - (Mathf.Abs(yy + 0.5f - (N / 2f)) / (N / 2f)));
      for (var xx = 0; xx < N; xx++)
      {
        px[(yy * N) + xx] = xx + 0.5f <= limit ? Color.white : new Color(1f, 1f, 1f, 0f);
      }
    }

    t.SetPixels(px);
    t.Apply();
    return t;
  }

  private static Texture2D Solid(Color c)
  {
    var t = new Texture2D(2, 2, TextureFormat.ARGB32, false);
    t.SetPixels(new[] { c, c, c, c });
    t.Apply();
    return t;
  }
}
