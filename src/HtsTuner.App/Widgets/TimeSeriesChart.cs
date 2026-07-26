using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using HtsTuner.Core.Datalogging;

namespace HtsTuner.App.Widgets;

/// <summary>
/// A lightweight scrolling multi-channel line chart, drawn directly. Each series
/// is normalized to its own min/max so channels with different ranges (RPM vs
/// AFR) share the plot. New samples push in from the right.
/// </summary>
public sealed class TimeSeriesChart : Control
{
    private const int Capacity = 320;

    private sealed class Series
    {
        public required IPen Pen;
        public double Min, Max;
        public readonly List<double> Values = new();
    }

    private readonly Dictionary<Sensor, Series> _series = new();
    private readonly IBrush _bg = new SolidColorBrush(Color.Parse("#12141a"));
    private readonly IPen _grid = new Pen(new SolidColorBrush(Color.Parse("#242833")), 1);

    public TimeSeriesChart() => MinHeight = 180;

    public void AddSeries(Sensor sensor, Color color, double min, double max) =>
        _series[sensor] = new Series
        {
            Pen = new Pen(new SolidColorBrush(color), 1.6),
            Min = min,
            Max = max,
        };

    public void Push(DatalogFrame frame)
    {
        foreach (var (sensor, series) in _series)
        {
            if (!frame.Has(sensor)) continue;
            series.Values.Add(frame[sensor]);
            if (series.Values.Count > Capacity)
                series.Values.RemoveAt(0);
        }
        InvalidateVisual();
    }

    public void Clear()
    {
        foreach (var s in _series.Values) s.Values.Clear();
        InvalidateVisual();
    }

    public override void Render(DrawingContext ctx)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        ctx.FillRectangle(_bg, new Rect(0, 0, w, h));

        // horizontal grid lines
        for (var i = 1; i < 4; i++)
        {
            var y = h * i / 4;
            ctx.DrawLine(_grid, new Point(0, y), new Point(w, y));
        }

        foreach (var series in _series.Values)
        {
            var n = series.Values.Count;
            if (n < 2) continue;
            var range = series.Max - series.Min;
            if (range <= 0) range = 1;

            var dx = w / (Capacity - 1);
            Point? prev = null;
            for (var i = 0; i < n; i++)
            {
                var frac = (series.Values[i] - series.Min) / range;
                frac = frac < 0 ? 0 : frac > 1 ? 1 : frac;
                var x = i * dx;
                var y = h - frac * h;
                var pt = new Point(x, y);
                if (prev is { } p) ctx.DrawLine(series.Pen, p, pt);
                prev = pt;
            }
        }
    }
}
