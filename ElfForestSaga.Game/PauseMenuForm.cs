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
    public string? LevelBackgroundFileToImport;
    public string? ElfFileToImport;

    public string? OrcAppearanceFileToImport;
    public string? GoblinAppearanceFileToImport;
    public string? ZombieAppearanceFileToImport;
    public string? WizardAppearanceFileToImport;
    public string? DragonAppearanceFileToImport;

    public bool ResetMusic;
    public bool ResetBackground;
    public bool ResetSelectedLevelBackground;
    public bool ResetElf;
    public bool ResetOrcAppearance;
    public bool ResetGoblinAppearance;
    public bool ResetZombieAppearance;
    public bool ResetWizardAppearance;
    public bool ResetDragonAppearance;
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
        Width = 860;
        Height = 700;
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
            RowCount = 16,
            Padding = new Padding(14),
            BackColor = BackColor
        };

        for (var i = 0; i < 16; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 16f));
        }

        var title = new Label
        {
            Text = "ELF FOREST SAGA - MENU",
            Dock = DockStyle.Fill,
            Font = new Font("Trebuchet MS", 13, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var levelLabel = new Label { Text = "Wybierz poziom:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
        _levelInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = maxLevel,
            Value = Math.Clamp(currentLevel, 1, maxLevel),
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };

        _brightnessLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
        _brightnessBar = new TrackBar { Minimum = 30, Maximum = 160, TickFrequency = 10, Value = Math.Clamp((int)(currentBrightness * 100f), 30, 160), Dock = DockStyle.Top };
        _brightnessBar.Scroll += (_, _) => _brightnessLabel.Text = $"Jasność: {_brightnessBar.Value}%";
        _brightnessLabel.Text = $"Jasność: {_brightnessBar.Value}%";

        var globalAssetsPanel = NewFlow();
        globalAssetsPanel.Controls.Add(NewButton("Wgraj muzykę (.wav)", PickMusicFile));
        globalAssetsPanel.Controls.Add(NewButton("Wgraj tło globalne (.png/.jpg)", PickGlobalBackgroundFile));
        globalAssetsPanel.Controls.Add(NewButton("Wgraj tło dla wybranego poziomu", PickLevelBackgroundFile));
        globalAssetsPanel.Controls.Add(NewButton("Wgraj model/render elfa", PickElfFile));

        var enemyPanel = NewFlow();
        enemyPanel.Controls.Add(NewButton("Ork: model/render", () => PickEnemyFile(e => Result.OrcAppearanceFileToImport = e, () => Result.ResetOrcAppearance = false, "Ork")));
        enemyPanel.Controls.Add(NewButton("Goblin: model/render", () => PickEnemyFile(e => Result.GoblinAppearanceFileToImport = e, () => Result.ResetGoblinAppearance = false, "Goblin")));
        enemyPanel.Controls.Add(NewButton("Zombie: model/render", () => PickEnemyFile(e => Result.ZombieAppearanceFileToImport = e, () => Result.ResetZombieAppearance = false, "Zombie")));
        enemyPanel.Controls.Add(NewButton("Czarodziej: model/render", () => PickEnemyFile(e => Result.WizardAppearanceFileToImport = e, () => Result.ResetWizardAppearance = false, "Czarodziej")));
        enemyPanel.Controls.Add(NewButton("Smok: model/render", () => PickEnemyFile(e => Result.DragonAppearanceFileToImport = e, () => Result.ResetDragonAppearance = false, "Smok")));

        var resetPanel = NewFlow();
        resetPanel.Controls.Add(NewButton("Cofnij muzykę", () => { Result.ResetMusic = true; Result.MusicFileToImport = null; RefreshAssetInfo(); }));
        resetPanel.Controls.Add(NewButton("Cofnij tło globalne", () => { Result.ResetBackground = true; Result.BackgroundFileToImport = null; RefreshAssetInfo(); }));
        resetPanel.Controls.Add(NewButton("Cofnij tło wybranego poziomu", () => { Result.ResetSelectedLevelBackground = true; Result.LevelBackgroundFileToImport = null; RefreshAssetInfo(); }));
        resetPanel.Controls.Add(NewButton("Cofnij elfa", () => { Result.ResetElf = true; Result.ElfFileToImport = null; RefreshAssetInfo(); }));

        var enemyResetPanel = NewFlow();
        enemyResetPanel.Controls.Add(NewButton("Reset Ork", () => { Result.ResetOrcAppearance = true; Result.OrcAppearanceFileToImport = null; RefreshAssetInfo(); }));
        enemyResetPanel.Controls.Add(NewButton("Reset Goblin", () => { Result.ResetGoblinAppearance = true; Result.GoblinAppearanceFileToImport = null; RefreshAssetInfo(); }));
        enemyResetPanel.Controls.Add(NewButton("Reset Zombie", () => { Result.ResetZombieAppearance = true; Result.ZombieAppearanceFileToImport = null; RefreshAssetInfo(); }));
        enemyResetPanel.Controls.Add(NewButton("Reset Czarodziej", () => { Result.ResetWizardAppearance = true; Result.WizardAppearanceFileToImport = null; RefreshAssetInfo(); }));
        enemyResetPanel.Controls.Add(NewButton("Reset Smok", () => { Result.ResetDragonAppearance = true; Result.DragonAppearanceFileToImport = null; RefreshAssetInfo(); }));
        enemyResetPanel.Controls.Add(NewButton("Cofnij wszystko", ResetAll));

        _assetsLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft, ForeColor = Color.LightSteelBlue, Text = "Assety: domyślne" };

        var actions = NewFlow();
        actions.Controls.Add(NewButton("Wznów", () => SubmitAndClose(() => Result.ResumeRequested = true)));
        actions.Controls.Add(NewButton("Restart poziomu", () => SubmitAndClose(() => Result.RestartRequested = true)));
        actions.Controls.Add(NewButton("Przejdź do poziomu", () => SubmitAndClose(() => Result.ResumeRequested = true)));
        actions.Controls.Add(NewButton("Wyjście z gry", () => SubmitAndClose(() => Result.ExitRequested = true)));

        panel.Controls.Add(title, 0, 0);
        panel.Controls.Add(levelLabel, 0, 1);
        panel.Controls.Add(_levelInput, 0, 2);
        panel.Controls.Add(_brightnessLabel, 0, 3);
        panel.Controls.Add(_brightnessBar, 0, 4);
        panel.Controls.Add(globalAssetsPanel, 0, 5);
        panel.Controls.Add(enemyPanel, 0, 6);
        panel.Controls.Add(resetPanel, 0, 7);
        panel.Controls.Add(enemyResetPanel, 0, 8);
        panel.Controls.Add(_assetsLabel, 0, 9);
        panel.Controls.Add(actions, 0, 10);

        Controls.Add(panel);
    }

    private static FlowLayoutPanel NewFlow() => new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };

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

    private static string CharacterFilter => "Modele/render (*.png;*.jpg;*.jpeg;*.obj;*.fbx;*.gltf;*.glb;*.blend)|*.png;*.jpg;*.jpeg;*.obj;*.fbx;*.gltf;*.glb;*.blend";

    private void PickMusicFile()
    {
        using var dialog = new OpenFileDialog { Title = "Wybierz plik muzyczny", Filter = "Pliki audio WAV (*.wav)|*.wav", CheckFileExists = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Result.MusicFileToImport = dialog.FileName;
            Result.ResetMusic = false;
            RefreshAssetInfo();
        }
    }

    private void PickGlobalBackgroundFile()
    {
        using var dialog = new OpenFileDialog { Title = "Wybierz globalne tło", Filter = "Obrazy (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg", CheckFileExists = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Result.BackgroundFileToImport = dialog.FileName;
            Result.ResetBackground = false;
            RefreshAssetInfo();
        }
    }

    private void PickLevelBackgroundFile()
    {
        using var dialog = new OpenFileDialog { Title = "Wybierz tło dla wybranego poziomu", Filter = "Obrazy (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg", CheckFileExists = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Result.LevelBackgroundFileToImport = dialog.FileName;
            Result.ResetSelectedLevelBackground = false;
            RefreshAssetInfo();
        }
    }

    private void PickElfFile()
    {
        using var dialog = new OpenFileDialog { Title = "Wybierz model/render elfa", Filter = CharacterFilter, CheckFileExists = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Result.ElfFileToImport = dialog.FileName;
            Result.ResetElf = false;
            RefreshAssetInfo();
        }
    }

    private void PickEnemyFile(Action<string> setFile, Action clearReset, string name)
    {
        using var dialog = new OpenFileDialog { Title = $"Wybierz model/render: {name}", Filter = CharacterFilter, CheckFileExists = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            setFile(dialog.FileName);
            clearReset();
            RefreshAssetInfo();
        }
    }

    private void ResetAll()
    {
        Result.ResetMusic = true;
        Result.ResetBackground = true;
        Result.ResetSelectedLevelBackground = true;
        Result.ResetElf = true;
        Result.ResetOrcAppearance = true;
        Result.ResetGoblinAppearance = true;
        Result.ResetZombieAppearance = true;
        Result.ResetWizardAppearance = true;
        Result.ResetDragonAppearance = true;

        Result.MusicFileToImport = null;
        Result.BackgroundFileToImport = null;
        Result.LevelBackgroundFileToImport = null;
        Result.ElfFileToImport = null;
        Result.OrcAppearanceFileToImport = null;
        Result.GoblinAppearanceFileToImport = null;
        Result.ZombieAppearanceFileToImport = null;
        Result.WizardAppearanceFileToImport = null;
        Result.DragonAppearanceFileToImport = null;

        RefreshAssetInfo();
    }

    private void RefreshAssetInfo()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Result.MusicFileToImport)) parts.Add("muzyka ✓");
        if (!string.IsNullOrWhiteSpace(Result.BackgroundFileToImport)) parts.Add("tło globalne ✓");
        if (!string.IsNullOrWhiteSpace(Result.LevelBackgroundFileToImport)) parts.Add($"tło poziomu {_levelInput.Value} ✓");
        if (!string.IsNullOrWhiteSpace(Result.ElfFileToImport)) parts.Add("elf ✓");
        if (!string.IsNullOrWhiteSpace(Result.OrcAppearanceFileToImport)) parts.Add("ork ✓");
        if (!string.IsNullOrWhiteSpace(Result.GoblinAppearanceFileToImport)) parts.Add("goblin ✓");
        if (!string.IsNullOrWhiteSpace(Result.ZombieAppearanceFileToImport)) parts.Add("zombie ✓");
        if (!string.IsNullOrWhiteSpace(Result.WizardAppearanceFileToImport)) parts.Add("czarodziej ✓");
        if (!string.IsNullOrWhiteSpace(Result.DragonAppearanceFileToImport)) parts.Add("smok ✓");

        if (Result.ResetMusic) parts.Add("reset muzyki");
        if (Result.ResetBackground) parts.Add("reset tła globalnego");
        if (Result.ResetSelectedLevelBackground) parts.Add($"reset tła poziomu {_levelInput.Value}");
        if (Result.ResetElf) parts.Add("reset elfa");
        if (Result.ResetOrcAppearance) parts.Add("reset orka");
        if (Result.ResetGoblinAppearance) parts.Add("reset goblina");
        if (Result.ResetZombieAppearance) parts.Add("reset zombie");
        if (Result.ResetWizardAppearance) parts.Add("reset czarodzieja");
        if (Result.ResetDragonAppearance) parts.Add("reset smoka");

        _assetsLabel.Text = parts.Count == 0 ? "Assety: domyślne" : $"Zmiany: {string.Join(", ", parts)}";
    }

    private void SaveCommon()
    {
        Result.SelectedLevel = (int)_levelInput.Value;
        Result.Brightness = _brightnessBar.Value / 100f;
    }
}
