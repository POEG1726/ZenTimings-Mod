# Third-party notices

This fork retains the upstream [GNU GPL version 3 license](LICENSE), source
copyright notices and submodule license files. Its changes are described in
[MODIFICATIONS.md](MODIFICATIONS.md).

- [ZenTimings](https://github.com/irusanov/ZenTimings), Ivan Rusanov and upstream
  contributors: source base `v142.713` / `9e482d3e7724fb92d9077e151428828d410b4957`.
- [ZenStates-Core](https://github.com/irusanov/ZenStates-Core): pinned submodule
  `869b7e82572337e5a673e92230edc92bddf0e44b`, license at `ZenStates-Core/LICENSE`.
- InpOut: the unchanged `inpoutx64.dll` from the official upstream release;
  preserve `ZenStates-Core/External/InpOut/license.txt` with redistribution.
- PawnIO modules: embedded upstream modules from
  [PawnIO.Modules 0.2.11](https://github.com/namazso/PawnIO.Modules/releases/tag/0.2.11),
  with the notice and LGPL 2.1 text in `ZenStates-Core/Resources/PawnIo/`.
- Other upstream core notices, including `ZenStates-Core/External/WinIo/LICENSE.txt`,
  remain in the corresponding source directories.
- Microsoft compiler and reference-assembly packages are separately downloaded
  build inputs; they are not bundled in the release runtime.

The release's full source archive includes the pinned submodule and its notices.
The runtime ZIP includes the GPL text, InpOut and PawnIO notices, source revision
information and links to the corresponding source archive. No driver installer
or signing credentials are included.
