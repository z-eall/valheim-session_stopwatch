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
  private const float PanelW = 268f;
  private const float PanelH = 128f;
  private const float Gap = 6f;
  private const float MinimapMargin = 12f;

  private static readonly Color Bg = new(0.07f, 0.07f, 0.08f, 0.92f);
  private static readonly Color PlayOn = new(0.2f, 0.55f, 0.28f, 1f);
  private static readonly Color PauseOn = new(0.7f, 0.55f, 0.12f, 1f);
  private static readonly Color StopCol = new(0.65f, 0.22f, 0.18f, 1f);
  private static readonly Color Dim = new(0.22f, 0.2f, 0.18f, 1f);

  private static Rect _pebble;
  private static Rect _panel;
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
  private static bool _dragged;
  private static Vector2 _posAtMouseDown;
  private static bool _defaultParkTried;
  private static float _defaultParkSince = -1f;

  internal static bool PointerOver { get; private set; }
  internal static bool Dragging { get; private set; }

  internal static void Draw()
  {
    if (Player.m_localPlayer == null || Hud.instance == null || Hud.IsUserHidden())
    {
      PointerOver = false;
      Dragging = false;
      return;
    }

    EnsureStyles();
    EnsureDefaultPark();
    if (Settings.NeedsDefaultPark)
    {
      // Waiting for minimap (or fallback) — avoid parking on top of it at a stale cfg.
      PointerOver = false;
      Dragging = false;
      return;
    }

    var pos = Settings.HudPosition;
    _pebble = new Rect(pos.x, pos.y, Pebble, Pebble);

    var prev = GUI.backgroundColor;
    GUI.backgroundColor = Bg;
    _pebble = GUI.Window(PebbleId, _pebble, DrawPebbleWindow, GUIContent.none, _box);
    GUI.backgroundColor = prev;

    PersistPebble(_pebble.position);

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

  private static void EnsureDefaultPark()
  {
    if (!Settings.NeedsDefaultPark)
    {
      return;
    }

    if (_defaultParkSince < 0f)
    {
      _defaultParkSince = Time.unscaledTime;
    }

    var below = TryBelowMinimap();
    if (below.HasValue)
    {
      Settings.HudPosition = below.Value;
      SessionStopwatchPlugin.LogAt(BepInEx.Logging.LogLevel.Info,
        $"HUD parked below minimap at ({below.Value.x:0},{below.Value.y:0}).");
      return;
    }

    // Minimap not ready yet — wait a few seconds, then fall back once.
    if (_defaultParkTried || Time.unscaledTime - _defaultParkSince < 5f)
    {
      return;
    }

    _defaultParkTried = true;
    Settings.HudPosition = new Vector2(Mathf.Max(20f, Screen.width - 80f), 220f);
    SessionStopwatchPlugin.LogAt(BepInEx.Logging.LogLevel.Info,
      "HUD park fallback (minimap not ready); drag the pebble if needed.");
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
    // Overlay canvas: world Y is screen Y from bottom.
    var midX = (corners[0].x + corners[3].x) * 0.5f - Pebble * 0.5f;
    var bottomGuiY = Screen.height - corners[0].y + MinimapMargin;
    return new Vector2(Mathf.Clamp(midX, 8f, Screen.width - Pebble - 8f),
      Mathf.Clamp(bottomGuiY, 8f, Screen.height - Pebble - 8f));
  }

  private static void PersistPebble(Vector2 pos)
  {
    // Do not lock in a transient rect while we are still waiting to snap below the minimap.
    if (Settings.NeedsDefaultPark)
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
        e.Use();
      }
    }

    var blink = Clock.IsTicking && Mathf.Repeat(Time.unscaledTime, 2f) >= 1f;
    var old = GUI.color;
    if (blink)
    {
      GUI.color = new Color(1f, 1f, 1f, 0.35f);
    }

    GUI.Label(new Rect(4f, 4f, Pebble - 8f, Pebble - 8f), "S", _btn);
    GUI.color = old;
    // Whole pebble is the drag handle; click vs drag decided on MouseUp above.
    GUI.DragWindow(new Rect(0f, 0f, Pebble, Pebble));
  }

  private static void DrawPanelWindow(int id)
  {
    GUILayout.Space(2f);
    GUILayout.BeginHorizontal();
    GUILayout.FlexibleSpace();
    GUILayout.Label(Clock.ActiveDisplay, _time);
    GUILayout.FlexibleSpace();
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    var ticking = Clock.IsTicking;
    var paused = Clock.HasSitting && !ticking;
    if (GUILayout.Button("►", ticking ? _btnPlay : _btn, GUILayout.Height(28f), GUILayout.Width(72f)))
    {
      if (!Clock.HasSitting || Clock.ManualPaused || !Clock.IsTicking)
      {
        Clock.Start();
      }
    }

    if (GUILayout.Button("||", paused ? _btnPause : _btn, GUILayout.Height(28f), GUILayout.Width(72f)))
    {
      Clock.Pause();
    }

    if (GUILayout.Button("■", _btnStop, GUILayout.Height(28f), GUILayout.Width(72f)))
    {
      Clock.Stop();
    }

    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    GUILayout.Label("AFK timer", _label, GUILayout.Width(78f));
    if (GUILayout.Button("-", _btn, GUILayout.Width(28f), GUILayout.Height(22f)))
    {
      NudgeAfk(-Settings.AfkStepSeconds);
    }

    GUILayout.Label(SittingLog.FormatDuration(Settings.AfkTimeoutSeconds), _label, GUILayout.Width(72f));
    if (GUILayout.Button("+", _btn, GUILayout.Width(28f), GUILayout.Height(22f)))
    {
      NudgeAfk(Settings.AfkStepSeconds);
    }

    GUILayout.EndHorizontal();

    GUILayout.Label("Ctrl+F1 shows the cursor", _hint);
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

    _box = new GUIStyle(GUI.skin.window)
    {
      normal = { background = _texBg, textColor = Color.white },
      onNormal = { background = _texBg, textColor = Color.white },
      border = new RectOffset(6, 6, 6, 6),
      padding = new RectOffset(6, 6, 6, 6)
    };

    _label = new GUIStyle(GUI.skin.label)
    {
      alignment = TextAnchor.MiddleCenter,
      fontSize = 13,
      normal = { textColor = new Color(0.9f, 0.88f, 0.8f) }
    };

    _time = new GUIStyle(_label) { fontSize = 18, fontStyle = FontStyle.Bold };

    _hint = new GUIStyle(_label)
    {
      fontSize = 11,
      normal = { textColor = new Color(0.65f, 0.62f, 0.55f) }
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
      fontSize = 16,
      fontStyle = FontStyle.Bold,
      alignment = TextAnchor.MiddleCenter,
      normal = { background = bg, textColor = text },
      hover = { background = bg, textColor = text },
      active = { background = bg, textColor = text },
      onNormal = { background = bg, textColor = text },
      padding = new RectOffset(4, 4, 2, 2)
    };
  }

  private static Texture2D Solid(Color c)
  {
    var t = new Texture2D(2, 2, TextureFormat.ARGB32, false);
    t.SetPixels(new[] { c, c, c, c });
    t.Apply();
    return t;
  }
}
