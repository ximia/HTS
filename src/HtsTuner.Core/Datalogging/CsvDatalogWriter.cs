using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace HtsTuner.Core.Datalogging;

/// <summary>
/// Writes datalog frames to a CSV file using the original HTS column order, so
/// existing analysis workflows keep working. Thread-unsafe by design — feed it
/// from a single logging loop.
/// </summary>
public sealed class CsvDatalogWriter : IDisposable
{
    private readonly StreamWriter _writer;
    private bool _headerWritten;

    public CsvDatalogWriter(string path)
    {
        _writer = new StreamWriter(path, append: false, Encoding.UTF8) { AutoFlush = true };
    }

    public string FilePath { get; private set; } = string.Empty;

    public static CsvDatalogWriter CreateTimestamped(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"datalog_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        return new CsvDatalogWriter(path) { FilePath = path };
    }

    public void Write(DatalogFrame frame)
    {
        if (!_headerWritten)
        {
            _writer.WriteLine("Time," + string.Join(",",
                ChannelCatalog.Channels.Select(c => Escape(ChannelCatalog.Header(c.Sensor)))));
            _headerWritten = true;
        }

        var cells = ChannelCatalog.Channels.Select(c =>
        {
            var v = frame[c.Sensor];
            return double.IsNaN(v) ? "" : v.ToString("0.###", CultureInfo.InvariantCulture);
        });

        _writer.WriteLine(frame.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                          + "," + string.Join(",", cells));
    }

    private static string Escape(string s) =>
        s.Contains(',') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

    public void Dispose() => _writer.Dispose();
}
