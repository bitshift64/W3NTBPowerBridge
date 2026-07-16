param(
    [switch]$NoDesktopShortcut
)

$ErrorActionPreference = "Stop"

$appName = "W3NTB Power Bridge"
$appId = "W3NTBPowerBridge"
$sourceDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$installDir = Join-Path $env:LOCALAPPDATA "Programs\W3NTB Power Bridge"
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\W3NTB Power Bridge"
$desktopShortcut = Join-Path ([Environment]::GetFolderPath("DesktopDirectory")) "W3NTB Power Bridge.lnk"
$exePath = Join-Path $installDir "W3NTBPowerBridge.exe"
$uninstallScript = Join-Path $installDir "Uninstall-W3NTBPowerBridge.ps1"

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
New-Item -ItemType Directory -Force -Path $startMenuDir | Out-Null

Copy-Item -LiteralPath (Join-Path $sourceDir "W3NTBPowerBridge.exe") -Destination $installDir -Force
$xmlPath = Join-Path $sourceDir "W3NTBPowerBridge.xml"
if (Test-Path -LiteralPath $xmlPath) {
    Copy-Item -LiteralPath $xmlPath -Destination $installDir -Force
}

$uninstallContent = @'
$ErrorActionPreference = "Stop"
$appName = "W3NTB Power Bridge"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\W3NTB Power Bridge"
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\W3NTB Power Bridge"
$desktopShortcut = Join-Path ([Environment]::GetFolderPath("DesktopDirectory")) "W3NTB Power Bridge.lnk"
$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\W3NTBPowerBridge"
Remove-Item -LiteralPath $startMenuDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $desktopShortcut -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $uninstallKey -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $installDir -Recurse -Force -ErrorAction SilentlyContinue
[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null
[System.Windows.Forms.MessageBox]::Show("$appName has been removed.", $appName) | Out-Null
'@

Set-Content -LiteralPath $uninstallScript -Value $uninstallContent -Encoding UTF8

$shell = New-Object -ComObject WScript.Shell

$startShortcut = $shell.CreateShortcut((Join-Path $startMenuDir "W3NTB Power Bridge.lnk"))
$startShortcut.TargetPath = $exePath
$startShortcut.WorkingDirectory = $installDir
$startShortcut.IconLocation = "$exePath,0"
$startShortcut.Description = "Remote station power and N3FJP-to-wfview bridge for W3NTB"
$startShortcut.Save()

$uninstallShortcut = $shell.CreateShortcut((Join-Path $startMenuDir "Uninstall W3NTB Power Bridge.lnk"))
$uninstallShortcut.TargetPath = "powershell.exe"
$uninstallShortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$uninstallScript`""
$uninstallShortcut.WorkingDirectory = $installDir
$uninstallShortcut.IconLocation = "$exePath,0"
$uninstallShortcut.Description = "Uninstall W3NTB Power Bridge"
$uninstallShortcut.Save()

if (-not $NoDesktopShortcut) {
    $desktop = $shell.CreateShortcut($desktopShortcut)
    $desktop.TargetPath = $exePath
    $desktop.WorkingDirectory = $installDir
    $desktop.IconLocation = "$exePath,0"
    $desktop.Description = "Remote station power and N3FJP-to-wfview bridge for W3NTB"
    $desktop.Save()
}

$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$appId"
New-Item -Path $uninstallKey -Force | Out-Null
Set-ItemProperty -Path $uninstallKey -Name "DisplayName" -Value $appName
Set-ItemProperty -Path $uninstallKey -Name "DisplayVersion" -Value "0.1.0-beta"
Set-ItemProperty -Path $uninstallKey -Name "Publisher" -Value "W3NTB"
Set-ItemProperty -Path $uninstallKey -Name "DisplayIcon" -Value $exePath
Set-ItemProperty -Path $uninstallKey -Name "InstallLocation" -Value $installDir
Set-ItemProperty -Path $uninstallKey -Name "UninstallString" -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$uninstallScript`""
Set-ItemProperty -Path $uninstallKey -Name "NoModify" -Value 1 -Type DWord
Set-ItemProperty -Path $uninstallKey -Name "NoRepair" -Value 1 -Type DWord

[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null
[System.Windows.Forms.MessageBox]::Show("$appName has been installed.", $appName) | Out-Null
Start-Process -FilePath $exePath
