using System.Media;

namespace ElfForestSaga.Game;

public sealed class SoundtrackPlayer
{
    private readonly int[] _melody = [523, 659, 784, 698, 659, 523, 392, 440, 523, 659, 523, 440];
    private CancellationTokenSource _cts = new();
    private readonly string _customTrackPath = Path.Combine(AppContext.BaseDirectory, "Assets", "music", "custom-theme.wav");
    private SoundPlayer? _soundPlayer;

    public string CustomTrackPath => _customTrackPath;

    public void Start()
    {
        if (TryStartCustomTrack())
        {
            return;
        }

        var token = _cts.Token;
        Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var tone in _melody)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        Console.Beep(tone, 120);
                    }
                    catch
                    {
                        Thread.Sleep(120);
                    }
                }
            }
        }, token);
    }

    public void Restart()
    {
        Stop();
        _cts = new CancellationTokenSource();
        Start();
    }

    public void Stop()
    {
        _cts.Cancel();
        _soundPlayer?.Stop();
        _soundPlayer?.Dispose();
        _soundPlayer = null;
    }

    private bool TryStartCustomTrack()
    {
        if (!File.Exists(_customTrackPath))
        {
            return false;
        }

        try
        {
            _soundPlayer = new SoundPlayer(_customTrackPath);
            _soundPlayer.Load();
            _soundPlayer.PlayLooping();
            return true;
        }
        catch
        {
            _soundPlayer?.Dispose();
            _soundPlayer = null;
            return false;
        }
    }
}
