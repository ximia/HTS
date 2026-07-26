using System;
using System.Collections.Generic;

namespace HtsTuner.Core.Datalogging;

/// <summary>One timestamped set of channel values from the ECU.</summary>
public sealed class DatalogFrame
{
    private readonly Dictionary<Sensor, double> _values = new();

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public double this[Sensor sensor]
    {
        get => _values.TryGetValue(sensor, out var v) ? v : double.NaN;
        set => _values[sensor] = value;
    }

    public bool Has(Sensor sensor) => _values.ContainsKey(sensor);

    public IReadOnlyDictionary<Sensor, double> Values => _values;
}
