using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GrblPlotter.Wpf.Views;

/// <summary>Fullscreen-ish projector overlay for tracing workpieces (WinForms ControlProjector equivalent).</summary>
public class ProjectorWindow : Window
{
    public ProjectorWindow()
    {
        Title = "Projector Overlay";
        Width = 800; Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.Black;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResizeWithGrip;

        var root = new Grid();
        try
        {
            var img = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/Assets/modell.png")),
                Stretch = Stretch.Uniform,
                Opacity = 0.85
            };
            root.Children.Add(img);
        }
        catch { }

        var hint = new TextBlock
        {
            Text = "Projector mode â€?Esc to close Â· F11 fullscreen",
            Foreground = Brushes.White,
            Margin = new Thickness(12),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Opacity = 0.7
        };
        root.Children.Add(hint);
        Content = root;

        KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Escape) Close();
            if (e.Key == System.Windows.Input.Key.F11)
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        };
    }
}
