using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace HtsTuner.App.Widgets;

/// <summary>A compact readout tile: label, big value, unit, and a fill bar.</summary>
public sealed class Gauge : Border
{
    private readonly TextBlock _value;
    private readonly Rectangle _fill;
    private readonly double _min;
    private readonly double _max;

    public Gauge(string label, string unit, double min, double max)
    {
        _min = min;
        _max = max;

        Width = 160;
        Height = 110;
        Margin = new Thickness(6);
        CornerRadius = new CornerRadius(8);
        Background = new SolidColorBrush(Color.Parse("#232833"));
        Padding = new Thickness(12, 10);

        _value = new TextBlock
        {
            Text = "—",
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#e8ebf2")),
        };

        _fill = new Rectangle
        {
            Height = 6,
            RadiusX = 3,
            RadiusY = 3,
            HorizontalAlignment = HorizontalAlignment.Left,
            Fill = new SolidColorBrush(Color.Parse("#ff7a1a")),
            Width = 0,
        };

        Child = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Text = label.ToUpperInvariant(),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.Parse("#8a90a0")),
                },
                _value,
                new TextBlock
                {
                    Text = unit,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.Parse("#8a90a0")),
                },
                new Border
                {
                    Height = 6,
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(Color.Parse("#12141a")),
                    Child = _fill,
                },
            },
        };
    }

    public void Update(double value)
    {
        _value.Text = value.ToString("0.##");
        var frac = _max > _min ? (value - _min) / (_max - _min) : 0;
        frac = frac < 0 ? 0 : frac > 1 ? 1 : frac;
        // Bar width is set relative to the tile's inner width (~136px).
        _fill.Width = 136 * frac;
    }
}
