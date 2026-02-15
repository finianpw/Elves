namespace ElfForestSaga.Game;

public sealed class PauseMenuResult
{
    public bool ResumeRequested;
    public bool RestartRequested;
    public bool ExitRequested;
    public int SelectedLevel;
    public float Brightness;
}

public sealed class PauseMenuForm : Form
{
    private readonly NumericUpDown _levelInput;
    private readonly TrackBar _brightnessBar;
    private readonly Label _brightnessLabel;

    public PauseMenuResult Result { get; } = new();

    public PauseMenuForm(int currentLevel, int maxLevel, float currentBrightness)
    {
        Text = "Menu Pauzy";
        Width = 420;
        Height = 390;
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
            RowCount = 8,
            Padding = new Padding(18),
            BackColor = BackColor
        };

        panel.RowStyles.Clear();
        for (var i = 0; i < 8; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5f));
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
            Value = currentLevel,
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };

        _brightnessLabel = new Label
        {
            Text = "Jasność: 100%",
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

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        var resumeButton = NewButton("Wznów", () =>
        {
            Result.ResumeRequested = true;
            SaveCommon();
            DialogResult = DialogResult.OK;
            Close();
        });

        var restartButton = NewButton("Restart poziomu", () =>
        {
            Result.RestartRequested = true;
            SaveCommon();
            DialogResult = DialogResult.OK;
            Close();
        });

        var applyLevelButton = NewButton("Przejdź do poziomu", () =>
        {
            Result.ResumeRequested = true;
            SaveCommon();
            DialogResult = DialogResult.OK;
            Close();
        });

        var exitButton = NewButton("Wyjście z gry", () =>
        {
            Result.ExitRequested = true;
            SaveCommon();
            DialogResult = DialogResult.OK;
            Close();
        });

        actions.Controls.Add(resumeButton);
        actions.Controls.Add(restartButton);
        actions.Controls.Add(applyLevelButton);
        actions.Controls.Add(exitButton);

        panel.Controls.Add(title, 0, 0);
        panel.Controls.Add(levelLabel, 0, 1);
        panel.Controls.Add(_levelInput, 0, 2);
        panel.Controls.Add(_brightnessLabel, 0, 3);
        panel.Controls.Add(_brightnessBar, 0, 4);
        panel.Controls.Add(actions, 0, 5);

        Controls.Add(panel);
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

    private void SaveCommon()
    {
        Result.SelectedLevel = (int)_levelInput.Value;
        Result.Brightness = _brightnessBar.Value / 100f;
    }
}
