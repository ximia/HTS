using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using HtsTuner.Core.Protocol;

namespace HtsTuner.Core.Hardware;

/// <summary>One decoded frame of live datalog values from the ECU.</summary>
public readonly record struct DatalogSample(byte ChannelA, byte ChannelB, byte ChannelC, DateTime Timestamp);

/// <summary>
/// Reads the live datalog stream from a Moates HULOG cable. The firmware streams
/// tagged bytes: a tag (0xAF / 0xBF / 0xCF) followed by that channel's value.
/// This mirrors the exact read loop recovered from the original HTS datalogger.
/// </summary>
public sealed class HulogDatalogger : IDisposable
{
    private static readonly byte[] StartCommand = { 0x4C, 0x4C }; // "LL"

    private readonly SerialPort _port;
    private byte _a, _b, _c;

    public HulogDatalogger(string portName, int baud = 19200)
    {
        _port = new SerialPort(portName, baud)
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000,
        };
    }

    public event Action<DatalogSample>? SampleReceived;

    public void Open()
    {
        if (!_port.IsOpen)
            _port.Open();
    }

    /// <summary>
    /// Streams samples until cancelled. The original discarded the input buffer
    /// whenever more than 36 bytes backed up, to keep the log real-time.
    /// </summary>
    public async Task RunAsync(CancellationToken token)
    {
        Open();
        _port.Write(StartCommand, 0, StartCommand.Length);

        await Task.Run(() =>
        {
            while (!token.IsCancellationRequested && _port.IsOpen)
            {
                try
                {
                    switch ((byte)_port.ReadByte())
                    {
                        case DatalogTags.Ack:      _a = (byte)_port.ReadByte(); break;
                        case DatalogTags.ChannelB: _b = (byte)_port.ReadByte(); break;
                        case DatalogTags.ChannelC: _c = (byte)_port.ReadByte(); break;
                        default: continue;
                    }

                    SampleReceived?.Invoke(new DatalogSample(_a, _b, _c, DateTime.UtcNow));

                    if (_port.BytesToRead > 36)
                        _port.DiscardInBuffer();
                }
                catch (TimeoutException) { /* keep waiting */ }
                catch (Exception) { Thread.Sleep(100); }
            }
        }, token);
    }

    public void Dispose() => _port.Dispose();
}
