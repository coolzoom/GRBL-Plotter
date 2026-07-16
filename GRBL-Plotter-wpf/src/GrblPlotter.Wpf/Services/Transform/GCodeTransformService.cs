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
