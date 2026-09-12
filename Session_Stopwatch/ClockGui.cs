using UnityEngine;

namespace Session_Stopwatch;

/// <summary>
/// Separate floating pebble (always) + optional panel beside it (IMGUI).
/// Pebble is the park/drag/fold control; panel never moves the pebble.
/// </summary>
internal static class ClockGui
{
  private const int PebbleId = 0x53535731;
  private const int PanelId = 0x53535732;
  private const float Pebble = 48f;
  /// <summary>Content width: three 64px buttons + button margins + box pad (was 252 — fat L/R).</summary>
  private const float PanelW = 204f;
  private const float PanelH = 96f;
  private const float Gap = 6f;
  private const float RowH = 22f;
  private const float RowGap = 3f;
  /// <summary>Gap below minimap bottom (clockTest4: +100 from original 12).</summary>
  private const float MinimapGapY = 112f;

  private static readonly Color Bg = new(0.07f, 0.07f, 0.08f, 0.92f);
  private static readonly Color PlayOn = new(0.2f, 0.55f, 0.28f, 1f);
  private static readonly Color PauseOn = new(0.7f, 0.55f, 0.12f, 1f);
  private static readonly Color StopCol = new(0.65f, 0.22f, 0.18f, 1f);
  private static readonly Color Dim = new(0.22f, 0.2f, 0.18f, 1f);
  private static readonly Color Glyph = new(0.92f, 0.9f, 0.82f, 1f);

  private static Rect _pebble;
  private static Rect _panel;
  private static Vector2 _pebblePos;
  private static bool _pebblePosReady;
  private static bool _stylesReady;
  private static GUIStyle _box = null!;
  private static GUIStyle _label = null!;
  private static GUIStyle _time = null!;
  private static GUIStyle _btn = null!;
  private static GUIStyle _btnPlay = null!;
  private static GUIStyle _btnPause = null!;
  private static GUIStyle _btnStop = null!;
  private static GUIStyle _hint = null!;
  private static Texture2D? _texBg;
  private static Texture2D? _texDim;
  private static Texture2D? _texPlay;
  private static Texture2D? _texPause;
  private static Texture2D? _texStop;
  private static Texture2D? _texGlyph;
  private static Texture2D? _texRing;
  private static bool _dragged;
  private static Vector2 _posAtMouseDown;
  private static bool _minimapSnapDone;
  private static float _minimapSnapSince = -1f;

  internal static bool PointerOver { get; private set; }
  internal static bool Dragging { get; private set; }

  private static bool OffScreen(Vector2 pos)
  {
    return pos.x < -Pebble || pos.y < -Pebble
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
      _pebblePosReady = false;
      // Next world load starts expanded (F1 hint visible); park still from cfg.
      UiState.Folded = false;
      return;
    }

    EnsureStyles();
    EnsurePark();

    if (!_pebblePosReady)
    {
      var saved = Settings.HudPosition;
      _pebblePos = OffScreen(saved) || Settings.WantsMinimapSnap
        ? new Vector2(Mathf.Max(20f, Screen.width - 80f), 220f)
        : saved;
      _pebblePosReady = true;
    }

    _pebble = new Rect(_pebblePos.x, _pebblePos.y, Pebble, Pebble);

    var prev = GUI.backgroundColor;
    GUI.backgroundColor = Bg;
    _pebble = GUI.Window(PebbleId, _pebble, DrawPebbleWindow, GUIContent.none, _box);
    GUI.backgroundColor = prev;

    _pebblePos = _pebble.position;
    if (!Dragging)
    {
      PersistPebble(_pebblePos);
    }

    if (!UiState.Folded)
    {
      PlacePanelBesidePebble();
      GUI.backgroundColor = Bg;
      _panel = GUI.Window(PanelId, _panel, DrawPanelWindow, GUIContent.none, _box);
      GUI.backgroundColor = prev;
    }

    var screenMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
    PointerOver = _pebble.Contains(screenMouse) || (!UiState.Folded && _panel.Contains(screenMouse));
  }

  private static void EnsurePark()
  {
    if (_minimapSnapDone)
    {
      // CM Reset / typing -1 mid-session: allow another snap.
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
      _pebblePos = below.Value;
      _pebblePosReady = true;
      _minimapSnapDone = true;
      return;
    }

    if (Time.unscaledTime - _minimapSnapSince < 5f)
    {
      return;
    }

    _minimapSnapDone = true;
    if (!_pebblePosReady)
    {
      var fallback = new Vector2(Mathf.Max(20f, Screen.width - 80f), 220f);
      Settings.HudPosition = fallback;
      _pebblePos = fallback;
      _pebblePosReady = true;
      SessionStopwatchPlugin.LogAt(BepInEx.Logging.LogLevel.Info,
        "HUD park fallback (minimap not ready); drag the pebble if needed.");
    }
  }

  /// <summary>
  /// Align pebble <b>right edge</b> to small-minimap <b>right edge</b>
  /// (<see cref="Minimap.m_mapImageSmall"/> world corners: 3 = bottom-right).
  /// Writes real positive X/Y into cfg (so CM Reset to -1 then snap does not stay at -1).
  /// </summary>
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
    var pebbleX = mapRightX - Pebble;
    var pebbleY = mapBottomGuiY + MinimapGapY;
    var pos = new Vector2(
      Mathf.Clamp(pebbleX, 8f, Screen.width - Pebble - 8f),
      Mathf.Clamp(pebbleY, 8f, Screen.height - Pebble - 8f));

    SessionStopwatchPlugin.LogAt(BepInEx.Logging.LogLevel.Info,
      $"HUD park from minimap: mapRightX={mapRightX:0.#} mapBottomGuiY={mapBottomGuiY:0.#} " +
      $"pebbleTL=({pos.x:0},{pos.y:0}) pebbleRight={pos.x + Pebble:0} gapY={MinimapGapY} screen={Screen.width}x{Screen.height}.");
    return pos;
  }

  private static void PersistPebble(Vector2 pos)
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

  private static void PlacePanelBesidePebble()
  {
    var openLeft = _pebble.center.x >= Screen.width * 0.5f;
    _panel = openLeft
      ? new Rect(_pebble.x - Gap - PanelW, _pebble.y, PanelW, PanelH)
      : new Rect(_pebble.xMax + Gap, _pebble.y, PanelW, PanelH);
  }

  private static void DrawPebbleWindow(int id)
  {
    var e = Event.current;
    if (e != null && e.button == 0)
    {
      if (e.type == EventType.MouseDown)
      {
        _posAtMouseDown = _pebble.position;
        _dragged = false;
        Dragging = true;
      }
      else if (e.type == EventType.MouseDrag || e.type == EventType.MouseUp)
      {
        if ((_pebble.position - _posAtMouseDown).sqrMagnitude > 16f)
        {
          _dragged = true;
        }
      }

      if (e.type == EventType.MouseUp)
      {
        if (!_dragged)
        {
          UiState.Folded = !UiState.Folded;
          SessionStopwatchPlugin.LogAt(BepInEx.Logging.LogLevel.Debug,
            UiState.Folded ? "HUD folded." : "HUD unfolded.");
        }

        Dragging = false;
        PersistPebble(_pebble.position);
        e.Use();
      }
    }

    DrawClockGlyph(new Rect(6f, 6f, Pebble - 12f, Pebble - 12f));
    GUI.DragWindow(new Rect(0f, 0f, Pebble, Pebble));
  }

  private static void DrawClockGlyph(Rect area)
  {
    if (_texRing != null)
    {
      GUI.DrawTexture(area, _texRing);
    }

    if (_texGlyph == null)
    {
      return;
    }

    var cx = area.x + area.width * 0.5f;
    var cy = area.y + area.height * 0.5f;
    var sec = (int)Clock.ActiveSeconds;
    var ang = (sec % 60) / 60f * 360f;
    var matrix = GUI.matrix;
    GUIUtility.RotateAroundPivot(ang, new Vector2(cx, cy));
    GUI.DrawTexture(new Rect(cx - 1f, cy - area.height * 0.32f, 2f, area.height * 0.32f), _texGlyph);
    GUI.matrix = matrix;
    GUI.DrawTexture(new Rect(cx - 2f, cy - 2f, 4f, 4f), _texGlyph);
  }

  private static void DrawPanelWindow(int id)
  {
    GUILayout.BeginVertical();

    GUILayout.BeginHorizontal(GUILayout.Height(RowH));
    GUILayout.FlexibleSpace();
    GUILayout.Label(Clock.ActiveDisplay, _time, GUILayout.Height(RowH));
    GUILayout.FlexibleSpace();
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal(GUILayout.Height(RowH));
    GUILayout.FlexibleSpace();
    var ticking = Clock.IsTicking;
    var paused = Clock.HasSitting && !ticking;
    if (GUILayout.Button("►", ticking ? _btnPlay : _btn, GUILayout.Height(RowH), GUILayout.Width(64f)))
    {
      if (!Clock.HasSitting || Clock.ManualPaused || !Clock.IsTicking)
      {
        Clock.Start();
      }
    }

    if (GUILayout.Button("||", paused ? _btnPause : _btn, GUILayout.Height(RowH), GUILayout.Width(64f)))
    {
      Clock.Pause();
    }

    if (GUILayout.Button("■", _btnStop, GUILayout.Height(RowH), GUILayout.Width(64f)))
    {
      Clock.Stop("stop");
    }

    GUILayout.FlexibleSpace();
    GUILayout.EndHorizontal();

    GUILayout.Space(RowGap);

    GUILayout.BeginHorizontal(GUILayout.Height(RowH));
    GUILayout.FlexibleSpace();
    GUILayout.Label("AFK timer", _label, GUILayout.Width(70f), GUILayout.Height(RowH));
    if (GUILayout.Button("-", _btn, GUILayout.Width(22f), GUILayout.Height(RowH)))
    {
      NudgeAfk(-Settings.AfkStepSeconds);
    }

    GUILayout.Label(SittingLog.FormatDuration(Settings.AfkTimeoutSeconds), _label,
      GUILayout.Width(64f), GUILayout.Height(RowH));
    if (GUILayout.Button("+", _btn, GUILayout.Width(22f), GUILayout.Height(RowH)))
    {
      NudgeAfk(Settings.AfkStepSeconds);
    }

    GUILayout.FlexibleSpace();
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal(GUILayout.Height(RowH));
    GUILayout.FlexibleSpace();
    GUILayout.Label("Ctrl+F1 shows the cursor", _hint, GUILayout.Height(RowH));
    GUILayout.FlexibleSpace();
    GUILayout.EndHorizontal();

    GUILayout.EndVertical();
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
    _texGlyph = Solid(Glyph);
    _texRing = BakeRing(Glyph);

    // box (not window) — avoids IMGUI title-bar dead space that inflated bottom/side margins.
    _box = new GUIStyle(GUI.skin.box)
    {
      normal = { background = _texBg, textColor = Color.white },
      onNormal = { background = _texBg, textColor = Color.white },
      border = new RectOffset(1, 1, 1, 1),
      padding = new RectOffset(3, 3, 3, 3),
      margin = new RectOffset(0, 0, 0, 0),
      overflow = new RectOffset(0, 0, 0, 0)
    };

    _label = new GUIStyle(GUI.skin.label)
    {
      alignment = TextAnchor.MiddleCenter,
      fontSize = 12,
      padding = new RectOffset(0, 0, 0, 0),
      margin = new RectOffset(0, 0, 0, 0),
      normal = { textColor = new Color(0.9f, 0.88f, 0.8f) }
    };

    _time = new GUIStyle(_label) { fontSize = 16, fontStyle = FontStyle.Bold };

    _hint = new GUIStyle(_label)
    {
      fontSize = 12,
      fontStyle = FontStyle.Bold,
      normal = { textColor = new Color(0.92f, 0.88f, 0.55f) }
    };

    _btn = ButtonStyle(_texDim, Color.white);
    _btnPlay = ButtonStyle(_texPlay, Color.white);
    _btnPause = ButtonStyle(_texPause, Color.white);
    _btnStop = ButtonStyle(_texStop, Color.white);
    _stylesReady = true;
  }

  private static GUIStyle ButtonStyle(Texture2D? bg, Color text)
  {
    return new GUIStyle(GUI.skin.button)
    {
      fontSize = 13,
      fontStyle = FontStyle.Bold,
      alignment = TextAnchor.MiddleCenter,
      normal = { background = bg, textColor = text },
      hover = { background = bg, textColor = text },
      active = { background = bg, textColor = text },
      onNormal = { background = bg, textColor = text },
      padding = new RectOffset(0, 0, 0, 0),
      margin = new RectOffset(1, 1, 0, 0)
    };
  }

  private static Texture2D Solid(Color c)
  {
    var t = new Texture2D(2, 2, TextureFormat.ARGB32, false);
    t.SetPixels(new[] { c, c, c, c });
    t.Apply();
    return t;
  }

  private static Texture2D BakeRing(Color c)
  {
    const int n = 64;
    var t = new Texture2D(n, n, TextureFormat.ARGB32, false);
    var clear = new Color(0, 0, 0, 0);
    var cx = (n - 1) * 0.5f;
    var outer = cx - 1f;
    var inner = outer - 3.5f;
    for (var y = 0; y < n; y++)
    {
      for (var x = 0; x < n; x++)
      {
        var dx = x - cx;
        var dy = y - cx;
        var r = Mathf.Sqrt((dx * dx) + (dy * dy));
        t.SetPixel(x, y, r <= outer && r >= inner ? c : clear);
      }
    }

    t.Apply();
    return t;
  }
}
