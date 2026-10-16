# Changelog

This document summarizes the features currently present in the shipped source,
not a promise of planned work.

## [0.7.0] - 2026-10-15

### Included

- .NET 8 / MonoGame DesktopGL Sarnia prototype with truck delivery loop.
- Real mapped street and building geometry, neighbourhood exploration,
  minimap, searchable map, waypoints, routing and landmark tour.
- Stylized building presentation, bridges, woodland, signage and interiors.
- Multiple drivable vehicle classes, chase view, persistent vehicle state.
- Ambient traffic, generated pedestrians, routines and traffic signals.
- Theft, wanted response, police pursuit, impact and persistent damage.
- Smoother coordinate-based on-foot controls, improved camera behaviour,
  mitigation for depth flicker, knockdowns and vehicle collision impulses.
- Burning and destruction with saved wreck state, fuel/repair/recovery actions.

### Repository release preparation

- Removed copied self-contained Windows build output and duplicated pass notes.
- Organized player, architecture, testing, geographic and release documentation.
- Added GitHub CI, source-only ignore rules, Windows release packaging,
  asset validation, contribution templates, license and attribution.

### Prototype limits

No authoritative road regulation model, verified interiors, modern truck
suspension/trailers or complete quest/world systems. Windows graphics and
controls require playtesting on actual target machines.
