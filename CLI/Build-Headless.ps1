[CmdletBinding()]
param(
    [string]$ToolsRoot = (Join-Path $PSScriptRoot '..\artifacts\build-tools'),
    [string]$OutputRoot = (Join-Path $PSScriptRoot '..\artifacts\headless'),
    [string]$NativeDll
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$compiler = Join-Path $ToolsRoot 'roslyn\tasks\net472\csc.exe'
$referenceRoot = Join-Path $ToolsRoot 'net472\build\.NETFramework\v4.7.2'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Portable Microsoft.Net.Compilers.Toolset 4.14.0 is required; see CLI README.' }
if (-not (Test-Path -LiteralPath $referenceRoot)) { throw 'Microsoft.NETFramework.ReferenceAssemblies.net472 1.0.3 is required.' }
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$output = (Resolve-Path -LiteralPath $OutputRoot).Path
$obj = Join-Path $output 'obj'
New-Item -ItemType Directory -Path $obj -Force | Out-Null
$references = @(Get-ChildItem -LiteralPath $referenceRoot -File -Filter '*.dll' |
    Where-Object {$_.Name -notin @('System.EnterpriseServices.Wrapper.dll','System.EnterpriseServices.Thunk.dll')} |
    ForEach-Object { '/reference:"' + $_.FullName + '"' })
$common = @('/nologo','/nostdlib+','/langversion:latest','/optimize+','/unsafe+','/platform:x64','/deterministic+') + $references
$core = Join-Path $root 'ZenStates-Core'
$version = @'
using System.Reflection;
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.7.2", FrameworkDisplayName=".NET Framework 4.7.2")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("upstream-869b7e82572337e5a673e92230edc92bddf0e44b;net472-compat;NET20")]
'@
$version | Set-Content -LiteralPath (Join-Path $obj 'CoreVersion.cs') -Encoding utf8
$sources = @(Get-ChildItem -LiteralPath $core -Filter '*.cs' -Recurse -File |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' } | ForEach-Object {'"'+$_.FullName+'"'})
$resourceArgs = @('AMDFamily17.bin','LpcIO.bin','RyzenSMU.bin','SmbusPIIX4.bin') | ForEach-Object {
    '/resource:"' + (Join-Path $core ('Resources\PawnIo\'+$_)) + '",ZenStates.Core.Resources.PawnIo.' + $_
}
$coreArgs = $common + @('/target:library','/define:NET20,NETFRAMEWORK',('/out:"'+(Join-Path $output 'ZenStates-Core.dll')+'"')) + $resourceArgs + $sources + ('"'+(Join-Path $obj 'CoreVersion.cs')+'"')
$rsp = Join-Path $obj 'core.rsp'
$coreArgs | Set-Content -LiteralPath $rsp -Encoding utf8
& $compiler /noconfig ('@'+$rsp)
if ($LASTEXITCODE -ne 0) { throw 'Core compilation failed' }
$appVersion = @'
using System.Reflection;
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.7.2", FrameworkDisplayName=".NET Framework 4.7.2")]
[assembly: AssemblyTitle("ZenTimings-Mod Headless Export")]
[assembly: AssemblyProduct("ZenTimings-Mod Headless Export")]
[assembly: AssemblyVersion("142.0.713.0")]
[assembly: AssemblyFileVersion("142.0.713.0")]
[assembly: AssemblyInformationalVersion("142.0.713-mod.1")]
'@
$appVersion | Set-Content -LiteralPath (Join-Path $obj 'AppVersion.cs') -Encoding utf8
$linked = @('Common\CpuSingleton.cs','Common\AsusWMI.cs','Common\AsusSensorInfo.cs','Common\BiosMemController.cs',
    'WPF\Plugin\IPlugin.cs','WPF\Export\CoreOptionsScope.cs','WPF\Export\SnapshotBuilder.cs',
    'WPF\Export\SnapshotWriter.cs','WPF\Export\SnapshotHtmlWriter.cs','WPF\Export\SnapshotObject.cs','WPF\Export\SnapshotOptions.cs',
    'CLI\Program.cs','CLI\HeadlessSettings.cs') | ForEach-Object {'"'+(Join-Path $root $_)+'"'}
$appArgs = $common + @('/target:exe',('/out:"'+(Join-Path $output 'ZenTimings.Export.exe')+'"'),
    ('/reference:"'+(Join-Path $output 'ZenStates-Core.dll')+'"')) + $linked + ('"'+(Join-Path $obj 'AppVersion.cs')+'"')
$rsp = Join-Path $obj 'cli.rsp'
$appArgs | Set-Content -LiteralPath $rsp -Encoding utf8
& $compiler /noconfig ('@'+$rsp)
if ($LASTEXITCODE -ne 0) { throw 'CLI compilation failed' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ZenTimings.Export.exe.config') -Destination (Join-Path $output 'ZenTimings.Export.exe.config')
if ($NativeDll) { Copy-Item -LiteralPath $NativeDll -Destination (Join-Path $output 'inpoutx64.dll') }
$files = @(Get-ChildItem -LiteralPath $output -File | Where-Object {$_.Name -ne 'build-manifest.json'} | ForEach-Object {
    @{name=$_.Name;size=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
})
$sourceFiles = @($sources + $linked + ('"'+$PSCommandPath+'"') | ForEach-Object {
    $file=$_.Trim('"')
    @{path=$file.Substring($root.Length+1);sha256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()}
})
@{upstream_commit='9e482d3e7724fb92d9077e151428828d410b4957';core_commit='869b7e82572337e5a673e92230edc92bddf0e44b';
    build='Roslyn4.14.0/net472-reference1.0.3; core NET20+NETFRAMEWORK compatibility code paths';files=$files;source_files=$sourceFiles} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'build-manifest.json') -Encoding utf8
Write-Output ('Built '+(Join-Path $output 'ZenTimings.Export.exe'))
