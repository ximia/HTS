using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HtsTuner.App.Widgets;
using HtsTuner.Core.Datalogging;
using HtsTuner.Core.Hardware;
using HtsTuner.Core.Tuning;

namespace HtsTuner.App;

public partial class MainWindow : Window
{
    private readonly StringBuilder _log = new();
    private SerialDevice? _selected;
    private RomImage? _rom;
    private RomDefinition? _definition;

    // Tune tab
    private Table? _activeTable;
    private TextBox[,]? _cells;
    private double _tableMin, _tableMax;

    // Datalog tab
    private readonly Dictionary<Sensor, Gauge> _gauges = new();
    private CancellationTokenSource? _logCts;
    private CsvDatalogWriter? _csv;

    // Named controls
    private TextBlock _statusChip = null!, _logBox = null!, _tableTitle = null!, _tableHint = null!, _logStatus = null!;
    private ScrollViewer _logScroller = null!;
    private ListBox _deviceList = null!, _tableList = null!;
    private Grid _tableGrid = null!;
    private WrapPanel _gaugePanel = null!;
    private CheckBox _demoCheck = null!;
    private Button _startLogButton = null!, _stopLogButton = null!;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        _statusChip = this.FindControl<TextBlock>("StatusChip")!;
        _logBox = this.FindControl<TextBlock>("LogBox")!;
        _logScroller = this.FindControl<ScrollViewer>("LogScroller")!;
        _deviceList = this.FindControl<ListBox>("DeviceList")!;
        _tableList = this.FindControl<ListBox>("TableList")!;
        _tableGrid = this.FindControl<Grid>("TableGrid")!;
        _tableTitle = this.FindControl<TextBlock>("TableTitle")!;
        _tableHint = this.FindControl<TextBlock>("TableHint")!;
        _gaugePanel = this.FindControl<WrapPanel>("GaugePanel")!;
        _demoCheck = this.FindControl<CheckBox>("DemoCheck")!;
        _logStatus = this.FindControl<TextBlock>("LogStatus")!;
        _startLogButton = this.FindControl<Button>("StartLogButton")!;
        _stopLogButton = this.FindControl<Button>("StopLogButton")!;

        _deviceList.SelectionChanged += (_, _) => _selected = _deviceList.SelectedItem as SerialDevice;

        BuildGauges();
        Log("Ready. Connect tab: scan hardware. Tune tab: load a ROM. Datalog tab: try Demo mode.");
    }

    // ---------------- Connect ----------------

    private void OnScanClick(object? sender, RoutedEventArgs e)
    {
        _deviceList.Items.Clear();
        var devices = SerialDeviceScanner.Scan();
        if (devices.Count == 0)
        {
            Log("No serial devices found. On macOS the cables appear as /dev/cu.usbserial-* once the FTDI driver is installed.");
            _statusChip.Text = "No devices";
            return;
        }

        foreach (var d in devices)
            _deviceList.Items.Add(d);
        Log($"Found {devices.Count} serial device(s).");
        _statusChip.Text = $"{devices.Count} port(s) found";
    }

    private void OnHandshakeOstrichClick(object? sender, RoutedEventArgs e)
    {
        if (_selected is null) { Log("Select a port in the list first."); return; }
        try
        {
            using var ostrich = new OstrichEmulator(_selected.PortName);
            var ok = ostrich.Handshake();
            Log(ok
                ? $"Ostrich acknowledged on {_selected.PortName} at {OstrichEmulator.Baud} baud."
                : $"No acknowledgement from {_selected.PortName}. Check cable / driver / that this is the Ostrich port.");
            _statusChip.Text = ok ? "Ostrich connected" : "No ack";
        }
        catch (Exception ex)
        {
            Log($"Error opening {_selected.PortName}: {ex.Message}");
        }
    }

    // ---------------- Tune ----------------

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
        catch (Exception ex) { Log($"Failed to load ROM: {ex.Message}"); }
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
        catch (Exception ex) { Log($"Failed to load definition: {ex.Message}"); }
    }

    private async void OnSaveRomClick(object? sender, RoutedEventArgs e)
    {
        if (_rom is null) { Log("Load a ROM first."); return; }
        var files = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save ROM image",
            DefaultExtension = "bin",
            SuggestedFileName = "tune.bin",
        });
        var path = files?.TryGetLocalPath();
        if (path is null) return;
        try { _rom.Save(path); Log($"Saved ROM to {path}."); }
        catch (Exception ex) { Log($"Failed to save ROM: {ex.Message}"); }
    }

    private void RefreshTables()
    {
        _tableList.Items.Clear();
        if (_definition is null) return;
        foreach (var def in _definition.Tables)
            _tableList.Items.Add($"{def.Category}/{def.Name}");
    }

    private void OnTableSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_definition is null || _tableList.SelectedIndex < 0) return;
        var def = _definition.Tables[_tableList.SelectedIndex];

        if (_rom is null)
        {
            _tableTitle.Text = def.Name;
            _tableHint.Text = "Load a ROM to view and edit this table's values.";
            _tableGrid.Children.Clear();
            return;
        }

        _activeTable = new Table(_rom, def);
        BuildTableGrid(def);
    }

    private void BuildTableGrid(TableDefinition def)
    {
        _tableTitle.Text = $"{def.Category} · {def.Name}";
        _tableHint.Text = $"{def.Columns} × {def.Rows} @ 0x{def.Address:X}, {def.CellSize} cells"
                        + (string.IsNullOrEmpty(def.Unit) ? "" : $", unit: {def.Unit}");

        _tableGrid.Children.Clear();
        _tableGrid.RowDefinitions.Clear();
        _tableGrid.ColumnDefinitions.Clear();
        _cells = new TextBox[def.Rows, def.Columns];

        for (var c = 0; c < def.Columns; c++)
            _tableGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        for (var r = 0; r < def.Rows; r++)
            _tableGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        ComputeRange();

        for (var r = 0; r < def.Rows; r++)
        for (var c = 0; c < def.Columns; c++)
        {
            var value = _activeTable!.Get(r, c);
            var box = new TextBox
            {
                Text = value.ToString("0.##", CultureInfo.InvariantCulture),
                Width = 56,
                MinHeight = 26,
                Margin = new Avalonia.Thickness(1),
                TextAlignment = TextAlignment.Center,
                FontSize = 12,
                Foreground = Brushes.Black,
                Background = new SolidColorBrush(CellColor(value)),
                Tag = (r, c),
            };
            box.LostFocus += OnCellCommit;
            _cells[r, c] = box;
            Grid.SetRow(box, r);
            Grid.SetColumn(box, c);
            _tableGrid.Children.Add(box);
        }
    }

    private void OnCellCommit(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox box || box.Tag is not ValueTuple<int, int> pos || _activeTable is null) return;
        if (!double.TryParse(box.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
        {
            box.Text = _activeTable.Get(pos.Item1, pos.Item2).ToString("0.##", CultureInfo.InvariantCulture);
            return;
        }
        _activeTable.Set(pos.Item1, pos.Item2, v);
        var stored = _activeTable.Get(pos.Item1, pos.Item2);
        box.Text = stored.ToString("0.##", CultureInfo.InvariantCulture);
        RecolorCells();
    }

    private void ComputeRange()
    {
        _tableMin = double.MaxValue;
        _tableMax = double.MinValue;
        for (var r = 0; r < _activeTable!.Rows; r++)
        for (var c = 0; c < _activeTable.Columns; c++)
        {
            var v = _activeTable.Get(r, c);
            if (v < _tableMin) _tableMin = v;
            if (v > _tableMax) _tableMax = v;
        }
        if (_tableMax <= _tableMin) _tableMax = _tableMin + 1;
    }

    private Color CellColor(double value)
    {
        var frac = (value - _tableMin) / (_tableMax - _tableMin);
        return Heatmap.Color(frac);
    }

    private void RecolorCells()
    {
        if (_cells is null || _activeTable is null) return;
        ComputeRange();
        for (var r = 0; r < _activeTable.Rows; r++)
        for (var c = 0; c < _activeTable.Columns; c++)
            _cells[r, c].Background = new SolidColorBrush(CellColor(_activeTable.Get(r, c)));
    }

    // ---------------- Datalog ----------------

    private void BuildGauges()
    {
        AddGauge(Sensor.Rpm, "RPM", "rpm", 0, 8000);
        AddGauge(Sensor.Map, "MAP", "kPa", 0, 110);
        AddGauge(Sensor.Tps, "TPS", "%", 0, 100);
        AddGauge(Sensor.Afr, "AFR", "afr", 10, 18);
        AddGauge(Sensor.IgnFnl, "Ignition", "°", 0, 45);
        AddGauge(Sensor.InjDur, "Inj Dur", "ms", 0, 14);
        AddGauge(Sensor.Ect, "Coolant", "°C", 0, 120);
        AddGauge(Sensor.Iat, "Intake Air", "°C", 0, 80);
        AddGauge(Sensor.Vss, "Speed", "km/h", 0, 200);
        AddGauge(Sensor.BatV, "Battery", "V", 8, 16);
    }

    private void AddGauge(Sensor sensor, string label, string unit, double min, double max)
    {
        var g = new Gauge(label, unit, min, max);
        _gauges[sensor] = g;
        _gaugePanel.Children.Add(g);
    }

    private void OnStartLogClick(object? sender, RoutedEventArgs e)
    {
        if (_logCts is not null) return;
        _logCts = new CancellationTokenSource();
        _startLogButton.IsEnabled = false;
        _stopLogButton.IsEnabled = true;

        try { _csv = CsvDatalogWriter.CreateTimestamped(DatalogDir()); Log($"Logging to {_csv.FilePath}"); }
        catch (Exception ex) { Log($"Could not open log file: {ex.Message}"); }

        if (_demoCheck.IsChecked == true)
        {
            _logStatus.Text = "Demo mode running";
            var demo = new DemoDatalogSource();
            demo.SampleReceived += OnFrame;
            _ = demo.RunAsync(_logCts.Token);
        }
        else if (_selected is not null)
        {
            _logStatus.Text = $"Logging from {_selected.PortName}";
            var logger = new HulogDatalogger(_selected.PortName);
            logger.SampleReceived += raw =>
            {
                var f = new DatalogFrame();
                f[Sensor.Rpm] = raw.ChannelA;
                f[Sensor.Map] = raw.ChannelB;
                f[Sensor.Afr] = raw.ChannelC;
                OnFrame(f);
            };
            _ = RunLoggerAsync(logger, _logCts.Token);
        }
        else
        {
            _logStatus.Text = "No port selected — enable Demo mode or select a device on the Connect tab.";
            OnStopLogClick(sender, e);
        }
    }

    private async Task RunLoggerAsync(HulogDatalogger logger, CancellationToken token)
    {
        try { await logger.RunAsync(token); }
        catch (Exception ex) { Dispatcher.UIThread.Post(() => Log($"Datalog error: {ex.Message}")); }
        finally { logger.Dispose(); }
    }

    private void OnFrame(DatalogFrame frame)
    {
        _csv?.Write(frame);
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var (sensor, gauge) in _gauges)
                if (frame.Has(sensor))
                    gauge.Update(frame[sensor]);
        });
    }

    private void OnStopLogClick(object? sender, RoutedEventArgs e)
    {
        _logCts?.Cancel();
        _logCts = null;
        _csv?.Dispose();
        _csv = null;
        _startLogButton.IsEnabled = true;
        _stopLogButton.IsEnabled = false;
        _logStatus.Text = "Stopped";
    }

    private static string DatalogDir() =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HTS Datalogs");

    // ---------------- shared ----------------

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
        _logBox.Text = _log.ToString();
        _logScroller.ScrollToEnd();
    }
}
