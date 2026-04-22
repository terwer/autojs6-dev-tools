[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ExecutablePath,

    [ValidateRange(3, 30)]
    [int]$StartupSeconds = 8
)

$ErrorActionPreference = 'Stop'

function Resolve-AbsolutePath {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    return (Resolve-Path -LiteralPath $Path -ErrorAction Stop).Path
}

$resolvedExecutablePath = Resolve-AbsolutePath -Path $ExecutablePath
$process = Start-Process -FilePath $resolvedExecutablePath -PassThru

try {
    Start-Sleep -Seconds $StartupSeconds

    if ($process.HasExited) {
        throw "便携版冒烟失败：进程在 $StartupSeconds 秒内退出，ExitCode=$($process.ExitCode)"
    }

    Write-Host "Portable smoke test passed: $resolvedExecutablePath"
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
}
