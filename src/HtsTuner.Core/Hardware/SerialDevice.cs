namespace HtsTuner.Core.Hardware;

/// <summary>The kind of Moates device detected on a serial port.</summary>
public enum DeviceKind
{
    Unknown,
    OstrichEmulator, // real-time emulator (tune)
    HulogDatalogger, // Honda datalog cable
}

/// <summary>A serial port that looks like a piece of tuning hardware.</summary>
public sealed record SerialDevice(string PortName, DeviceKind Kind, string Description)
{
    public override string ToString() => $"{PortName} — {Kind} ({Description})";
}
