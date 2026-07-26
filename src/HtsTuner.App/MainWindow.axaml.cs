using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using HtsTuner.Core.Hardware;

namespace HtsTuner.App;

public partial class MainWindow : Window
{
    private readonly StringBuilder _log = new();
    private SerialDevice? _selected;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        DeviceList.SelectionChanged += (_, _) => _selected = DeviceList.SelectedItem as SerialDevice;
        Log("Ready. Plug in the Ostrich and HULOG, then click \"Scan for hardware\".");
    }

    private void OnScanClick(object? sender, RoutedEventArgs e)
    {
        DeviceList.Items.Clear();
        var devices = SerialDeviceScanner.Scan();
        if (devices.Count == 0)
        {
            Log("No serial devices found. On macOS the cables appear as /dev/cu.usbserial-* once the FTDI driver is installed.");
            return;
        }

        foreach (var d in devices)
            DeviceList.Items.Add(d);
        Log($"Found {devices.Count} serial device(s).");
    }

    private void OnHandshakeOstrichClick(object? sender, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            Log("Select a port in the list first.");
            return;
        }

        try
        {
            using var ostrich = new OstrichEmulator(_selected.PortName);
            var ok = ostrich.Handshake();
            Log(ok
                ? $"Ostrich acknowledged on {_selected.PortName} at {OstrichEmulator.Baud} baud."
                : $"No acknowledgement from {_selected.PortName}. Check cable / driver / that this is the Ostrich port.");
        }
        catch (Exception ex)
        {
            Log($"Error opening {_selected.PortName}: {ex.Message}");
        }
    }

    private void Log(string message)
    {
        _log.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        LogBox.Text = _log.ToString();
    }
}
