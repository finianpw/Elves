namespace ElfForestSaga.Game;

public sealed class MainMenuForm : Form
{
    private readonly ComboBox _carPicker = new();

    public MainMenuForm()
    {
        Text = "Highway Pursuit - Menu";
        Width = 520;
        Height = 360;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(20, 25, 34);

        var title = new Label
        {
            Text = "HIGHWAY PURSUIT",
            ForeColor = Color.Gold,
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(90, 28)
        };

        var prompt = new Label
        {
            Text = "Wybierz rodzaj zielonego samochodu:",
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(95, 120)
        };

        _carPicker.Items.AddRange(["Sport", "Muscle", "Futuristic"]);
        _carPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _carPicker.SelectedIndex = 0;
        _carPicker.Location = new Point(95, 148);
        _carPicker.Width = 320;

        var start = new Button
        {
            Text = "Start",
            Width = 150,
            Height = 42,
            Location = new Point(95, 220)
        };
        start.Click += (_, _) =>
        {
            using var game = new RacingGameForm((PlayerCarStyle)_carPicker.SelectedIndex);
            Hide();
            game.ShowDialog(this);
            Show();
        };

        var exit = new Button
        {
            Text = "Wyjście z gry",
            Width = 150,
            Height = 42,
            Location = new Point(265, 220)
        };
        exit.Click += (_, _) => Close();

        Controls.Add(title);
        Controls.Add(prompt);
        Controls.Add(_carPicker);
        Controls.Add(start);
        Controls.Add(exit);
    }
}
