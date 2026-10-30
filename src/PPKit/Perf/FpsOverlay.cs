using UnityEngine;

namespace PPKit.Perf
{
    /// <summary>Счётчик FPS с бюджетом кадра. Рисуется в OnGUI, стили кэшируются.</summary>
    public sealed class FpsOverlay
    {
        private readonly ModConfig _cfg;
        private readonly PerformanceManager _perf;

        private float _fps;
        private float _worstMs;
        private float _avgMs;

        private GUIStyle _style;
        private Texture2D _bg;

        public bool Visible { get; private set; }

        public FpsOverlay(ModConfig cfg, PerformanceManager perf)
        {
            _cfg = cfg;
            _perf = perf;
        }

        public void Toggle() => Visible = !Visible;

        public void Sample()
        {
            float ms = Time.unscaledDeltaTime * 1000f;
            if (ms <= 0f) return;

            float instantFps = 1f / Time.unscaledDeltaTime;
            _fps = _fps <= 0f ? instantFps : Mathf.Lerp(_fps, instantFps, 0.15f);
            _avgMs = _avgMs <= 0f ? ms : Mathf.Lerp(_avgMs, ms, 0.1f);
            _worstMs = Mathf.Max(_worstMs * 0.98f, ms);
        }

        public void Draw()
        {
            if (_bg == null) _bg = MakeTex(new Color(0.05f, 0.06f, 0.09f, 0.82f));

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    richText = true,
                    padding = new RectOffset(8, 8, 6, 6)
                };
                var font = Localization.CyrillicFont(_cfg.WindowsFont.Value, 13);
                if (font != null) _style.font = font;
            }

            bool low = _fps < 40f;
            string color = low ? "#ff7a7a" : (_fps < 55f ? "#ffd479" : "#8ef58a");

            string text =
                $"<b><color={color}>{_fps:0} FPS</color></b>  ({_avgMs:0.0} мс)\n" +
                $"худший кадр: {_worstMs:0.0} мс\n" +
                $"пресет: {_perf.CurrentPreset}";

            var content = new GUIContent(text);
            var size = _style.CalcSize(content);
            var rect = new Rect(10f, 10f, size.x + 16f, size.y + 10f);

            GUI.DrawTexture(rect, _bg);
            GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, size.x + 8f, size.y + 6f), text, _style);
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }
    }
}
