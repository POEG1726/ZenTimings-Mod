# Fork modifications

2026-11-04 — ZenTimings-Mod v142.713-mod.1, maintained by POEG1726.

- Added a separate `CLI/` console exporter for a single complete JSON snapshot.
- Added offline synthetic/preflight tests and hash-pinned portable build setup.
- Added `SnapshotSource.SynchronousCapture`; freshly captured CLI readings are
  not marked stale merely because GUI auto-refresh is disabled. The default is
  false, so the existing GUI path retains its prior behavior.
- Added English fork documentation and an offline Windows CI workflow.

Upstream GUI source, copyright notices, GPL license and the pinned ZenStates-Core
submodule are retained. The CLI live guard currently restricts execution to the
9950X3D/B850M target. This fork is not an official upstream release and its custom
CLI/core binaries are not signed by upstream.
