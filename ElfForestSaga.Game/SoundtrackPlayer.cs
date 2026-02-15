using System.Media;

namespace ElfForestSaga.Game;

public sealed class SoundtrackPlayer
{
    private readonly int[] _melody = [523, 659, 784, 698, 659, 523, 392, 440, 523, 659, 523, 440];
    private readonly CancellationTokenSource _cts = new();
    private readonly string _customTrackPath = Path.Combine(AppContext.BaseDirectory, "Assets", "music", "custom-theme.wav");
    private SoundPlayer? _soundPlayer;

    public string CustomTrackPath => _customTrackPath;

    public void Start()
    {
        if (File.Exists(_customTrackPath))
        {
            try
            {
                _soundPlayer = new SoundPlayer(_customTrackPath);
                _soundPlayer.Load();
                _soundPlayer.PlayLooping();
                return;
            }
            catch
            {
                _soundPlayer = null;
            }
        }

        Task.Run(() =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                foreach (var tone in _melody)
                {
                    if (_cts.Token.IsCancellationRequested)
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
        }, _cts.Token);
    }

    public void Stop()
    {
        _cts.Cancel();
        _soundPlayer?.Stop();
    }
}
