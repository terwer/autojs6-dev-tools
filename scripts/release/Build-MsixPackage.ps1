[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ProjectPath,

    [Parameter(Mandatory)]
    [string]$Platform,

    [Parameter(Mandatory)]
    [string]$RuntimeIdentifier,

    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$OutputRoot,

    [Parameter(Mandatory)]
    [string]$PackageCertificatePath,

    [Parameter(Mandatory)]
    [string]$PackageCertificatePassword
)

$ErrorActionPreference = 'Stop'

$fourPartVersion = "$($Version.TrimStart('v')).0"
$packageDirectory = Join-Path $OutputRoot "msix/$RuntimeIdentifier"
$releaseAssetDirectory = Join-Path $OutputRoot 'release-assets'

New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $releaseAssetDirectory -Force | Out-Null

msbuild $ProjectPath `
    /restore `
    /p:Configuration=Release `
    /p:Platform=$Platform `
    /p:RuntimeIdentifier=$RuntimeIdentifier `
    /p:GenerateAppxPackageOnBuild=true `
    /p:AppxBundle=Never `
    /p:UapAppxPackageBuildMode=SideloadOnly `
    /p:AppxPackageDir="$packageDirectory\" `
    /p:PackageCertificateKeyFile=$PackageCertificatePath `
    /p:PackageCertificatePassword=$PackageCertificatePassword `
    /p:PackageCertificateThumbprint= `
    /p:AppxPackageSigningEnabled=true `
    /p:WindowsAppSDKSelfContained=true `
    /p:SelfContained=true `
    /p:PublishTrimmed=false `
    /p:PublishReadyToRun=false `
    /p:Version=$Version `
    /p:AssemblyVersion=$fourPartVersion `
    /p:FileVersion=$fourPartVersion `
    /p:InformationalVersion=$Version

if ($LASTEXITCODE -ne 0) {
    throw "MSIX build failed for $RuntimeIdentifier"
}

$msix = Get-ChildItem -Path $packageDirectory -Filter *.msix -Recurse |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -eq $msix) {
    throw "Unable to locate generated .msix under $packageDirectory"
}

$destination = Join-Path $releaseAssetDirectory "autojs6-dev-tools-$RuntimeIdentifier.msix"
Copy-Item -LiteralPath $msix.FullName -Destination $destination -Force

Write-Host "MSIX package created: $destination"
