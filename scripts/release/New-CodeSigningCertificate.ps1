[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Subject,

    [Parameter(Mandatory)]
    [string]$OutputDirectory,

    [string]$FriendlyName = 'AutoJS6 Visual Development Toolkit CI Signing',
    [string]$PackageManifestPath = 'App/Package.appxmanifest',
    [string]$TrustedPeopleStorePath = 'Cert:\CurrentUser\TrustedPeople',
    [string]$TrustedRootStorePath = 'Cert:\CurrentUser\Root'
)

$ErrorActionPreference = 'Stop'

function Write-Step {
    param(
        [Parameter(Mandatory)]
        [string]$Message
    )

    Write-Host "[cert] $Message"
}

function Invoke-Step {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [scriptblock]$Action
    )

    Write-Step "$Name ..."
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    & $Action
    $stopwatch.Stop()
    Write-Step "$Name completed in $($stopwatch.Elapsed.TotalSeconds.ToString('F1'))s"
}

function Resolve-AbsolutePath {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
}

function Get-PackagePublisher {
    param(
        [Parameter(Mandatory)]
        [string]$ManifestPath
    )

    [xml]$packageManifest = Get-Content -LiteralPath $ManifestPath
    $namespaceManager = New-Object System.Xml.XmlNamespaceManager($packageManifest.NameTable)
    $namespaceManager.AddNamespace('pkg', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')

    $identityNode = $packageManifest.SelectSingleNode('/pkg:Package/pkg:Identity', $namespaceManager)
    if ($null -eq $identityNode) {
        throw "无法从 $ManifestPath 读取 Package Identity。"
    }

    $publisher = $identityNode.GetAttribute('Publisher')
    if ([string]::IsNullOrWhiteSpace($publisher)) {
        throw "Package Identity 的 Publisher 为空：$ManifestPath"
    }

    return $publisher.Trim()
}

Write-Step "Starting code-signing certificate setup"

$resolvedOutputDirectory = Resolve-AbsolutePath -Path $OutputDirectory
$resolvedManifestPath = Resolve-AbsolutePath -Path $PackageManifestPath
$normalizedSubject = $Subject.Trim()

Write-Step "Manifest path: $resolvedManifestPath"
Write-Step "Output directory: $resolvedOutputDirectory"
Write-Step "Requested subject: $normalizedSubject"

$expectedPublisher = Invoke-Step -Name "Reading package publisher from manifest" -Action {
    Get-PackagePublisher -ManifestPath $resolvedManifestPath
}

Write-Step "Package publisher: $expectedPublisher"

if ($normalizedSubject -ne $expectedPublisher) {
    throw "证书 Subject（$normalizedSubject）必须与 Package.appxmanifest 中的 Publisher（$expectedPublisher）完全一致。"
}

Invoke-Step -Name "Ensuring output directory exists" -Action {
    New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null
}

$pfxPath = Join-Path $resolvedOutputDirectory 'autojs6-dev-tools-signing.pfx'
$cerPath = Join-Path $resolvedOutputDirectory 'autojs6-dev-tools-signing.cer'
$password = "rp$([Guid]::NewGuid().ToString('N'))"
$securePassword = ConvertTo-SecureString -String $password -AsPlainText -Force

$certificate = $null
Invoke-Step -Name "Creating self-signed certificate" -Action {
    $script:certificate = New-SelfSignedCertificate `
        -Subject $normalizedSubject `
        -FriendlyName $FriendlyName `
        -Type Custom `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -KeyUsage DigitalSignature `
        -KeySpec Signature `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -NotAfter (Get-Date).AddYears(3) `
        -TextExtension @(
            '2.5.29.19={text}',
            '2.5.29.37={text}1.3.6.1.5.5.7.3.3'
        )
}

Write-Step "Certificate thumbprint: $($certificate.Thumbprint)"

Invoke-Step -Name "Exporting PFX certificate" -Action {
    Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $securePassword | Out-Null
}

Invoke-Step -Name "Exporting CER certificate" -Action {
    Export-Certificate -Cert $certificate -FilePath $cerPath | Out-Null
}

Invoke-Step -Name "Importing certificate into TrustedPeople" -Action {
    Import-Certificate -FilePath $cerPath -CertStoreLocation $TrustedPeopleStorePath | Out-Null
}

Invoke-Step -Name "Importing certificate into Root store" -Action {
    Import-Certificate -FilePath $cerPath -CertStoreLocation $TrustedRootStorePath | Out-Null
}

$resolvedPfxPath = (Resolve-Path -LiteralPath $pfxPath).Path
$resolvedCerPath = (Resolve-Path -LiteralPath $cerPath).Path

Write-Host "Generated self-signed certificate at $resolvedPfxPath"
Write-Step "CER path: $resolvedCerPath"

if (Test-Path Env:GITHUB_OUTPUT) {
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "pfx_path=$resolvedPfxPath"
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "cer_path=$resolvedCerPath"
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "password=$password"
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "thumbprint=$($certificate.Thumbprint)"
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "trusted_people_store=$TrustedPeopleStorePath"
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "trusted_root_store=$TrustedRootStorePath"
}

Write-Step "Code-signing certificate setup finished"
