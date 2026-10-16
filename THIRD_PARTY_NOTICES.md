# Third-party notices and map attribution

**Map data © OpenStreetMap contributors**, licensed under the [Open Database
License (ODbL) 1.0](https://opendatacommons.org/licenses/odbl/1-0/).
See [OpenStreetMap copyright and attribution](https://www.openstreetmap.org/copyright).

- `MapSource/sarnia.osm.pbf` is the supplied geographic source extract.
- `Source/Data/world.json` is a modified/transformed database made from that
  extract. It is distributed under ODbL 1.0, **not** under the project's MIT
  code license. The source data and transformation tool are included in the
  repository for reproducibility and redistribution.
- In-game and published map screenshots show data derived from OpenStreetMap;
  retain map attribution when distributing them.
- The game engine and original gameplay code are independent from the map
  database. The map data's ODbL terms must not be represented as applying to
  that independent source code.

**DejaVu Sans Mono** is used to generate `Source/Data/font.png` (a bitmap font
atlas). The applicable font license is included verbatim in
[`Source/FONT_LICENSE.txt`](Source/FONT_LICENSE.txt). Project licensing does not
replace the font's license.

**MonoGame.Framework.DesktopGL 3.8.4.1** is restored from NuGet. MonoGame is
primarily licensed under the Microsoft Public License (Ms-PL), with portions
under MIT and separate licences for its dependencies. See the
[MonoGame license](https://github.com/MonoGame/MonoGame/blob/develop/LICENSE.txt)
and [third-party notices](https://github.com/MonoGame/MonoGame/blob/develop/THIRD-PARTY-NOTICES.txt).

**Microsoft .NET 8** runtime components and native dependencies included when
publishing a self-contained Windows build retain their respective licences.
Consult the [Microsoft .NET licensing information](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT)
and bundled dependency documentation. The CI release workflow copies these
project notices into the Windows ZIP, but any additional notices included by
future dependencies must also be retained.

Research references provide links and paraphrases only. No official logos,
newspaper images, third-party photos or scraped website assets are included.
This unofficial game is not affiliated with or endorsed by the City of Sarnia,
Point Edward, or OpenStreetMap.
