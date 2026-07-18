using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GrblPlotter.Wpf.ViewModels;

namespace GrblPlotter.Wpf.Views;

public partial class MainWindow : Window
{
    private readonly Dictionary<string, Window> _tools = new();

    public MainWindow()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private MainViewModel Vm => (MainViewModel)DataContext;

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O) { Vm.OpenFileCommand.Execute(null); e.Handled = true; }
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S) { Vm.SaveFileCommand.Execute(null); e.Handled = true; }
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z) { Vm.UndoCommand.Execute(null); e.Handled = true; }
        if (e.Key == Key.Escape) { Vm.JogCancelCommand.Execute(null); e.Handled = true; }
        if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.None &&
            !(Keyboard.FocusedElement is TextBox))
        {
            Vm.FeedHoldCommand.Execute(null);
            e.Handled = true;
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
        ShowTool("proj", () => new ProjectorWindow());

    private void OpenSecondSerial_Click(object sender, RoutedEventArgs e) =>
        ShowTool("serial2", () => new SecondSerialWindow());

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
