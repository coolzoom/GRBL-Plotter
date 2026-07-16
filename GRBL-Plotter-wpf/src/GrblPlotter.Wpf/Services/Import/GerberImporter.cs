using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using GrblPlotter.Wpf.Models;

namespace GrblPlotter.Wpf.Services.Import;

/// <summary>
/// Very basic RS-274X (Gerber) importer. Reads the %FS (format spec) and %MO
/// (units) parameters to decode fixed-point X/Y coordinates, then converts
/// D02 (move), D01 (interpolate/draw) and D03 (flash) commands into rapid
/// moves, cut lines and dots. Aperture definitions, layer polarity and macros
/// are ignored. If no interpretable coordinate commands are found at all, a
/// commented stub is produced instead of an empty file.
/// </summary>
public static class GerberImporter
{
    private static readonly Regex CoordRx = new(
        @"^(?:G0?(?<g>\d{1,2})\*?)?(?:X(?<x>[-+]?\d+))?(?:Y(?<y>[-+]?\d+))?(?:I(?<i>[-+]?\d+))?(?:J(?<j>[-+]?\d+))?D0?(?<d>[123])$",
        RegexOptions.Compiled);
    private static readonly Regex FsRx = new(@"FS([LT])([AI])X(\d)(\d)Y(\d)(\d)", RegexOptions.Compiled);

    public static GCodeDocument Load(string path) => Parse(File.ReadAllText(path), path);

    public static GCodeDocument Parse(string gerberContent, string? path = null)
    {
        var writer = new GCodeWriter();
        writer.Header("Gerber import (basic RS-274X)");

        int decX = 4, decY = 4; // default: assume 2.4 format if no FS found
        double unitScale = 1.0; // mm per unit (1.0 for MM, 25.4 for IN)
        int movesFound = 0;
        double curX = 0, curY = 0;

        try
        {
            var flat = gerberContent.Replace("\r", "").Replace("\n", "");
            var tokens = flat.Split('*').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();

            foreach (var tokRaw in tokens)
            {
                var tok = tokRaw.TrimStart('%').Trim();
                if (tok.Length == 0) continue;

                var fs = FsRx.Match(tok);
                if (fs.Success)
                {
                    decX = int.Parse(fs.Groups[4].Value, CultureInfo.InvariantCulture);
                    decY = int.Parse(fs.Groups[6].Value, CultureInfo.InvariantCulture);
                    writer.Comment($"Format spec: X int{fs.Groups[3].Value}.dec{fs.Groups[4].Value} Y int{fs.Groups[5].Value}.dec{fs.Groups[6].Value}");
                    continue;
                }

                if (tok.StartsWith("MOMM", StringComparison.OrdinalIgnoreCase)) { unitScale = 1.0; continue; }
                if (tok.StartsWith("MOIN", StringComparison.OrdinalIgnoreCase)) { unitScale = 25.4; continue; }

                if (tok.StartsWith("AD", StringComparison.OrdinalIgnoreCase)) continue; // aperture define - ignore
                if (tok.StartsWith("AM", StringComparison.OrdinalIgnoreCase)) continue; // aperture macro - ignore
                if (tok.Equals("M02", StringComparison.OrdinalIgnoreCase) || tok.Equals("M00", StringComparison.OrdinalIgnoreCase)) break;

                var m = CoordRx.Match(tok);
                if (!m.Success) continue; // comments (G04), LP, tool selects (Dxx alone), etc. - ignore gracefully

                if (m.Groups["x"].Success) curX = ScaleCoord(m.Groups["x"].Value, decX) * unitScale;
                if (m.Groups["y"].Success) curY = ScaleCoord(m.Groups["y"].Value, decY) * unitScale;

                var dCode = m.Groups["d"].Value;
                switch (dCode)
                {
                    case "2":
                        writer.MoveTo(curX, curY);
                        movesFound++;
                        break;
                    case "1":
                        writer.LineTo(curX, curY);
                        movesFound++;
                        break;
                    case "3":
                        writer.Dot(curX, curY);
                        movesFound++;
                        break;
                }
            }

            if (movesFound == 0)
                writer.Comment("No interpretable D01/D02/D03 coordinate commands were found in this Gerber file.");
        }
        catch (Exception ex)
        {
            writer.Comment($"Gerber parse error: {ex.Message}");
        }

        writer.Footer();
        return GCodeParser.Parse(writer.ToText(), path);
    }

    private static double ScaleCoord(string digits, int decimals)
    {
        bool neg = digits.StartsWith('-');
        var d = digits.TrimStart('+', '-');
        if (!long.TryParse(d, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return 0;
        double value = v / Math.Pow(10, decimals);
        return neg ? -value : value;
    }
}
