# PPKit — оптимизация + русский интерфейс для People Playground

Мод-пак для **своей, легально купленной** копии People Playground.
Ставится через BepInEx 5, ничего из файлов игры не содержит и не распространяет.

> **Важно.** Это не «модифицированная сборка игры». Игра платная и закрытая, файлы игры
> распространять нельзя. PPKit — это плагин: он подключается к вашей установке и меняет
> только настройки графики/физики и текст интерфейса в рантайме.

---

## Что внутри

### 1. Оптимизация (`Perf`)
- 4 готовых пресета: **Слабый ПК / Средний / Сильный / Кино** + автоопределение по железу.
- Тени: качество, разрешение, дистанция.
- Сглаживание (MSAA), vsync, лимит FPS, качество текстур, анизотропия.
- Отключение realtime-отражений и soft particles — самые дорогие штуки в сценах с мясом.
- Лимит частиц (`ParticleSystem.maxParticles`) — помогает, когда на карте 200 тел и кровь.
- Настройка `Time.fixedDeltaTime` / `maximumDeltaTime` — физика перестаёт «захлёбываться» на слабых CPU.
- `Resources.UnloadUnusedAssets()` после загрузки сцены (по желанию).
- Ограничение `farClipPlane` камер и повторное применение настроек после каждой загрузки сцены.
- Счётчик FPS/времени кадра по `F3`.

### 2. Русский интерфейс (`Loc`)
- Словарь `ru.json` — **внешний**, лежит в `BepInEx/config/PPKit/ru.json`.
  Правится блокнотом, применяется по `F5` без пересборки мода.
- В комплекте стартовый набор строк (меню, кнопки, настройки, пауза).
- Перевод делается на уровне рендера текста (`Text.set_text` / `TMP_Text.set_text`),
  поэтому работает и для UI, и для подсказок, и для модов, которые используют штатные классы Unity.
- `F12` — дамп всех строк интерфейса в `BepInEx/config/PPKit/strings_dump.txt`,
  чтобы добить перевод (см. `docs/TRANSLATION.md`).
- Защита от «перелома» игры: не переводятся строки, похожие на ID/пути
  (`snake_case`, длинные, с цифрами) — их игра использует как ключи.

### 3. Меню настроек (`F1`)
Своё окно поверх игры: пресеты, ползунки, тумблеры, язык, горячие клавиши.
Все значения сохраняются в `BepInEx/config/PPKit/ru.ppkit.optimizer.cfg`.

---

## Установка (для друзей)

1. Скачать **BepInEx 5.4.x x64** — https://github.com/BepInEx/BepInEx/releases
2. Распаковать в корень игры: `...\steamapps\common\People Playground\`
   (рядом с `People Playground.exe` должна появиться папка `BepInEx`).
3. Запустить игру один раз и закрыть — BepInEx создаст `BepInEx/plugins` и `BepInEx/config`.
4. Положить `dist/BepInEx/plugins/PPKit/` в `...\People Playground\BepInEx\plugins\`.
5. Запустить игру. В логе `BepInEx/LogOutput.log` должна быть строка про PPKit.

### Горячие клавиши
| Клавиша | Действие |
|---|---|
| `F1` | меню PPKit |
| `F2` | переключить пресет по кругу |
| `F3` | счётчик FPS |
| `F5` | перечитать `ru.json` |
| `F12` | дамп строк интерфейса |

Клавиши меняются в конфиге (`BepInEx/config/PPKit/ru.ppkit.optimizer.cfg`).

---

## Сборка из исходников

Нужен .NET SDK 6+ и установленная игра (из неё берутся reference-сборки Unity/BepInEx,
поэтому NuGet и версии Unity не важны).

```powershell
# Windows
$env:PP_GAME_DIR = "C:\Program Files (x86)\Steam\steamapps\common\People Playground"
./build.ps1
```

```bash
# Linux/macOS/CI (только компиляция, без копирования в игру)
PP_GAME_DIR="/path/to/People Playground" ./build.sh
```

Результат: `dist/BepInEx/plugins/PPKit/PPKit.dll` + `dist/ppkit-thunderstore.zip`
(r2modman/Thunderstore-совместимый архив).

Переменные:
- `PP_GAME_DIR` — корень игры (в нём `People Playground_Data/Managed` и `BepInEx/core`).
- `PP_REF_DIR` — если папки `Managed` нет: путь до папки с `UnityEngine*.dll` и `BepInEx.dll`.

---

## Структура

```
src/PPKit/
  PPKit.csproj          — ссылки на DLL из установленной игры
  Plugin.cs             — точка входа
  ModConfig.cs          — конфиг (BepInEx ConfigFile)
  Perf/PerformanceManager.cs
  Perf/FpsOverlay.cs
  UI/SettingsWindow.cs  — меню на IMGUI
  Loc/Localization.cs   — словарь + мини-парсер JSON
  Loc/TextPatches.cs    — Harmony-патчи текста
  Loc/ru.json           — стартовый перевод (embedded resource)
build.ps1 / build.sh
docs/TRANSLATION.md
packaging/manifest.json — Thunderstore
```

## Лицензия
Код — MIT (см. `LICENSE`). Тексты перевода — CC0. Файлы игры сюда не входят и входить не могут.
