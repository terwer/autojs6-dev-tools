[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ProjectPath,

    [Parameter(Mandatory)]
    [string]$RuntimeIdentifier,

    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$OutputRoot,

    [string]$ExecutableName = 'autojs6-dev-tools'
)

$ErrorActionPreference = 'Stop'

$fourPartVersion = "$($Version.TrimStart('v')).0"
$publishDirectory = Join-Path $OutputRoot "publish/$RuntimeIdentifier"
$releaseAssetDirectory = Join-Path $OutputRoot 'release-assets'
$zipPath = Join-Path $releaseAssetDirectory "autojs6-dev-tools-$RuntimeIdentifier-portable.zip"
$expectedExecutable = Join-Path $publishDirectory "$ExecutableName.exe"

New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $releaseAssetDirectory -Force | Out-Null

dotnet publish $ProjectPath `
    -c Release `
    -r $RuntimeIdentifier `
    --self-contained true `
    -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=false `
    -p:Version=$Version `
    -p:AssemblyVersion=$fourPartVersion `
    -p:FileVersion=$fourPartVersion `
    -p:InformationalVersion=$Version `
    -o $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for $RuntimeIdentifier"
}

if (-not (Test-Path -LiteralPath $expectedExecutable)) {
    throw "Portable publish is missing executable: $expectedExecutable"
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "Portable package created: $zipPath"
