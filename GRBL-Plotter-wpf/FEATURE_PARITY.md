# GRBL-Plotter WPF — Feature Parity Map

## WinForms → WPF coverage

| WinForms area | WPF status |
|---------------|------------|
| Main shell / menus | Done — File, Creation, Transform, Workpiece, Machine, View, Simulation |
| Recent files | Done |
| Export/Import settings | Done — JSON |
| Serial COM CNC | Done — `SerialWindow` + inline COM panel |
| 2nd / 3rd serial / DIY | Done — `SecondSerialWindow` |
| Streaming / Check / Hold / from line | Done — `GCodeStreamer` |
| Path simulation + speed | Done — `PathSimulator` |
| DRO / Zero / Jog (incl. diagonals) / Overrides ±1/±10 | Done |
| Coolant / spindle accessories | Done — M3/M5/M7/M8/M9 |
| Origin corner presets | Done |
| Canvas modes (edit / jog click) + URL load | Done |
| Add imported to 2D view | Done |
| Laser / Plotter / Router tabs | Done + settings persistence |
| Custom script buttons | Done |
| G-code editor + undo | Done |
| Import SVG/DXF/HPGL/Gerber/CSV/Image | Done |
| Text / Shape / Image / Barcode / Wire | Done |
| Jog path / frame creator | Done — `JogPathCreateWindow` |
| Transforms (mirror/scale/rotate/origin/reverse) | Done |
| Rotate 90/180, scale to W/H, arcs→lines, polar, Z→S, remove Z | Done |
| Setup / settings JSON | Done — `SetupWindow` + `AppSettings` |
| GRBL Setup ($$) | Done — `GrblSetupWindow` |
| Laser tools / material test | Done — `LaserToolsWindow` |
| Probing / Height map / Camera / Automation | Done |
| Coordinate systems G54–G59 | Done |
| Projector overlay | Done — `ProjectorWindow` |
| View toggles (toolpath / rapid / dimension) | Done |
| Bring forms to front | Done |
| About / brand image | Done |
| 2D toolpath preview | Done |
| Drag-drop open | Done |
| GamePad | Not ported (optional hardware) |
| Full AForge live camera | Stub (still image) |
| Graphic tablet | Not ported |
| Full Hershey font library | WPF FormattedText outlines instead |
| All WinForms localizations | English UI only for now |
| Exact Graphic pipeline (hatch/tangential/clip) | Partial via imports + transforms |
| Editor block fold/sort context menu | Not ported |
| 2D path selection / crop / duplicate context | Not ported |
| Tool table / color grouping | Not ported |
| Rotary axis scale / radius compensation | Not ported |

## Run

```bat
dotnet run --project src\GrblPlotter.Wpf\GrblPlotter.Wpf.csproj -c Debug
```
