namespace HtsTuner.Core.Datalogging;

/// <summary>
/// Core P28 datalog channels, named and ordered to match the original HTS
/// datalog export. (The original firmware exposes ~150 channels; this is the
/// commonly-used core set — extend as channels are validated on hardware.)
/// </summary>
public enum Sensor
{
    Frame,
    Duration,
    Rpm,
    Vss,
    Gear,
    Map,
    Boost,
    Pa,
    Tps,
    TpsV,
    InjDur,
    InjDuty,
    InjFV,
    IgnTbl,
    IgnFnl,
    Ect,
    Iat,
    Afr,
    EcuO2V,
    BatV,
    EldV,
    MapV,
    Mil,
}
