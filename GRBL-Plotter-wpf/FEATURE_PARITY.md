# GRBL-Plotter WPF — Feature Parity Map

## WinForms → WPF coverage

| WinForms area | WPF status |
|---------------|------------|
| Main shell / menus | Done — File, Creation, Transform, Workpiece, Machine, Simulation |
| Serial COM CNC | Done — `SerialWindow` + inline COM panel |
| 2nd serial / DIY | Done — `SecondSerialWindow` |
| Streaming / Check / Hold | Done — `GCodeStreamer` |
| Path simulation | Done — `PathSimulator` + marker |
| DRO / Zero / Jog / Overrides | Done |
| Origin corner presets | Done |
| Laser / Plotter / Router tabs | Done + settings persistence |
| Custom script buttons | Done |
| G-code editor + undo | Done |
| Import SVG | Done — `SvgImporter` |
| Import DXF | Done — `DxfImporter` |
| Import HPGL/PLT | Done — `HpglImporter` |
| Import Gerber | Done — `GerberImporter` |
| Import CSV/Excellon drill | Done — `CsvDrillImporter` |
| Import images (line scan) | Done — `ImageLineTracer` |
| Text → G-code | Done — `TextCreateWindow` |
| Shape → G-code | Done — `ShapeCreateWindow` |
| Image create dialog | Done — `ImageCreateWindow` |
| Barcode | Done — `BarcodeCreateWindow` |
| Wire cutter | Done — `WireCutterWindow` |
| Transforms (mirror/scale/rotate/…) | Done — `GCodeTransformService` |
| Setup / settings JSON | Done — `SetupWindow` + `AppSettings` |
| Probing | Done — `ProbingWindow` |
| Height map | Done — `HeightMapWindow` + `HeightMapService` |
| Camera (teach offsets) | Done — `CameraWindow` (still-image + teach) |
| Process automation | Done — `AutomationWindow` |
| Coordinate systems G54–G59 | Done — `CoordSystemWindow` |
| Projector overlay | Done — `ProjectorWindow` |
| About / brand image | Done — `AboutWindow` |
| 2D toolpath preview | Done |
| Drag-drop open | Done |
| GamePad | Not ported (optional hardware) |
| Full AForge live camera | Stub (still image) — live webcam later |
| Full Hershey font library | WPF FormattedText outlines instead |
| All WinForms localizations | English UI only for now |
| Exact Graphic pipeline (hatch/tangential/clip) | Partial via imports + transforms |

## Run

```bat
dotnet run --project src\GrblPlotter.Wpf\GrblPlotter.Wpf.csproj -c Debug
```
