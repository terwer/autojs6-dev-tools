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

function Resolve-MsBuildCommand {
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        return @{
            Path = $command.Source
            UseDotNet = $false
        }
    }

    $vsWherePath = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vsWherePath) {
        $resolvedPath = & $vsWherePath -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
            Select-Object -First 1
        if (-not [string]::IsNullOrWhiteSpace($resolvedPath)) {
            return @{
                Path = $resolvedPath.Trim()
                UseDotNet = $false
            }
        }
    }

    $searchRoots = @(
        'C:\Program Files\Microsoft Visual Studio',
        'C:\Program Files (x86)\Microsoft Visual Studio',
        'D:\Software\Microsoft Visual Studio'
    )

    foreach ($root in $searchRoots) {
        if (-not (Test-Path -LiteralPath $root)) {
            continue
        }

        $candidate = Get-ChildItem -Path $root -Recurse -Filter MSBuild.exe -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '\\MSBuild\\Current\\Bin\\MSBuild\.exe$' } |
            Sort-Object FullName -Descending |
            Select-Object -First 1

        if ($candidate) {
            return @{
                Path = $candidate.FullName
                UseDotNet = $false
            }
        }
    }

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnet) {
        return @{
            Path = $dotnet.Source
            UseDotNet = $true
        }
    }

    throw '未找到 msbuild.exe 或 dotnet。请安装 Visual Studio / Build Tools（含 MSBuild）后重试。'
}

function Resolve-SignToolCommand {
    $command = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $windowsKitsRoots = @(
        'C:\Program Files (x86)\Windows Kits\10\bin',
        'D:\Windows Kits\10\bin'
    )

    foreach ($windowsKitsBin in $windowsKitsRoots) {
        if (Test-Path -LiteralPath $windowsKitsBin) {
            $candidate = Get-ChildItem -Path $windowsKitsBin -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -match '\\(x64|x86)\\signtool\.exe$' } |
                Sort-Object FullName -Descending |
                Select-Object -First 1

            if ($candidate) {
                return $candidate.FullName
            }
        }
    }

    throw '未找到 signtool.exe。请安装 Windows 10/11 SDK（SignTool）或 Visual Studio Build Tools 后重试。'
}

$resolvedProjectPath = Resolve-AbsolutePath -Path $ProjectPath -MustExist
$resolvedPackageCertificatePath = Resolve-AbsolutePath -Path $PackageCertificatePath -MustExist
$resolvedOutputRoot = Resolve-AbsolutePath -Path $OutputRoot
$assetVersion = $Version.TrimStart('v')
$fourPartVersion = "$($Version.TrimStart('v')).0"
$packageDirectory = Join-Path $resolvedOutputRoot "msix/$RuntimeIdentifier"
$releaseAssetDirectory = Join-Path $resolvedOutputRoot 'release-assets'
$destination = Join-Path $releaseAssetDirectory "autojs6-dev-tools-$assetVersion-$RuntimeIdentifier.msix"

if (Test-Path -LiteralPath $packageDirectory) {
    Remove-Item -LiteralPath $packageDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $releaseAssetDirectory -Force | Out-Null

$buildTool = Resolve-MsBuildCommand
$signTool = Resolve-SignToolCommand
$appxPackageDir = "$($packageDirectory.TrimEnd('\'))\"

$msbuildArguments = @(
    $resolvedProjectPath,
    '/restore',
    '/p:Configuration=Release',
    "/p:Platform=$Platform",
    "/p:RuntimeIdentifier=$RuntimeIdentifier",
    '/p:GenerateAppxPackageOnBuild=true',
    '/p:AppxBundle=Never',
    '/p:UapAppxPackageBuildMode=SideloadOnly',
    "/p:AppxPackageDir=$appxPackageDir",
    '/p:AppxPackageSigningEnabled=false',
    '/p:WindowsAppSDKSelfContained=true',
    '/p:SelfContained=true',
    '/p:PublishTrimmed=false',
    '/p:PublishReadyToRun=false',
    "/p:Version=$Version",
    "/p:AssemblyVersion=$fourPartVersion",
    "/p:FileVersion=$fourPartVersion",
    "/p:InformationalVersion=$Version"
)

if ($buildTool.UseDotNet) {
    & $buildTool.Path 'msbuild' @msbuildArguments
}
else {
    & $buildTool.Path @msbuildArguments
}

if ($LASTEXITCODE -ne 0) {
    throw "MSIX build failed for $RuntimeIdentifier"
}

$msix = Get-ChildItem -Path $packageDirectory -Filter *.msix -Recurse |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -eq $msix) {
    throw "Unable to locate generated .msix under $packageDirectory"
}

& $signTool sign /fd SHA256 /f $resolvedPackageCertificatePath /p $PackageCertificatePassword /v $msix.FullName
if ($LASTEXITCODE -ne 0) {
    throw "MSIX 签名失败：$($msix.FullName)"
}

& $signTool verify /pa /v $msix.FullName
if ($LASTEXITCODE -ne 0) {
    throw "MSIX 签名校验失败：$($msix.FullName)"
}

Copy-Item -LiteralPath $msix.FullName -Destination $destination -Force

Write-Host "MSIX package created: $destination"
