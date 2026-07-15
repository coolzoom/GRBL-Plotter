using System.Windows;
using GrblPlotter.Wpf.ViewModels;

namespace GrblPlotter.Wpf.Views;

public partial class MainWindow : Window
{
    private SerialWindow? _serialWindow;

    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel Vm => (MainViewModel)DataContext;

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void OpenSerial_Click(object sender, RoutedEventArgs e)
    {
        if (_serialWindow == null || !_serialWindow.IsLoaded)
        {
            _serialWindow = new SerialWindow { DataContext = DataContext, Owner = this };
            _serialWindow.Closed += (_, _) => _serialWindow = null;
            _serialWindow.Show();
        }
        else
        {
            _serialWindow.Activate();
        }
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
        Vm.Serial.Disconnect();
        base.OnClosed(e);
    }
}
