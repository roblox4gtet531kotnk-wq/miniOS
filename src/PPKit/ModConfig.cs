using BepInEx.Configuration;
using UnityEngine;

namespace PPKit
{
    /// <summary>
    /// Все настройки мода. Файл: BepInEx/config/PPKit/ru.ppkit.optimizer.cfg
    /// Правки в игре — через меню (F1), правки руками — в этом файле.
    /// </summary>
    public sealed class ModConfig
    {
        // ---- клавиши ----
        public readonly ConfigEntry<KeyboardShortcut> MenuKey;
        public readonly ConfigEntry<KeyboardShortcut> PresetKey;
        public readonly ConfigEntry<KeyboardShortcut> FpsKey;
        public readonly ConfigEntry<KeyboardShortcut> ReloadLangKey;
        public readonly ConfigEntry<KeyboardShortcut> DumpKey;

        // ---- производительность ----
        public readonly ConfigEntry<bool> PerfEnabled;
        public readonly ConfigEntry<bool> AutoDetectPreset;
        public readonly ConfigEntry<string> Preset;
        public readonly ConfigEntry<int> TargetFps;
        public readonly ConfigEntry<bool> VSync;
        public readonly ConfigEntry<int> AntiAliasing;
        public readonly ConfigEntry<ShadowQuality> Shadows;
        public readonly ConfigEntry<ShadowResolution> ShadowResolution;
        public readonly ConfigEntry<float> ShadowDistance;
        public readonly ConfigEntry<int> TextureQuality;
        public readonly ConfigEntry<int> AnisotropicFiltering;
        public readonly ConfigEntry<bool> RealtimeReflectionProbes;
        public readonly ConfigEntry<bool> SoftParticles;
        public readonly ConfigEntry<bool> BillboardFaceCamera;
        public readonly ConfigEntry<int> MaxParticles;
        public readonly ConfigEntry<float> FixedDeltaTime;
        public readonly ConfigEntry<float> MaximumDeltaTime;
        public readonly ConfigEntry<float> CameraFarClip;
        public readonly ConfigEntry<bool> UnloadUnusedAssets;

        // ---- язык ----
        public readonly ConfigEntry<bool> TranslateUI;
        public readonly ConfigEntry<string> WindowsFont;
        public readonly ConfigEntry<bool> LogMissing;
        public readonly ConfigEntry<bool> SkipLikelyIds;

        public ModConfig(ConfigFile c)
        {
            const string kKey = "00. Клавиши";
            MenuKey = c.Bind(kKey, "Меню", new KeyboardShortcut(KeyCode.F1), "Открыть/закрыть меню PPKit.");
            PresetKey = c.Bind(kKey, "Следующий пресет", new KeyboardShortcut(KeyCode.F2), "Переключить пресет производительности по кругу.");
            FpsKey = c.Bind(kKey, "Счётчик FPS", new KeyboardShortcut(KeyCode.F3), "Показать/скрыть счётчик.");
            ReloadLangKey = c.Bind(kKey, "Перечитать перевод", new KeyboardShortcut(KeyCode.F5), "Перечитать ru.json без перезапуска.");
            DumpKey = c.Bind(kKey, "Дамп строк", new KeyboardShortcut(KeyCode.F12), "Слить все строки интерфейса в файл, чтобы дополнить перевод.");

            const string gKey = "01. Производительность";
            PerfEnabled = c.Bind(gKey, "Включено", true, "Главный выключатель оптимизаций.");
            AutoDetectPreset = c.Bind(gKey, "Автопресет по железу", true, "При запуске подобрать пресет по CPU/RAM/VRAM.");
            Preset = c.Bind(gKey, "Пресет", "Средний", new ConfigDescription(
                "Слабый ПК / Средний / Сильный / Кино",
                new AcceptableValueList<string>("Слабый ПК", "Средний", "Сильный", "Кино")));

            TargetFps = c.Bind(gKey, "Лимит FPS", 0, new ConfigDescription(
                "0 — без ограничения.", new AcceptableValueRange<int>(0, 500)));
            VSync = c.Bind(gKey, "Вертикальная синхронизация", false);
            AntiAliasing = c.Bind(gKey, "Сглаживание (MSAA)", 0, new ConfigDescription(
                "0 / 2 / 4 / 8", new AcceptableValueList<int>(0, 2, 4, 8)));
            Shadows = c.Bind(gKey, "Тени", ShadowQuality.All);
            ShadowResolution = c.Bind(gKey, "Разрешение теней", UnityEngine.ShadowResolution.Medium);
            ShadowDistance = c.Bind(gKey, "Дальность теней", 60f, new ConfigDescription(
                "Метры. Меньше — быстрее.", new AcceptableValueRange<float>(0f, 300f)));
            TextureQuality = c.Bind(gKey, "Качество текстур", 2, new ConfigDescription(
                "0 — 1/8, 1 — 1/4, 2 — 1/2, 3 — полное. Меньше — быстрее и меньше VRAM.",
                new AcceptableValueRange<int>(0, 3)));
            AnisotropicFiltering = c.Bind(gKey, "Анизотропная фильтрация", 1, new ConfigDescription(
                "0 выкл, 1 вкл, 2 форс.", new AcceptableValueRange<int>(0, 2)));
            RealtimeReflectionProbes = c.Bind(gKey, "Отражения в реальном времени", false,
                "Дорого. Почти всегда можно выключить.");
            SoftParticles = c.Bind(gKey, "Мягкие частицы", false);
            BillboardFaceCamera = c.Bind(gKey, "Билборды к камере", false);

            MaxParticles = c.Bind(gKey, "Лимит частиц на систему", 250, new ConfigDescription(
                "0 — не трогать. Сильно спасает на сценах с кровью и огнём.",
                new AcceptableValueRange<int>(0, 2000)));
            FixedDeltaTime = c.Bind(gKey, "Шаг физики (fixedDeltaTime)", 0.02f, new ConfigDescription(
                "0.02 — штатно (50 Гц). 0.033 — дешевле, но физика грубее. 0.0167 — точнее и дороже.",
                new AcceptableValueRange<float>(0.01f, 0.05f)));
            MaximumDeltaTime = c.Bind(gKey, "Потолок дельты времени", 0.1f, new ConfigDescription(
                "Защита от «проваливания» физики при просадках.",
                new AcceptableValueRange<float>(0.02f, 0.34f)));
            CameraFarClip = c.Bind(gKey, "Дальность прорисовки", 0f, new ConfigDescription(
                "0 — не менять. 80–150 уменьшает нагрузку на больших картах.",
                new AcceptableValueRange<float>(0f, 1000f)));
            UnloadUnusedAssets = c.Bind(gKey, "Выгружать мусор после сцены", true,
                "Resources.UnloadUnusedAssets после загрузки — меньше жрёт RAM.");

            const string lKey = "02. Язык";
            TranslateUI = c.Bind(lKey, "Русский интерфейс", true, "Переводить текст по словарю ru.json.");
            WindowsFont = c.Bind(lKey, "Шрифт меню PPKit", "Segoe UI",
                "Системный шрифт для собственного меню мода. Если видите квадраты — смените на Arial или Tahoma.");
            LogMissing = c.Bind(lKey, "Писать непереведённые строки", false,
                "Складывает ненайденные строки в BepInEx/config/PPKit/missing_strings.txt.");
            SkipLikelyIds = c.Bind(lKey, "Не трогать ID и пути", true,
                "Пропускать строки, похожие на технические идентификаторы (snake_case, пути, длинные).");
        }
    }
}
