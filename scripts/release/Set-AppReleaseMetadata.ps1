[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$ProductName = 'AutoJS6 Visual Development Toolkit',
    [string]$PackageIdentityName = 'space.terwer.autojs6devtools',
    [string]$Publisher = 'CN=terwer',
    [string]$PublisherDisplayName = 'terwer',
    [string]$PackageManifestPath = 'App/Package.appxmanifest',
    [string]$Win32ManifestPath = 'App/app.manifest'
)

$ErrorActionPreference = 'Stop'

function Get-FourPartVersion {
    param(
        [Parameter(Mandatory)]
        [string]$SemanticVersion
    )

    $normalizedVersion = $SemanticVersion.Trim()

    if ($normalizedVersion.StartsWith('v')) {
        $normalizedVersion = $normalizedVersion.Substring(1)
    }

    $versionParts = $normalizedVersion.Split('.')

    if ($versionParts.Count -ne 3) {
        throw "Version must use semantic format x.y.z. Actual: $SemanticVersion"
    }

    return "$normalizedVersion.0"
}

$fourPartVersion = Get-FourPartVersion -SemanticVersion $Version

[xml]$packageManifest = Get-Content -LiteralPath $PackageManifestPath
$packageNamespaceManager = New-Object System.Xml.XmlNamespaceManager($packageManifest.NameTable)
$packageNamespaceManager.AddNamespace('pkg', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
$packageNamespaceManager.AddNamespace('uap', 'http://schemas.microsoft.com/appx/manifest/uap/windows10')

$identityNode = $packageManifest.SelectSingleNode('/pkg:Package/pkg:Identity', $packageNamespaceManager)
$propertiesNode = $packageManifest.SelectSingleNode('/pkg:Package/pkg:Properties', $packageNamespaceManager)
$visualElementsNode = $packageManifest.SelectSingleNode('/pkg:Package/pkg:Applications/pkg:Application/uap:VisualElements', $packageNamespaceManager)

if ($null -eq $identityNode -or $null -eq $propertiesNode -or $null -eq $visualElementsNode) {
    throw 'Failed to locate required nodes in App/Package.appxmanifest.'
}

$identityNode.SetAttribute('Name', $PackageIdentityName)
$identityNode.SetAttribute('Publisher', $Publisher)
$identityNode.SetAttribute('Version', $fourPartVersion)

$displayNameNode = $propertiesNode.SelectSingleNode('pkg:DisplayName', $packageNamespaceManager)
$publisherDisplayNameNode = $propertiesNode.SelectSingleNode('pkg:PublisherDisplayName', $packageNamespaceManager)

if ($null -eq $displayNameNode -or $null -eq $publisherDisplayNameNode) {
    throw 'Failed to locate package display nodes in App/Package.appxmanifest.'
}

$displayNameNode.InnerText = $ProductName
$publisherDisplayNameNode.InnerText = $PublisherDisplayName

$visualElementsNode.SetAttribute('DisplayName', $ProductName)
$visualElementsNode.SetAttribute('Description', 'AutoJS6 image matching, widget inspection, and code generation workbench')
$packageManifest.Save($PackageManifestPath)

[xml]$win32Manifest = Get-Content -LiteralPath $Win32ManifestPath
$win32NamespaceManager = New-Object System.Xml.XmlNamespaceManager($win32Manifest.NameTable)
$win32NamespaceManager.AddNamespace('asmv1', 'urn:schemas-microsoft-com:asm.v1')

$assemblyIdentityNode = $win32Manifest.SelectSingleNode('/asmv1:assembly/asmv1:assemblyIdentity', $win32NamespaceManager)

if ($null -eq $assemblyIdentityNode) {
    throw 'Failed to locate assemblyIdentity in App/app.manifest.'
}

$assemblyIdentityNode.SetAttribute('version', $fourPartVersion)
$assemblyIdentityNode.SetAttribute('name', "$PackageIdentityName.app")
$win32Manifest.Save($Win32ManifestPath)

Write-Host "Updated application manifests to version $fourPartVersion for $ProductName."
