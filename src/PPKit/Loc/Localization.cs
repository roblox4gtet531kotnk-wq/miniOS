using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace PPKit
{
    /// <summary>
    /// Словарь «английский оригинал → русский текст».
    /// Источники: встроенный ru.json (embedded) + внешний файл, который главнее.
    /// Мини-парсер JSON без зависимостей: нужен только объект "strings": { ... }.
    /// </summary>
    public static class Localization
    {
        private const string EmbeddedName = "PPKit.Loc.ru.json";

        private static ManualLogSource _log;
        private static ModConfig _cfg;

        private static Dictionary<string, string> _map = new Dictionary<string, string>(StringComparer.Ordinal);
        private static Dictionary<string, string> _reverse = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly HashSet<string> _missing = new HashSet<string>(StringComparer.Ordinal);

        public static string FilePath { get; private set; }
        public static int Count => _map.Count;
        public static bool Enabled => _cfg != null && _cfg.TranslateUI.Value;

        public static void Init(ManualLogSource log, ModConfig cfg)
        {
            _log = log;
            _cfg = cfg;

            var dir = Path.Combine(Paths.ConfigPath, "PPKit");
            Directory.CreateDirectory(dir);
            FilePath = Path.Combine(dir, "ru.json");

            LoadEmbedded();
            EnsureExternalFile();
            MergeExternal();

            _log.LogInfo($"Перевод: {_map.Count} строк (внешний файл: {FilePath}).");
        }

        public static void Reload()
        {
            _map = new Dictionary<string, string>(StringComparer.Ordinal);
            _reverse = new Dictionary<string, string>(StringComparer.Ordinal);
            LoadEmbedded();
            MergeExternal();
            FlushMissing();
        }

        // ------------------------------------------------------------------ загрузка

        private static void LoadEmbedded()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream(EmbeddedName))
                {
                    if (s == null)
                    {
                        _log.LogWarning($"Встроенный словарь {EmbeddedName} не найден в сборке.");
                        return;
                    }
                    using (var r = new StreamReader(s, Encoding.UTF8))
                        ParseInto(r.ReadToEnd(), "embedded");
                }
            }
            catch (Exception e)
            {
                _log.LogWarning($"Ошибка чтения встроенного словаря: {e.Message}");
            }
        }

        private static void EnsureExternalFile()
        {
            if (File.Exists(FilePath)) return;

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream(EmbeddedName))
                {
                    if (s == null) return;
                    using (var r = new StreamReader(s, Encoding.UTF8))
                    using (var w = new StreamWriter(FilePath, false, new UTF8Encoding(false)))
                        w.Write(r.ReadToEnd());
                }
                _log.LogInfo($"Создан редактируемый словарь: {FilePath}");
            }
            catch (Exception e)
            {
                _log.LogWarning($"Не удалось создать {FilePath}: {e.Message}");
            }
        }

        private static void MergeExternal()
        {
            if (!File.Exists(FilePath)) return;
            try
            {
                ParseInto(File.ReadAllText(FilePath, Encoding.UTF8), "external");
            }
            catch (Exception e)
            {
                _log.LogWarning($"Ошибка чтения {FilePath}: {e.Message}");
            }
        }

        // ------------------------------------------------------------------ перевод

        /// <summary>Возвращает перевод или исходную строку (тот же объект, без аллокаций).</summary>
        public static string T(string original)
        {
            if (string.IsNullOrEmpty(original) || !Enabled) return original;
            if (_map.Count == 0) return original;

            string hit;
            if (_map.TryGetValue(original, out hit)) return hit;

            // Терпим лишние пробелы/переводы строк по краям — игра часто их добавляет.
            var trimmed = original.Trim();
            if (trimmed.Length != original.Length && _map.TryGetValue(trimmed, out hit)) return hit;

            // Не нашли: строки, похожие на ID/путь, не трогаем вовсе —
            // иначе ломается загрузка ассетов и сохранений.
            if (!LooksLikeTechnical(original, trimmed)) Track(trimmed);
            return original;
        }

        /// <summary>Обратный поиск: русский текст → оригинал (нужно для дампа строк).</summary>
        public static bool TryGetOriginal(string translated, out string original)
        {
            if (translated != null && _reverse.TryGetValue(translated, out original)) return true;
            original = translated;
            return false;
        }

        /// <summary>Не трогаем строки, которые игра использует как ключи/пути.</summary>
        private static bool LooksLikeTechnical(string original, string trimmed)
        {
            if (_cfg == null || !_cfg.SkipLikelyIds.Value) return false;
            if (trimmed.Length > 120) return true;
            if (trimmed.IndexOf('/') >= 0 || trimmed.IndexOf('\\') >= 0) return true;
            if (trimmed.IndexOf('_') >= 0 && trimmed.IndexOf(' ') < 0) return true;
            if (trimmed.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void Track(string s)
        {
            if (_cfg == null || !_cfg.LogMissing.Value) return;
            if (string.IsNullOrWhiteSpace(s) || s.Length > 120) return;
            if (s.Length < 2) return;

            // Уже русский текст не считаем «непереведённым».
            foreach (var ch in s)
                if (ch >= 'А' && ch <= 'я') return;

            if (_missing.Add(s))
                FlushMissing();
        }

        public static string MissingDumpPath =>
            _cfg == null ? "missing_strings.txt" : Path.Combine(Paths.ConfigPath, "PPKit", "missing_strings.txt");

        private static void FlushMissing()
        {
            if (_missing.Count == 0) return;
            try
            {
                var list = new List<string>(_missing);
                list.Sort(StringComparer.Ordinal);

                var sb = new StringBuilder();
                sb.AppendLine("// Строки интерфейса, для которых нет перевода.");
                sb.AppendLine("// Скопируйте нужные в ru.json внутрь \"strings\": { \"Оригинал\": \"Перевод\" }");
                sb.AppendLine();
                foreach (var s in list)
                    sb.Append("// ").AppendLine(s);

                File.WriteAllText(MissingDumpPath, sb.ToString(), new UTF8Encoding(false));
            }
            catch
            {
                // файл — не критичная штука, молча игнорируем
            }
        }

        // ------------------------------------------------------------------ шрифт с кириллицей

        private static Font _fontCache;

        /// <summary>
        /// Шрифт для собственного меню. Встроенный шрифт Unity IMGUI иногда не имеет
        /// кириллицы — берём системный с фолбэком.
        /// </summary>
        public static Font CyrillicFont(string preferred, int size)
        {
            if (_fontCache != null) return _fontCache;

            var names = new List<string>();
            if (!string.IsNullOrWhiteSpace(preferred)) names.Add(preferred);
            names.AddRange(new[] { "Segoe UI", "Arial", "Tahoma", "DejaVu Sans", "Liberation Sans", "Noto Sans" });

            try
            {
                _fontCache = Font.CreateDynamicFontFromOSFont(names.ToArray(), size);
            }
            catch (Exception e)
            {
                _log?.LogWarning($"Не удалось взять системный шрифт: {e.Message}");
            }
            return _fontCache;
        }

        // ------------------------------------------------------------------ мини-парсер JSON

        private static void ParseInto(string json, string source)
        {
            try
            {
                int i = 0;
                var root = ParseObject(json, ref i);
                Dictionary<string, string> strings;
                if (root != null && root.TryGetValue("strings", out var raw) && raw is Dictionary<string, string> d)
                    strings = d;
                else
                    strings = root != null ? new Dictionary<string, string>(StringComparer.Ordinal) : null;

                if (strings == null)
                {
                    _log.LogWarning($"В {source} нет блока \"strings\".");
                    return;
                }

                int added = 0;
                foreach (var kv in strings)
                {
                    if (string.IsNullOrEmpty(kv.Key)) continue;
                    _map[kv.Key] = kv.Value;
                    if (!string.IsNullOrEmpty(kv.Value)) _reverse[kv.Value] = kv.Key;
                    added++;
                }
                _log.LogInfo($"Словарь ({source}): +{added} строк.");
            }
            catch (Exception e)
            {
                _log.LogWarning($"Не удалось разобрать словарь ({source}): {e.Message}");
            }
        }

        /// <summary>Парсит объект верхнего уровня; вложенные объекты отдаёт как Dictionary&lt;string,string&gt;.</summary>
        private static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            SkipWs(s, ref i);
            if (i >= s.Length || s[i] != '{') throw new FormatException($"ожидался '{{' на позиции {i}");
            i++;

            while (true)
            {
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("неожиданный конец файла");
                if (s[i] == '}') { i++; break; }
                if (s[i] == ',') { i++; continue; }

                var key = ParseString(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException($"ожидался ':' после \"{key}\"");
                i++;
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("неожиданный конец файла");

                if (s[i] == '{')
                {
                    var nested = ParseObject(s, ref i);
                    var flat = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var kv in nested) flat[kv.Key] = kv.Value as string ?? string.Empty;
                    result[key] = flat;
                }
                else if (s[i] == '"')
                {
                    result[key] = ParseString(s, ref i);
                }
                else
                {
                    SkipValue(s, ref i);
                }
            }

            return result;
        }

        private static string ParseString(string s, ref int i)
        {
            if (i >= s.Length || s[i] != '"') throw new FormatException($"ожидалась '\"' на позиции {i}");
            i++;
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }

                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'u':
                        if (i + 4 <= s.Length)
                        {
                            if (int.TryParse(s.Substring(i, 4), System.Globalization.NumberStyles.HexNumber,
                                    System.Globalization.CultureInfo.InvariantCulture, out int code))
                                sb.Append((char)code);
                            i += 4;
                        }
                        break;
                    default: sb.Append(e); break;
                }
            }
            throw new FormatException("незакрытая строка");
        }

        private static void SkipValue(string s, ref int i)
        {
            while (i < s.Length && s[i] != ',' && s[i] != '}') i++;
        }

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') { i++; continue; }
                // комментарии в стиле // до конца строки — удобно для правок руками
                if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
                {
                    while (i < s.Length && s[i] != '\n') i++;
                    continue;
                }
                break;
            }
        }
    }
}
