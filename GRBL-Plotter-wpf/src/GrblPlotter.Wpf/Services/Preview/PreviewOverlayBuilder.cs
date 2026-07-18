using System.Windows;
using System.Windows.Media;

namespace GrblPlotter.Wpf.Services.Preview;

/// <summary>Builds WPF geometries for ruler / machine limits / tool table markers (Wave B).</summary>
public static class PreviewOverlayBuilder
{
    public static Geometry BuildMachineLimits(double minX, double minY, double maxX, double maxY,
        Func<double, double, Point> map)
    {
        var g = new StreamGeometry();
        using var ctx = g.Open();
        var p0 = map(minX, minY);
        var p1 = map(maxX, minY);
        var p2 = map(maxX, maxY);
        var p3 = map(minX, maxY);
        ctx.BeginFigure(p0, false, true);
        ctx.LineTo(p1, true, false);
        ctx.LineTo(p2, true, false);
        ctx.LineTo(p3, true, false);
        g.Freeze();
        return g;
    }

    public static Geometry BuildRuler(double minX, double minY, double maxX, double maxY,
        Func<double, double, Point> map, double stepHint = 10)
    {
        double span = Math.Max(maxX - minX, maxY - minY);
        double step = NiceStep(span / 8);
        var g = new StreamGeometry();
        using var ctx = g.Open();
        // X axis ticks along bottom (minY)
        for (double x = Math.Ceiling(minX / step) * step; x <= maxX + 1e-9; x += step)
        {
            var a = map(x, minY);
            var b = map(x, minY + step * 0.15);
            ctx.BeginFigure(a, false, false);
            ctx.LineTo(b, true, false);
        }
        // Y axis ticks along left (minX)
        for (double y = Math.Ceiling(minY / step) * step; y <= maxY + 1e-9; y += step)
        {
            var a = map(minX, y);
            var b = map(minX + step * 0.15, y);
            ctx.BeginFigure(a, false, false);
            ctx.LineTo(b, true, false);
        }
        // border
        var p0 = map(minX, minY);
        ctx.BeginFigure(p0, false, true);
        ctx.LineTo(map(maxX, minY), true, false);
        ctx.LineTo(map(maxX, maxY), true, false);
        ctx.LineTo(map(minX, maxY), true, false);
        g.Freeze();
        return g;
    }

    public static Geometry BuildToolMarkers(IEnumerable<(double X, double Y)> tools, Func<double, double, Point> map, double size = 3)
    {
        var g = new StreamGeometry();
        using var ctx = g.Open();
        foreach (var (x, y) in tools)
        {
            var c = map(x, y);
            ctx.BeginFigure(new Point(c.X - size, c.Y), false, false);
            ctx.LineTo(new Point(c.X + size, c.Y), true, false);
            ctx.BeginFigure(new Point(c.X, c.Y - size), false, false);
            ctx.LineTo(new Point(c.X, c.Y + size), true, false);
        }
        g.Freeze();
        return g;
    }

    private static double NiceStep(double raw)
    {
        if (raw <= 0) return 10;
        double exp = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double f = raw / exp;
        double nf = f < 1.5 ? 1 : f < 3 ? 2 : f < 7 ? 5 : 10;
        return nf * exp;
    }
}
