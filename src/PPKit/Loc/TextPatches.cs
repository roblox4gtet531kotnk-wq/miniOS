using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PPKit
{
    /// <summary>
    /// Перевод текста на уровне сеттера. Это самый устойчивый способ:
    /// перехватываются любые присваивания text, включая те, что делает игра и моды.
    /// Ничего не переводится внутри логики — только то, что попадает в UI-компонент.
    /// </summary>
    public static class TextPatches
    {
        private static Type _tmpTextType;

        public static void Apply(Harmony harmony)
        {
            TryPatchUiText(harmony);
            TryPatchTmp(harmony, "TMPro.TMP_Text");
            TryPatchTmp(harmony, "TMPro.TextMeshProUGUI");
            TryPatchTmp(harmony, "TMPro.TextMeshPro");
        }

        // ------------------------------------------------------------------ патчи

        private static void TryPatchUiText(Harmony harmony)
        {
            try
            {
                var setter = AccessTools.PropertySetter(typeof(Text), "text");
                if (setter == null)
                {
                    PPKitPlugin.Log.LogWarning("Text.set_text не найден — UI-текст не переводится.");
                    return;
                }

                harmony.Patch(setter, prefix: new HarmonyMethod(
                    typeof(TextPatches).GetMethod(nameof(TextPrefix), BindingFlags.Static | BindingFlags.NonPublic)));
                PPKitPlugin.Log.LogInfo("Патч UnityEngine.UI.Text установлен.");
            }
            catch (Exception e)
            {
                PPKitPlugin.Log.LogWarning($"Не удалось пропатчить UI Text: {e.Message}");
            }
        }

        private static void TryPatchTmp(Harmony harmony, string typeName)
        {
            try
            {
                var type = AccessTools.TypeByName(typeName);
                if (type == null) return;

                var setter = AccessTools.PropertySetter(type, "text");
                if (setter == null) return;

                // TMP_Text объявляет сеттер один раз, наследники его не переопределяют —
                // патчим один раз, иначе получим двойной проход.
                if (_tmpTextType != null) return;
                _tmpTextType = type;

                harmony.Patch(setter, prefix: new HarmonyMethod(
                    typeof(TextPatches).GetMethod(nameof(TextPrefix), BindingFlags.Static | BindingFlags.NonPublic)));
                PPKitPlugin.Log.LogInfo($"Патч {typeName} установлен.");
            }
            catch (Exception e)
            {
                PPKitPlugin.Log.LogWarning($"Не удалось пропатчить {typeName}: {e.Message}");
            }
        }

        /// <summary>__0 — параметр value у set_text.</summary>
        private static void TextPrefix(ref string __0)
        {
            if (__0 == null) return;
            var translated = Localization.T(__0);
            if (!ReferenceEquals(translated, __0)) __0 = translated;
        }

        // ------------------------------------------------------------------ дамп строк

        /// <summary>
        /// Собирает все строки интерфейса в файл, чтобы дополнить ru.json.
        /// Уже переведённые строки откатываются к оригиналу по обратному словарю.
        /// </summary>
        public static void DumpActiveStrings()
        {
            try
            {
                // оригинал -> был ли он уже переведён
                var found = new Dictionary<string, bool>(StringComparer.Ordinal);

                foreach (var t in Object.FindObjectsOfType<Text>())
                    if (t != null && !string.IsNullOrWhiteSpace(t.text)) AddString(found, t.text);

                if (_tmpTextType != null)
                {
                    var prop = _tmpTextType.GetProperty("text");
                    foreach (var t in Object.FindObjectsOfType(_tmpTextType))
                    {
                        var value = prop?.GetValue(t, null) as string;
                        if (!string.IsNullOrWhiteSpace(value)) AddString(found, value);
                    }
                }

                var dir = Path.Combine(Paths.ConfigPath, "PPKit");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "strings_dump.txt");

                var list = new List<string>(found.Keys);
                list.Sort(StringComparer.Ordinal);

                var sb = new StringBuilder();
                sb.AppendLine("// Строки интерфейса, видимые сейчас на экране.");
                sb.AppendLine("// [x] — перевод уже есть, [ ] — нужно добавить в ru.json:");
                sb.AppendLine("//   \"strings\": { \"Оригинал\": \"Перевод\" }");
                sb.AppendLine();
                foreach (var s in list)
                {
                    sb.Append(found[s] ? "// [x] " : "// [ ] ")
                      .AppendLine(s.Replace("\n", "\\n"));
                }

                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
                PPKitPlugin.Log.LogInfo($"Строк в дампе: {list.Count} -> {path}");
                ScreenMessages.TryShow($"PPKit: дамп строк сохранён ({list.Count})");
            }
            catch (Exception e)
            {
                PPKitPlugin.Log.LogWarning($"Ошибка дампа строк: {e.Message}");
            }
        }

        private static void AddString(Dictionary<string, bool> found, string value)
        {
            bool translated = Localization.TryGetOriginal(value, out var original);
            bool alreadyKnown = found.TryGetValue(original, out var hadTranslation);
            found[original] = alreadyKnown ? (hadTranslation || translated) : translated;
        }
    }

    /// <summary>Мелкие всплывашки поверх игры без вмешательства в её UI.</summary>
    public static class ScreenMessages
    {
        private static string _text;
        private static float _until;

        public static void TryShow(string text, float seconds = 3f)
        {
            _text = text;
            _until = Time.realtimeSinceStartup + seconds;
        }

        public static void Draw()
        {
            if (_text == null || Time.realtimeSinceStartup > _until) return;

            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            var font = Localization.CyrillicFont(null, 14);
            if (font != null) style.font = font;

            var rect = new Rect(0f, Screen.height * 0.12f, Screen.width, 28f);
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(rect.x, rect.y - 4f, rect.width, rect.height + 8f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, _text, style);
            GUI.color = prev;
        }
    }
}
