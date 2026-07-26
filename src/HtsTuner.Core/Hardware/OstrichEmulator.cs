using System;
using System.IO.Ports;
using System.Threading;

namespace HtsTuner.Core.Hardware;

/// <summary>
/// Talks to a Moates Ostrich 2.0 real-time emulator over its FTDI virtual COM
/// port. The original HTS opened the Ostrich as a managed <see cref="SerialPort"/>
/// at 312500 baud and waited for a 0xAF acknowledgement byte — exactly what this
/// class does, so it works unchanged on macOS against /dev/cu.usbserial-*.
/// </summary>
public sealed class OstrichEmulator : IDisposable
{
    public const int Baud = 312500;

    // Recovered command bytes: a two-byte reset ('L','L' = 0x4C,0x4C) primes the
    // board, after which it answers with the ack byte 0xAF.
    private static readonly byte[] EnterCommand = { 0x4C, 0x4C };

    private readonly SerialPort _port;

    public OstrichEmulator(string portName)
    {
        _port = new SerialPort(portName, Baud)
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000,
            DtrEnable = true,
            RtsEnable = true,
        };
    }

    public bool IsOpen => _port.IsOpen;

    public void Open()
    {
        if (!_port.IsOpen)
            _port.Open();
    }

    /// <summary>
    /// Sends the enter/handshake command and waits for the 0xAF ack, matching the
    /// original firmware exchange. Retries a few times like the original did.
    /// </summary>
    public bool Handshake(int attempts = 5)
    {
        Open();
        for (var i = 0; i < attempts; i++)
        {
            _port.Write(EnterCommand, 0, EnterCommand.Length);
            Thread.Sleep(300);
            try
            {
                if ((byte)_port.ReadByte() == Protocol.DatalogTags.Ack)
                    return true;
            }
            catch (TimeoutException) { /* retry */ }
        }
        return false;
    }

    /// <summary>Writes a raw block to the emulator (e.g. a ROM image).</summary>
    public void Write(ReadOnlySpan<byte> data)
    {
        Open();
        _port.Write(data.ToArray(), 0, data.Length);
    }

    public void Dispose() => _port.Dispose();
}
