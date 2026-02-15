using System.Drawing.Drawing2D;

namespace ElfForestSaga.Game;

public sealed class GameForm : Form
{
    private readonly Timer _timer = new() { Interval = 16 };
    private readonly HashSet<Keys> _keys = new();
    private readonly Player _player = new() { Bounds = new RectangleF(60, 560, 44, 66) };
    private readonly List<Projectile> _projectiles = new();
    private readonly List<Level> _levels = LevelFactory.BuildCampaign();
    private readonly SoundtrackPlayer _soundtrack = new();
    private readonly Font _hudFont = new("Segoe UI", 10, FontStyle.Bold);
    private readonly Font _storyFont = new("Georgia", 10, FontStyle.Italic);

    private int _currentLevelIndex;
    private float _cameraX;
    private bool _onGround;
    private int _shotCooldown;
    private int _invulnerabilityTicks;
    private bool _facingRight = true;
    private float _brightness = 1.0f;
    private float _ambiencePhase;

    private readonly string[] _storyParagraphs =
    [
        "Aerin, leśny elf o długich blond włosach, rusza przez Aelorię.",
        "W mroku czają się orki, gobliny, zombie i starzy czarodzieje.",
        "Do wyboru ma łuk lub ogień z dłoni, by ocalić Serce Lasu."
    ];

    public GameForm()
    {
        DoubleBuffered = true;
        KeyPreview = true;
        Text = "Elf Forest Saga - Edycja Fantasy 2000";
        Width = 1280;
        Height = 768;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;

        _timer.Tick += (_, _) => TickGame();
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        FormClosed += (_, _) => _soundtrack.Stop();

        _soundtrack.Start();
        _timer.Start();
    }

    private Level CurrentLevel => _levels[_currentLevelIndex];

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            ShowPauseMenu();
            return;
        }

        _keys.Add(e.KeyCode);

        if (e.KeyCode == Keys.D1)
        {
            _player.SelectedWeapon = WeaponType.Bow;
        }

        if (e.KeyCode == Keys.D2)
        {
            _player.SelectedWeapon = WeaponType.Fire;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e) => _keys.Remove(e.KeyCode);

    private void ShowPauseMenu()
    {
        _timer.Stop();
        using var menu = new PauseMenuForm(_currentLevelIndex + 1, _levels.Count, _brightness);
        _ = menu.ShowDialog(this);
        var result = menu.Result;

        _brightness = Math.Clamp(result.Brightness <= 0 ? _brightness : result.Brightness, 0.3f, 1.6f);

        if (result.ExitRequested)
        {
            Close();
            return;
        }

        if (result.SelectedLevel != _currentLevelIndex + 1)
        {
            LoadLevel(result.SelectedLevel - 1, true);
        }

        if (result.RestartRequested)
        {
            RestartLevel();
        }

        _keys.Clear();
        _timer.Start();
    }

    private void TickGame()
    {
        var level = CurrentLevel;
        var move = 0f;

        if (_keys.Contains(Keys.A) || _keys.Contains(Keys.Left))
        {
            move -= 4.5f;
            _facingRight = false;
        }

        if (_keys.Contains(Keys.D) || _keys.Contains(Keys.Right))
        {
            move += 4.5f;
            _facingRight = true;
        }

        _player.VelocityX = move;

        if ((_keys.Contains(Keys.Space) || _keys.Contains(Keys.W) || _keys.Contains(Keys.Up)) && _onGround)
        {
            _player.VelocityY = -_player.JumpPower;
            _onGround = false;
        }

        _player.VelocityY += 0.9f;
        _player.Bounds = new RectangleF(_player.Bounds.X + _player.VelocityX, _player.Bounds.Y + _player.VelocityY, _player.Bounds.Width, _player.Bounds.Height);

        ResolvePlatforms(level);
        UpdateCombat(level);
        UpdateEnemies(level);
        CollectPickups(level);

        if (_player.Health <= 0)
        {
            RestartLevel();
        }

        if (!level.Enemies.Any() && _player.Bounds.Right >= level.Width - 120)
        {
            NextLevel();
            return;
        }

        _cameraX = Math.Clamp(_player.Bounds.X - (ClientSize.Width / 2f), 0, Math.Max(0, level.Width - ClientSize.Width));
        _shotCooldown = Math.Max(0, _shotCooldown - 1);
        _invulnerabilityTicks = Math.Max(0, _invulnerabilityTicks - 1);
        _ambiencePhase += 0.02f;
        Invalidate();
    }

    private void ResolvePlatforms(Level level)
    {
        _onGround = false;
        foreach (var platform in level.Platforms)
        {
            if (!_player.Bounds.IntersectsWith(platform.Bounds))
            {
                continue;
            }

            if (_player.VelocityY >= 0 && _player.Bounds.Bottom <= platform.Bounds.Bottom)
            {
                _player.Bounds = new RectangleF(_player.Bounds.X, platform.Bounds.Y - _player.Bounds.Height, _player.Bounds.Width, _player.Bounds.Height);
                _player.VelocityY = 0;
                _onGround = true;
            }
        }

        if (_player.Bounds.Y > ClientSize.Height + 200)
        {
            _player.Health = 0;
        }
    }

    private void UpdateCombat(Level level)
    {
        if ((_keys.Contains(Keys.F) || _keys.Contains(Keys.ControlKey)) && _shotCooldown == 0)
        {
            var offset = _facingRight ? _player.Bounds.Right - 6 : _player.Bounds.Left - 10;
            var speedX = _facingRight ? 11f : -11f;
            var projectile = new Projectile
            {
                Weapon = _player.SelectedWeapon,
                Bounds = new RectangleF(offset, _player.Bounds.Y + 24, 16, 8),
                VelocityX = _player.SelectedWeapon == WeaponType.Bow ? speedX : speedX * 0.8f,
                VelocityY = _player.SelectedWeapon == WeaponType.Bow ? -0.12f : 0,
                Damage = _player.SelectedWeapon == WeaponType.Bow ? 12 : 17
            };

            _projectiles.Add(projectile);
            _shotCooldown = _player.FireCooldownTicks;
        }

        foreach (var projectile in _projectiles)
        {
            projectile.Bounds = new RectangleF(projectile.Bounds.X + projectile.VelocityX, projectile.Bounds.Y + projectile.VelocityY, projectile.Bounds.Width, projectile.Bounds.Height);
        }

        foreach (var enemy in level.Enemies.ToList())
        {
            foreach (var projectile in _projectiles.ToList())
            {
                if (!enemy.Bounds.IntersectsWith(projectile.Bounds))
                {
                    continue;
                }

                enemy.Health -= projectile.Damage;
                _projectiles.Remove(projectile);

                if (enemy.Health <= 0)
                {
                    level.Enemies.Remove(enemy);
                    break;
                }
            }
        }

        _projectiles.RemoveAll(p => p.Bounds.X > level.Width + 30 || p.Bounds.Right < -40);
    }

    private void UpdateEnemies(Level level)
    {
        foreach (var enemy in level.Enemies)
        {
            var dir = enemy.MovingRight ? 1 : -1;
            var newX = enemy.Bounds.X + (enemy.Speed * dir);

            if (newX < enemy.PatrolStart)
            {
                enemy.MovingRight = true;
                newX = enemy.PatrolStart;
            }
            else if (newX > enemy.PatrolEnd)
            {
                enemy.MovingRight = false;
                newX = enemy.PatrolEnd;
            }

            enemy.Bounds = new RectangleF(newX, enemy.Bounds.Y, enemy.Bounds.Width, enemy.Bounds.Height);

            if (enemy.Bounds.IntersectsWith(_player.Bounds) && _invulnerabilityTicks == 0)
            {
                _player.Health -= enemy.Damage;
                _invulnerabilityTicks = 24;
            }
        }
    }

    private void CollectPickups(Level level)
    {
        foreach (var pickup in level.Pickups.Where(p => !p.Collected))
        {
            if (!pickup.Bounds.IntersectsWith(_player.Bounds))
            {
                continue;
            }

            pickup.Collected = true;
            if (pickup.Type == PickupType.Health)
            {
                _player.Health = Math.Min(100, _player.Health + 30);
            }
            else
            {
                _player.FireCooldownTicks = Math.Max(4, _player.FireCooldownTicks - 2);
            }
        }
    }

    private void RestartLevel()
    {
        LoadLevel(_currentLevelIndex, true);
    }

    private void LoadLevel(int levelIndex, bool resetStats)
    {
        _currentLevelIndex = Math.Clamp(levelIndex, 0, _levels.Count - 1);
        _levels[_currentLevelIndex] = LevelFactory.BuildCampaign()[_currentLevelIndex];
        _player.Bounds = new RectangleF(60, 560, 44, 66);
        _player.VelocityX = 0;
        _player.VelocityY = 0;
        if (resetStats)
        {
            _player.Health = 100;
            _player.FireCooldownTicks = 12;
        }

        _projectiles.Clear();
    }

    private void NextLevel()
    {
        _currentLevelIndex++;
        if (_currentLevelIndex >= _levels.Count)
        {
            _timer.Stop();
            _soundtrack.Stop();
            using var ending = new EndingCinematicForm();
            ending.ShowDialog(this);
            Close();
            return;
        }

        LoadLevel(_currentLevelIndex, false);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        DrawBackground(g);

        var transform = g.Transform;
        g.TranslateTransform(-_cameraX, 0);

        DrawPlatforms(g);
        DrawDecor(g);
        DrawPickups(g);
        DrawEnemies(g);
        DrawProjectiles(g);
        DrawElf(g);

        g.Transform = transform;

        DrawHud(g);
        DrawBrightnessOverlay(g);
    }

    private void DrawBackground(Graphics g)
    {
        using var sky = new LinearGradientBrush(ClientRectangle, Color.FromArgb(18, 40, 72), Color.FromArgb(24, 78, 48), 90f);
        g.FillRectangle(sky, ClientRectangle);

        var moonY = 90 + (float)Math.Sin(_ambiencePhase) * 6;
        g.FillEllipse(Brushes.LightGoldenrodYellow, ClientSize.Width - 190, moonY, 90, 90);

        for (var i = 0; i < 11; i++)
        {
            var x = (i * 260) - (_cameraX * 0.18f % 260);
            g.FillRectangle(new SolidBrush(Color.FromArgb(28, 33, 37)), x, 260, 42, 400);
            g.FillEllipse(new SolidBrush(Color.FromArgb(36, 90, 60)), x - 82, 165, 200, 125);
        }
    }

    private void DrawPlatforms(Graphics g)
    {
        foreach (var platform in CurrentLevel.Platforms)
        {
            using var stone = new SolidBrush(Color.FromArgb(64, 58, 52));
            g.FillRectangle(stone, platform.Bounds);
            g.DrawRectangle(Pens.DimGray, platform.Bounds.X, platform.Bounds.Y, platform.Bounds.Width, platform.Bounds.Height);

            for (var x = platform.Bounds.X + 24; x < platform.Bounds.Right; x += 46)
            {
                g.DrawLine(Pens.Gray, x, platform.Bounds.Y, x, platform.Bounds.Bottom);
            }

            g.FillRectangle(new SolidBrush(Color.FromArgb(34, 84, 42)), platform.Bounds.X, platform.Bounds.Y - 7, platform.Bounds.Width, 7);
        }
    }

    private void DrawDecor(Graphics g)
    {
        for (var i = 0; i < 22; i++)
        {
            var x = i * 170 + 120;
            var y = 610 + (i % 3) * 4;
            g.FillEllipse(new SolidBrush(Color.FromArgb(38, 120, 74)), x, y, 16, 12);
            g.FillEllipse(new SolidBrush(Color.FromArgb(75, 45, 15)), x + 6, y + 5, 4, 11);
        }
    }

    private void DrawPickups(Graphics g)
    {
        foreach (var pickup in CurrentLevel.Pickups.Where(p => !p.Collected))
        {
            var pulse = 2f + (float)Math.Sin(_ambiencePhase * 2.2f) * 1.2f;
            var bounds = new RectangleF(pickup.Bounds.X - pulse / 2, pickup.Bounds.Y - pulse / 2, pickup.Bounds.Width + pulse, pickup.Bounds.Height + pulse);
            g.FillEllipse(pickup.Type == PickupType.Health ? Brushes.LightGreen : Brushes.Gold, bounds);
            g.DrawEllipse(Pens.WhiteSmoke, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
    }

    private void DrawEnemies(Graphics g)
    {
        foreach (var enemy in CurrentLevel.Enemies)
        {
            var brush = enemy.Type switch
            {
                EnemyType.Orc => new SolidBrush(Color.OliveDrab),
                EnemyType.Goblin => new SolidBrush(Color.SeaGreen),
                EnemyType.Zombie => new SolidBrush(Color.SlateGray),
                _ => new SolidBrush(Color.MediumPurple)
            };

            g.FillRoundedRectangle(brush, enemy.Bounds, 7);
            g.DrawEllipse(Pens.Black, enemy.Bounds.X + 9, enemy.Bounds.Y + 12, 6, 6);
            g.DrawEllipse(Pens.Black, enemy.Bounds.X + 27, enemy.Bounds.Y + 12, 6, 6);
            g.DrawLine(Pens.Black, enemy.Bounds.X + 10, enemy.Bounds.Bottom - 10, enemy.Bounds.Right - 10, enemy.Bounds.Bottom - 10);
            brush.Dispose();
        }
    }

    private void DrawProjectiles(Graphics g)
    {
        foreach (var projectile in _projectiles)
        {
            if (projectile.Weapon == WeaponType.Bow)
            {
                g.FillEllipse(Brushes.BurlyWood, projectile.Bounds);
                g.DrawLine(Pens.SaddleBrown, projectile.Bounds.X, projectile.Bounds.Y + projectile.Bounds.Height / 2, projectile.Bounds.Right + 10, projectile.Bounds.Y + projectile.Bounds.Height / 2);
            }
            else
            {
                using var fire = new LinearGradientBrush(Rectangle.Round(projectile.Bounds), Color.Orange, Color.OrangeRed, 0f);
                g.FillEllipse(fire, projectile.Bounds);
            }
        }
    }

    private void DrawElf(Graphics g)
    {
        var p = _player.Bounds;
        var dx = _facingRight ? 1f : -1f;
        var xShift = _facingRight ? 0f : p.Width;

        using var cape = new SolidBrush(Color.FromArgb(25, 72, 48));
        g.FillEllipse(cape, p.X + 6, p.Y + 16, 28, 48);

        using var tunic = new LinearGradientBrush(Rectangle.Round(new RectangleF(p.X + 7, p.Y + 18, 30, 42)), Color.FromArgb(105, 138, 98), Color.FromArgb(61, 96, 68), 90f);
        g.FillRoundedRectangle(tunic, new RectangleF(p.X + 7, p.Y + 18, 30, 42), 8);

        var face = new RectangleF(p.X + 11, p.Y - 2, 22, 23);
        using var faceBrush = new SolidBrush(Color.FromArgb(244, 214, 188));
        g.FillEllipse(faceBrush, face);

        using var hair = new SolidBrush(Color.FromArgb(232, 199, 112));
        g.FillEllipse(hair, p.X + 7, p.Y - 8, 30, 16);
        g.FillRectangle(hair, p.X + 8, p.Y + 4, 5, 16);
        g.FillRectangle(hair, p.X + 31, p.Y + 4, 5, 16);

        var eyeY = p.Y + 8;
        g.FillEllipse(Brushes.White, p.X + 15 + (dx < 0 ? 3 : 0), eyeY, 4, 3);
        g.FillEllipse(Brushes.White, p.X + 23 + (dx < 0 ? 3 : 0), eyeY, 4, 3);
        g.FillEllipse(Brushes.Black, p.X + 16 + (dx < 0 ? 3 : 0), eyeY + 1, 2, 2);
        g.FillEllipse(Brushes.Black, p.X + 24 + (dx < 0 ? 3 : 0), eyeY + 1, 2, 2);
        g.DrawArc(Pens.SaddleBrown, p.X + 18 + (dx < 0 ? 1 : 0), p.Y + 13, 8, 5, 10, 160);

        using var pants = new SolidBrush(Color.FromArgb(72, 52, 42));
        g.FillRectangle(pants, p.X + 10, p.Bottom - 18, 10, 18);
        g.FillRectangle(pants, p.X + 24, p.Bottom - 18, 10, 18);
        g.FillRectangle(Brushes.Sienna, p.X + 9, p.Bottom - 4, 12, 4);
        g.FillRectangle(Brushes.Sienna, p.X + 23, p.Bottom - 4, 12, 4);

        using var arm = new SolidBrush(Color.FromArgb(244, 214, 188));
        g.FillRectangle(arm, p.X + (_facingRight ? 31 : 3), p.Y + 24, 8, 16);

        if (_player.SelectedWeapon == WeaponType.Bow)
        {
            var bowX = _facingRight ? p.Right + 2 : p.Left - 8;
            g.DrawArc(new Pen(Color.SaddleBrown, 2), bowX, p.Y + 20, 12, 20, -90, 180);
        }
        else
        {
            var fireX = _facingRight ? p.Right + 2 : p.Left - 12;
            using var fire = new LinearGradientBrush(new RectangleF(fireX, p.Y + 24, 12, 16), Color.Yellow, Color.Red, 90f);
            g.FillEllipse(fire, fireX, p.Y + 24, 12, 16);
        }
    }

    private void DrawHud(Graphics g)
    {
        using var hudBg = new SolidBrush(Color.FromArgb(152, 0, 0, 0));
        g.FillRectangle(hudBg, 0, 0, ClientSize.Width, 110);
        g.DrawString($"Poziom: {CurrentLevel.Number}/20", _hudFont, Brushes.White, 18, 13);
        g.DrawString($"Zdrowie: {_player.Health}", _hudFont, Brushes.White, 18, 35);
        g.DrawString($"Broń: {(_player.SelectedWeapon == WeaponType.Bow ? "Łuk" : "Ogień")}", _hudFont, Brushes.White, 18, 57);
        g.DrawString($"Jasność: {(int)(_brightness * 100)}%", _hudFont, Brushes.White, 18, 79);
        g.DrawString("ESC - menu (poziom, jasność, restart, wyjście)", _hudFont, Brushes.White, 290, 79);

        for (var i = 0; i < _storyParagraphs.Length; i++)
        {
            g.DrawString(_storyParagraphs[i], _storyFont, Brushes.LightCyan, 290, 12 + (i * 20));
        }
    }

    private void DrawBrightnessOverlay(Graphics g)
    {
        if (_brightness > 1f)
        {
            var alpha = Math.Min(120, (int)((_brightness - 1f) * 170));
            using var light = new SolidBrush(Color.FromArgb(alpha, 255, 248, 220));
            g.FillRectangle(light, ClientRectangle);
        }
        else if (_brightness < 1f)
        {
            var alpha = Math.Min(180, (int)((1f - _brightness) * 220));
            using var dark = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0));
            g.FillRectangle(dark, ClientRectangle);
        }
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, RectangleF bounds, int radius)
    {
        using var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
        path.AddArc(bounds.Right - radius, bounds.Y, radius, radius, 270, 90);
        path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
