# Player guide

## First delivery

1. Start the game; the initial delivery and truck are already assigned.
2. Press **F1** to close help and **M** to open the map. A gold marker shows
   the delivery destination; the cyan line follows a street route.
3. Drive along the route with **W/S** for throttle and braking and **A/D** to
   steer. Keep your speed down near traffic, pedestrians and intersections.
4. Park within about **70 m** of the handover; press **E** to exit the truck.
5. Walk within about **35 m** of the marker and press **E** to deliver.
6. When a delivery is completed, use **Q** to select the next fictional job.

The drop-off locations are on or near roads. You do not need to drive into a
park or across the international border.

## Controls

| Key / input | Driving | On foot / menus |
| --- | --- | --- |
| W / S | Throttle, service braking and reversing | Walk forward / backward |
| A / D | Steer | Strafe |
| Mouse, Left / Right | Look | Look |
| Space | Service brake | — |
| Shift | Handbrake | Run |
| E | Leave vehicle (while stopped) | Enter vehicle/building, interact, talk, collect, deliver |
| V | Toggle chase / first-person | Toggle third-person / first-person outdoors |
| Mouse wheel during game | Move chase camera | Move chase camera |
| R | Refuel near mapped fuel station (within 45 m) | Refuel if vehicle is in range |
| H | Recover to nearby road | Tow wreck to repair point under game conditions |
| X | — | Damage nearby building exterior |
| K | Repair vehicle near eligible fuel/repair location | Repair eligible nearby vehicle |
| B | Surrender / pay fines near police | Surrender / pay fines near police |
| Q | Next job after completing current contract | Same |
| M | Open or close map | Same |
| J | Dispatch and field notes | Same |
| F5 / F9 | Save / reload save | Same |
| F1 | Help overlay | Help overlay |
| Escape | Close panel, otherwise save and quit | Same |

Some actions are only available when outdoors, near a suitable mapped place,
or outside an active police search. The game displays context-sensitive notices.

## Map and navigation

- Press **M** to open the zoomable, north-up full map. The minimap remains
  visible during normal play.
- **Mouse wheel** zooms the map around the cursor. **Drag** to pan. **Click**
  anywhere for a custom waypoint.
- **Tab** focuses the map search, and **Enter** selects the first result.
  Searches can match streets, addresses, place names, or categories such as `fuel`.
- **D** chooses the delivery, **T** the truck, **C** clears your waypoint.
- **F / Home** centres on the player / fits the region. **Arrow keys** pan when
  search is not focused.
- **F6** cycles through landmark tour locations (development/demo shortcut).
- **Cyan** shows a route along known connected roads; a **dotted yellow** final
  segment means the destination isn't directly on the network. It is not a
  guaranteed accessible footpath or drivable segment.
- One-way restrictions are modelled for driving. Pedestrian routing simplifies
  the graph and does not yet support all designated footpaths.

## Buildings, cars and people

Walk near an eligible Canadian-side building facade and press **E** to enter.
The entrance is a generated approximation of a doorway (not a surveyed door).
Most interiors are a simplified two-room layout. Inside, **E** can exit, speak
with a fictional caretaker, or collect an envelope. Some state survives saves.

Blue map squares identify parked vehicles that can be driven. White dots show
moving traffic; citizen locations and schedules are simulated, not surveyed.
Delivery cargo remains with the original truck even if you change vehicles.

Unlawful vehicle use, dangerous impacts or damaging buildings may trigger
witnesses, wanted levels and police responses. Repairs and fines are game
mechanics, not simulations of real municipal services or law.

## Saving and recovery

- **F5** saves, **F9** loads.
- **Escape** while no panel is open saves then quits.
- On Windows your save is in `%LOCALAPPDATA%\BluewaterHaul\save-v1.json`.
  Different operating systems use their user-local application-data location.
- Back up the file before manually editing it or trying an experimental build.
- A crash may write `crash.txt` in the same folder. See
  [troubleshooting](TROUBLESHOOTING.md).
