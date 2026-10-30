using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PPKit.Perf
{
    /// <summary>
    /// Применяет настройки графики/физики. Ничего не хардкодит в игре:
    /// всё идёт через штатные Unity API и переприменяется после загрузки сцены,
    /// т.к. игра сама сбрасывает часть QualitySettings при переходах.
    /// </summary>
    public sealed class PerformanceManager : IDisposable
    {
        public static readonly string[] PresetNames = { "Слабый ПК", "Средний", "Сильный", "Кино" };

        private readonly ModConfig _cfg;
        private int _sceneHandle = int.MinValue;
        private float _reapplyTimer;
        private float _unloadTimer;
        private bool _disposed;

        public string CurrentPreset => _cfg.Preset.Value;
        public int CurrentPresetIndex
        {
            get
            {
                int i = Array.IndexOf(PresetNames, _cfg.Preset.Value);
                return i >= 0 ? i : 1;
            }
        }
        public float LastUnloadMs { get; private set; }

        public PerformanceManager(ModConfig cfg)
        {
            _cfg = cfg;
        }

        // ------------------------------------------------------------------ запуск

        public void ApplyStartupPreset()
        {
            if (!_cfg.PerfEnabled.Value) return;

            var name = _cfg.Preset.Value;
            if (_cfg.AutoDetectPreset.Value)
            {
                name = DetectPreset();
                PPKitPlugin.Log.LogInfo($"Автопресет по железу: {name} " +
                                        $"(CPU {SystemInfo.processorCount} ядер, RAM {SystemInfo.systemMemorySize} МБ, VRAM {SystemInfo.graphicsMemorySize} МБ)");
            }

            _cfg.Preset.Value = name;
            ApplyPreset(name);
        }

        /// <summary>Грубая, но предсказуемая эвристика. Не «магия», а порог.</summary>
        private static string DetectPreset()
        {
            int cores = SystemInfo.processorCount;
            int ram = SystemInfo.systemMemorySize;      // МБ
            int vram = SystemInfo.graphicsMemorySize;   // МБ

            if (ram <= 4096 || vram <= 1024 || cores <= 2) return "Слабый ПК";
            if (ram <= 8192 || vram <= 2048 || cores <= 4) return "Средний";
            if (ram <= 16384 || vram <= 4096) return "Сильный";
            return "Кино";
        }

        public void CyclePreset()
        {
            int next = (CurrentPresetIndex + 1) % PresetNames.Length;
            _cfg.AutoDetectPreset.Value = false;
            _cfg.Preset.Value = PresetNames[next];
            ApplyPreset(PresetNames[next]);
        }

        // ------------------------------------------------------------------ пресеты

        public void ApplyPreset(string name)
        {
            switch (name)
            {
                case "Слабый ПК":
                    _cfg.AntiAliasing.Value = 0;
                    _cfg.Shadows.Value = ShadowQuality.HardOnly;
                    _cfg.ShadowResolution.Value = UnityEngine.ShadowResolution.Low;
                    _cfg.ShadowDistance.Value = 25f;
                    _cfg.TextureQuality.Value = 1;
                    _cfg.AnisotropicFiltering.Value = 0;
                    _cfg.RealtimeReflectionProbes.Value = false;
                    _cfg.SoftParticles.Value = false;
                    _cfg.MaxParticles.Value = 120;
                    _cfg.CameraFarClip.Value = 90f;
                    _cfg.VSync.Value = false;
                    _cfg.TargetFps.Value = 60;
                    break;

                case "Средний":
                    _cfg.AntiAliasing.Value = 0;
                    _cfg.Shadows.Value = ShadowQuality.All;
                    _cfg.ShadowResolution.Value = UnityEngine.ShadowResolution.Medium;
                    _cfg.ShadowDistance.Value = 50f;
                    _cfg.TextureQuality.Value = 2;
                    _cfg.AnisotropicFiltering.Value = 1;
                    _cfg.RealtimeReflectionProbes.Value = false;
                    _cfg.SoftParticles.Value = false;
                    _cfg.MaxParticles.Value = 250;
                    _cfg.CameraFarClip.Value = 0f;
                    _cfg.VSync.Value = false;
                    _cfg.TargetFps.Value = 0;
                    break;

                case "Сильный":
                    _cfg.AntiAliasing.Value = 2;
                    _cfg.Shadows.Value = ShadowQuality.All;
                    _cfg.ShadowResolution.Value = UnityEngine.ShadowResolution.High;
                    _cfg.ShadowDistance.Value = 90f;
                    _cfg.TextureQuality.Value = 3;
                    _cfg.AnisotropicFiltering.Value = 1;
                    _cfg.RealtimeReflectionProbes.Value = false;
                    _cfg.SoftParticles.Value = true;
                    _cfg.MaxParticles.Value = 500;
                    _cfg.CameraFarClip.Value = 0f;
                    _cfg.VSync.Value = false;
                    _cfg.TargetFps.Value = 0;
                    break;

                default: // Кино
                    _cfg.AntiAliasing.Value = 4;
                    _cfg.Shadows.Value = ShadowQuality.All;
                    _cfg.ShadowResolution.Value = UnityEngine.ShadowResolution.VeryHigh;
                    _cfg.ShadowDistance.Value = 150f;
                    _cfg.TextureQuality.Value = 3;
                    _cfg.AnisotropicFiltering.Value = 2;
                    _cfg.RealtimeReflectionProbes.Value = true;
                    _cfg.SoftParticles.Value = true;
                    _cfg.MaxParticles.Value = 0;
                    _cfg.CameraFarClip.Value = 0f;
                    _cfg.VSync.Value = true;
                    _cfg.TargetFps.Value = 0;
                    break;
            }

            ApplyNow();
            PPKitPlugin.Log.LogInfo($"Пресет «{name}» применён.");
        }

        // ------------------------------------------------------------------ применение

        public void ApplyNow()
        {
            if (!_cfg.PerfEnabled.Value) return;

            try
            {
                QualitySettings.vSyncCount = _cfg.VSync.Value ? 1 : 0;
                Application.targetFrameRate = _cfg.TargetFps.Value;

                QualitySettings.antiAliasing = _cfg.AntiAliasing.Value;
                QualitySettings.shadows = _cfg.Shadows.Value;
                QualitySettings.shadowResolution = _cfg.ShadowResolution.Value;
                QualitySettings.shadowDistance = _cfg.ShadowDistance.Value;
                QualitySettings.masterTextureLimit = 3 - Mathf.Clamp(_cfg.TextureQuality.Value, 0, 3);
                QualitySettings.anisotropicFiltering = (AnisotropicFiltering)_cfg.AnisotropicFiltering.Value;
                QualitySettings.realtimeReflectionProbes = _cfg.RealtimeReflectionProbes.Value;
                QualitySettings.softParticles = _cfg.SoftParticles.Value;
                QualitySettings.billboardsFaceCameraPosition = _cfg.BillboardFaceCamera.Value;

                if (_cfg.FixedDeltaTime.Value > 0f)
                    Time.fixedDeltaTime = _cfg.FixedDeltaTime.Value;
                if (_cfg.MaximumDeltaTime.Value > 0f)
                    Time.maximumDeltaTime = _cfg.MaximumDeltaTime.Value;

                ApplyCameras();
                ApplyParticleCap();

                _reapplyTimer = 3f;
            }
            catch (Exception e)
            {
                PPKitPlugin.Log.LogWarning($"Не удалось применить настройки: {e.Message}");
            }
        }

        private void ApplyCameras()
        {
            float clip = _cfg.CameraFarClip.Value;
            if (clip <= 0f) return;

            foreach (var cam in Camera.allCameras)
            {
                if (cam == null) continue;
                // Не режем камеру, которая смотрит слишком далеко вперёд по логике игры:
                // только уменьшаем, никогда не увеличиваем.
                if (cam.farClipPlane > clip) cam.farClipPlane = clip;
                cam.allowMSAA = _cfg.AntiAliasing.Value > 0;
            }
        }

        private void ApplyParticleCap()
        {
            int cap = _cfg.MaxParticles.Value;
            if (cap <= 0) return;

            foreach (var ps in Object.FindObjectsOfType<ParticleSystem>())
            {
                if (ps == null) continue;
                var main = ps.main;
                if (main.maxParticles > cap) main.maxParticles = cap;
            }
        }

        // ------------------------------------------------------------------ каждый кадр

        public void Tick()
        {
            if (_disposed || !_cfg.PerfEnabled.Value) return;

            // Игра пересоздаёт сцену при загрузке/сохранении — настройки нужно дозалить.
            var scene = SceneManager.GetActiveScene();
            if (scene.handle != _sceneHandle)
            {
                _sceneHandle = scene.handle;
                _reapplyTimer = 1.5f;
                _unloadTimer = 2f;
            }

            if (_reapplyTimer > 0f)
            {
                _reapplyTimer -= Time.unscaledDeltaTime;
                if (_reapplyTimer <= 0f) ApplyNow();
            }

            if (_unloadTimer > 0f && _cfg.UnloadUnusedAssets.Value)
            {
                _unloadTimer -= Time.unscaledDeltaTime;
                if (_unloadTimer <= 0f)
                {
                    float t0 = Time.realtimeSinceStartup;
                    Resources.UnloadUnusedAssets();
                    LastUnloadMs = (Time.realtimeSinceStartup - t0) * 1000f;
                }
            }
        }

        public void Dispose() => _disposed = true;
    }
}
