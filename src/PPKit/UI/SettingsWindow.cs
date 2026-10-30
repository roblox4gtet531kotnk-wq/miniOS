using System;
using PPKit.Perf;
using UnityEngine;

namespace PPKit.UI
{
    /// <summary>
    /// Своё окно настроек на IMGUI: без вмешательства в UI игры и без префабов.
    /// Всё, что меняется здесь, сразу пишется в конфиг BepInEx.
    /// </summary>
    public sealed class SettingsWindow
    {
        private readonly ModConfig _cfg;
        private readonly PerformanceManager _perf;

        private const int WindowId = 0x50504B;
        private Rect _rect = new Rect(60f, 60f, 460f, 560f);
        private Vector2 _scroll;
        private GUIStyle _header, _sub, _button, _label, _hint;
        private Texture2D _bg, _panel, _accent;
        private bool _stylesReady;
        private bool _advanced;

        public bool Visible { get; private set; }

        public SettingsWindow(ModConfig cfg, PerformanceManager perf)
        {
            _cfg = cfg;
            _perf = perf;
        }

        public void Toggle()
        {
            Visible = !Visible;
            if (Visible) EnsureStyles();
        }

        // ------------------------------------------------------------------ стили

        private void EnsureStyles()
        {
            if (_stylesReady && _header != null && _header.font != null) return;

            var font = Localization.CyrillicFont(_cfg.WindowsFont.Value, 14);
            var fontBig = Localization.CyrillicFont(_cfg.WindowsFont.Value, 17);

            _bg = MakeTex(new Color(0.08f, 0.09f, 0.13f, 0.97f));
            _panel = MakeTex(new Color(0.13f, 0.15f, 0.21f, 1f));
            _accent = MakeTex(new Color(0.93f, 0.35f, 0.28f, 1f));

            _header = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17, fontStyle = FontStyle.Bold, richText = true, wordWrap = false
            };
            if (fontBig != null) _header.font = fontBig;

            _sub = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, richText = true };
            if (font != null) _sub.font = font;

            _label = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            if (font != null) _label.font = font;

            _hint = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, fontStyle = FontStyle.Italic };
            if (font != null) _hint.font = font;
            _hint.normal.textColor = new Color(0.65f, 0.68f, 0.78f);

            _button = new GUIStyle(GUI.skin.button) { fontSize = 13 };
            if (font != null) _button.font = font;

            _stylesReady = true;
        }

        // ------------------------------------------------------------------ отрисовка

        public void Draw()
        {
            EnsureStyles();
            _rect = GUI.Window(WindowId, _rect, DrawContents, GUIContent.none, GUIStyle.none);
            // окно без рамки — рисуем свою подложку
        }

        private void DrawContents(int id)
        {
            var r = new Rect(0f, 0f, _rect.width, _rect.height);
            GUI.DrawTexture(r, _bg);
            GUI.DrawTexture(new Rect(0f, 0f, r.width, 34f), _panel);
            GUI.DrawTexture(new Rect(0f, 0f, 4f, r.height), _accent);

            GUI.Label(new Rect(12f, 7f, r.width - 90f, 24f),
                "<color=#ffffff>PPKit</color>  <color=#9aa3b8>· оптимизация и русский интерфейс</color>", _header);

            if (GUI.Button(new Rect(r.width - 30f, 5f, 24f, 24f), "✕", _button))
            {
                Visible = false;
                return;
            }

            _scroll = GUI.BeginScrollView(
                new Rect(12f, 42f, r.width - 24f, r.height - 54f), _scroll,
                new Rect(0f, 0f, r.width - 38f, ContentHeight()));

            float y = 0f;
            y = DrawPerformance(y);
            y = DrawAdvanced(y);
            y = DrawLanguage(y);
            y = DrawFooter(y);

            GUI.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, r.width, 34f));
        }

        private float ContentHeight() => _advanced ? 980f : 760f;

        private float DrawPerformance(float y)
        {
            GUI.Label(new Rect(0f, y, 300f, 20f), "Производительность", _sub);
            y += 24f;

            _cfg.PerfEnabled.Value = Toggle(new Rect(0f, y, 380f, 22f), "Включить оптимизации", _cfg.PerfEnabled.Value);
            y += 26f;

            GUI.Label(new Rect(0f, y, 380f, 18f), $"Пресет: <b>{_perf.CurrentPreset}</b>", _label);
            y += 22f;

            float bw = 100f;
            for (int i = 0; i < PerformanceManager.PresetNames.Length; i++)
            {
                var name = PerformanceManager.PresetNames[i];
                bool active = name == _perf.CurrentPreset;
                var prev = GUI.backgroundColor;
                if (active) GUI.backgroundColor = new Color(0.93f, 0.35f, 0.28f);
                if (GUI.Button(new Rect(i * (bw + 6f), y, bw, 26f), name, _button))
                {
                    _cfg.AutoDetectPreset.Value = false;
                    _cfg.Preset.Value = name;
                    _perf.ApplyPreset(name);
                }
                GUI.backgroundColor = prev;
            }
            y += 34f;

            _cfg.AutoDetectPreset.Value = Toggle(new Rect(0f, y, 380f, 22f),
                "Подбирать пресет по железу при запуске", _cfg.AutoDetectPreset.Value);
            y += 28f;

            y = Slider(y, "Лимит FPS (0 = без лимита)", _cfg.TargetFps, 0f, 300f, v => v.ToString("0"));
            y = Slider(y, "Дальность теней, м", _cfg.ShadowDistance, 0f, 200f, v => v.ToString("0"));
            y = Slider(y, "Лимит частиц (0 = не трогать)", _cfg.MaxParticles, 0f, 1000f, v => v.ToString("0"));
            y = Slider(y, "Качество текстур", _cfg.TextureQuality, 0f, 3f,
                v => v < 0.5f ? "1/8" : v < 1.5f ? "1/4" : v < 2.5f ? "1/2" : "полное");

            _cfg.VSync.Value = Toggle(new Rect(0f, y, 380f, 22f), "Вертикальная синхронизация", _cfg.VSync.Value);
            y += 26f;
            _cfg.RealtimeReflectionProbes.Value = Toggle(new Rect(0f, y, 380f, 22f),
                "Отражения в реальном времени", _cfg.RealtimeReflectionProbes.Value);
            y += 26f;
            _cfg.UnloadUnusedAssets.Value = Toggle(new Rect(0f, y, 380f, 22f),
                "Выгружать неиспользуемые ресурсы", _cfg.UnloadUnusedAssets.Value);
            y += 30f;

            if (GUI.Button(new Rect(0f, y, 180f, 26f), "Применить сейчас", _button))
                _perf.ApplyNow();
            y += 30f;

            GUI.Label(new Rect(0f, y, 380f, 18f),
                $"Мусор выгружен за {_perf.LastUnloadMs:0} мс, кадр ~{1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f):0} FPS", _hint);
            y += 26f;

            return y;
        }

        private float DrawAdvanced(float y)
        {
            if (GUI.Button(new Rect(0f, y, 200f, 24f), _advanced ? "Скрыть тонкие настройки" : "Тонкие настройки", _button))
                _advanced = !_advanced;
            y += 30f;
            if (!_advanced) return y;

            y = Slider(y, "Шаг физики, с", _cfg.FixedDeltaTime, 0.010f, 0.05f, v => v.ToString("0.000"));
            y = Slider(y, "Дальность прорисовки, м (0 = не менять)", _cfg.CameraFarClip, 0f, 400f, v => v.ToString("0"));
            y = Slider(y, "Разрешение теней", _cfg.ShadowResolution, 0f, 3f,
                v => v < 0.5f ? "низкое" : v < 1.5f ? "среднее" : v < 2.5f ? "высокое" : "очень высокое");
            y = Slider(y, "Анизотропия", _cfg.AnisotropicFiltering, 0f, 2f,
                v => v < 0.5f ? "выкл" : v < 1.5f ? "вкл" : "форс");

            _cfg.SoftParticles.Value = Toggle(new Rect(0f, y, 380f, 22f), "Мягкие частицы", _cfg.SoftParticles.Value);
            y += 26f;

            return y;
        }

        private float DrawLanguage(float y)
        {
            GUI.Label(new Rect(0f, y, 300f, 20f), "Русский интерфейс", _sub);
            y += 24f;

            _cfg.TranslateUI.Value = Toggle(new Rect(0f, y, 380f, 22f), "Переводить текст по словарю", _cfg.TranslateUI.Value);
            y += 26f;

            GUI.Label(new Rect(0f, y, 390f, 20f), $"Строк в словаре: <b>{Localization.Count}</b>", _label);
            y += 22f;

            if (GUI.Button(new Rect(0f, y, 180f, 26f), "Перечитать словарь (F5)", _button))
                Localization.Reload();
            if (GUI.Button(new Rect(190f, y, 180f, 26f), "Дамп строк (F12)", _button))
                TextPatches.DumpActiveStrings();
            y += 32f;

            GUI.Label(new Rect(0f, y, 390f, 34f), $"Файл словаря:\n{Localization.FilePath}", _hint);
            y += 40f;

            _cfg.LogMissing.Value = Toggle(new Rect(0f, y, 380f, 22f),
                "Собирать непереведённые строки", _cfg.LogMissing.Value);
            y += 26f;

            GUI.Label(new Rect(0f, y, 390f, 30f),
                "Шрифт меню и подсказки: меняются в конфиге (02. Язык → Шрифт меню PPKit).", _hint);
            y += 34f;

            return y;
        }

        private float DrawFooter(float y)
        {
            GUI.Label(new Rect(0f, y, 390f, 40f),
                "F1 — это меню · F2 — пресет · F3 — FPS · F5 — словарь · F12 — дамп строк", _hint);
            return y + 44f;
        }

        // ------------------------------------------------------------------ контролы

        private bool Toggle(Rect r, string text, bool value)
        {
            bool next = GUI.Toggle(r, value, "  " + text, _button);
            return next;
        }

        private float Slider(float y, string label, BepInEx.Configuration.ConfigEntry<int> entry,
                             float min, float max, Func<float, string> fmt)
        {
            float v = entry.Value;
            GUI.Label(new Rect(0f, y, 280f, 18f), label, _label);
            float next = GUI.HorizontalSlider(new Rect(288f, y + 5f, 130f, 18f), v, min, max);
            GUI.Label(new Rect(424f, y, 60f, 18f), fmt(next), _hint);
            int rounded = Mathf.RoundToInt(next);
            if (rounded != entry.Value) entry.Value = rounded;
            return y + 26f;
        }

        private float Slider(float y, string label, BepInEx.Configuration.ConfigEntry<float> entry,
                             float min, float max, Func<float, string> fmt)
        {
            GUI.Label(new Rect(0f, y, 280f, 18f), label, _label);
            float next = GUI.HorizontalSlider(new Rect(288f, y + 5f, 130f, 18f), entry.Value, min, max);
            GUI.Label(new Rect(424f, y, 60f, 18f), fmt(next), _hint);
            entry.Value = Mathf.Round(next * 1000f) / 1000f;
            return y + 26f;
        }

        private float Slider(float y, string label, BepInEx.Configuration.ConfigEntry<UnityEngine.ShadowResolution> entry,
                             float min, float max, Func<float, string> fmt)
        {
            return Slider(y, label, min, max, fmt, (int)entry.Value,
                v => entry.Value = (UnityEngine.ShadowResolution)Mathf.Clamp(v, 0, 3));
        }

        private float Slider(float y, string label, float min, float max, Func<float, string> fmt,
                             int current, Action<int> set)
        {
            GUI.Label(new Rect(0f, y, 280f, 18f), label, _label);
            int next = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(288f, y + 5f, 130f, 18f), current, min, max));
            GUI.Label(new Rect(424f, y, 60f, 18f), fmt(next), _hint);
            if (next != current) set(next);
            return y + 26f;
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
