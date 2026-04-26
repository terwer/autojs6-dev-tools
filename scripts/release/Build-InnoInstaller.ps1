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
    [string]$AppExeName = 'autojs6-dev-tools.exe',
    [string]$InnoSetupExecutablePath
)

$ErrorActionPreference = 'Stop'

function Resolve-AbsolutePath {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [switch]$MustExist
    )

    if ($MustExist) {
        return (Resolve-Path -LiteralPath $Path -ErrorAction Stop).Path
    }

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
}

function Resolve-InnoSetupCompiler {
    param(
        [string]$PreferredPath
    )

    $candidates = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($PreferredPath)) {
        $candidates.Add((Resolve-AbsolutePath -Path $PreferredPath))
    }

    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) {
        $candidates.Add($command.Source)
    }

    $envHome = $env:INNO_SETUP_HOME
    if (-not [string]::IsNullOrWhiteSpace($envHome)) {
        $candidates.Add((Join-Path $envHome 'ISCC.exe'))
    }

    $candidates.Add('D:\Software\Inno Setup 6\ISCC.exe')
    $candidates.Add('C:\Program Files (x86)\Inno Setup 6\ISCC.exe')
    $candidates.Add('C:\Program Files\Inno Setup 6\ISCC.exe')

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if (Test-Path -LiteralPath $candidate) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    $searched = $candidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique
    throw "未找到 ISCC.exe。请安装 Inno Setup 6，或设置 INNO_SETUP_HOME，或传入 -InnoSetupExecutablePath。已检查路径：$($searched -join '; ')"
}

$resolvedScriptPath = Resolve-AbsolutePath -Path $ScriptPath -MustExist
$resolvedSourceDirectory = Resolve-AbsolutePath -Path $SourceDirectory -MustExist
$resolvedOutputDirectory = Resolve-AbsolutePath -Path $OutputDirectory
$iscc = Resolve-InnoSetupCompiler -PreferredPath $InnoSetupExecutablePath

New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

$windowsVersion = "$($Version.TrimStart('v')).0"
$outputBaseFilename = "autojs6-dev-tools-$RuntimeIdentifier-setup"
$expectedInstallerPath = Join-Path $resolvedOutputDirectory "$outputBaseFilename.exe"

& $iscc `
    "/DAppName=$AppName" `
    "/DAppVersion=$Version" `
    "/DAppVersionWin=$windowsVersion" `
    "/DAppPublisher=$AppPublisher" `
    "/DAppId=$AppId" `
    "/DAppExeName=$AppExeName" `
    "/DSourceDir=$resolvedSourceDirectory" `
    "/DOutputDir=$resolvedOutputDirectory" `
    "/DOutputBaseFilename=$outputBaseFilename" `
    "/DArchitecturesAllowed=$ArchitecturesAllowed" `
    "/DArchitecturesInstallIn64BitMode=$ArchitecturesInstallIn64BitMode" `
    $resolvedScriptPath

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup build failed for $RuntimeIdentifier"
}

if (-not (Test-Path -LiteralPath $expectedInstallerPath)) {
    throw "安装器未生成：$expectedInstallerPath"
}

Write-Host "Installer created: $expectedInstallerPath"
