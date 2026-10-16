# Troubleshooting

**The Windows EXE does not start.** Make sure you downloaded a Windows x64
release ZIP instead of GitHub's automatic **Source code** ZIP. Extract it
fully, retaining all DLLs and the `Data/` folder. Update your OpenGL graphics
driver. The GitHub source-only package requires the .NET SDK to compile.

**`Data/world.json` / `font.png` not found.** Run the executable from its
published directory and retain the `Data` directory. In source mode, use
`dotnet run --project Source/SarniaPrototype.csproj` (MSBuild copies assets to
output). The checked-in runtime JSON must not be removed by cleanup tools.

**NuGet restore fails.** Check Internet access to NuGet, your SDK version and
any corporate proxy settings. The project requires
`MonoGame.Framework.DesktopGL` version `3.8.4.1`.

**OpenGL, SDL or audio error on Linux/macOS.** This project uses DesktopGL;
install compatible graphics drivers and native libraries for your platform.
Only the GitHub Windows ZIP is currently packaged and documented as an
end-user binary. Other operating systems are source-build targets.

**Map route stops near the destination.** Blue navigation lines follow the
road graph. The yellow final approach and the entrances are approximate. Some
road networks are disconnected, and walking paths/real restrictions are not
fully represented.

**Vehicle feels strange / gets stuck / flickering surfaces.** The simulation
still uses approximate collision boxes, generated curb and bridge heights,
and simplified handling. Try **H** outdoors for recovery if the current game
state permits it. Report the specific landmark, saved state and hardware.

**I lost my save.** Windows saves are stored in
`%LOCALAPPDATA%\BluewaterHaul\save-v1.json`. Try restoring a backup. The
`--smoke` test writes a separate save under `verification/`.

**Crash report.** The game attempts to write details to
`%LOCALAPPDATA%\BluewaterHaul\crash.txt` on Windows. For an issue report,
include the relevant error message and steps to reproduce; review it for
personal information before posting publicly. Never upload an entire user
profile or private filesystem paths by accident.

Use a GitHub issue with the provided bug template for reproducible problems.
