# Build, publish and GitHub release guide

## Source checkout

Clone this repository or download its source ZIP. The source archive is **not**
a ready-to-run Windows executable. Requirements: .NET 8 SDK and access to the
MonoGame DesktopGL NuGet package (the first restore requires network access).

```sh
dotnet restore Source/SarniaPrototype.csproj
dotnet build Source/SarniaPrototype.csproj -c Release
dotnet run --project Source/SarniaPrototype.csproj -c Release -- --verify
```

## Manual Windows x64 publish

On Windows PowerShell:

```powershell
dotnet publish Source/SarniaPrototype.csproj `
  --configuration Release --runtime win-x64 --self-contained true `
  --output dist/publish
```

This creates `dist/publish/BluewaterHaul.exe` and the required runtime/data
files. Keep **all** published DLLs and `Data/` alongside the EXE; do not
commit them to the source repository. For redistributions, package license
and attribution notices plus the OSM data source as the automated workflow
does. The published executable needs Windows x64 with functional OpenGL.

## Automated GitHub release

- Every push/PR runs `.github/workflows/ci.yml` (Python asset validation,
  restore, compilation and `--verify` on Windows/Ubuntu).
- `.github/workflows/release.yml` can be run from **Actions** manually to
  generate a Windows x64 ZIP as a downloadable workflow artifact.
- Pushing a **`v*` tag** runs the same publish job and creates a **GitHub
  prerelease** with the ZIP and the public-facing `RELEASE_NOTES.md` text.

Example after reviewing your changes:

```sh
git add .
git commit -m "Prepare Bluewater Haul v0.7 source release"
git tag v0.7.0
git push origin main --tags
```

Do not run the tag/push commands until you have created your own repository,
reviewed source and licenses, and verified the build. `main` is an example
branch name, not a required branch.

## Maintainer release checklist

- [ ] Confirm source compiles on your own Windows system and `--verify` passes.
- [ ] Play-test driving, E interactions, map, saves, traffic and damage.
- [ ] Check that `README.md` and screenshot captions match the actual build.
- [ ] Confirm attribution and third-party notices for any new content or DLLs.
- [ ] Run CI and inspect generated Windows ZIP before publishing a tag.
- [ ] Update `CHANGELOG.md` and `RELEASE_NOTES.md` to match the release.
- [ ] Check the release ZIP contains `BluewaterHaul.exe`, `Data/`, notices,
      source map PBF, and no test captures or development files.
- [ ] Mark `v0.7.0` as a prerelease because the game is still a prototype.

The upstream v0.7 archive supplied an old prebuilt Windows directory; the
GitHub source package intentionally excludes it. Builds are to be produced
**from the checked-in source code**, not copied from that prior binary.
