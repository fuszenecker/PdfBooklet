#Requires -Version 7
<#
.SYNOPSIS
    Publishes PdfBooklet for Windows (win-x64, self-contained, no .NET needed) and builds the NSIS installer.

.DESCRIPTION
    The release workflow runs it on a Windows runner: only a build on Windows puts the icon and the version into
    PdfBooklet.exe. It also runs on Linux (with makensis from the nsis package), e.g. to check the installer script;
    PdfBooklet.exe then has the default icon.

.PARAMETER Version
    The version (e.g. 1.0.1.0). Defaults to version.txt.

.PARAMETER Output
    The folder of the installer. Defaults to installer/nsis/ in the repository.

.EXAMPLE
    scripts/Build-Installer.ps1 -Version 1.0.1.0
#>
[CmdletBinding()]
param(
    [string] $Version,
    [string] $Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Project = Join-Path $RepoRoot 'src/PdfBooklet.csproj'
$ScriptDir = Join-Path $RepoRoot 'packaging/windows'
$Script = Join-Path $ScriptDir 'PdfBooklet.nsi'
$Publish = Join-Path $RepoRoot 'publish/win-x64'
$Output = if ($Output) { [IO.Path]::GetFullPath($Output) } else { Join-Path $RepoRoot 'installer/nsis' }

if (-not $Version) {
    $Version = (Get-Content (Join-Path $RepoRoot 'version.txt') -Raw).Trim()
}
if (-not $Version) { throw 'The version is empty: update version.txt or pass -Version.' }

# The file version has four numbers and no pre-release suffix: 1.1.0-beta.1 is 1.1.0.0
$Numbers = ($Version -split '[-+]')[0].Split('.')
$FileVersion = (@($Numbers) + @('0', '0', '0', '0'))[0..3] -join '.'

function Find-Makensis {
    $command = Get-Command makensis -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    foreach ($candidate in "${env:ProgramFiles(x86)}\NSIS\makensis.exe", "$env:ProgramFiles\NSIS\makensis.exe") {
        if ($candidate -and (Test-Path $candidate)) { return $candidate }
    }

    throw 'makensis was not found: install NSIS (https://nsis.sourceforge.io, choco install nsis, or apt install nsis).'
}

$Makensis = Find-Makensis

if (Test-Path $Publish) { Remove-Item -Recurse -Force $Publish }

Write-Host "==> Publishing PdfBooklet $Version (win-x64, self-contained)"
& dotnet publish $Project -c Release -r win-x64 --self-contained true `
    -p:Version=$Version -p:FileVersion=$FileVersion -p:AssemblyVersion=$FileVersion -p:DebugType=none `
    -o $Publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)." }

# The native libraries bring their debug symbols, which an installer does not need
Get-ChildItem $Publish -Filter *.pdb -Recurse | Remove-Item

Write-Host '==> Building the installer'
New-Item -ItemType Directory -Force $Output | Out-Null
$Installer = Join-Path $Output "PdfBooklet-$Version-win-x64-setup.exe"

# -D works with makensis on Windows and on Linux; the script finds its other files relative to itself
Push-Location $ScriptDir
try {
    & $Makensis -V2 "-DAPP_VERSION=$Version" "-DVI_VERSION=$FileVersion" "-DPUBLISH_DIR=$Publish" "-DOUT_FILE=$Installer" $Script
    if ($LASTEXITCODE -ne 0) { throw "makensis failed ($LASTEXITCODE)." }
}
finally {
    Pop-Location
}

$Size = [math]::Round((Get-Item $Installer).Length / 1MB, 1)
Write-Host "==> $Installer ($Size MB)"
