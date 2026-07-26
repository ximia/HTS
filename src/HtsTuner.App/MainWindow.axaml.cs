using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using HtsTuner.Core.Hardware;
using HtsTuner.Core.Tuning;

namespace HtsTuner.App;

public partial class MainWindow : Window
{
    private readonly StringBuilder _log = new();
    private SerialDevice? _selected;
    private RomImage? _rom;
    private RomDefinition? _definition;

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

    private async void OnLoadRomClick(object? sender, RoutedEventArgs e)
    {
        var file = await PickFileAsync("Select ROM image", "ROM", new[] { "*.bin", "*.hex", "*" });
        if (file is null) return;

        try
        {
            _rom = RomImage.Load(file);
            Log($"Loaded ROM {System.IO.Path.GetFileName(file)} — {_rom.Length} bytes"
                + (_rom.IsStockSize ? " (stock 32 KiB)." : " (non-stock / expanded size)."));
            RefreshTables();
        }
        catch (Exception ex)
        {
            Log($"Failed to load ROM: {ex.Message}");
        }
    }

    private async void OnLoadDefClick(object? sender, RoutedEventArgs e)
    {
        var file = await PickFileAsync("Select definition (JSON)", "Definition", new[] { "*.json" });
        if (file is null) return;

        try
        {
            _definition = RomDefinition.Load(file);
            Log($"Loaded definition \"{_definition.Name}\" with {_definition.Tables.Count} table(s).");
            RefreshTables();
        }
        catch (Exception ex)
        {
            Log($"Failed to load definition: {ex.Message}");
        }
    }

    private void RefreshTables()
    {
        TableList.Items.Clear();
        if (_definition is null) return;

        foreach (var def in _definition.Tables)
        {
            var bound = _rom is not null;
            TableList.Items.Add($"{def.Category}/{def.Name} — {def.Columns}×{def.Rows} @ 0x{def.Address:X}"
                + (bound ? "" : " (load a ROM to edit)"));
        }
    }

    private async Task<string?> PickFileAsync(string title, string typeName, string[] patterns)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType(typeName) { Patterns = patterns } },
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private void Log(string message)
    {
        _log.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        LogBox.Text = _log.ToString();
    }
}
