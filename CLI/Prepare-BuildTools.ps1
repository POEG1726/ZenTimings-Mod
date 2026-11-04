[CmdletBinding()]
param([string]$ArtifactsRoot = (Join-Path $PSScriptRoot '..\artifacts'))
$ErrorActionPreference = 'Stop'
# Downloads/extracts build inputs only. Never executes a driver or installer.
$packages = @(
    @{Name='roslyn'; Folder='build-tools\roslyn'; File='roslyn.zip';
      Url='https://api.nuget.org/v3-flatcontainer/microsoft.net.compilers.toolset/4.14.0/microsoft.net.compilers.toolset.4.14.0.nupkg';
      Hash='941a9cf3ea618d88d01a3dd6b1a45a06bcf07716a9f81ce4031caa3edd24a845'},
    @{Name='net472'; Folder='build-tools\net472'; File='net472.zip';
      Url='https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net472/1.0.3/microsoft.netframework.referenceassemblies.net472.1.0.3.nupkg';
      Hash='ffa0a5570a39f911399164d0581ffddef99b5e3dfbaa5f220e5ce22969bcf57c'},
    @{Name='upstream'; Folder='upstream_v142.713'; File='upstream_v142.713.zip';
      Url='https://github.com/irusanov/ZenTimings/releases/download/v142.713/ZenTimings_v142.713.zip';
      Hash='c49f5a4a66a2c438d8b2bddfde5338df67ad8bed835a5f2e3ad421b21c4d76a0'}
)
New-Item -ItemType Directory -Path $ArtifactsRoot -Force | Out-Null
foreach ($package in $packages) {
    $archive = Join-Path $ArtifactsRoot $package.File
    if (-not (Test-Path -LiteralPath $archive)) {
        Invoke-WebRequest -Uri $package.Url -OutFile $archive
    }
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $package.Hash) {
        throw ('Build input SHA256 mismatch: ' + $package.Name)
    }
    Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $ArtifactsRoot $package.Folder) -Force
}
Write-Output 'Verified build inputs extracted; no compiler, driver or installer was executed.'
