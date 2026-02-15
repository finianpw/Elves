using System.Drawing.Drawing2D;

namespace ElfForestSaga.Game;

public sealed class GameForm : Form
{
    private readonly Timer _timer = new() { Interval = 16 };
    private readonly HashSet<Keys> _keys = new();
    private readonly Player _player = new() { Bounds = new RectangleF(60, 560, 42, 60) };
    private readonly List<Projectile> _projectiles = new();
    private readonly List<Level> _levels = LevelFactory.BuildCampaign();
    private readonly SoundtrackPlayer _soundtrack = new();

    private int _currentLevelIndex;
    private float _cameraX;
    private bool _onGround;
    private int _shotCooldown;
    private int _invulnerabilityTicks;
    private readonly Font _hudFont = new("Segoe UI", 11, FontStyle.Bold);
    private readonly string[] _storyParagraphs =
    [
        "W pradawnym lesie Aeloria żyje elf Aerin o długich blond włosach.",
        "Królestwo drzew zaatakowały hordy orków, goblinów, zombie i starych czarodziei.",
        "Aerin otrzymuje dwa dary: legendarny łuk i ogień płonący w dłoniach.",
        "Przemierz 20 poziomów, uratuj serce lasu i zapisz historię jak w wielkiej powieści fantasy."
    ];

    public GameForm()
    {
        DoubleBuffered = true;
        Text = "Elf Forest Saga";
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

    private void TickGame()
    {
        var level = CurrentLevel;
        var move = 0f;

        if (_keys.Contains(Keys.A) || _keys.Contains(Keys.Left))
        {
            move -= 4.2f;
        }

        if (_keys.Contains(Keys.D) || _keys.Contains(Keys.Right))
        {
            move += 4.2f;
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
            var projectile = new Projectile
            {
                Weapon = _player.SelectedWeapon,
                Bounds = new RectangleF(_player.Bounds.Right - 4, _player.Bounds.Y + 20, 14, 8),
                VelocityX = _player.SelectedWeapon == WeaponType.Bow ? 11f : 8f,
                VelocityY = _player.SelectedWeapon == WeaponType.Bow ? -0.15f : 0,
                Damage = _player.SelectedWeapon == WeaponType.Bow ? 11 : 16
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

        _projectiles.RemoveAll(p => p.Bounds.X > level.Width + 30);
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
                _invulnerabilityTicks = 25;
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
        var number = CurrentLevel.Number;
        _levels[_currentLevelIndex] = LevelFactory.BuildCampaign()[number - 1];
        _player.Bounds = new RectangleF(60, 560, 42, 60);
        _player.Health = 100;
        _player.FireCooldownTicks = 12;
        _player.VelocityX = 0;
        _player.VelocityY = 0;
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

        _player.Bounds = new RectangleF(60, 560, 42, 60);
        _player.VelocityY = 0;
        _projectiles.Clear();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        DrawBackground(g);

        var transform = g.Transform;
        g.TranslateTransform(-_cameraX, 0);

        foreach (var platform in CurrentLevel.Platforms)
        {
            g.FillRectangle(Brushes.SaddleBrown, platform.Bounds);
            g.FillRectangle(Brushes.DarkGreen, platform.Bounds.X, platform.Bounds.Y - 8, platform.Bounds.Width, 8);
        }

        foreach (var pickup in CurrentLevel.Pickups.Where(p => !p.Collected))
        {
            g.FillEllipse(pickup.Type == PickupType.Health ? Brushes.LightGreen : Brushes.Gold, pickup.Bounds);
        }

        foreach (var enemy in CurrentLevel.Enemies)
        {
            var brush = enemy.Type switch
            {
                EnemyType.Orc => Brushes.OliveDrab,
                EnemyType.Goblin => Brushes.ForestGreen,
                EnemyType.Zombie => Brushes.SlateGray,
                _ => Brushes.MediumPurple
            };
            g.FillRectangle(brush, enemy.Bounds);
        }

        foreach (var projectile in _projectiles)
        {
            g.FillEllipse(projectile.Weapon == WeaponType.Bow ? Brushes.BurlyWood : Brushes.OrangeRed, projectile.Bounds);
        }

        DrawElf(g);
        g.Transform = transform;

        DrawHud(g);
    }

    private void DrawBackground(Graphics g)
    {
        using var brush = new LinearGradientBrush(ClientRectangle, Color.DarkOliveGreen, Color.MidnightBlue, 90f);
        g.FillRectangle(brush, ClientRectangle);

        for (var i = 0; i < 14; i++)
        {
            var x = (i * 200) - (_cameraX * 0.2f % 200);
            g.FillRectangle(Brushes.DarkGreen, x, 220, 30, 420);
            g.FillEllipse(Brushes.ForestGreen, x - 70, 150, 160, 120);
        }
    }

    private void DrawElf(Graphics g)
    {
        var p = _player.Bounds;
        g.FillRectangle(Brushes.LightGoldenrodYellow, p);
        g.FillEllipse(Brushes.PeachPuff, p.X + 8, p.Y - 18, 24, 24);
        g.FillEllipse(Brushes.Gold, p.X + 3, p.Y - 20, 34, 12);
    }

    private void DrawHud(Graphics g)
    {
        g.FillRectangle(new SolidBrush(Color.FromArgb(140, 0, 0, 0)), 0, 0, ClientSize.Width, 120);
        g.DrawString($"Poziom: {CurrentLevel.Number}/20", _hudFont, Brushes.White, 20, 15);
        g.DrawString($"Zdrowie: {_player.Health}", _hudFont, Brushes.White, 20, 42);
        g.DrawString($"Broń: {(_player.SelectedWeapon == WeaponType.Bow ? "Łuk" : "Ogień")}", _hudFont, Brushes.White, 20, 69);
        g.DrawString("Sterowanie: A/D ruch, Space skok, F/CTRL strzał, 1-2 zmiana broni", _hudFont, Brushes.White, 320, 69);

        for (var i = 0; i < _storyParagraphs.Length; i++)
        {
            g.DrawString(_storyParagraphs[i], _hudFont, Brushes.LightCyan, 320, 12 + (i * 18));
        }
    }
}
