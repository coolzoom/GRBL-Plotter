using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GrblPlotter.Wpf.Services;
using GrblPlotter.Wpf.Services.Import;
using GrblPlotter.Wpf.ViewModels;

namespace GrblPlotter.Wpf.Views;

public partial class MainWindow : Window
{
    private readonly Dictionary<string, Window> _tools = new();
    private GamePadService? _gamePad;

    public MainWindow()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private MainViewModel Vm => (MainViewModel)DataContext;

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox && e.Key is not (Key.Escape or Key.F5 or Key.F6 or Key.F7))
            return;

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O) { Vm.OpenFileCommand.Execute(null); e.Handled = true; }
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S) { Vm.SaveFileCommand.Execute(null); e.Handled = true; }
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z) { Vm.UndoCommand.Execute(null); e.Handled = true; }
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.R) { Vm.SoftResetCommand.Execute(null); e.Handled = true; }
        if (e.Key == Key.Escape) { Vm.JogCancelCommand.Execute(null); e.Handled = true; }
        if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.None)
        {
            Vm.FeedHoldCommand.Execute(null);
            e.Handled = true;
        }
        if (e.Key == Key.F5) { Vm.StreamStartCommand.Execute(null); e.Handled = true; }
        if (e.Key == Key.F6) { Vm.StreamPauseCommand.Execute(null); e.Handled = true; }
        if (e.Key == Key.F7) { Vm.StreamStopCommand.Execute(null); e.Handled = true; }
        if (e.Key == Key.PageUp) { Vm.JogCommand.Execute("Z+"); e.Handled = true; }
        if (e.Key == Key.PageDown) { Vm.JogCommand.Execute("Z-"); e.Handled = true; }
        if (Keyboard.Modifiers == ModifierKeys.None)
        {
            switch (e.Key)
            {
                case Key.Left: Vm.JogCommand.Execute("X-"); e.Handled = true; break;
                case Key.Right: Vm.JogCommand.Execute("X+"); e.Handled = true; break;
                case Key.Up: Vm.JogCommand.Execute("Y+"); e.Handled = true; break;
                case Key.Down: Vm.JogCommand.Execute("Y-"); e.Handled = true; break;
            }
        }
    }

    private void ShowTool(string key, Func<Window> factory)
    {
        if (_tools.TryGetValue(key, out var existing) && existing.IsLoaded)
        {
            existing.Activate();
            return;
        }
        var w = factory();
        w.Owner = this;
        w.Closed += (_, _) => _tools.Remove(key);
        _tools[key] = w;
        w.Show();
    }

    private void OpenSerial_Click(object sender, RoutedEventArgs e) =>
        ShowTool("serial", () => new SerialWindow { DataContext = DataContext });

    private void OpenSetup_Click(object sender, RoutedEventArgs e) =>
        ShowTool("setup", () => new SetupWindow(Vm.Settings));

    private void OpenAbout_Click(object sender, RoutedEventArgs e) =>
        ShowTool("about", () => new AboutWindow());

    private void OpenProbing_Click(object sender, RoutedEventArgs e) =>
        ShowTool("probe", () => new ProbingWindow(Vm.Serial));

    private void OpenHeightMap_Click(object sender, RoutedEventArgs e) =>
        ShowTool("hmap", () => new HeightMapWindow(Vm.Serial, () => Vm.Document, d => Vm.ApplyDocument(d)));

    private void OpenCamera_Click(object sender, RoutedEventArgs e) =>
        ShowTool("cam", () => new CameraWindow(c => Vm.Serial.SendLine(c), () => Vm.CurrentWorkPos));

    private void OpenAutomation_Click(object sender, RoutedEventArgs e) =>
        ShowTool("auto", () => new AutomationWindow(Vm.Serial, path => Vm.LoadPath(path), msg => MessageBox.Show(msg, "Automation")));

    private void OpenCoord_Click(object sender, RoutedEventArgs e) =>
        ShowTool("coord", () => new CoordSystemWindow(Vm.Serial));

    private void OpenText_Click(object sender, RoutedEventArgs e) =>
        ShowTool("text", () => new TextCreateWindow(g => Vm.ApplyGeneratedGCode(g, "text.nc")));

    private void OpenShape_Click(object sender, RoutedEventArgs e) =>
        ShowTool("shape", () => new ShapeCreateWindow(g => Vm.ApplyGeneratedGCode(g, "shape.nc")));

    private void OpenImage_Click(object sender, RoutedEventArgs e) =>
        ShowTool("image", () => new ImageCreateWindow(g => Vm.ApplyGeneratedGCode(g, "image.nc")));

    private void OpenBarcode_Click(object sender, RoutedEventArgs e) =>
        ShowTool("barcode", () => new BarcodeCreateWindow(g => Vm.ApplyGeneratedGCode(g, "barcode.nc")));

    private void OpenWire_Click(object sender, RoutedEventArgs e) =>
        ShowTool("wire", () => new WireCutterWindow(g => Vm.ApplyGeneratedGCode(g, "wire.nc")));

    private void OpenProjector_Click(object sender, RoutedEventArgs e) =>
        ShowTool("proj", () =>
        {
            var w = new ProjectorWindow(Vm.ToolpathGeometry, Vm.RapidGeometry);
            return w;
        });

    private void OpenSecondSerial_Click(object sender, RoutedEventArgs e) =>
        ShowTool("serial2", () => new SecondSerialWindow());

    private void OpenTablet_Click(object sender, RoutedEventArgs e) =>
        ShowTool("tablet", () => new TabletCreateWindow(g => Vm.ApplyGeneratedGCode(g, "tablet.nc")));

    private void OpenHershey_Click(object sender, RoutedEventArgs e)
    {
        var input = new Window
        {
            Title = "Hershey stroke text", Width = 360, Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this
        };
        var box = new TextBox { Text = "GRBL", Margin = new Thickness(12) };
        var ok = new Button { Content = "Generate", Width = 90, Margin = new Thickness(12), IsDefault = true };
        ok.Click += (_, _) =>
        {
            var doc = HersheyStrokeFont.Generate(box.Text ?? "GRBL", heightMm: 12);
            Vm.ApplyDocument(doc);
            input.Close();
        };
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = "Text (A–Z / 0–9):", Margin = new Thickness(12, 12, 12, 0) });
        sp.Children.Add(box);
        sp.Children.Add(ok);
        input.Content = sp;
        input.ShowDialog();
    }

    private void LangEn_Click(object sender, RoutedEventArgs e) => LocalizationService.Apply("en");
    private void LangZh_Click(object sender, RoutedEventArgs e) => LocalizationService.Apply("zh");

    private void GamePadToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        if (mi.IsChecked)
        {
            _gamePad?.Dispose();
            _gamePad = new GamePadService(
                line => Vm.Serial.SendLine(line),
                () => Vm.FeedHoldCommand.Execute(null),
                () => Vm.CycleStartCommand.Execute(null),
                () => Vm.JogStep,
                () => Vm.JogFeed);
            _gamePad.StatusChanged += s => Dispatcher.Invoke(() => Vm.SetStatus(s));
            _gamePad.Start();
            Vm.SetStatus("GamePad enabled (A=Hold B=Resume stick=jog)");
        }
        else
        {
            _gamePad?.Dispose();
            _gamePad = null;
            Vm.SetStatus("GamePad disabled");
        }
    }

    private void ExtensionsMenu_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menu) return;
        menu.Items.Clear();
        var dirs = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Extensions"),
            Path.Combine(AppSettings.SettingsDirectory, "Extensions")
        };
        var files = new List<string>();
        foreach (var d in dirs)
        {
            if (!Directory.Exists(d)) continue;
            files.AddRange(Directory.GetFiles(d, "*.*"));
        }
        if (files.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = "(no Extensions folder files)", IsEnabled = false });
            return;
        }
        foreach (var f in files.OrderBy(x => x))
        {
            var item = new MenuItem { Header = Path.GetFileName(f), Tag = f };
            item.Click += (_, _) =>
            {
                try
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext is ".nc" or ".gcode" or ".svg" or ".dxf" or ".ngc" or ".hpgl" or ".plt")
                        Vm.LoadPath(f);
                    else
                        Process.Start(new ProcessStartInfo(f) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Extension");
                }
            };
            menu.Items.Add(item);
        }
    }

    private void OpenThirdSerial_Click(object sender, RoutedEventArgs e) =>
        ShowTool("serial3", () =>
        {
            var w = new SecondSerialWindow { Title = "3rd Serial COM" };
            return w;
        });

    private void OpenGrblSetup_Click(object sender, RoutedEventArgs e) =>
        ShowTool("grblsetup", () => new GrblSetupWindow(Vm.Serial));

    private void OpenLaserTools_Click(object sender, RoutedEventArgs e) =>
        ShowTool("laser", () => new LaserToolsWindow(Vm.Serial, g => Vm.ApplyGeneratedGCode(g, "laser-test.nc")));

    private void OpenJogPath_Click(object sender, RoutedEventArgs e) =>
        ShowTool("jogpath", () => new JogPathCreateWindow(g => Vm.ApplyGeneratedGCode(g, "jog-path.nc")));

    private void BringFormsToFront_Click(object sender, RoutedEventArgs e)
    {
        Activate();
        foreach (var w in _tools.Values.Where(x => x.IsLoaded))
        {
            w.Activate();
            w.Topmost = true;
            w.Topmost = false;
        }
    }

    private void RecentMenu_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menu) return;
        menu.Items.Clear();
        if (Vm.RecentFiles.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = "(empty)", IsEnabled = false });
            return;
        }
        foreach (var path in Vm.RecentFiles.ToList())
        {
            var item = new MenuItem { Header = path, Tag = path };
            item.Click += (_, _) => Vm.OpenRecentCommand.Execute(path);
            menu.Items.Add(item);
        }
    }

    private void PreviewCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Canvas canvas) return;
        var p = e.GetPosition(canvas);
        Vm.CanvasClick(p.X, p.Y);
    }

    private void PreviewCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Canvas canvas) return;
        var p = e.GetPosition(canvas);
        // Right-click also selects under cursor for context actions
        Vm.CanvasClick(p.X, p.Y);
        _lastCanvasClick = p;
    }

    private System.Windows.Point _lastCanvasClick;

    private void SetMarkerHere_Click(object sender, RoutedEventArgs e)
    {
        Vm.CanvasSetMarker(_lastCanvasClick.X, _lastCanvasClick.Y);
    }

    private void SendEditorLine_Click(object sender, RoutedEventArgs e)
    {
        if (EditorBox == null) return;
        int idx = EditorBox.CaretIndex;
        var text = EditorBox.Text ?? "";
        int start = text.LastIndexOf('\n', Math.Max(0, idx - 1)) + 1;
        int end = text.IndexOf('\n', idx);
        if (end < 0) end = text.Length;
        var line = text[start..end];
        Vm.SendEditorLineCommand.Execute(line);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            Vm.LoadPath(files[0]);
    }

    protected override void OnClosed(EventArgs e)
    {
        _gamePad?.Dispose();
        Vm.Settings.JogStep = Vm.JogStep;
        Vm.Settings.JogFeed = Vm.JogFeed;
        Vm.Settings.Save();
        Vm.Serial.Disconnect();
        foreach (var w in _tools.Values.ToList())
        {
            try { w.Close(); } catch { /* ignore */ }
        }
        base.OnClosed(e);
    }
}
