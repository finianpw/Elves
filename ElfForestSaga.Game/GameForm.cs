using System.Drawing.Drawing2D;

namespace ElfForestSaga.Game;

public sealed class GameForm : Form
{
    private readonly Timer _timer = new() { Interval = 16 };
    private readonly HashSet<Keys> _keys = new();
    private readonly Player _player = new() { Bounds = new RectangleF(60, 560, 44, 66) };
    private readonly List<Projectile> _projectiles = new();
    private readonly List<EnemyProjectile> _enemyProjectiles = new();
    private readonly List<Level> _levels = LevelFactory.BuildCampaign();
    private readonly SoundtrackPlayer _soundtrack = new();
    private readonly Font _hudFont = new("Segoe UI", 10, FontStyle.Bold);
    private readonly Font _storyFont = new("Georgia", 10, FontStyle.Italic);
    private Image? _backgroundArtwork;
    private Image? _elfArtwork;

    private int _currentLevelIndex;
    private float _cameraX;
    private bool _onGround;
    private int _shotCooldown;
    private int _invulnerabilityTicks;
    private bool _facingRight = true;
    private float _brightness = 1.0f;
    private float _ambiencePhase;

    private int _enemiesDefeatedTotal;
    private int _damageTakenTotal;
    private bool _dragonDefeated;

    private readonly string[] _storyParagraphs =
    [
        "Aerin walczy o Serce Lasu, a ostatnim przeciwnikiem jest smok strażnik.",
        "Wrogowie atakują aktywnie, a czarodzieje i smok strzelają pociskami.",
        "Naciśnij Z, aby przełączać łuk i ogień dłoni."
    ];

    public GameForm()
    {
        DoubleBuffered = true;
        KeyPreview = true;
        Text = "Elf Forest Saga - Fantasy Remaster";
        Width = 1360;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;

        _timer.Tick += (_, _) => TickGame();
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        FormClosed += (_, _) =>
        {
            _soundtrack.Stop();
            _backgroundArtwork?.Dispose();
            _elfArtwork?.Dispose();
        };

        ReloadVisualAssets();

        _soundtrack.Start();
        _timer.Start();
    }

    private Image? LoadArtwork(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "visuals", fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch
        {
            return null;
        }
    }

    private string VisualsDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "visuals");

    private void ReloadVisualAssets()
    {
        _backgroundArtwork?.Dispose();
        _elfArtwork?.Dispose();
        _backgroundArtwork = LoadArtwork("background-main.png");
        _elfArtwork = LoadArtwork("elf-3d.png");
    }

    private void ImportMenuAssets(PauseMenuResult result)
    {
        Directory.CreateDirectory(VisualsDirectory);
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Assets", "music"));

        if (!string.IsNullOrWhiteSpace(result.MusicFileToImport) && File.Exists(result.MusicFileToImport))
        {
            File.Copy(result.MusicFileToImport, _soundtrack.CustomTrackPath, true);
            _soundtrack.Restart();
        }

        if (!string.IsNullOrWhiteSpace(result.BackgroundFileToImport) && File.Exists(result.BackgroundFileToImport))
        {
            File.Copy(result.BackgroundFileToImport, Path.Combine(VisualsDirectory, "background-main.png"), true);
            ReloadVisualAssets();
        }

        if (!string.IsNullOrWhiteSpace(result.ElfFileToImport) && File.Exists(result.ElfFileToImport))
        {
            File.Copy(result.ElfFileToImport, Path.Combine(VisualsDirectory, "elf-3d.png"), true);
            ReloadVisualAssets();
        }
    }

    private Level CurrentLevel => _levels[_currentLevelIndex];

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            ShowPauseMenu();
            return;
        }

        if (e.KeyCode == Keys.Z)
        {
            _player.SelectedWeapon = _player.SelectedWeapon == WeaponType.Bow ? WeaponType.Fire : WeaponType.Bow;
            return;
        }

        _keys.Add(e.KeyCode);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e) => _keys.Remove(e.KeyCode);

    private void ShowPauseMenu()
    {
        _timer.Stop();
        using var menu = new PauseMenuForm(_currentLevelIndex + 1, _levels.Count, _brightness);
        _ = menu.ShowDialog(this);
        var result = menu.Result;

        ImportMenuAssets(result);
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
            move -= 4.6f;
            _facingRight = false;
        }

        if (_keys.Contains(Keys.D) || _keys.Contains(Keys.Right))
        {
            move += 4.6f;
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
            _projectiles.Add(new Projectile
            {
                Weapon = _player.SelectedWeapon,
                Bounds = new RectangleF(offset, _player.Bounds.Y + 24, 16, 8),
                VelocityX = _player.SelectedWeapon == WeaponType.Bow ? speedX : speedX * 0.8f,
                VelocityY = _player.SelectedWeapon == WeaponType.Bow ? -0.12f : 0,
                Damage = _player.SelectedWeapon == WeaponType.Bow ? 12 : 17
            });

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
                    _enemiesDefeatedTotal++;
                    if (enemy.Type == EnemyType.Dragon)
                    {
                        _dragonDefeated = true;
                    }

                    level.Enemies.Remove(enemy);
                    break;
                }
            }
        }

        _projectiles.RemoveAll(p => p.Bounds.X > level.Width + 40 || p.Bounds.Right < -40);
    }

    private void UpdateEnemies(Level level)
    {
        foreach (var enemy in level.Enemies)
        {
            var playerDistance = _player.Bounds.X - enemy.Bounds.X;
            var closeEnoughToChase = enemy.Type == EnemyType.Dragon ? Math.Abs(playerDistance) < 460 : Math.Abs(playerDistance) < 300;
            var directionToPlayer = playerDistance >= 0 ? 1 : -1;
            var dir = closeEnoughToChase ? directionToPlayer : (enemy.MovingRight ? 1 : -1);
            var speed = closeEnoughToChase ? enemy.Speed * (enemy.Type == EnemyType.Dragon ? 1.05f : 1.15f) : enemy.Speed;
            var newX = enemy.Bounds.X + (speed * dir);

            if (!closeEnoughToChase)
            {
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
            }

            enemy.Bounds = new RectangleF(newX, enemy.Bounds.Y, enemy.Bounds.Width, enemy.Bounds.Height);
            enemy.AnimationPhase += enemy.Type == EnemyType.Dragon ? 0.12f : 0.2f + (closeEnoughToChase ? 0.08f : 0f);

            enemy.AttackCooldown = Math.Max(0, enemy.AttackCooldown - 1);
            enemy.IsAttacking = false;

            if (enemy.Bounds.IntersectsWith(_player.Bounds) && _invulnerabilityTicks == 0)
            {
                var damage = enemy.Type == EnemyType.Dragon ? enemy.Damage + 4 : enemy.Damage;
                _player.Health -= damage;
                _damageTakenTotal += damage;
                _invulnerabilityTicks = 24;
                enemy.IsAttacking = true;
                enemy.AttackCooldown = enemy.Type == EnemyType.Dragon ? 32 : 24;
            }

            if ((enemy.Type == EnemyType.ElderWizard || enemy.Type == EnemyType.Dragon) && closeEnoughToChase && enemy.AttackCooldown == 0)
            {
                var vx = directionToPlayer * (enemy.Type == EnemyType.Dragon ? 6.4f : 5f);
                _enemyProjectiles.Add(new EnemyProjectile
                {
                    Bounds = enemy.Type == EnemyType.Dragon
                        ? new RectangleF(enemy.Bounds.X + (directionToPlayer > 0 ? enemy.Bounds.Width - 18 : 8), enemy.Bounds.Y + 28, 16, 14)
                        : new RectangleF(enemy.Bounds.X + 18, enemy.Bounds.Y + 16, 10, 10),
                    VelocityX = vx,
                    VelocityY = 0,
                    Damage = enemy.Type == EnemyType.Dragon ? enemy.Damage + 8 : enemy.Damage + 3
                });
                enemy.IsAttacking = true;
                enemy.AttackCooldown = enemy.Type == EnemyType.Dragon ? 42 : 60;
            }
        }

        foreach (var shot in _enemyProjectiles.ToList())
        {
            shot.Bounds = new RectangleF(shot.Bounds.X + shot.VelocityX, shot.Bounds.Y + shot.VelocityY, shot.Bounds.Width, shot.Bounds.Height);
            if (shot.Bounds.IntersectsWith(_player.Bounds) && _invulnerabilityTicks == 0)
            {
                _player.Health -= shot.Damage;
                _damageTakenTotal += shot.Damage;
                _invulnerabilityTicks = 24;
                _enemyProjectiles.Remove(shot);
                continue;
            }

            if (shot.Bounds.X < -30 || shot.Bounds.X > level.Width + 30)
            {
                _enemyProjectiles.Remove(shot);
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

    private void RestartLevel() => LoadLevel(_currentLevelIndex, true);

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
        _enemyProjectiles.Clear();
    }

    private void NextLevel()
    {
        _currentLevelIndex++;
        if (_currentLevelIndex >= _levels.Count)
        {
            _timer.Stop();
            _soundtrack.Stop();
            using var ending = new EndingCinematicForm(_enemiesDefeatedTotal, _damageTakenTotal, _dragonDefeated);
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
        if (_backgroundArtwork is not null)
        {
            DrawArtworkBackground(g);
            return;
        }

        using var sky = new LinearGradientBrush(ClientRectangle, Color.FromArgb(8, 28, 56), Color.FromArgb(26, 69, 52), 90f);
        g.FillRectangle(sky, ClientRectangle);

        var moonY = 86 + (float)Math.Sin(_ambiencePhase) * 4;
        using var moonBrush = new SolidBrush(Color.FromArgb(190, 236, 236, 210));
        g.FillEllipse(moonBrush, ClientSize.Width - 220, moonY, 112, 112);

        DrawMountainLayer(g, 0.10f, Color.FromArgb(34, 40, 53), 460);
        DrawMountainLayer(g, 0.18f, Color.FromArgb(26, 56, 52), 520);

        for (var i = 0; i < 13; i++)
        {
            var x = (i * 230) - (_cameraX * 0.28f % 230);
            using var trunk = new SolidBrush(Color.FromArgb(47, 34, 25));
            using var leaves = new SolidBrush(Color.FromArgb(30, 94, 58));
            g.FillRectangle(trunk, x, 250, 34, 430);
            g.FillEllipse(leaves, x - 82, 152, 196, 132);
        }
    }

    private void DrawArtworkBackground(Graphics g)
    {
        var src = new RectangleF(0, 0, _backgroundArtwork!.Width, _backgroundArtwork.Height);
        var scale = Math.Max(ClientSize.Width / src.Width, ClientSize.Height / src.Height);
        var drawWidth = src.Width * scale;
        var drawHeight = src.Height * scale;
        var parallaxX = (_cameraX * 0.08f) % Math.Max(1f, drawWidth - ClientSize.Width + 1);
        var dest = new RectangleF(-parallaxX, (ClientSize.Height - drawHeight) / 2f, drawWidth, drawHeight);

        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(_backgroundArtwork, dest, src, GraphicsUnit.Pixel);

        using var vignette = new LinearGradientBrush(ClientRectangle, Color.FromArgb(70, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), 90f);
        g.FillRectangle(vignette, ClientRectangle);
    }

    private void DrawMountainLayer(Graphics g, float parallax, Color color, float baseY)
    {
        using var brush = new SolidBrush(color);
        using var path = new GraphicsPath();
        var offset = -(_cameraX * parallax % 420);
        path.StartFigure();
        path.AddLine(offset, baseY, offset + 140, baseY - 170);
        path.AddLine(offset + 140, baseY - 170, offset + 280, baseY);
        path.AddLine(offset + 280, baseY, offset + 430, baseY - 140);
        path.AddLine(offset + 430, baseY - 140, offset + 580, baseY);
        path.AddLine(offset + 580, baseY, offset + 950, baseY);
        path.AddLine(offset + 950, baseY, offset + 950, ClientSize.Height);
        path.AddLine(offset + 950, ClientSize.Height, offset, ClientSize.Height);
        path.CloseFigure();
        g.FillPath(brush, path);
    }

    private void DrawPlatforms(Graphics g)
    {
        foreach (var platform in CurrentLevel.Platforms)
        {
            using var stone = new LinearGradientBrush(Rectangle.Round(platform.Bounds), Color.FromArgb(92, 83, 74), Color.FromArgb(55, 50, 45), 90f);
            using var outline = new Pen(Color.FromArgb(44, 44, 44));
            using var moss = new SolidBrush(Color.FromArgb(31, 91, 47));

            g.FillRectangle(stone, platform.Bounds);
            g.DrawRectangle(outline, platform.Bounds.X, platform.Bounds.Y, platform.Bounds.Width, platform.Bounds.Height);

            for (var x = platform.Bounds.X + 22; x < platform.Bounds.Right; x += 40)
            {
                g.DrawLine(Pens.Gray, x, platform.Bounds.Y, x, platform.Bounds.Bottom);
            }

            g.FillRectangle(moss, platform.Bounds.X, platform.Bounds.Y - 7, platform.Bounds.Width, 7);
        }
    }

    private void DrawDecor(Graphics g)
    {
        for (var i = 0; i < 24; i++)
        {
            var x = i * 155 + 100;
            var y = 610 + (i % 2) * 8;
            using var grass = new SolidBrush(Color.FromArgb(45, 129, 77));
            using var stem = new SolidBrush(Color.FromArgb(82, 58, 20));
            g.FillEllipse(grass, x, y, 17, 11);
            g.FillEllipse(stem, x + 6, y + 6, 4, 10);
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
            if (enemy.Type == EnemyType.Dragon)
            {
                DrawDragon(g, enemy);
                continue;
            }

            using var bodyBrush = new SolidBrush(enemy.Type switch
            {
                EnemyType.Orc => Color.OliveDrab,
                EnemyType.Goblin => Color.SeaGreen,
                EnemyType.Zombie => Color.SlateGray,
                _ => Color.MediumPurple
            });
            using var limbPen = new Pen(Color.FromArgb(38, 30, 26), 4);

            var t = enemy.AnimationPhase;
            var limbSwing = (float)Math.Sin(t) * 5f;
            var armRaise = enemy.IsAttacking ? -8f : 0f;

            g.FillEllipse(bodyBrush, enemy.Bounds.X + 4, enemy.Bounds.Y + 2, enemy.Bounds.Width - 8, enemy.Bounds.Height - 8);
            g.FillEllipse(bodyBrush, enemy.Bounds.X + 9, enemy.Bounds.Y - 12, 26, 24);
            g.DrawLine(limbPen, enemy.Bounds.X + 14, enemy.Bounds.Bottom - 2, enemy.Bounds.X + 12 + limbSwing, enemy.Bounds.Bottom + 12);
            g.DrawLine(limbPen, enemy.Bounds.Right - 14, enemy.Bounds.Bottom - 2, enemy.Bounds.Right - 12 - limbSwing, enemy.Bounds.Bottom + 12);
            g.DrawLine(limbPen, enemy.Bounds.X + 5, enemy.Bounds.Y + 20, enemy.Bounds.X - 8, enemy.Bounds.Y + 28 + armRaise);
            g.DrawLine(limbPen, enemy.Bounds.Right - 5, enemy.Bounds.Y + 20, enemy.Bounds.Right + 8, enemy.Bounds.Y + 28 + armRaise);

            g.FillEllipse(Brushes.White, enemy.Bounds.X + 14, enemy.Bounds.Y - 1, 4, 4);
            g.FillEllipse(Brushes.White, enemy.Bounds.X + 24, enemy.Bounds.Y - 1, 4, 4);
        }

        foreach (var shot in _enemyProjectiles)
        {
            using var fire = new LinearGradientBrush(Rectangle.Round(shot.Bounds), Color.MediumPurple, Color.OrangeRed, 0f);
            g.FillEllipse(fire, shot.Bounds);
        }
    }

    private static void DrawDragon(Graphics g, Enemy dragon)
    {
        var b = dragon.Bounds;
        var wingOffset = (float)Math.Sin(dragon.AnimationPhase) * 8f;
        using var body = new LinearGradientBrush(Rectangle.Round(b), Color.FromArgb(124, 44, 32), Color.FromArgb(76, 22, 16), 90f);
        using var wing = new SolidBrush(Color.FromArgb(88, 30, 24));
        using var outline = new Pen(Color.FromArgb(30, 10, 8), 3);

        g.FillEllipse(wing, b.X + 18, b.Y + 8 + wingOffset, 68, 48);
        g.FillEllipse(wing, b.Right - 86, b.Y + 8 - wingOffset, 68, 48);
        g.FillRoundedRectangle(body, b, 18);
        g.FillEllipse(Brushes.DarkRed, b.Right - 28, b.Y + 26, 34, 22);
        g.DrawArc(outline, b.Right - 32, b.Y + 30, 38, 20, -30, 140);

        g.DrawLine(outline, b.X + 24, b.Bottom - 6, b.X - 16, b.Bottom + 8);
        g.DrawLine(outline, b.X + 44, b.Bottom - 4, b.X + 12, b.Bottom + 14);
        g.DrawLine(outline, b.Right - 56, b.Bottom - 4, b.Right - 30, b.Bottom + 14);

        g.FillEllipse(Brushes.Gold, b.Right - 16, b.Y + 32, 6, 6);
        g.FillEllipse(Brushes.Orange, b.Right - 4, b.Y + 38, 8, 8);
    }

    private void DrawProjectiles(Graphics g)
    {
        foreach (var projectile in _projectiles)
        {
            if (projectile.Weapon == WeaponType.Bow)
            {
                g.FillEllipse(Brushes.BurlyWood, projectile.Bounds);
                g.DrawLine(Pens.SaddleBrown, projectile.Bounds.X, projectile.Bounds.Y + projectile.Bounds.Height / 2, projectile.Bounds.Right + 10 * Math.Sign(projectile.VelocityX), projectile.Bounds.Y + projectile.Bounds.Height / 2);
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
        if (_elfArtwork is not null)
        {
            DrawElfArtwork(g);
            return;
        }

        var p = _player.Bounds;
        var dx = _facingRight ? 1f : -1f;

        using var cape = new SolidBrush(Color.FromArgb(24, 68, 44));
        g.FillEllipse(cape, p.X + 6, p.Y + 16, 28, 48);

        using var tunic = new LinearGradientBrush(Rectangle.Round(new RectangleF(p.X + 7, p.Y + 18, 30, 42)), Color.FromArgb(110, 145, 102), Color.FromArgb(64, 101, 70), 90f);
        g.FillRoundedRectangle(tunic, new RectangleF(p.X + 7, p.Y + 18, 30, 42), 8);

        var face = new RectangleF(p.X + 11, p.Y - 2, 22, 23);
        using var faceBrush = new SolidBrush(Color.FromArgb(245, 215, 191));
        g.FillEllipse(faceBrush, face);

        using var hair = new SolidBrush(Color.FromArgb(236, 204, 119));
        g.FillEllipse(hair, p.X + 7, p.Y - 8, 30, 16);
        g.FillRectangle(hair, p.X + 8, p.Y + 4, 5, 16);
        g.FillRectangle(hair, p.X + 31, p.Y + 4, 5, 16);

        var eyeOffset = dx < 0 ? 3 : 0;
        var eyeY = p.Y + 8;
        g.FillEllipse(Brushes.White, p.X + 15 + eyeOffset, eyeY, 4, 3);
        g.FillEllipse(Brushes.White, p.X + 23 + eyeOffset, eyeY, 4, 3);
        g.FillEllipse(Brushes.Black, p.X + 16 + eyeOffset, eyeY + 1, 2, 2);
        g.FillEllipse(Brushes.Black, p.X + 24 + eyeOffset, eyeY + 1, 2, 2);
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
            using var bowPen = new Pen(Color.SaddleBrown, 2);
            g.DrawArc(bowPen, bowX, p.Y + 20, 12, 20, -90, 180);
        }
        else
        {
            var fireX = _facingRight ? p.Right + 2 : p.Left - 12;
            using var fire = new LinearGradientBrush(new RectangleF(fireX, p.Y + 24, 12, 16), Color.Yellow, Color.Red, 90f);
            g.FillEllipse(fire, fireX, p.Y + 24, 12, 16);
        }
    }

    private void DrawElfArtwork(Graphics g)
    {
        var p = _player.Bounds;
        var bob = (float)Math.Sin(_ambiencePhase * 3f) * 1.5f;
        var scale = 1.9f;
        var width = _elfArtwork!.Width * scale * 0.18f;
        var height = _elfArtwork.Height * scale * 0.18f;
        var x = p.X - (width - p.Width) / 2f;
        var y = p.Bottom - height + bob;

        var state = g.Save();
        if (!_facingRight)
        {
            g.TranslateTransform(x + width / 2f, 0);
            g.ScaleTransform(-1, 1);
            g.TranslateTransform(-(x + width / 2f), 0);
        }

        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(_elfArtwork, x, y, width, height);
        g.Restore(state);

        if (_player.SelectedWeapon == WeaponType.Fire)
        {
            var fireX = _facingRight ? x + width - 4 : x - 10;
            using var fire = new LinearGradientBrush(new RectangleF(fireX, y + height * 0.46f, 14, 18), Color.Yellow, Color.Red, 90f);
            g.FillEllipse(fire, fireX, y + height * 0.46f, 14, 18);
        }
    }

    private void DrawHud(Graphics g)
    {
        using var hudBg = new SolidBrush(Color.FromArgb(152, 0, 0, 0));
        g.FillRectangle(hudBg, 0, 0, ClientSize.Width, 110);
        g.DrawString($"Poziom: {CurrentLevel.Number}/20", _hudFont, Brushes.White, 18, 13);
        g.DrawString($"Zdrowie: {_player.Health}", _hudFont, Brushes.White, 18, 35);
        g.DrawString($"Pokonani: {_enemiesDefeatedTotal}", _hudFont, Brushes.White, 18, 57);
        g.DrawString($"Broń: {(_player.SelectedWeapon == WeaponType.Bow ? "Łuk" : "Ogień")} (Z)", _hudFont, Brushes.White, 18, 79);
        g.DrawString($"Jasność: {(int)(_brightness * 100)}%", _hudFont, Brushes.White, 240, 79);
        g.DrawString("ESC - menu (poziom, jasność, restart, upload muzyki/tła/modelu)", _hudFont, Brushes.White, 450, 79);

        for (var i = 0; i < _storyParagraphs.Length; i++)
        {
            g.DrawString(_storyParagraphs[i], _storyFont, Brushes.LightCyan, 450, 12 + (i * 20));
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
