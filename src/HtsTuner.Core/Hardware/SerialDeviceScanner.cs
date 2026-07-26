using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;

namespace HtsTuner.Core.Hardware;

/// <summary>
/// Cross-platform replacement for the original <c>snake4hts32.dll</c> native
/// <c>snake_search()</c> helper, which enumerated the FTDI USB devices and
/// reported which COM port the Ostrich lived on.
///
/// The original was a 32-bit Windows-only DLL. On macOS the same FTDI cables
/// enumerate as /dev/cu.usbserial-* (VCP driver), so we can discover them with
/// pure managed code and no native dependency.
/// </summary>
public static class SerialDeviceScanner
{
    /// <summary>Returns every serial port that plausibly hosts tuning hardware.</summary>
    public static IReadOnlyList<SerialDevice> Scan()
    {
        var results = new List<SerialDevice>();

        foreach (var port in EnumeratePorts())
        {
            var kind = Classify(port);
            if (kind == DeviceKind.Unknown && !LooksLikeUsbSerial(port))
                continue;

            results.Add(new SerialDevice(port, kind, DescribePort(port)));
        }

        return results;
    }

    /// <summary>
    /// On macOS/Linux SerialPort.GetPortNames() is incomplete, so we also glob
    /// the /dev tree for the USB-serial nodes FTDI cables create.
    /// </summary>
    internal static IEnumerable<string> EnumeratePorts()
    {
        var ports = new SortedSet<string>(SerialPort.GetPortNames());

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Directory.Exists("/dev"))
        {
            foreach (var pattern in new[] { "cu.usbserial*", "cu.usbmodem*", "tty.usbserial*", "ttyUSB*", "ttyACM*" })
            {
                foreach (var path in SafeGlob("/dev", pattern))
                    ports.Add(path);
            }
        }

        return ports;
    }

    private static IEnumerable<string> SafeGlob(string dir, string pattern)
    {
        try { return Directory.EnumerateFiles(dir, pattern); }
        catch { return System.Array.Empty<string>(); }
    }

    private static bool LooksLikeUsbSerial(string port) =>
        port.Contains("usbserial") || port.Contains("usbmodem") ||
        port.Contains("ttyUSB") || port.Contains("ttyACM") ||
        port.StartsWith("COM");

    /// <summary>
    /// Best-effort classification. Definitive identification happens on connect
    /// (the Ostrich answers a "who are you" handshake at 312500 baud), but the
    /// port name is a useful first hint for the UI.
    /// </summary>
    internal static DeviceKind Classify(string port)
    {
        var name = port.ToLowerInvariant();
        if (name.Contains("usbmodem"))
            return DeviceKind.HulogDatalogger;
        return DeviceKind.Unknown;
    }

    private static string DescribePort(string port) => Path.GetFileName(port);
}
