[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Subject,

    [Parameter(Mandatory)]
    [string]$OutputDirectory,

    [string]$FriendlyName = 'AutoJS6 Visual Development Toolkit CI Signing'
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$pfxPath = Join-Path $OutputDirectory 'autojs6-dev-tools-signing.pfx'
$cerPath = Join-Path $OutputDirectory 'autojs6-dev-tools-signing.cer'
$password = "rp$([Guid]::NewGuid().ToString('N'))"
$securePassword = ConvertTo-SecureString -String $password -AsPlainText -Force

$certificate = New-SelfSignedCertificate `
    -Subject $Subject `
    -FriendlyName $FriendlyName `
    -Type Custom `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -KeyExportPolicy Exportable `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -NotAfter (Get-Date).AddYears(3) `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3')

Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $securePassword | Out-Null
Export-Certificate -Cert $certificate -FilePath $cerPath | Out-Null

Write-Host "Generated self-signed certificate at $pfxPath"

if (Test-Path Env:GITHUB_OUTPUT) {
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "pfx_path=$pfxPath"
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "cer_path=$cerPath"
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "password=$password"
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "thumbprint=$($certificate.Thumbprint)"
}
