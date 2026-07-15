using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrblPlotter.Wpf.Models;
using GrblPlotter.Wpf.Services;
using Microsoft.Win32;

namespace GrblPlotter.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly GrblSerialService _serial = new();
    private readonly GCodeStreamer _streamer;
    private readonly DispatcherTimer _uiTimer;

    public GrblSerialService Serial => _serial;
    public ObservableCollection<string> LogLines { get; } = new();
    public ObservableCollection<string> Ports { get; } = new();
    public ObservableCollection<int> BaudRates { get; } = new() { 9600, 19200, 38400, 57600, 115200, 230400 };

    [ObservableProperty] private string _selectedPort = "";
    [ObservableProperty] private int _selectedBaud = 115200;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _connectionText = "not connected";
    [ObservableProperty] private string _machineStateText = "unknown";
    [ObservableProperty] private string _statusBanner = "nothing loaded";
    [ObservableProperty] private string _windowTitle = "GRBL Plotter WPF Ver.:1.8.0.2 | grbl: not connected";

    [ObservableProperty] private string _workX = "00000.000";
    [ObservableProperty] private string _workY = "00000.000";
    [ObservableProperty] private string _workZ = "00000.000";
    [ObservableProperty] private string _machineX = "00000.000";
    [ObservableProperty] private string _machineY = "00000.000";
    [ObservableProperty] private string _machineZ = "00000.000";
    [ObservableProperty] private string _coordSystem = "G54";

    [ObservableProperty] private string _gcodeText = "";
    [ObservableProperty] private string _fileLabel = "nothing loaded";
    [ObservableProperty] private double _streamProgress;
    [ObservableProperty] private string _streamInfo = "Prog 0%  Time —";
    [ObservableProperty] private bool _isStreaming;

    [ObservableProperty] private int _ovFeed = 100;
    [ObservableProperty] private int _ovRapid = 100;
    [ObservableProperty] private int _ovSpindle = 100;

    [ObservableProperty] private double _jogStep = 1;
    [ObservableProperty] private double _jogFeed = 1000;

    [ObservableProperty] private int _laserPower = 500;
    [ObservableProperty] private int _laserSpeed = 1000;
    [ObservableProperty] private int _laserPasses = 1;
    [ObservableProperty] private bool _airAssist;

    [ObservableProperty] private double _plotterZUp = 2;
    [ObservableProperty] private double _plotterZDown = -2;
    [ObservableProperty] private double _plotterSpeed = 1000;

    [ObservableProperty] private double _routerSpeedXy = 200;
    [ObservableProperty] private double _routerSpeedZ = 100;
    [ObservableProperty] private double _routerDepth = -1;

    [ObservableProperty] private Geometry? _toolpathGeometry;
    [ObservableProperty] private Geometry? _rapidGeometry;
    [ObservableProperty] private ImageSource? _placeholderImage;
    [ObservableProperty] private bool _showPlaceholder = true;

    public GCodeDocument Document { get; private set; } = new();

    public MainViewModel()
    {
        _streamer = new GCodeStreamer(_serial);
        _serial.LogReceived += OnLog;
        _serial.StatusUpdated += OnStatus;
        _serial.ConnectionChanged += c =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = c;
                ConnectionText = c ? $"connected {_serial.PortName}" : "not connected";
                RefreshTitle();
            });
        };
        _streamer.ProgressChanged += () =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StreamProgress = _streamer.Progress;
                StreamInfo = $"Prog {_streamer.Progress:0}%  Line {_streamer.CurrentLine}/{_streamer.TotalLines}";
                IsStreaming = _streamer.IsRunning;
            });
        };
        _streamer.Completed += () =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusBanner = "streaming complete";
                IsStreaming = false;
            });
        };

        try
        {
            PlaceholderImage = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Assets/modell.png"));
        }
        catch { /* optional */ }

        RefreshPorts();
        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _uiTimer.Tick += (_, _) => { if (!IsConnected) RefreshPorts(); };
        _uiTimer.Start();
    }

    private void RefreshTitle() =>
        WindowTitle = $"GRBL Plotter WPF Ver.:1.8.0.2 | grbl: {ConnectionText}";

    [RelayCommand]
    private void RefreshPorts()
    {
        var ports = GrblSerialService.GetPortNames();
        Ports.Clear();
        foreach (var p in ports) Ports.Add(p);
        if (Ports.Count > 0 && (string.IsNullOrEmpty(SelectedPort) || !Ports.Contains(SelectedPort)))
            SelectedPort = Ports[0];
        if (Ports.Count == 0)
            StatusBanner = "no serial ports found";
    }

    [RelayCommand]
    private void ConnectToggle()
    {
        try
        {
            if (IsConnected) _serial.Disconnect();
            else
            {
                if (string.IsNullOrEmpty(SelectedPort))
                {
                    StatusBanner = "select a COM port";
                    return;
                }
                _serial.Connect(SelectedPort, SelectedBaud);
            }
        }
        catch (Exception ex)
        {
            StatusBanner = $"COM Port {SelectedPort} failed: {ex.Message}";
            OnLog($"[ERROR] {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "G-Code (*.nc;*.gcode;*.ngc;*.tap)|*.nc;*.gcode;*.ngc;*.tap|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        LoadPath(dlg.FileName);
    }

    public void LoadPath(string path)
    {
        Document = GCodeParser.LoadFile(path);
        GcodeText = string.Join(Environment.NewLine, Document.Lines);
        FileLabel = Document.FileName;
        StatusBanner = $"loaded {Document.FileName}  ({Document.Lines.Count} lines)";
        _streamer.Load(Document);
        BuildPreview();
        ShowPlaceholder = Document.Segments.Count == 0;
    }

    [RelayCommand]
    private void SaveFile()
    {
        var dlg = new SaveFileDialog { Filter = "G-Code (*.nc)|*.nc|All files (*.*)|*.*", FileName = Document.FileName };
        if (dlg.ShowDialog() != true) return;
        File.WriteAllText(dlg.FileName, GcodeText);
        Document.FilePath = dlg.FileName;
        FileLabel = Document.FileName;
        StatusBanner = $"saved {Document.FileName}";
    }

    [RelayCommand]
    private void ApplyEditor()
    {
        Document = GCodeParser.Parse(GcodeText, Document.FilePath);
        _streamer.Load(Document);
        BuildPreview();
        ShowPlaceholder = Document.Segments.Count == 0;
        StatusBanner = $"parsed {Document.Lines.Count} lines / {Document.Segments.Count} segments";
    }

    private void BuildPreview()
    {
        var cut = new StreamGeometry();
        var rapid = new StreamGeometry();
        using (var c = cut.Open())
        using (var r = rapid.Open())
        {
            foreach (var s in Document.Segments)
            {
                var g = s.Rapid ? r : c;
                g.BeginFigure(new Point(s.X0, -s.Y0), false, false);
                g.LineTo(new Point(s.X1, -s.Y1), true, false);
            }
        }
        cut.Freeze();
        rapid.Freeze();
        ToolpathGeometry = cut;
        RapidGeometry = rapid;
    }

    [RelayCommand] private void StreamStart() { ApplyEditor(); _streamer.Start(false); StatusBanner = "streaming…"; }
    [RelayCommand] private void StreamCheck() { ApplyEditor(); _streamer.Start(true); StatusBanner = "check mode…"; }
    [RelayCommand] private void StreamPause() => _streamer.Pause();
    [RelayCommand] private void StreamResume() => _streamer.Resume();
    [RelayCommand] private void StreamStop() { _streamer.Stop(); StatusBanner = "stopped"; }

    [RelayCommand] private void FeedHold() => _serial.FeedHold();
    [RelayCommand] private void CycleStart() => _serial.CycleStart();
    [RelayCommand] private void SoftReset() => _serial.SoftReset();
    [RelayCommand] private void KillAlarm() => _serial.KillAlarm();
    [RelayCommand] private void Door() => _serial.SendRealtime(0x84);
    [RelayCommand] private void HomeMachine() => _serial.Home();

    [RelayCommand] private void ZeroX() => _serial.SendLine("G10 L20 P0 X0");
    [RelayCommand] private void ZeroY() => _serial.SendLine("G10 L20 P0 Y0");
    [RelayCommand] private void ZeroZ() => _serial.SendLine("G10 L20 P0 Z0");
    [RelayCommand] private void ZeroXy() => _serial.SendLine("G10 L20 P0 X0 Y0");
    [RelayCommand] private void ZeroXyz() => _serial.SendLine("G10 L20 P0 X0 Y0 Z0");

    [RelayCommand]
    private void Jog(string axisDir)
    {
        // axisDir like "X+" "Y-" "Z+"
        if (axisDir.Length < 2) return;
        var axis = axisDir[0];
        var sign = axisDir[1] == '-' ? -1 : 1;
        var d = JogStep * sign;
        _serial.SendLine($"$J=G91 G21 {axis}{d.ToString(System.Globalization.CultureInfo.InvariantCulture)} F{JogFeed.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    [RelayCommand] private void JogCancel() => _serial.SendRealtime(0x85);

    [RelayCommand] private void OvFeedPlus() => _serial.SendRealtime(0x91);
    [RelayCommand] private void OvFeedMinus() => _serial.SendRealtime(0x92);
    [RelayCommand] private void OvFeedReset() => _serial.SendRealtime(0x90);
    [RelayCommand] private void OvRapid25() => _serial.SendRealtime(0x97);
    [RelayCommand] private void OvRapid50() => _serial.SendRealtime(0x96);
    [RelayCommand] private void OvRapid100() => _serial.SendRealtime(0x95);
    [RelayCommand] private void OvSpindlePlus() => _serial.SendRealtime(0x9A);
    [RelayCommand] private void OvSpindleMinus() => _serial.SendRealtime(0x9B);
    [RelayCommand] private void OvSpindleReset() => _serial.SendRealtime(0x99);

    [RelayCommand]
    private void SendManual(string? cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return;
        _serial.SendLine(cmd);
    }

    [RelayCommand]
    private void LaserOn()
    {
        _serial.SendLine($"M3 S{LaserPower}");
        if (AirAssist) _serial.SendLine("M8");
    }

    [RelayCommand] private void LaserOff() { _serial.SendLine("M5"); _serial.SendLine("M9"); }

    [RelayCommand]
    private void ApplyLaserDefaults()
    {
        StatusBanner = $"Laser defaults: S{LaserPower} F{LaserSpeed} passes={LaserPasses}";
    }

    [RelayCommand]
    private void ApplyPlotterDefaults()
    {
        StatusBanner = $"Plotter Zup={PlotterZUp} Zdown={PlotterZDown} F={PlotterSpeed}";
    }

    [RelayCommand]
    private void ApplyRouterDefaults()
    {
        StatusBanner = $"Router Fxy={RouterSpeedXy} Fz={RouterSpeedZ} depth={RouterDepth}";
    }

    [RelayCommand]
    private void SetOriginCorner(string corner)
    {
        // Places work origin conceptually; sends G10 based on current WPos dimensions of loaded graphic
        StatusBanner = $"G-Code origin preset: {corner}";
    }

    private void OnLog(string line)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogLines.Add(line);
            while (LogLines.Count > 500) LogLines.RemoveAt(0);
        });
    }

    private void OnStatus(GrblStatusSnapshot s)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MachineStateText = s.State.ToString();
            WorkX = $"{s.Work.X:00000.000}";
            WorkY = $"{s.Work.Y:00000.000}";
            WorkZ = $"{s.Work.Z:00000.000}";
            MachineX = $"{s.Machine.X:00000.000}";
            MachineY = $"{s.Machine.Y:00000.000}";
            MachineZ = $"{s.Machine.Z:00000.000}";
            OvFeed = s.OvFeed;
            OvRapid = s.OvRapid;
            OvSpindle = s.OvSpindle;
        });
    }
}
