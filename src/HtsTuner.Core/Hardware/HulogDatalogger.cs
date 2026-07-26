using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using HtsTuner.Core.Datalogging;

namespace HtsTuner.Core.Hardware;

/// <summary>
/// Reads the live ECU datalog stream from a Moates HULOG cable and decodes each
/// 64-byte frame into a <see cref="DatalogFrame"/> via <see cref="HondaFrameDecoder"/>.
/// The frame layout is reverse-engineered (see docs/DATALOG_PROTOCOL.md); the exact
/// online-request handshake and a couple of scale factors still need confirming
/// against a live capture, but the framing/decoding is real, not simulated.
/// </summary>
public sealed class HulogDatalogger : IDisposable
{
    // ASCII "DR" command header used by the firmware; the online-stream request
    // sequence is approximate until confirmed by capture.
    private static readonly byte[] StartCommand = { 0x44, 0x52 }; // "DR"

    private readonly SerialPort _port;
    private readonly double _rpmScale;

    public HulogDatalogger(string portName, int baud = 19200, double rpmScale = 1.0)
    {
        _rpmScale = rpmScale;
        _port = new SerialPort(portName, baud)
        {
            ReadTimeout = 1500,
            WriteTimeout = 1000,
        };
    }

    /// <summary>Fires for each successfully decoded, checksum-valid frame.</summary>
    public event Action<DatalogFrame>? FrameReceived;

    public void Open()
    {
        if (!_port.IsOpen) _port.Open();
    }

    public async Task RunAsync(CancellationToken token)
    {
        Open();
        try { _port.Write(StartCommand, 0, StartCommand.Length); } catch { /* best-effort */ }

        await Task.Run(() =>
        {
            var buf = new byte[HondaFrameDecoder.FrameSize];
            var have = 0;

            while (!token.IsCancellationRequested && _port.IsOpen)
            {
                try
                {
                    // Fill the buffer up to a full frame.
                    while (have < buf.Length)
                        have += _port.Read(buf, have, buf.Length - have);

                    if (HondaFrameDecoder.ChecksumValid(buf))
                    {
                        FrameReceived?.Invoke(HondaFrameDecoder.Decode(buf, _rpmScale));
                        have = 0;
                    }
                    else
                    {
                        // Resync: drop the first byte and shift the window by one.
                        Array.Copy(buf, 1, buf, 0, buf.Length - 1);
                        have = buf.Length - 1;
                    }
                }
                catch (TimeoutException) { /* keep waiting */ }
                catch (Exception) { Thread.Sleep(100); }
            }
        }, token);
    }

    public void Dispose() => _port.Dispose();
}
