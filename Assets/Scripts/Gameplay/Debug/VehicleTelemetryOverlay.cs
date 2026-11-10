#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Development-only driving telemetry readout for Phase-6 handling and camera playtests
    /// (TDD §36.10/§38.4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The component is compiled only in the editor and development builds. It consumes
    /// <see cref="IVehicleTelemetry"/> (and the optional <see cref="IVehicleWheelTelemetry"/>), and
    /// never writes vehicle or race state.
    /// </para>
    /// <para>
    /// Steady-state rendering is allocation-free: glyphs are rasterized once into a pre-baked
    /// atlas, numbers are written into preallocated character buffers with fixed-point math, and
    /// the draw path only submits cached textures/Rects. No <c>string.Format</c>, interpolation,
    /// <c>ToString()</c>, LINQ, closures, or per-frame managed allocations are used.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class VehicleTelemetryOverlay : MonoBehaviour
    {
        private const int GlyphColumns = 16;
        private const int GlyphRows = 4;
        private const int GlyphCell = 8;
        private const int AtlasWidth = GlyphColumns * GlyphCell;
        private const int AtlasHeight = GlyphRows * GlyphCell;
        private const int BufferWidth = TelemetryOverlayFormat.DefaultBufferWidth;

        private static readonly string[] WheelLabels = { "FL", "FR", "RL", "RR" };

        private static readonly Color LabelColor = new Color(0.68f, 0.73f, 0.80f, 1f);
        private static readonly Color ValueColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color GoodColor = new Color(0.35f, 0.95f, 0.45f, 1f);
        private static readonly Color BadColor = new Color(1f, 0.35f, 0.35f, 1f);
        private static readonly Color SlipColor = new Color(0.35f, 0.75f, 1f, 1f);
        private static readonly Color CompressionColor = new Color(1f, 0.72f, 0.25f, 1f);

        private static readonly string[] GlyphPatterns =
        {
            // space
            "     " + "     " + "     " + "     " + "     " + "     " + "     ",
            // -
            "     " + "     " + "     " + "11111" + "     " + "     " + "     ",
            // .
            "     " + "     " + "     " + "     " + "     " + " 11  " + " 11  ",
            // /
            "    1" + "   1 " + "   1 " + "  1  " + " 1   " + " 1   " + "1    ",
            // :
            "     " + " 11  " + " 11  " + "     " + " 11  " + " 11  " + "     ",
            // 0
            " 111 " + "1   1" + "1  11" + "1 1 1" + "11  1" + "1   1" + " 111 ",
            // 1
            "  1  " + " 11  " + "  1  " + "  1  " + "  1  " + "  1  " + " 111 ",
            // 2
            " 111 " + "1   1" + "    1" + "   1 " + "  1  " + " 1   " + "11111",
            // 3
            "1111 " + "    1" + "    1" + " 111 " + "    1" + "    1" + "1111 ",
            // 4
            "   1 " + "  11 " + " 1 1 " + "1  1 " + "11111" + "   1 " + "   1 ",
            // 5
            "11111" + "1    " + "1    " + "1111 " + "    1" + "    1" + "1111 ",
            // 6
            " 111 " + "1    " + "1    " + "1111 " + "1   1" + "1   1" + " 111 ",
            // 7
            "11111" + "    1" + "   1 " + "  1  " + " 1   " + " 1   " + " 1   ",
            // 8
            " 111 " + "1   1" + "1   1" + " 111 " + "1   1" + "1   1" + " 111 ",
            // 9
            " 111 " + "1   1" + "1   1" + " 1111" + "    1" + "    1" + " 111 ",
            // A
            " 111 " + "1   1" + "1   1" + "11111" + "1   1" + "1   1" + "1   1",
            // B
            "1111 " + "1   1" + "1   1" + "1111 " + "1   1" + "1   1" + "1111 ",
            // C
            " 111 " + "1   1" + "1    " + "1    " + "1    " + "1   1" + " 111 ",
            // D
            "1111 " + "1   1" + "1   1" + "1   1" + "1   1" + "1   1" + "1111 ",
            // E
            "11111" + "1    " + "1    " + "1111 " + "1    " + "1    " + "11111",
            // F
            "11111" + "1    " + "1    " + "1111 " + "1    " + "1    " + "1    ",
            // G
            " 111 " + "1   1" + "1    " + "1 111" + "1   1" + "1   1" + " 111 ",
            // H
            "1   1" + "1   1" + "1   1" + "11111" + "1   1" + "1   1" + "1   1",
            // I
            "11111" + "  1  " + "  1  " + "  1  " + "  1  " + "  1  " + "11111",
            // J
            "  111" + "   1 " + "   1 " + "   1 " + "   1 " + "1  1 " + " 11  ",
            // K
            "1   1" + "1  1 " + "1 1  " + "11   " + "1 1  " + "1  1 " + "1   1",
            // L
            "1    " + "1    " + "1    " + "1    " + "1    " + "1    " + "11111",
            // M
            "1   1" + "11 11" + "1 1 1" + "1 1 1" + "1   1" + "1   1" + "1   1",
            // N
            "1   1" + "11  1" + "1 1 1" + "1  11" + "1   1" + "1   1" + "1   1",
            // O
            " 111 " + "1   1" + "1   1" + "1   1" + "1   1" + "1   1" + " 111 ",
            // P
            "1111 " + "1   1" + "1   1" + "1111 " + "1    " + "1    " + "1    ",
            // Q
            " 111 " + "1   1" + "1   1" + "1   1" + "1 1 1" + "1  1 " + " 11 1",
            // R
            "1111 " + "1   1" + "1   1" + "1111 " + "1 1  " + "1  1 " + "1   1",
            // S
            " 1111" + "1    " + "1    " + " 111 " + "    1" + "    1" + "1111 ",
            // T
            "11111" + "  1  " + "  1  " + "  1  " + "  1  " + "  1  " + "  1  ",
            // U
            "1   1" + "1   1" + "1   1" + "1   1" + "1   1" + "1   1" + " 111 ",
            // V
            "1   1" + "1   1" + "1   1" + "1   1" + "1   1" + " 1 1 " + "  1  ",
            // W
            "1   1" + "1   1" + "1   1" + "1 1 1" + "1 1 1" + "1 1 1" + " 1 1 ",
            // X
            "1   1" + "1   1" + " 1 1 " + "  1  " + " 1 1 " + "1   1" + "1   1",
            // Y
            "1   1" + "1   1" + " 1 1 " + "  1  " + "  1  " + "  1  " + "  1  ",
            // Z
            "11111" + "    1" + "   1 " + "  1  " + " 1   " + "1    " + "11111"
        };

        [SerializeField] private MonoBehaviour telemetrySource;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField, Range(1, 60)] private int refreshRate = 20;
        [SerializeField] private bool visibleOnStart = true;

        private readonly char[] _speedMs = new char[BufferWidth];
        private readonly char[] _speedKmh = new char[BufferWidth];
        private readonly char[] _forwardSpeed = new char[BufferWidth];
        private readonly char[] _lateralSpeed = new char[BufferWidth];
        private readonly char[] _slipAngle = new char[BufferWidth];
        private readonly char[] _wheelSlip = new char[BufferWidth];
        private readonly WheelTelemetry[] _wheels = new WheelTelemetry[4];

        private IVehicleTelemetry _telemetry;
        private IVehicleWheelTelemetry _wheelTelemetry;
        private Texture2D _atlas;
        private Texture2D _white;
        private bool _visible;
        private float _nextRefresh;
        private int _wheelCount;
        private float _slipAngleDegrees;
        private float _aggregateSlip;
        private bool _grounded;

        private void Awake()
        {
            _visible = visibleOnStart;
            _telemetry = telemetrySource as IVehicleTelemetry;
            if (_telemetry == null)
            {
                Debug.LogError(
                    "[VelocityRush.Overlay] No telemetry source assigned; it must implement IVehicleTelemetry. Overlay disabled.",
                    this);
                enabled = false;
                return;
            }

            _wheelTelemetry = telemetrySource as IVehicleWheelTelemetry;
            CreateAtlas();
            Refresh();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                _visible = !_visible;
            }

            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + (1f / Mathf.Max(1, refreshRate));
            Refresh();
        }

        /// <summary>Forces one sample/format pass; used by allocation validation in EditMode.</summary>
        public void RefreshNow()
        {
            Refresh();
        }

        private void OnDestroy()
        {
            if (_atlas != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_atlas);
                }
                else
                {
                    DestroyImmediate(_atlas);
                }

                _atlas = null;
            }

            if (_white != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_white);
                }
                else
                {
                    DestroyImmediate(_white);
                }

                _white = null;
            }
        }

        private void Refresh()
        {
            var telemetry = _telemetry.Telemetry;

            TelemetryOverlayFormat.WriteFixed(_speedMs, telemetry.Speed, 1, BufferWidth);
            TelemetryOverlayFormat.WriteInt(_speedKmh, telemetry.Speed * 3.6f, BufferWidth);
            TelemetryOverlayFormat.WriteFixed(_forwardSpeed, telemetry.ForwardSpeed, 1, BufferWidth);
            TelemetryOverlayFormat.WriteFixed(_lateralSpeed, telemetry.LateralSpeed, 1, BufferWidth);

            _slipAngleDegrees = TelemetryOverlayFormat.SlipAngleDegrees(
                telemetry.ForwardSpeed,
                telemetry.LateralSpeed);
            TelemetryOverlayFormat.WriteFixed(_slipAngle, _slipAngleDegrees, 1, BufferWidth);
            _aggregateSlip = telemetry.WheelSlip;
            TelemetryOverlayFormat.WriteFixed(_wheelSlip, _aggregateSlip, 2, BufferWidth);

            _grounded = telemetry.IsGrounded;
            _wheelCount = _wheelTelemetry != null ? Mathf.Clamp(_wheelTelemetry.WheelCount, 0, 4) : 0;

            for (var i = 0; i < _wheels.Length; i++)
            {
                _wheels[i] = i < _wheelCount ? _wheelTelemetry.GetWheel(i) : default;
            }
        }

        private void CreateAtlas()
        {
            _atlas = new Texture2D(AtlasWidth, AtlasHeight, TextureFormat.RGBA32, false)
            {
                name = "TelemetryGlyphAtlas",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[AtlasWidth * AtlasHeight];
            var white = new Color32(255, 255, 255, 255);

            for (var glyph = 0; glyph < GlyphPatterns.Length; glyph++)
            {
                var pattern = GlyphPatterns[glyph];
                var cellX = (glyph % GlyphColumns) * GlyphCell;
                var cellY = (glyph / GlyphColumns) * GlyphCell;

                for (var row = 0; row < 7; row++)
                {
                    for (var column = 0; column < 5; column++)
                    {
                        if (pattern[(row * 5) + column] != '1')
                        {
                            continue;
                        }

                        // Texture pixel row 0 is the bottom; glyph row 0 is the top.
                        var pixelY = cellY + (6 - row);
                        var pixelX = cellX + 1 + column;
                        pixels[(pixelY * AtlasWidth) + pixelX] = white;
                    }
                }
            }

            _atlas.SetPixels32(pixels);
            _atlas.Apply(false, true);

            _white = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "TelemetryPanelWhite",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _white.SetPixel(0, 0, Color.white);
            _white.Apply(false, true);
        }

        private void OnGUI()
        {
            if (!_visible || _atlas == null || Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            var scale = Mathf.Clamp(Screen.height / 900f, 1f, 2f);
            var textScale = 1.6f * scale;
            var line = 15f * textScale;
            var panel = new Rect(12f * scale, 12f * scale, 430f * scale, 370f * scale);
            var x = panel.x + (12f * scale);
            var y = panel.y + (10f * scale);

            GUI.color = new Color(0.02f, 0.03f, 0.04f, 0.84f);
            GUI.DrawTexture(panel, _white);
            GUI.color = Color.white;

            DrawText(x, y, "TELEMETRY  F1 TOGGLE", textScale, ValueColor);
            y += line;

            DrawLabelValue(x, y, "SPEED", _speedMs, "M/S", textScale, ValueColor);
            y += line;
            DrawLabelValue(x, y, "SPEED", _speedKmh, "KM/H", textScale, ValueColor);
            y += line;
            DrawLabelValue(x, y, "FWD", _forwardSpeed, "M/S", textScale, ValueColor);
            y += line;
            DrawLabelValue(x, y, "LAT", _lateralSpeed, "M/S", textScale, ValueColor);
            y += line;
            DrawLabelValue(x, y, "SLIP ANGLE", _slipAngle, "DEG", textScale, ValueColor);
            y += line;
            DrawLabelValue(x, y, "WHEEL SLIP", _wheelSlip, "MAX", textScale, ValueColor);
            DrawBar(x + (335f * scale), y + (2f * scale), 75f * scale, 8f * scale, Mathf.Clamp01(_aggregateSlip / 1.5f), SlipColor);
            y += line;
            DrawText(x, y, "GROUND", textScale, LabelColor);
            DrawText(x + (110f * scale), y, _grounded ? "YES" : "NO", textScale, _grounded ? GoodColor : BadColor);
            y += line;
            y += line * 0.25f;

            if (_wheelCount <= 0)
            {
                DrawText(x, y, "NO PER-WHEEL DATA", textScale, LabelColor);
            }
            else
            {
                for (var i = 0; i < _wheels.Length; i++)
                {
                    if (i >= _wheelCount)
                    {
                        break;
                    }

                    var wheel = _wheels[i];
                    DrawText(x, y, WheelLabels[i], textScale, LabelColor);
                    DrawText(x + (36f * scale), y, wheel.IsGrounded ? "G" : "-", textScale, wheel.IsGrounded ? GoodColor : BadColor);
                    DrawValueAt(x + (58f * scale), y, wheel.Slip, 2, textScale, ValueColor);
                    DrawBar(x + (150f * scale), y + (2f * scale), 70f * scale, 8f * scale, Mathf.Clamp01(wheel.Slip / 1.5f), SlipColor);
                    DrawText(x + (232f * scale), y, "C", textScale, LabelColor);
                    DrawValueAt(x + (250f * scale), y, wheel.SuspensionCompression, 2, textScale, ValueColor);
                    DrawBar(x + (342f * scale), y + (2f * scale), 70f * scale, 8f * scale, wheel.SuspensionCompression, CompressionColor);
                    y += line;
                }
            }

            GUI.color = Color.white;
        }

        private void DrawLabelValue(
            float x,
            float y,
            string label,
            char[] value,
            string unit,
            float scale,
            Color valueColor)
        {
            DrawText(x, y, label, scale, LabelColor);
            DrawChars(x + (110f * scale), y, value, value.Length, scale, valueColor);
            DrawText(x + (235f * scale), y, unit, scale, LabelColor);
        }

        private void DrawValueAt(float x, float y, float value, int decimals, float scale, Color color)
        {
            var buffer = _scratchValue;
            TelemetryOverlayFormat.WriteFixed(buffer, value, decimals, BufferWidth);
            DrawChars(x, y, buffer, buffer.Length, scale, color);
        }

        private readonly char[] _scratchValue = new char[BufferWidth];

        private void DrawText(float x, float y, string text, float scale, Color color)
        {
            GUI.color = color;
            var cursor = x;
            for (var i = 0; i < text.Length; i++)
            {
                var character = text[i];
                if (character == ' ')
                {
                    cursor += 6f * scale;
                    continue;
                }

                DrawGlyph(cursor, y, GlyphIndex(character), scale);
                cursor += 6f * scale;
            }
        }

        private void DrawChars(float x, float y, char[] buffer, int length, float scale, Color color)
        {
            GUI.color = color;
            var cursor = x;
            for (var i = 0; i < length; i++)
            {
                var character = buffer[i];
                if (character == ' ')
                {
                    cursor += 6f * scale;
                    continue;
                }

                DrawGlyph(cursor, y, GlyphIndex(character), scale);
                cursor += 6f * scale;
            }
        }

        private void DrawGlyph(float x, float y, int glyph, float scale)
        {
            var cellX = (glyph % GlyphColumns) * GlyphCell;
            var cellY = (glyph / GlyphColumns) * GlyphCell;
            var rect = new Rect(x, y, GlyphCell * scale, GlyphCell * scale);
            var uv = new Rect(
                cellX / (float)AtlasWidth,
                cellY / (float)AtlasHeight,
                GlyphCell / (float)AtlasWidth,
                GlyphCell / (float)AtlasHeight);
            GUI.DrawTextureWithTexCoords(rect, _atlas, uv);
        }

        private void DrawBar(float x, float y, float width, float height, float fill, Color color)
        {
            GUI.color = new Color(0.18f, 0.20f, 0.24f, 1f);
            GUI.DrawTexture(new Rect(x, y, width, height), _white);
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, width * Mathf.Clamp01(fill), height), _white);
        }

        private static int GlyphIndex(char character)
        {
            if (character == ' ')
            {
                return 0;
            }

            if (character == '-')
            {
                return 1;
            }

            if (character == '.')
            {
                return 2;
            }

            if (character == '/')
            {
                return 3;
            }

            if (character == ':')
            {
                return 4;
            }

            if (character >= '0' && character <= '9')
            {
                return 5 + (character - '0');
            }

            if (character >= 'A' && character <= 'Z')
            {
                return 15 + (character - 'A');
            }

            if (character >= 'a' && character <= 'z')
            {
                return 15 + (character - 'a');
            }

            return 0;
        }
    }
}
#endif
