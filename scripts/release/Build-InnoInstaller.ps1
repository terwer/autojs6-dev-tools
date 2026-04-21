[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ScriptPath,

    [Parameter(Mandatory)]
    [string]$RuntimeIdentifier,

    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$SourceDirectory,

    [Parameter(Mandatory)]
    [string]$OutputDirectory,

    [Parameter(Mandatory)]
    [string]$ArchitecturesAllowed,

    [Parameter(Mandatory)]
    [string]$ArchitecturesInstallIn64BitMode,

    [string]$AppName = 'AutoJS6 Visual Development Toolkit',
    [string]$AppPublisher = 'terwer',
    [string]$AppId = 'space.terwer.autojs6devtools',
    [string]$AppExeName = 'autojs6-dev-tools.exe'
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$windowsVersion = "$($Version.TrimStart('v')).0"
$outputBaseFilename = "autojs6-dev-tools-$RuntimeIdentifier-setup"
$iscc = (Get-Command ISCC.exe -ErrorAction Stop).Source

& $iscc `
    "/DAppName=$AppName" `
    "/DAppVersion=$Version" `
    "/DAppVersionWin=$windowsVersion" `
    "/DAppPublisher=$AppPublisher" `
    "/DAppId=$AppId" `
    "/DAppExeName=$AppExeName" `
    "/DSourceDir=$SourceDirectory" `
    "/DOutputDir=$OutputDirectory" `
    "/DOutputBaseFilename=$outputBaseFilename" `
    "/DArchitecturesAllowed=$ArchitecturesAllowed" `
    "/DArchitecturesInstallIn64BitMode=$ArchitecturesInstallIn64BitMode" `
    $ScriptPath

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup build failed for $RuntimeIdentifier"
}

Write-Host "Installer created: $(Join-Path $OutputDirectory "$outputBaseFilename.exe")"
