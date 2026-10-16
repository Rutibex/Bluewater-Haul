# Geographic data and accuracy

## Data sources and licensing

**Map data © OpenStreetMap contributors**, under the **Open Database License
(ODbL) 1.0**. See [copyright](https://www.openstreetmap.org/copyright),
[ODbL](https://opendatacommons.org/licenses/odbl/1-0/) and the repository's
[third-party notices](../THIRD_PARTY_NOTICES.md).

The checked-in source is `MapSource/sarnia.osm.pbf`; the runtime loads a
transformed database at `Source/Data/world.json`. The source and transformed
OSM-derived database are covered by ODbL and **excluded from the MIT code
license**. The import script is included for re-creating the map data.

## Coverage

This prototype covers a bounded portion of **Sarnia, Point Edward and scenery
across the St. Clair River**, not all of Sarnia or Lambton County. The supplied
extract used bounds of approximately longitude -82.423 to -82.269 and latitude
42.942 to 43.023. A map window is not an official municipal boundary.
The US side is scenery; the bridge does not implement a border crossing.

### Grounded features vs generated scenery

| Grounded in OSM or linked location research | Generated or simplified |
| --- | --- |
| Road centre-lines and many names | Road widths, traffic lanes, signals, closures, road access |
| Mapped building footprints and some labels | Heights, facades, exterior colours, doorways, two-room interiors |
| Parks, water edges, landuse, POI positions | Incomplete shoreline reconstruction and small landscape detail |
| Named Sarnia/Point Edward landmarks | Stylized bridges, aircraft memorial, most architectural specifics |
| One-way flags when present in OSM | Navigation availability and pedestrian path accessibility |
| Location descriptions with linked sources | Fictional characters, cargo, contracts and crimes |

**Never use the game for real navigation**. OSM features can be incomplete or
out of date, and the game may generate convincing but inaccurate scenery.

## Notable places

The playable map and 13 JSON location notes include Canatara Park,
Centennial Park, Point Edward Waterfront, Blue Water Bridge, Sarnia City Hall,
Sarnia Library, Lawrence House, Germain Park, Lambton College, Wawanosh
Wetlands, Mitton Village, Bluewater Health and Golden Hawk Memorial.

The Lawrence House facade is an inferred, stylized historic-house treatment;
interiors are fictional. Bridge geometry follows mapped road alignments but is
not an engineering-scale reproduction. Golden Hawk is a stylized memorial
model. Other place details are researched descriptions, not claims of exact
virtual replicas.

Further public research references:

- [City of Sarnia parks and trails](https://www.sarnia.ca/play/parks-and-trails/)
- [City of Sarnia cultural heritage](https://www.sarnia.ca/doing-business/cultural-heritage/)
- [Point Edward parks](https://www.villageofpointedward.com/community-services/parks)
- [Point Edward local history](https://www.villageofpointedward.com/living-point-edward/history)
- [Lawrence House history](https://lawrencehouse.ca/history-2/)
- [Lambton College](https://www.lambtoncollege.ca/)
- [Veterans Affairs Golden Hawk memorial](https://www.veterans.gc.ca/en/remembrance/memorials/canada/sarnias-f-86-rcaf-memorial)

No local person, business or public institution is implied to have approved
or participated in this project. Fictional misconduct belongs to fictional
NPCs, not to real organizations.

## Rebuilding map content

Prerequisites: **Python 3.10+**, **Shapely 2.1+**, and the included PBF source.
From the repository root:

```sh
python -m pip install "shapely>=2.1,<3"
python Source/Tools/import_osm.py MapSource/sarnia.osm.pbf Source/Data/world.json
python Source/Tools/prepare_content.py
```

The importer is specialized for the included extract and may interpret
incomplete water relations. Check the diff and landmark anchors before
committing a regenerated map. Rebuilt data should be tested with
`python tests/check_assets.py` and `dotnet run --project Source/SarniaPrototype.csproj -- --verify`.

The stored transformed map is intentional content required for out-of-the-box
source builds: do not delete it as a "generated" build artifact.
