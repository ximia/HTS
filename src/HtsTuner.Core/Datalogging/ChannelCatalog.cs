using System.Collections.Generic;
using System.Linq;

namespace HtsTuner.Core.Datalogging;

/// <summary>Display metadata for a datalog channel.</summary>
public sealed record ChannelInfo(Sensor Sensor, string DisplayName, string Unit);

/// <summary>
/// Names, units and canonical column order for the core datalog channels. The
/// order mirrors the original HTS CSV export row so logs are drop-in familiar.
/// </summary>
public static class ChannelCatalog
{
    public static readonly IReadOnlyList<ChannelInfo> Channels = new[]
    {
        new ChannelInfo(Sensor.Frame,    "Frame",     ""),
        new ChannelInfo(Sensor.Duration, "Duration",  "s"),
        new ChannelInfo(Sensor.Rpm,      "RPM",       "rpm"),
        new ChannelInfo(Sensor.Vss,      "Speed",     "km/h"),
        new ChannelInfo(Sensor.Gear,     "Gear",      ""),
        new ChannelInfo(Sensor.Map,      "MAP",       "kPa"),
        new ChannelInfo(Sensor.Boost,    "Boost",     "psi"),
        new ChannelInfo(Sensor.Pa,       "Baro",      "kPa"),
        new ChannelInfo(Sensor.Tps,      "TPS",       "%"),
        new ChannelInfo(Sensor.TpsV,     "TPS",       "V"),
        new ChannelInfo(Sensor.InjDur,   "Inj Dur",   "ms"),
        new ChannelInfo(Sensor.InjDuty,  "Inj Duty",  "%"),
        new ChannelInfo(Sensor.InjFV,    "Inj FV",    ""),
        new ChannelInfo(Sensor.IgnTbl,   "Ign Table", "°"),
        new ChannelInfo(Sensor.IgnFnl,   "Ign Final", "°"),
        new ChannelInfo(Sensor.Ect,      "ECT",       "°C"),
        new ChannelInfo(Sensor.Iat,      "IAT",       "°C"),
        new ChannelInfo(Sensor.Afr,      "AFR",       "afr"),
        new ChannelInfo(Sensor.EcuO2V,   "ECU O2",    "V"),
        new ChannelInfo(Sensor.BatV,     "Battery",   "V"),
        new ChannelInfo(Sensor.EldV,     "ELD",       "V"),
        new ChannelInfo(Sensor.MapV,     "MAP",       "V"),
        new ChannelInfo(Sensor.Mil,      "MIL",       ""),
    };

    private static readonly Dictionary<Sensor, ChannelInfo> BySensor =
        Channels.ToDictionary(c => c.Sensor);

    public static ChannelInfo Info(Sensor sensor) => BySensor[sensor];

    /// <summary>CSV header label, e.g. "RPM (rpm)".</summary>
    public static string Header(Sensor sensor)
    {
        var info = BySensor[sensor];
        return string.IsNullOrEmpty(info.Unit) ? info.DisplayName : $"{info.DisplayName} ({info.Unit})";
    }
}
