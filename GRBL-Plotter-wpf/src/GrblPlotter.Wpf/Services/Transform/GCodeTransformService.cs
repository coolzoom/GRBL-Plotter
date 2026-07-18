using GrblPlotter.Wpf.Models;

namespace GrblPlotter.Wpf.Services.Transform;

/// <summary>
/// Geometric transforms for an already-parsed <see cref="GCodeDocument"/>.
/// Every operation mutates <see cref="GCodeDocument.Segments"/> in place and
/// then regenerates <see cref="GCodeDocument.Lines"/> (via
/// <see cref="GCodeParser.BuildFromSegments"/>) plus the bounding box, so the
/// document stays internally consistent and ready to preview/save/stream.
/// Note: because the rebuild is purely motion-based, arbitrary non-motion
/// text (comments, M-codes, spindle/feed setup) in the original file is not
/// preserved - only X/Y rapid and linear motion survives the transform.
/// </summary>
public static class GCodeTransformService
{
    public static void Translate(GCodeDocument doc, double dx, double dy)
    {
        foreach (var s in doc.Segments) { s.X0 += dx; s.Y0 += dy; s.X1 += dx; s.Y1 += dy; }
        Rebuild(doc);
    }

    public static void Scale(GCodeDocument doc, double factor, bool aroundBboxCenter = false) =>
        Scale(doc, factor, factor, aroundBboxCenter);

    public static void Scale(GCodeDocument doc, double factorX, double factorY, bool aroundBboxCenter = false)
    {
        var (px, py) = PivotOf(doc, aroundBboxCenter);
        foreach (var s in doc.Segments)
        {
            s.X0 = px + (s.X0 - px) * factorX; s.Y0 = py + (s.Y0 - py) * factorY;
            s.X1 = px + (s.X1 - px) * factorX; s.Y1 = py + (s.Y1 - py) * factorY;
        }
        Rebuild(doc);
    }

    /// <summary>Rotates counter-clockwise by <paramref name="angleDeg"/> around the origin (0,0) or the bounding-box center.</summary>
    public static void Rotate(GCodeDocument doc, double angleDeg, bool aroundBboxCenter = false)
    {
        var (px, py) = PivotOf(doc, aroundBboxCenter);
        double rad = angleDeg * Math.PI / 180.0;
        double cos = Math.Cos(rad), sin = Math.Sin(rad);

        (double X, double Y) Rot(double x, double y)
        {
            double dx = x - px, dy = y - py;
            return (px + dx * cos - dy * sin, py + dx * sin + dy * cos);
        }

        foreach (var s in doc.Segments)
        {
            (s.X0, s.Y0) = Rot(s.X0, s.Y0);
            (s.X1, s.Y1) = Rot(s.X1, s.Y1);
        }
        Rebuild(doc);
    }

    /// <summary>Flips vertically (Y negated) about the bounding-box horizontal centerline, keeping the shape within its original bbox.</summary>
    public static void MirrorX(GCodeDocument doc)
    {
        var (_, cy) = BboxCenter(doc);
        foreach (var s in doc.Segments) { s.Y0 = 2 * cy - s.Y0; s.Y1 = 2 * cy - s.Y1; }
        Rebuild(doc);
    }

    /// <summary>Flips horizontally (X negated) about the bounding-box vertical centerline, keeping the shape within its original bbox.</summary>
    public static void MirrorY(GCodeDocument doc)
    {
        var (cx, _) = BboxCenter(doc);
        foreach (var s in doc.Segments) { s.X0 = 2 * cx - s.X0; s.X1 = 2 * cx - s.X1; }
        Rebuild(doc);
    }

    /// <summary>Reverses the drawing order of the whole segment list (and each segment's own direction) so the toolpath is retraced backwards.</summary>
    public static void Reverse(GCodeDocument doc)
    {
        doc.Segments.Reverse();
        foreach (var s in doc.Segments) { (s.X0, s.X1) = (s.X1, s.X0); (s.Y0, s.Y1) = (s.Y1, s.Y0); }
        Rebuild(doc);
    }

    public static void SetOriginToCenter(GCodeDocument doc)
    {
        var (cx, cy) = BboxCenter(doc);
        Translate(doc, -cx, -cy);
    }

    public static void SetOriginToMinXY(GCodeDocument doc)
    {
        RecomputeBoundsFromSegments(doc, out var minX, out var minY, out _, out _);
        Translate(doc, -minX, -minY);
    }

    public static void ScaleToWidth(GCodeDocument doc, double targetWidth)
    {
        RecomputeBoundsFromSegments(doc, out var minX, out _, out var maxX, out _);
        double span = Math.Max(maxX - minX, 1e-9);
        if (targetWidth <= 0) return;
        Scale(doc, targetWidth / span, aroundBboxCenter: true);
    }

    public static void ScaleToHeight(GCodeDocument doc, double targetHeight)
    {
        RecomputeBoundsFromSegments(doc, out _, out var minY, out _, out var maxY);
        double span = Math.Max(maxY - minY, 1e-9);
        if (targetHeight <= 0) return;
        Scale(doc, targetHeight / span, aroundBboxCenter: true);
    }

    /// <summary>Strips Z words from motion lines and drops pure Z moves.</summary>
    public static void RemoveZMoves(GCodeDocument doc)
    {
        var cleaned = new List<string>();
        foreach (var raw in doc.Lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('('))
            {
                cleaned.Add(raw);
                continue;
            }
            var upper = line.ToUpperInvariant();
            bool hasXy = upper.Contains('X') || upper.Contains('Y');
            bool hasZ = System.Text.RegularExpressions.Regex.IsMatch(upper, @"\bZ");
            if (hasZ && !hasXy && (upper.Contains("G0") || upper.Contains("G00") || upper.Contains("G1") || upper.Contains("G01")))
                continue; // drop pure Z plunge/retract
            if (hasZ)
                line = System.Text.RegularExpressions.Regex.Replace(line, @"[Zz]\s*-?\d*\.?\d+", "").Trim();
            if (line.Length > 0) cleaned.Add(line);
        }
        ReplaceFromParsed(doc, cleaned);
    }

    /// <summary>Converts G2/G3 arc words to linear G1 (simple chord approximation via parser segments already being lines).</summary>
    public static void ReplaceArcsWithLines(GCodeDocument doc)
    {
        // Segments are already linearized by the importer/parser; rewrite source lines G2/G3 → G1 and drop IJK.
        var cleaned = new List<string>();
        foreach (var raw in doc.Lines)
        {
            var line = raw;
            var upper = line.ToUpperInvariant();
            if (upper.Contains("G02") || upper.Contains("G2") || upper.Contains("G03") || upper.Contains("G3"))
            {
                line = System.Text.RegularExpressions.Regex.Replace(line, @"[Gg]0?2\b", "G1");
                line = System.Text.RegularExpressions.Regex.Replace(line, @"[Gg]0?3\b", "G1");
                line = System.Text.RegularExpressions.Regex.Replace(line, @"[Ii]\s*-?\d*\.?\d+", "");
                line = System.Text.RegularExpressions.Regex.Replace(line, @"[Jj]\s*-?\d*\.?\d+", "");
                line = System.Text.RegularExpressions.Regex.Replace(line, @"[Kk]\s*-?\d*\.?\d+", "");
                line = System.Text.RegularExpressions.Regex.Replace(line, @"\s+", " ").Trim();
            }
            cleaned.Add(line);
        }
        ReplaceFromParsed(doc, cleaned);
    }

    /// <summary>Maps Cartesian XY to polar: X'=radius, Y'=angle degrees (around bbox center).</summary>
    public static void ConvertToPolar(GCodeDocument doc)
    {
        var (cx, cy) = BboxCenter(doc);
        foreach (var s in doc.Segments)
        {
            (s.X0, s.Y0) = ToPolar(s.X0, s.Y0, cx, cy);
            (s.X1, s.Y1) = ToPolar(s.X1, s.Y1, cx, cy);
        }
        Rebuild(doc);

        static (double R, double A) ToPolar(double x, double y, double ox, double oy)
        {
            double dx = x - ox, dy = y - oy;
            return (Math.Sqrt(dx * dx + dy * dy), Math.Atan2(dy, dx) * 180.0 / Math.PI);
        }
    }

    /// <summary>Maps Z depth of each segment start to an S spindle word comment; keeps XY motion.</summary>
    public static void ConvertZToSpindle(GCodeDocument doc, double sMin = 0, double sMax = 1000)
    {
        RecomputeBoundsFromSegments(doc, out _, out var minY, out _, out var maxY);
        // Use Y span as proxy when Z not in segments — annotate lines containing Z
        var cleaned = new List<string>();
        double zMin = double.MaxValue, zMax = double.MinValue;
        var zRe = new System.Text.RegularExpressions.Regex(@"[Zz]\s*(-?\d*\.?\d+)");
        foreach (var raw in doc.Lines)
        {
            var m = zRe.Match(raw);
            if (m.Success && double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var z))
            {
                zMin = Math.Min(zMin, z);
                zMax = Math.Max(zMax, z);
            }
        }
        if (zMin == double.MaxValue) { Rebuild(doc); return; }
        double span = Math.Max(zMax - zMin, 1e-9);
        foreach (var raw in doc.Lines)
        {
            var m = zRe.Match(raw);
            if (!m.Success) { cleaned.Add(raw); continue; }
            double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var z);
            double s = sMin + (z - zMin) / span * (sMax - sMin);
            var withoutZ = zRe.Replace(raw, "").Trim();
            cleaned.Add($"{withoutZ} S{s:0.###}".Trim());
        }
        ReplaceFromParsed(doc, cleaned);
        _ = minY; _ = maxY;
    }

    private static void ReplaceFromParsed(GCodeDocument doc, List<string> cleaned)
    {
        var rebuilt = GCodeParser.Parse(string.Join(Environment.NewLine, cleaned), doc.FilePath);
        doc.Lines.Clear();
        doc.Lines.AddRange(rebuilt.Lines);
        doc.Segments.Clear();
        doc.Segments.AddRange(rebuilt.Segments);
        doc.MinX = rebuilt.MinX; doc.MaxX = rebuilt.MaxX;
        doc.MinY = rebuilt.MinY; doc.MaxY = rebuilt.MaxY;
    }

    /// <summary>Parses <paramref name="gcodeText"/>, applies <paramref name="transform"/>, and returns the resulting G-code text.</summary>
    public static string TransformText(string gcodeText, Action<GCodeDocument> transform)
    {
        var doc = GCodeParser.Parse(gcodeText);
        transform(doc);
        return string.Join(Environment.NewLine, doc.Lines);
    }

    private static (double X, double Y) PivotOf(GCodeDocument doc, bool aroundBboxCenter) =>
        aroundBboxCenter ? BboxCenter(doc) : (0, 0);

    private static (double X, double Y) BboxCenter(GCodeDocument doc)
    {
        RecomputeBoundsFromSegments(doc, out var minX, out var minY, out var maxX, out var maxY);
        return ((minX + maxX) / 2.0, (minY + maxY) / 2.0);
    }

    private static void RecomputeBoundsFromSegments(GCodeDocument doc, out double minX, out double minY, out double maxX, out double maxY)
    {
        if (doc.Segments.Count == 0)
        {
            minX = doc.MinX; maxX = doc.MaxX; minY = doc.MinY; maxY = doc.MaxY;
            return;
        }
        minX = doc.Segments.Min(s => Math.Min(s.X0, s.X1));
        maxX = doc.Segments.Max(s => Math.Max(s.X0, s.X1));
        minY = doc.Segments.Min(s => Math.Min(s.Y0, s.Y1));
        maxY = doc.Segments.Max(s => Math.Max(s.Y0, s.Y1));
    }

    private static void Rebuild(GCodeDocument doc)
    {
        var segs = doc.Segments.ToList();
        var rebuilt = GCodeParser.BuildFromSegments(segs, path: doc.FilePath);

        doc.Lines.Clear();
        doc.Lines.AddRange(rebuilt.Lines);
        doc.Segments.Clear();
        doc.Segments.AddRange(rebuilt.Segments);
        doc.MinX = rebuilt.MinX; doc.MaxX = rebuilt.MaxX;
        doc.MinY = rebuilt.MinY; doc.MaxY = rebuilt.MaxY;
    }
}
