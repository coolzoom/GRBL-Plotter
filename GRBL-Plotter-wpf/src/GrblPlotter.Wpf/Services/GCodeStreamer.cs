using GrblPlotter.Wpf.Models;

namespace GrblPlotter.Wpf.Services;

/// <summary>Character-counting style streamer compatible with GRBL ok handshake.</summary>
public sealed class GCodeStreamer
{
    private readonly GrblSerialService _serial;
    private IReadOnlyList<string> _lines = Array.Empty<string>();
    private int _index;
    private bool _running;
    private bool _paused;
    private bool _checkMode;

    public event Action? ProgressChanged;
    public event Action? Completed;

    public int CurrentLine => _index;
    public int TotalLines => _lines.Count;
    public bool IsRunning => _running;
    public bool IsPaused => _paused;
    public double Progress => TotalLines == 0 ? 0 : 100.0 * Math.Min(_index, TotalLines) / TotalLines;

    public GCodeStreamer(GrblSerialService serial)
    {
        _serial = serial;
        _serial.OkReceived += OnOk;
    }

    public void Load(GCodeDocument doc)
    {
        Stop();
        _lines = doc.Lines
            .Select(l => l.Split(';')[0].Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('('))
            .ToList();
        _index = 0;
        ProgressChanged?.Invoke();
    }

    public void Start(bool checkMode = false)
    {
        if (_lines.Count == 0) return;
        _checkMode = checkMode;
        _running = true;
        _paused = false;
        if (_checkMode) _serial.SendLine("$C");
        Pump();
        ProgressChanged?.Invoke();
    }

    public void Pause()
    {
        if (!_running) return;
        _paused = true;
        _serial.FeedHold();
        ProgressChanged?.Invoke();
    }

    public void Resume()
    {
        if (!_running) return;
        _paused = false;
        _serial.CycleStart();
        Pump();
        ProgressChanged?.Invoke();
    }

    public void Stop()
    {
        _running = false;
        _paused = false;
        _serial.ClearQueue();
        _serial.FeedHold();
        if (_checkMode)
        {
            _serial.SendLine("$C");
            _checkMode = false;
        }
        ProgressChanged?.Invoke();
    }

    private void OnOk()
    {
        if (!_running || _paused) return;
        Pump();
    }

    private void Pump()
    {
        if (!_running || _paused) return;
        // queue a few lines; serial service limits by ok-pending
        int burst = 0;
        while (burst < 8 && _index < _lines.Count)
        {
            _serial.SendLine(_lines[_index]);
            _index++;
            burst++;
        }
        ProgressChanged?.Invoke();
        if (_index >= _lines.Count && !_serial.IsConnected)
        {
            // simulation path: mark done when disconnected and drained
        }
        if (_index >= _lines.Count)
        {
            // wait for pending oks via further OnOk; when queue empty, finish
            Completed?.Invoke();
            _running = false;
            if (_checkMode)
            {
                _serial.SendLine("$C");
                _checkMode = false;
            }
            ProgressChanged?.Invoke();
        }
    }
}
