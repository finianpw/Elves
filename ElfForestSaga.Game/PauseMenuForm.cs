namespace ElfForestSaga.Game;

public sealed class PauseMenuResult
{
    public bool ResumeRequested;
    public bool RestartRequested;
    public bool ExitRequested;
    public int SelectedLevel;
    public float Brightness;
    public string? MusicFileToImport;
    public string? BackgroundFileToImport;
    public string? ElfFileToImport;
    public bool ResetMusic;
    public bool ResetBackground;
    public bool ResetElf;
}

public sealed class PauseMenuForm : Form
{
    private readonly NumericUpDown _levelInput;
    private readonly TrackBar _brightnessBar;
    private readonly Label _brightnessLabel;
    private readonly Label _assetsLabel;

    public PauseMenuResult Result { get; } = new();

    public PauseMenuForm(int currentLevel, int maxLevel, float currentBrightness)
    {
        Text = "Menu Pauzy";
        Width = 700;
        Height = 560;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(24, 24, 35);
        ForeColor = Color.WhiteSmoke;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 12,
            Padding = new Padding(16),
            BackColor = BackColor
        };

        for (var i = 0; i < 12; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 12f));
        }

        var title = new Label
        {
            Text = "ELF FOREST SAGA - MENU",
            Dock = DockStyle.Fill,
            Font = new Font("Trebuchet MS", 13, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var levelLabel = new Label
        {
            Text = "Wybierz poziom:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        };

        _levelInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = maxLevel,
            Value = Math.Clamp(currentLevel, 1, maxLevel),
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };

        _brightnessLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        };

        _brightnessBar = new TrackBar
        {
            Minimum = 30,
            Maximum = 160,
            TickFrequency = 10,
            Value = Math.Clamp((int)(currentBrightness * 100f), 30, 160),
            Dock = DockStyle.Top
        };
        _brightnessBar.Scroll += (_, _) => _brightnessLabel.Text = $"Jasność: {_brightnessBar.Value}%";
        _brightnessLabel.Text = $"Jasność: {_brightnessBar.Value}%";

        var importPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        importPanel.Controls.Add(NewButton("Wgraj muzykę (.wav)", PickMusicFile));
        importPanel.Controls.Add(NewButton("Wgraj tło (.png/.jpg)", PickBackgroundFile));
        importPanel.Controls.Add(NewButton("Wgraj model elfa (.png)", PickElfFile));

        var resetPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        resetPanel.Controls.Add(NewButton("Cofnij muzykę", () =>
        {
            Result.ResetMusic = true;
            Result.MusicFileToImport = null;
            RefreshAssetInfo();
        }));

        resetPanel.Controls.Add(NewButton("Cofnij tło", () =>
        {
            Result.ResetBackground = true;
            Result.BackgroundFileToImport = null;
            RefreshAssetInfo();
        }));

        resetPanel.Controls.Add(NewButton("Cofnij elfa", () =>
        {
            Result.ResetElf = true;
            Result.ElfFileToImport = null;
            RefreshAssetInfo();
        }));

        resetPanel.Controls.Add(NewButton("Cofnij wszystko", () =>
        {
            Result.ResetMusic = true;
            Result.ResetBackground = true;
            Result.ResetElf = true;
            Result.MusicFileToImport = null;
            Result.BackgroundFileToImport = null;
            Result.ElfFileToImport = null;
            RefreshAssetInfo();
        }));

        _assetsLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = Color.LightSteelBlue,
            Text = "Assety: domyślne"
        };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        actions.Controls.Add(NewButton("Wznów", () => SubmitAndClose(() => Result.ResumeRequested = true)));
        actions.Controls.Add(NewButton("Restart poziomu", () => SubmitAndClose(() => Result.RestartRequested = true)));
        actions.Controls.Add(NewButton("Przejdź do poziomu", () => SubmitAndClose(() => Result.ResumeRequested = true)));
        actions.Controls.Add(NewButton("Wyjście z gry", () => SubmitAndClose(() => Result.ExitRequested = true)));

        panel.Controls.Add(title, 0, 0);
        panel.Controls.Add(levelLabel, 0, 1);
        panel.Controls.Add(_levelInput, 0, 2);
        panel.Controls.Add(_brightnessLabel, 0, 3);
        panel.Controls.Add(_brightnessBar, 0, 4);
        panel.Controls.Add(importPanel, 0, 5);
        panel.Controls.Add(resetPanel, 0, 6);
        panel.Controls.Add(_assetsLabel, 0, 7);
        panel.Controls.Add(actions, 0, 8);

        Controls.Add(panel);
    }

    private void SubmitAndClose(Action setAction)
    {
        setAction();
        SaveCommon();
        DialogResult = DialogResult.OK;
        Close();
    }

    private Button NewButton(string text, Action onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            BackColor = Color.FromArgb(65, 78, 110),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Popup,
            Margin = new Padding(4)
        };
        button.Click += (_, _) => onClick();
        return button;
    }

    private void PickMusicFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Wybierz plik muzyczny",
            Filter = "Pliki audio WAV (*.wav)|*.wav",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Result.MusicFileToImport = dialog.FileName;
            Result.ResetMusic = false;
            RefreshAssetInfo();
        }
    }

    private void PickBackgroundFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Wybierz obraz tła",
            Filter = "Obrazy (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Result.BackgroundFileToImport = dialog.FileName;
            Result.ResetBackground = false;
            RefreshAssetInfo();
        }
    }

    private void PickElfFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Wybierz render/model elfa",
            Filter = "Obrazy PNG (*.png)|*.png",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Result.ElfFileToImport = dialog.FileName;
            Result.ResetElf = false;
            RefreshAssetInfo();
        }
    }

    private void RefreshAssetInfo()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Result.MusicFileToImport)) parts.Add("nowa muzyka ✓");
        if (!string.IsNullOrWhiteSpace(Result.BackgroundFileToImport)) parts.Add("nowe tło ✓");
        if (!string.IsNullOrWhiteSpace(Result.ElfFileToImport)) parts.Add("nowy elf ✓");
        if (Result.ResetMusic) parts.Add("cofnięcie muzyki");
        if (Result.ResetBackground) parts.Add("cofnięcie tła");
        if (Result.ResetElf) parts.Add("cofnięcie elfa");
        _assetsLabel.Text = parts.Count == 0 ? "Assety: domyślne" : $"Zmiany: {string.Join(", ", parts)}";
    }

    private void SaveCommon()
    {
        Result.SelectedLevel = (int)_levelInput.Value;
        Result.Brightness = _brightnessBar.Value / 100f;
    }
}
