using System;

namespace HtsTuner.Core.Datalogging;

/// <summary>
/// Decodes the original HTS 64-byte ECU datalog frame into engineering values.
/// The frame layout, checksum and conversions were reverse-engineered from the
/// original binary — see docs/DATALOG_PROTOCOL.md. Conversions flagged there as
/// "(verify)" are best-effort until confirmed against a live capture; the frame
/// structure (offsets, endianness, checksum) is solid.
/// </summary>
public static class HondaFrameDecoder
{
    public const int FrameSize = 64;

    /// <summary>16-bit little-endian field: lo + hi*256.</summary>
    private static int Le16(ReadOnlySpan<byte> f, int loOffset) => f[loOffset] + f[loOffset + 1] * 256;

    /// <summary>Aux analog field: high byte first, then rescaled 0..1024 -> 0..255.</summary>
    private static double Analog(ReadOnlySpan<byte> f, int hiOffset) =>
        (f[hiOffset] * 256 + f[hiOffset + 1]) * 255.0 / 1024.0;

    /// <summary>Verifies the running-sum checksum against the last frame byte.</summary>
    public static bool ChecksumValid(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < FrameSize) return false;
        var sum = 0;
        for (var i = 0; i < FrameSize - 1; i++) sum += frame[i];
        return (byte)sum == frame[FrameSize - 1];
    }

    /// <summary>
    /// Decodes a 64-byte frame. Set <paramref name="rpmScale"/> once a live capture
    /// confirms it (default keeps the raw 16-bit count).
    /// </summary>
    public static DatalogFrame Decode(ReadOnlySpan<byte> frame, double rpmScale = 1.0)
    {
        if (frame.Length < FrameSize)
            throw new ArgumentException($"Frame must be {FrameSize} bytes.", nameof(frame));

        var d = new DatalogFrame();

        d[Sensor.Ect] = frame[1] / 4.0;                 // °C
        d[Sensor.Iat] = frame[2] / 4.0;                 // °C
        d[Sensor.Pa]  = (frame[4] / 2.0 + 24) * 7.221 - 59.0;  // baro, kPa
        d[Sensor.Map] = (frame[5] / 2.0 + 24) * 7.221 - 59.0;  // MAP, kPa (K≈24)
        d[Sensor.Tps] = frame[6] * 100.0 / 255.0;       // % (verify)
        d[Sensor.Rpm] = Le16(frame, 7) * rpmScale;      // rpm (scale to verify)
        d[Sensor.Vss] = frame[17] * 0.1;                // speed
        d[Sensor.InjDur] = Le16(frame, 18) / 4.0;       // ms
        d[Sensor.IgnFnl] = frame[20];                   // degrees (verify)
        d[Sensor.IgnTbl] = frame[21];                   // degrees (verify)
        d[Sensor.BatV] = 26.0 * frame[26] / 270.0;      // volts

        // Aux analog inputs (e.g. a serial/analog wideband) live at 53..62.
        // AFR wiring depends on the user's wideband; expose the first aux channel
        // raw here until the source is confirmed.
        d[Sensor.EcuO2V] = frame[3] * 5.0 / 255.0;      // approx 0..5V (verify)

        return d;
    }
}
