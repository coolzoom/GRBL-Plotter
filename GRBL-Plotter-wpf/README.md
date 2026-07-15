# GRBL-Plotter WPF

WPF rewrite of GRBL-Plotter with an industrial dark theme (slate + teal).

## Run

```bat
dotnet run --project src\GrblPlotter.Wpf\GrblPlotter.Wpf.csproj -c Debug
```

Or open `GRBL-Plotter-Wpf.slnx` in Visual Studio / Cursor.

Output: `src\GrblPlotter.Wpf\bin\Debug\net8.0-windows\GRBL-Plotter-Wpf.exe`

## Implemented (core parity)

- Main shell matching WinForms layout (left stream/flow/overrides/origin, center DRO + workspace, right devices/jog/COM)
- Serial connect / disconnect, port scan, baud select
- GRBL realtime: `? ! ~` Ctrl-X, `$X`, `$H`, overrides
- Status parse: `<Idle|MPos|WPos|WCO|FS|Ov>`
- G-code open/save/drag-drop, editor reparse
- Character-counting style streamer (Start / Check / Pause / Resume / Stop)
- 2D toolpath preview (G0/G1)
- Laser / Plotter / Router device tabs
- Jog ($J), zero axes, COM CNC window + log
- Industrial theme + brand placeholder image

## Not yet ported (see PLAN.md)

- Full SVG/DXF/HPGL import & Graphic pipeline
- Camera, HeightMap, Process Automation
- Localization packs
- Exact WinForms pixel layouts

## Theme

Defined in `Themes/IndustrialTheme.xaml` — deep slate backgrounds, teal accent `#3D8B8A`, danger red for RESET.
