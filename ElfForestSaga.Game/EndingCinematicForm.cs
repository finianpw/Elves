namespace ElfForestSaga.Game;

public sealed class EndingCinematicForm : Form
{
    private readonly Timer _timer = new() { Interval = 1600 };
    private int _frame;
    private readonly string[] _frames =
    [
        "KONIEC OPOWIEŚCI\n\nAerin dotarł do Serca Lasu i rozproszył mrok.",
        "Wrogie hordy rozpłynęły się w porannej mgle,\na stare drzewa znów zaczęły śpiewać.",
        "Królowa Leśnych Elfów oddała mu Kronikę Aelorii,\naby zapisał tę historię jak w książce.",
        "Dziękujemy za grę w Elf Forest Saga!"
    ];

    public EndingCinematicForm()
    {
        Text = "Elf Forest Saga - Film Końcowy";
        Width = 900;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.Black;
        ForeColor = Color.White;
        _timer.Tick += (_, _) =>
        {
            _frame++;
            if (_frame >= _frames.Length)
            {
                _timer.Stop();
                Close();
            }

            Invalidate();
        };
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var frame = Math.Min(_frame, _frames.Length - 1);
        using var font = new Font("Georgia", 24, FontStyle.Bold);
        using var small = new Font("Georgia", 14, FontStyle.Italic);

        e.Graphics.DrawString(_frames[frame], font, Brushes.Gold, new RectangleF(40, 120, Width - 80, Height - 160));
        e.Graphics.DrawString("(Animowany film końcowy uruchamiany po ukończeniu 20 poziomów)", small, Brushes.LightGray, 40, Height - 100);
    }
}
