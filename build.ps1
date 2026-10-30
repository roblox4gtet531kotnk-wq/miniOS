#Requires -Version 5.1
<#
    Сборка PPKit. Требуется .NET SDK 6+ и установленная (легально купленная) игра —
    из неё берутся reference-сборки Unity и BepInEx.

    Примеры:
      ./build.ps1
      ./build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\People Playground"
      ./build.ps1 -RefDir "C:\refs" -NoCopy
#>
[CmdletBinding()]
param(
    [string]$GameDir = $env:PP_GAME_DIR,
    [string]$RefDir  = $env:PP_REF_DIR,
    [switch]$NoCopy
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

function Find-GameDir {
    $candidates = @(
        "$env:ProgramFiles(x86)\Steam\steamapps\common\People Playground",
        "$env:ProgramFiles\Steam\steamapps\common\People Playground"
    )
    foreach ($drive in (Get-PSDrive -PSProvider FileSystem).Root) {
        foreach ($lib in @('SteamLibrary', 'Steam', 'Games')) {
            $candidates += Join-Path $drive "$lib\steamapps\common\People Playground"
        }
    }
    foreach ($c in $candidates) {
        if ($c -and (Test-Path (Join-Path $c 'People Playground.exe'))) { return $c }
    }
    return $null
}

if (-not $GameDir) { $GameDir = Find-GameDir }

if ($GameDir) {
    if (-not (Test-Path $GameDir)) { throw "Папка игры не найдена: $GameDir" }
    $env:PP_GAME_DIR = $GameDir
    $managed = Join-Path $GameDir 'People Playground_Data\Managed'
    $core    = Join-Path $GameDir 'BepInEx\core'
    if (-not (Test-Path (Join-Path $managed 'UnityEngine.dll'))) {
        throw "В $managed нет UnityEngine.dll — это не папка People Playground?"
    }
    if (-not (Test-Path (Join-Path $core 'BepInEx.dll'))) {
        Write-Warning "BepInEx не найден в $core. Установите BepInEx 5.4.x x64 в игру, иначе плагин не запустится."
    }
    Write-Host "Игра: $GameDir" -ForegroundColor Cyan
}
elseif ($RefDir) {
    $env:PP_REF_DIR = $RefDir
    Write-Host "Reference-папка: $RefDir" -ForegroundColor Cyan
}
else {
    throw "Укажите -GameDir, -RefDir или переменную PP_GAME_DIR."
}

Write-Host "Сборка..." -ForegroundColor Cyan
dotnet build (Join-Path $root 'src/PPKit/PPKit.csproj') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet build завершился с ошибкой $LASTEXITCODE." }

$dll = Join-Path $root 'src/PPKit/bin/Release/PPKit.dll'
if (-not (Test-Path $dll)) { throw "Не найден $dll" }

$dist = Join-Path $root 'dist'
$pluginDir = Join-Path $dist 'BepInEx/plugins/PPKit'
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
Copy-Item $dll $pluginDir -Force
Copy-Item (Join-Path $root 'src/PPKit/Loc/ru.json') $dist -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $root 'README.md') $dist -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $root 'packaging/manifest.json') $pluginDir -Force -ErrorAction SilentlyContinue

# Thunderstore / r2modman архив
$zip = Join-Path $dist 'ppkit-thunderstore.zip'
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $dist 'BepInEx'), (Join-Path $dist 'manifest.json') -DestinationPath $zip -ErrorAction SilentlyContinue
if (-not (Test-Path $zip)) {
    Compress-Archive -Path (Join-Path $dist 'BepInEx') -DestinationPath $zip
}

Write-Host "Готово: $pluginDir" -ForegroundColor Green
Write-Host "Архив: $zip" -ForegroundColor Green

if ($GameDir -and -not $NoCopy) {
    $target = Join-Path $GameDir 'BepInEx/plugins/PPKit'
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Copy-Item $dll $target -Force
    Write-Host "Установлено в игру: $target" -ForegroundColor Green
}
