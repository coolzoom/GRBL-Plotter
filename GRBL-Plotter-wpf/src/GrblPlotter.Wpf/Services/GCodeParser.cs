using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using GrblPlotter.Wpf.Models;

namespace GrblPlotter.Wpf.Services;

public static class GCodeParser
{
    private static readonly Regex WordRx = new(@"([GMTXYZABCIJKFSP])\s*([-+]?(?:\d+\.?\d*|\.\d+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static GCodeDocument Parse(string text, string? path = null)
    {
        var doc = new GCodeDocument { FilePath = path ?? "" };
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        double x = 0, y = 0, z = 0;
        bool absolute = true;
        bool first = true;
        double minX = 0, minY = 0, maxX = 0, maxY = 0;

        foreach (var raw in lines)
        {
            doc.Lines.Add(raw);
            var line = raw.Split(';')[0].Trim();
            if (line.Length == 0 || line.StartsWith('(')) continue;

            int? motion = null;
            double? nx = null, ny = null, nz = null;

            foreach (Match m in WordRx.Matches(line))
            {
                var letter = char.ToUpperInvariant(m.Groups[1].Value[0]);
                var val = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                switch (letter)
                {
                    case 'G':
                        var g = (int)val;
                        if (g is 0 or 1 or 2 or 3) motion = g;
                        if (g == 90) absolute = true;
                        if (g == 91) absolute = false;
                        break;
                    case 'X': nx = val; break;
                    case 'Y': ny = val; break;
                    case 'Z': nz = val; break;
                }
            }

            if (motion is null && (nx is null && ny is null && nz is null)) continue;
            motion ??= 1;

            var x1 = nx.HasValue ? (absolute ? nx.Value : x + nx.Value) : x;
            var y1 = ny.HasValue ? (absolute ? ny.Value : y + ny.Value) : y;
            var z1 = nz.HasValue ? (absolute ? nz.Value : z + nz.Value) : z;

            if (motion is 0 or 1)
            {
                doc.Segments.Add(new GCodeSegment
                {
                    Rapid = motion == 0,
                    X0 = x, Y0 = y, X1 = x1, Y1 = y1
                });
            }

            x = x1; y = y1; z = z1;
            if (first)
            {
                minX = maxX = x; minY = maxY = y; first = false;
            }
            else
            {
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            }
        }

        doc.MinX = minX; doc.MaxX = maxX; doc.MinY = minY; doc.MaxY = maxY;
        return doc;
    }

    public static GCodeDocument LoadFile(string path) =>
        Parse(File.ReadAllText(path), path);
}
