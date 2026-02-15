namespace ElfForestSaga.Game;

public sealed class EndingCinematicForm : Form
{
    private readonly Timer _timer = new() { Interval = 2100 };
    private int _frame;
    private readonly string[] _frames;

    public EndingCinematicForm(int enemiesDefeated, int damageTaken, bool dragonDefeated)
    {
        Text = "Elf Forest Saga - Film Końcowy";
        Width = 980;
        Height = 580;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.Black;
        ForeColor = Color.White;

        _frames =
        [
            "KSIĘGA ZAMKNIĘTA\n\nPo 21 poziomach Aerin dotarł do Serca Lasu.",
            dragonDefeated
                ? "Smok Strażnik upadł, a mrok nad Aelorią pękł jak szkło."
                : "Aerin ocalił las, choć cień smoka nadal krąży po ruinach...",
            $"PODSUMOWANIE WYPRAWY\n\nPokonani wrogowie: {enemiesDefeated}\nOtrzymane obrażenia: {damageTaken}",
            "Królowa Leśnych Elfów oddała Aerinowi Kronikę Aelorii.\nTa opowieść zostanie zapisana na wieki.",
            "Dziękujemy za grę w ELF FOREST SAGA!"
        ];

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
        using var title = new Font("Georgia", 28, FontStyle.Bold);
        using var body = new Font("Georgia", 20, FontStyle.Bold);
        using var small = new Font("Georgia", 13, FontStyle.Italic);

        using var bg = new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle, Color.FromArgb(10, 18, 32), Color.FromArgb(34, 53, 42), 90f);
        e.Graphics.FillRectangle(bg, ClientRectangle);

        e.Graphics.DrawString("FILM KOŃCOWY", title, Brushes.Gold, new RectangleF(40, 34, Width - 80, 50));
        e.Graphics.DrawString(_frames[frame], body, Brushes.WhiteSmoke, new RectangleF(56, 130, Width - 112, Height - 220));
        e.Graphics.DrawString("(Podsumowanie kampanii po ukończeniu wszystkich poziomów)", small, Brushes.LightGray, 56, Height - 96);
    }
}
