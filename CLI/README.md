# One-shot headless JSON export

The ZenTimings-Mod CLI exports a full snapshot without a WPF window, tray icon,
save dialog, updater, or GUI settings writes. It builds separately from the
upstream GUI and uses the shared snapshot builder/writer.

## Run on the supported target

Windows x64, .NET Framework 4.7.2 or a compatible later 4.x runtime, administrator
privileges, and the existing upstream hardware-access prerequisites are required.
The current live identity guard requires **Ryzen 9 9950X3D / Gigabyte B850M AORUS
ELITE WIFI6E ICE**. This is a deliberate platform restriction in `Program.cs`,
not a general claim of support for other machines.

```powershell
.\ZenTimings.Export.exe --export-json D:\Captures\unique-run.json --exit
```

The parent directory must already exist; the output name must be new. The CLI
writes a uniquely named partial file and moves it into place without replacing
an existing export. `--exit` is required. Exit codes: **0** success, **1** capture,
device, data or file failure, **2** invalid arguments. No polling GUI process is
left behind.

Live mode uses upstream CPU/core initialization, power-table refresh, motherboard
sensors and DIMM telemetry. It opens existing PawnIO/InpOut devices through the
upstream core; this is hardware access, not a synthetic or purely offline read.
The CLI issues no driver/service installation, update, start, stop, uninstall,
BIOS-setting or flashing command. Missing devices fail rather than installing a
replacement. The GUI DriverCleaner path is not linked. Existing initialization,
device ACL handling, module IOCTLs and handle disposal remain upstream behavior.

## JSON contract

Exports include `capture_mode=one_shot_cli` and these required non-null sections:

- `static.system`, `static.memory`, `static.spd`
- `config.timings`, `config.aod`, `config.apob`
- `readings.power_table`, `readings.sensors`, `readings.dimm`

Individual unavailable values retain upstream null semantics. All-sections export
does not mean every SPD byte or physical rail has been measured. Serial numbers
are omitted. `SynchronousCapture=true` marks freshly refreshed one-shot readings;
the default false preserves the GUI's existing stale-data rules. No user-hidden
sensors or GUI settings are loaded or saved.

## Build

Clone with submodules, then use PowerShell 7 on Windows:

```powershell
git clone --recurse-submodules https://github.com/POEG1726/ZenTimings-Mod.git
cd ZenTimings-Mod
pwsh -NoProfile -File CLI\Prepare-BuildTools.ps1
pwsh -NoProfile -File CLI\Build-Headless.ps1 -NativeDll artifacts\upstream_v142.713\inpoutx64.dll
python -m unittest discover -s CLI -p test_headless.py -v
```

Preparation downloads hash-pinned Microsoft Roslyn 4.14.0, net472 reference
assemblies 1.0.3 and the official upstream v142.713 ZIP into ignored `artifacts/`.
It extracts files without installing or executing drivers. The build invokes the
portable compiler; a Visual Studio installation is not required.

The source base is upstream `v142.713`, commit
`9e482d3e7724fb92d9077e151428828d410b4957`, with ZenStates-Core submodule
`869b7e82572337e5a673e92230edc92bddf0e44b`. The core is compiled with its
`NET20`/`NETFRAMEWORK` compatibility branches against net472 references. Its source
is unchanged. Do not substitute an arbitrary upstream core binary.

Output: `artifacts/headless/ZenTimings.Export.exe`, `ZenStates-Core.dll`,
`inpoutx64.dll`, `ZenTimings.Export.exe.config`, and `build-manifest.json` containing
source/artifact hashes. `Build-Headless.ps1` accepts optional `-ToolsRoot` and
`-OutputRoot` for separate build directories. The four runtime files and manifest
form the bundle expected by NanoKVM Control's `zentimings_cli_bundle` setting.

## Offline verification

`--help`, invalid arguments, existing-file preflight refusal and
`--self-test UNIQUE_PATH --exit` do not initialize hardware. The self-test writes
`capture_mode=synthetic_offline_test`, never `one_shot_cli`. Tests cover full JSON,
overwrite refusal, argument validation, help and concurrent writers.

Offline build/test results establish these software paths only. They do not
establish live device availability, target training state, voltage accuracy or
memory stability. Release verification deliberately does not invoke live export.
