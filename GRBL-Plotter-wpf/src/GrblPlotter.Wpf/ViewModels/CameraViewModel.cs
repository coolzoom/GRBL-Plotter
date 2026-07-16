using System.Globalization;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrblPlotter.Wpf.Models;
using Microsoft.Win32;

namespace GrblPlotter.Wpf.ViewModels;

/// <summary>Camera tool stub: no live webcam dependency (no OpenCvSharp), but supports loading a still
/// reference frame, a crosshair overlay (drawn in XAML), teaching X/Y offsets and jogging to them.</summary>
public partial class CameraViewModel : ObservableObject
{
    private readonly Action<string> _sendLine;
    private readonly Func<AxisPosition>? _getWorkPos;

    [ObservableProperty] private BitmapImage? _stillImage;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _offsetX;
    [ObservableProperty] private double _offsetY;
    [ObservableProperty] private double _teachX;
    [ObservableProperty] private double _teachY;
    [ObservableProperty] private string _statusText = "camera stopped (still-image mode)";
    [ObservableProperty] private string? _imagePath;

    public CameraViewModel(Action<string> sendLine, Func<AxisPosition>? getWorkPos = null)
    {
        _sendLine = sendLine;
        _getWorkPos = getWorkPos;
    }

    [RelayCommand]
    private void ToggleCamera()
    {
        IsRunning = !IsRunning;
        StatusText = IsRunning
            ? "camera 'running' - no live feed available in this build, use Load still image"
            : "camera stopped";
    }

    [RelayCommand]
    private void LoadStillImage()
    {
        var dlg = new OpenFileDialog { Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dlg.ShowDialog() != true) return;
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.UriSource = new Uri(dlg.FileName);
        bmp.EndInit();
        StillImage = bmp;
        ImagePath = dlg.FileName;
        StatusText = $"loaded still frame {System.IO.Path.GetFileName(dlg.FileName)}";
    }

    [RelayCommand]
    private void TeachOffset()
    {
        var pos = _getWorkPos?.Invoke();
        if (pos != null)
        {
            TeachX = pos.X;
            TeachY = pos.Y;
        }
        OffsetX = TeachX;
        OffsetY = TeachY;
        StatusText = $"taught camera offset X{OffsetX:0.000} Y{OffsetY:0.000}";
    }

    [RelayCommand]
    private void SendJogToOffset()
    {
        var x = OffsetX.ToString(CultureInfo.InvariantCulture);
        var y = OffsetY.ToString(CultureInfo.InvariantCulture);
        _sendLine($"G90 G0 X{x} Y{y}");
        StatusText = $"jogged to camera offset X{OffsetX:0.000} Y{OffsetY:0.000}";
    }
}
