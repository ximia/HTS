using Avalonia.Media;

namespace HtsTuner.App.Widgets;

/// <summary>Maps a normalized 0..1 value to a blue→green→yellow→red heat color,
/// the familiar tuning-table gradient.</summary>
public static class Heatmap
{
    public static Color Color(double frac)
    {
        frac = frac < 0 ? 0 : frac > 1 ? 1 : frac;

        // Piecewise gradient across four stops.
        (double stop, byte r, byte g, byte b)[] stops =
        {
            (0.00, 0x21, 0x4f, 0x8a),  // blue
            (0.40, 0x2f, 0x9e, 0x6a),  // green
            (0.70, 0xd8, 0xc4, 0x3a),  // yellow
            (1.00, 0xd0, 0x3a, 0x2f),  // red
        };

        for (var i = 0; i < stops.Length - 1; i++)
        {
            var a = stops[i];
            var b = stops[i + 1];
            if (frac <= b.stop)
            {
                var t = (frac - a.stop) / (b.stop - a.stop);
                return Avalonia.Media.Color.FromRgb(
                    Lerp(a.r, b.r, t), Lerp(a.g, b.g, t), Lerp(a.b, b.b, t));
            }
        }
        var last = stops[^1];
        return Avalonia.Media.Color.FromRgb(last.r, last.g, last.b);
    }

    private static byte Lerp(byte a, byte b, double t) => (byte)(a + (b - a) * t);
}
