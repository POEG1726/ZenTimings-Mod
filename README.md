# ZenTimings-Mod

A fork of [irusanov/ZenTimings](https://github.com/irusanov/ZenTimings) with a
**one-shot headless JSON exporter** for scripted memory-tuning experiments.
The upstream GUI remains in the source tree. This fork adds a separate console
application; it is maintained by [POEG1726](https://github.com/POEG1726).

[Download the headless bundle](https://github.com/POEG1726/ZenTimings-Mod/releases/latest)
· [CLI usage and build guide](CLI/README.md)
· [Changes from upstream](MODIFICATIONS.md)
· [License](LICENSE)

## What the mod adds

- `ZenTimings.Export.exe --export-json FILE --exit`: a complete JSON snapshot,
  followed by process exit, without opening or automating the GUI.
- Timings, AOD/APOB configuration, system/memory/SPD data, power table,
  motherboard sensors and DIMM telemetry in one export.
- Unique-file output with no overwrite, explicit exit status, and a synthetic
  self-test path that does not initialize hardware.
- A small shared-builder freshness flag for one-shot captures; ordinary GUI
  freshness behavior stays as it was.
- A verified build manifest for the four-file bundle used by the NanoKVM runner.

The live CLI currently checks for **AMD Ryzen 9 9950X3D / Gigabyte B850M AORUS
ELITE WIFI6E ICE** before initializing hardware. It is a platform-specific
experimental exporter, not a universal ZenTimings CLI. Ordinary upstream GUI
compatibility does not imply CLI support for another platform.

## Quick start

Extract the complete headless release ZIP to a separate directory. Keep
`ZenTimings.Export.exe`, `ZenStates-Core.dll`, `inpoutx64.dll` and
`ZenTimings.Export.exe.config` together. Windows x64, .NET Framework 4.7.2 or a
compatible later 4.x runtime, administrator privileges and the existing upstream
hardware-access prerequisites are required for live export.

```powershell
# No hardware access:
.\ZenTimings.Export.exe --help
.\ZenTimings.Export.exe --self-test C:\ExistingFolder\offline-unique.json --exit

# Live hardware capture, only on the supported and authorized target:
.\ZenTimings.Export.exe --export-json D:\Captures\unique-run.json --exit
```

The destination directory must exist and the output filename must be new.
Live exports contain `capture_mode=one_shot_cli`; synthetic exports explicitly
contain `capture_mode=synthetic_offline_test`. An offline test file must never be
used as target readback evidence.

The CLI does not install/update/uninstall drivers, change service startup,
change BIOS settings, flash firmware, or save GUI settings. It does use upstream
hardware-access initialization and device I/O for live collection. Prepare the
existing driver environment separately; a missing device is a capture failure.

## Build and verify

```powershell
git clone --recurse-submodules https://github.com/POEG1726/ZenTimings-Mod.git
cd ZenTimings-Mod
pwsh -NoProfile -File CLI\Prepare-BuildTools.ps1
pwsh -NoProfile -File CLI\Build-Headless.ps1 -NativeDll artifacts\upstream_v142.713\inpoutx64.dll
python -m unittest discover -s CLI -p test_headless.py -v
```

The preparation script downloads hash-pinned build inputs and the official
upstream native DLL without executing a driver installer. The build uses portable
Roslyn and net472 reference assemblies, with the existing pinned core submodule.
See [CLI/README.md](CLI/README.md) for output files, JSON sections and exit codes.
The original GUI can be built from `WPF/ZenTimings.sln` with its upstream build
dependencies; this release publishes the headless addition only.

## Provenance and verification limits

Based on upstream `v142.713`, commit
`9e482d3e7724fb92d9077e151428828d410b4957`, and ZenStates-Core
`869b7e82572337e5a673e92230edc92bddf0e44b`. The original submodule source is retained.
Release checks build the CLI and run synthetic/argument/file-preflight tests.
They do not perform live collection or establish memory stability, physical
voltage accuracy or firmware-training results. Custom CLI/core binaries are
unsigned; this is not an upstream-signed release.

## License and upstream acknowledgements

The upstream [GNU GPL version 3 license](LICENSE) and source notices are retained;
this fork is not MIT licensed. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)
for the bundled native component and core-module notices.

Upstream projects and references include [Adonis UI](https://benruehl.github.io/adonis-ui/),
[RTCSharp](https://github.com/tomrus88/RTCSharp), [ryzen_smu](https://gitlab.com/leogx9r/ryzen_smu/),
[ryzen_nb_smu](https://github.com/flygoat/ryzen_nb_smu), [zenpower](https://github.com/ocerman/zenpower),
[Linux](https://github.com/torvalds/linux), [AMD documentation](https://www.amd.com/en/support/tech-docs),
and [Open Hardware Monitor](https://github.com/openhardwaremonitor/openhardwaremonitor).
