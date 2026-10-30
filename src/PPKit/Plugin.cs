using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PPKit.Perf;
using PPKit.UI;
using UnityEngine;

namespace PPKit
{
    /// <summary>
    /// Точка входа плагина. Ничего не патчит в логике игры — только настройки
    /// графики/физики и текст интерфейса.
    /// </summary>
    [BepInPlugin(Guid, PluginName, PluginVersion)]
    public sealed class PPKitPlugin : BaseUnityPlugin
    {
        public const string Guid = "ru.ppkit.optimizer";
        public const string PluginName = "PPKit — оптимизация и русский интерфейс";
        public const string PluginVersion = "1.0.0";

        internal static PPKitPlugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }
        internal static ModConfig Cfg { get; private set; }

        private Harmony _harmony;
        private PerformanceManager _perf;
        private SettingsWindow _window;
        private FpsOverlay _fps;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Cfg = new ModConfig(Config);

            Localization.Init(Logger, Cfg);
            TextPatches.Apply(_harmony = new Harmony(Guid));

            _perf = new PerformanceManager(Cfg);
            _window = new SettingsWindow(Cfg, _perf);
            _fps = new FpsOverlay(Cfg, _perf);

            Log.LogInfo($"{PluginName} v{PluginVersion} загружен. Словарь: {Localization.FilePath}");
        }

        private void Start()
        {
            _perf.ApplyStartupPreset();
        }

        private void Update()
        {
            var k = Cfg.MenuKey.Value;
            if (k.IsDown()) _window.Toggle();

            if (Cfg.PresetKey.Value.IsDown()) _perf.CyclePreset();

            if (Cfg.FpsKey.Value.IsDown()) _fps.Toggle();

            if (Cfg.ReloadLangKey.Value.IsDown())
            {
                Localization.Reload();
                Log.LogInfo($"Словарь перечитан: {Localization.Count} строк.");
            }

            if (Cfg.DumpKey.Value.IsDown()) TextPatches.DumpActiveStrings();

            _perf.Tick();
            _fps.Sample();
        }

        private void OnGUI()
        {
            if (_fps.Visible) _fps.Draw();
            if (_window.Visible) _window.Draw();
            ScreenMessages.Draw();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _perf?.Dispose();
        }
    }
}
