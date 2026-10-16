# Development and testing

## Setup

Install the .NET 8 SDK or a compatible later version. For DesktopGL, an
OpenGL-capable driver and OS-native MonoGame dependencies are required.
From the repository root:

```sh
dotnet restore Source/SarniaPrototype.csproj
dotnet build Source/SarniaPrototype.csproj -c Release
dotnet run --project Source/SarniaPrototype.csproj
```

The C# project needs no `mgcb` build or external art pipeline. Runtime data is
copied from `Source/Data/` into the compiled output.

## Verification

```sh
python tests/check_assets.py
python -m compileall -q Source/Tools tests
dotnet run --project Source/SarniaPrototype.csproj -c Release -- --verify
```

`--verify` runs headless assertions over map geometry, road navigation,
vehicle behaviour, save-state serialization and game rules. CI runs data checks
and C# verification on Windows and Ubuntu.

`--smoke` runs in the graphical engine and automatically exercises interactions
and captures scenes before exiting:

```sh
dotnet run --project Source/SarniaPrototype.csproj -c Release -- --smoke
```

**Important:** graphical smoke testing opens a game window, may need a software
renderer or display server on Linux, and writes screenshots into the current
working directory under `verification/`. The smoke save is separate from the
normal save. A CI headless build is not a substitute for actual Windows play
and frame-to-frame rendering tests.

## Content changes

- Gameplay text and authored notes: `Source/Data/jobs.json` and
  `Source/Data/landmarks.json`; their generator is
  `Source/Tools/prepare_content.py`.
- Map geometry and street features: regenerate from
  `MapSource/sarnia.osm.pbf` using the importer. Keep attribution.
- Procedural geometry and animations: C# modules under `Source/`.
- Avoid adding photographs or third-party media without clear rights and
  explicit attribution.

The app is a prototype with intentionally simplified vehicle physics,
walking routes, interiors and NPC AI. Contributions should not describe
approximate map details as surveyed ground truth.

## Before a pull request

1. Build on .NET 8 without compiler errors or warnings introduced by the
   change.
2. Run `tests/check_assets.py` and `--verify`.
3. If graphical behaviour changed, test on Windows and capture before/after
   screenshots, noting which features were actually exercised.
4. Keep the repository source-only: do not commit `bin/`, `obj/`, `Windows/`,
   `verification/`, executables or NuGet restore caches.
5. Update README/CHANGELOG and license/attribution notices as needed.
