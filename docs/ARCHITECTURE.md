# Technical architecture

## Overview

Bluewater Haul is a MonoGame DesktopGL application targeting .NET 8. It does
not use Unity/Unreal, a scene editor, a .mgcb content project or an external
backend. The runtime reads local JSON and creates its world meshes and UI in
code. The initial area is an OSM-derived Sarnia/Point Edward extract.

### Source layout

| File / module | Responsibility |
| --- | --- |
| `Source/Program.cs` | Entrypoint, `--verify` and `--smoke` routing, exception logging |
| `Source/SarniaGame.cs` | Main `Game` loop, state persistence, player interaction and UI lifecycle |
| `Source/World.cs` | JSON world model, geometry utilities and rendering support |
| `Source/Architecture.cs` | Building presentation and generated structure detail |
| `Source/StreetScene.cs` | Street fixtures, sidewalks, signs, parking and bridges |
| `Source/Navigation.cs`, `NavigationUI.cs` | Road graph, route search and full-map interface |
| `Source/PlacePresentation.cs` | Landmarks and curated landmark tour presentations |
| `Source/Vehicles.cs` | Controllable fleet, driving model and vehicle state |
| `Source/StreetLife.cs` | Ambient vehicles, pedestrians and traffic signals |
| `Source/Trouble.cs`, `CrashPhysics.cs` | Crime state, police reactions and simplified physical collisions |
| `Source/Verification.cs` | Headless data and logic assertions |
| `Source/Tools/import_osm.py` | Source `.osm.pbf` transformation into `world.json` |
| `Source/Tools/prepare_content.py` | Landmark notes and delivery content JSON generation |

Some gameplay code is intentionally concentrated in partial `SarniaGame`
classes. The prototype uses procedural meshes and compact data structures
rather than a large content pipeline or entity-component framework.

## Data flow

```text
MapSource/sarnia.osm.pbf  --(optional Python + Shapely import)--> Source/Data/world.json
                                              |
Source/Tools/prepare_content.py --> landmarks.json and jobs.json
                                              |
                   C# game loads JSON and bitmap font atlas
                                              |
                   Road graph, city geometry, AI, navigation
                                              |
                   Gameplay / UI / local save file
```

The three JSON files and `font.png` are intentionally checked in and required
at runtime. They are **authored/derived game content**, not disposable output
of `dotnet build`. The `.pbf` and importer make geography regeneration
possible. Do not remove `world.json` just because it has a generator.

## Data & persistence

- The world JSON includes roads, buildings, surfaces, POIs, trees, addresses,
  bounds and projection origin in local game coordinates.
- NPC street population and traffic are synthesized; their routes and signal
  positions are not a live traffic or public transit feed.
- One-way road data affects driving routes; walking and destination approach
  are approximations.
- Save files contain player, vehicle, deliveries, visits, collection, law and
  time state. On load, parts of the ambient city population are regenerated.
- Saves are written to the user's LocalApplicationData location, not the repo.
- `--smoke` uses an isolated `verification/smoke-save.json` and generates PNG
  captures in `verification/captures-*` under the working directory.

## Extension points

For new regions, update importer bounds, map projection and curated landmark
anchors. For custom locations, add authored location data while preserving
fidelity flags: OSM is not evidence of building heights, door locations or
business operation. For larger worlds, consider spatial indexes and asset
streaming rather than assuming the current map size will scale linearly.
