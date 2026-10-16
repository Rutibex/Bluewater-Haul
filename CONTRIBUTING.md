# Contributing

Contributions, bug reports and documentation corrections are welcome.

1. Check existing issues, and open a focused issue for large changes.
2. Fork the repository and work in a descriptive branch.
3. Build on .NET 8 and run `python tests/check_assets.py` and
   `dotnet run --project Source/SarniaPrototype.csproj -- --verify`.
4. If gameplay changed, test the scenario in the running game and explain how
   it was verified (screenshots may help).
5. Keep PRs focused, include a concise summary, and update documentation
   when functionality or controls change.

**Do not commit** build outputs, game saves, cached packages, graphical smoke
captures, generated publish directories or proprietary assets. The intentionally
tracked `Source/Data/world.json` is a required game database, and changes to it
must preserve ODbL compliance, source provenance and accuracy documentation.

**Licensing:** Contributions to original code and original project content are
made under the MIT license of this repository. Third-party assets must be
compatible and explicitly attributed; map data stays under its own ODbL terms.
Please do not introduce real business or individual allegations as fictional
mission content. Follow the location fidelity rules in
[`docs/MAP_AND_ACCURACY.md`](docs/MAP_AND_ACCURACY.md).

See [development](docs/DEVELOPMENT.md),
[architecture](docs/ARCHITECTURE.md) and [troubleshooting](docs/TROUBLESHOOTING.md).
