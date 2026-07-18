# GRBL-Plotter WPF — Feature Parity Map

## WinForms → WPF coverage

| WinForms area | WPF status |
|---------------|------------|
| Main shell / menus | Done |
| Recent files / settings import-export | Done |
| Serial / 2nd / 3rd COM | Done |
| Streaming / sim / overrides / D0–D3 | Done |
| DRO / Jog diagonals / Move-to-graphic / Move-to-zero | Done |
| Canvas edit select + path context + marker + background | Done |
| Editor send line / fold comments / sort | Done |
| Tool list CSV | Done |
| Plotter pen Up/Down/Zero/Dot | Done |
| Transforms + rotary/radius/origin pivot/hatch | Done |
| Gerber apertures (circle) / Code128 barcode | Done |
| Live camera (FlashCap) + G54 teach | Done |
| Projector live toolpath | Done |
| Graphic tablet sketch | Done |
| GamePad (XInput) | Done |
| Hershey stroke text (subset) | Done |
| Hotkeys (arrows, PgUp/Dn, F5–F7, Ctrl+R, Esc, Space) | Done |
| i18n en/zh strings | Done (partial UI keys) |
| Extensions folder menu | Done |
| Full hatch/tangential/clip CAM | Partial |
| Full Hershey library / all locales | Partial |
| Exact WinForms editor block XML sort | Partial |

## Run

```bat
dotnet run --project src\GrblPlotter.Wpf\GrblPlotter.Wpf.csproj -c Debug
```
