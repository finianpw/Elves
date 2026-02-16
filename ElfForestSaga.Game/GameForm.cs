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
    private readonly Dictionary<int, Image> _levelBackgroundCache = new();
    private readonly Dictionary<EnemyType, Image?> _enemySkinCache = new();

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
        "Aerin walczy o Serce Lasu, a ostatni 21. poziom to pojedynek ze smokiem.",
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
            foreach (var background in _levelBackgroundCache.Values)
            {
                background.Dispose();
            }

            foreach (var skin in _enemySkinCache.Values.Where(s => s is not null))
            {
                skin!.Dispose();
            }
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
    private string CharactersDirectory => Path.Combine(VisualsDirectory, "characters");
    private string ModelsDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "models");

    private void ReloadVisualAssets()
    {
        _backgroundArtwork?.Dispose();
        _elfArtwork?.Dispose();
        _backgroundArtwork = LoadArtwork("background-main.png");
        _elfArtwork = LoadArtwork("elf-3d.png");

        foreach (var img in _levelBackgroundCache.Values)
        {
            img.Dispose();
        }

        _levelBackgroundCache.Clear();

        foreach (var type in Enum.GetValues<EnemyType>())
        {
            if (_enemySkinCache.TryGetValue(type, out var current) && current is not null)
            {
                current.Dispose();
            }

            _enemySkinCache[type] = LoadEnemySkin(type);
        }
    }

    private Image? LoadEnemySkin(EnemyType type)
    {
        var key = type.ToString().ToLowerInvariant();
        var path = Path.Combine(CharactersDirectory, $"{key}.png");
        return File.Exists(path) ? LoadArtwork(Path.Combine("characters", $"{key}.png")) : null;
    }

    private static bool IsModelFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".fbx" or ".obj" or ".gltf" or ".glb" or ".blend";
    }

    private static string? FindPreviewImageForModel(string modelPath)
    {
        var dir = Path.GetDirectoryName(modelPath);
        if (string.IsNullOrWhiteSpace(dir)) return null;
        var name = Path.GetFileNameWithoutExtension(modelPath);
        var candidates = new[] { ".png", ".jpg", ".jpeg" }
            .Select(ext => Path.Combine(dir, name + ext));
        return candidates.FirstOrDefault(File.Exists);
    }

    private static Bitmap CreateModelPlaceholder(string label, Color color)
    {
        var bmp = new Bitmap(220, 300);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var bg = new LinearGradientBrush(new Rectangle(0, 0, 220, 300), Color.FromArgb(28, 30, 40), Color.FromArgb(14, 16, 22), 90f);
        g.FillRectangle(bg, 0, 0, 220, 300);
        using var silhouette = new SolidBrush(color);
        g.FillEllipse(silhouette, 58, 30, 104, 92);
        g.FillRoundedRectangle(silhouette, new RectangleF(64, 110, 92, 140), 20);
        using var pen = new Pen(Color.FromArgb(245, 220, 120), 2f);
        g.DrawEllipse(pen, 52, 24, 116, 104);
        using var font = new Font("Segoe UI", 11, FontStyle.Bold);
        g.DrawString("3D MODEL", font, Brushes.Gold, 62, 258);
        g.DrawString(label, font, Brushes.WhiteSmoke, 26, 20);
        return bmp;
    }

    private void ImportElfAsset(string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return;
        }

        Directory.CreateDirectory(VisualsDirectory);
        Directory.CreateDirectory(ModelsDirectory);

        var elfTarget = Path.Combine(VisualsDirectory, "elf-3d.png");
        if (IsModelFile(sourcePath))
        {
            var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            File.Copy(sourcePath, Path.Combine(ModelsDirectory, "elf-3d" + ext), true);
            var preview = FindPreviewImageForModel(sourcePath);
            if (!string.IsNullOrWhiteSpace(preview) && File.Exists(preview))
            {
                File.Copy(preview, elfTarget, true);
            }
            else
            {
                using var placeholder = CreateModelPlaceholder("ELF", Color.ForestGreen);
                placeholder.Save(elfTarget);
            }

            return;
        }

        File.Copy(sourcePath, elfTarget, true);
    }

    private void ImportAppearanceAsset(string? sourcePath, string targetKey, Color placeholderColor)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return;
        }

        Directory.CreateDirectory(CharactersDirectory);
        Directory.CreateDirectory(ModelsDirectory);

        var targetPng = Path.Combine(CharactersDirectory, $"{targetKey}.png");

        if (IsModelFile(sourcePath))
        {
            var modelExt = Path.GetExtension(sourcePath).ToLowerInvariant();
            var modelTarget = Path.Combine(ModelsDirectory, $"{targetKey}{modelExt}");
            File.Copy(sourcePath, modelTarget, true);

            var preview = FindPreviewImageForModel(sourcePath);
            if (!string.IsNullOrWhiteSpace(preview) && File.Exists(preview))
            {
                File.Copy(preview, targetPng, true);
            }
            else
            {
                using var placeholder = CreateModelPlaceholder(targetKey.ToUpperInvariant(), placeholderColor);
                placeholder.Save(targetPng);
            }

            return;
        }

        File.Copy(sourcePath, targetPng, true);
    }

    private void ResetAppearanceAsset(string targetKey)
    {
        var png = Path.Combine(CharactersDirectory, $"{targetKey}.png");
        if (File.Exists(png)) File.Delete(png);

        foreach (var model in Directory.Exists(ModelsDirectory)
                     ? Directory.GetFiles(ModelsDirectory, targetKey + ".*")
                     : Array.Empty<string>())
        {
            File.Delete(model);
        }
    }

    private Image? GetBackgroundForCurrentLevel()
    {
        var level = CurrentLevel.Number;
        if (_levelBackgroundCache.TryGetValue(level, out var cached))
        {
            return cached;
        }

        var levelPath = Path.Combine(VisualsDirectory, $"background-level-{level}.png");
        if (!File.Exists(levelPath))
        {
            return _backgroundArtwork;
        }

        var loaded = LoadArtwork($"background-level-{level}.png");
        if (loaded is not null)
        {
            _levelBackgroundCache[level] = loaded;
            return loaded;
        }

        return _backgroundArtwork;
    }

    private void ImportMenuAssets(PauseMenuResult result)
    {
        Directory.CreateDirectory(VisualsDirectory);
        Directory.CreateDirectory(CharactersDirectory);
        var musicDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "music");
        Directory.CreateDirectory(musicDirectory);

        if (result.ResetMusic && File.Exists(_soundtrack.CustomTrackPath))
        {
            File.Delete(_soundtrack.CustomTrackPath);
            _soundtrack.Restart();
        }

        if (result.ResetBackground)
        {
            var bgPath = Path.Combine(VisualsDirectory, "background-main.png");
            if (File.Exists(bgPath))
            {
                File.Delete(bgPath);
            }
        }

        if (result.ResetSelectedLevelBackground)
        {
            var selectedBg = Path.Combine(VisualsDirectory, $"background-level-{result.SelectedLevel}.png");
            if (File.Exists(selectedBg))
            {
                File.Delete(selectedBg);
            }
        }

        if (result.ResetElf)
        {
            var elfPath = Path.Combine(VisualsDirectory, "elf-3d.png");
            if (File.Exists(elfPath))
            {
                File.Delete(elfPath);
            }
        }

        if (result.ResetOrcAppearance) ResetAppearanceAsset("orc");
        if (result.ResetGoblinAppearance) ResetAppearanceAsset("goblin");
        if (result.ResetZombieAppearance) ResetAppearanceAsset("zombie");
        if (result.ResetWizardAppearance) ResetAppearanceAsset("elderwizard");
        if (result.ResetDragonAppearance) ResetAppearanceAsset("dragon");

        if (!string.IsNullOrWhiteSpace(result.MusicFileToImport) && File.Exists(result.MusicFileToImport))
        {
            File.Copy(result.MusicFileToImport, _soundtrack.CustomTrackPath, true);
            _soundtrack.Restart();
        }

        if (!string.IsNullOrWhiteSpace(result.BackgroundFileToImport) && File.Exists(result.BackgroundFileToImport))
        {
            File.Copy(result.BackgroundFileToImport, Path.Combine(VisualsDirectory, "background-main.png"), true);
        }

        if (!string.IsNullOrWhiteSpace(result.LevelBackgroundFileToImport) && File.Exists(result.LevelBackgroundFileToImport))
        {
            File.Copy(result.LevelBackgroundFileToImport, Path.Combine(VisualsDirectory, $"background-level-{result.SelectedLevel}.png"), true);
        }

        ImportElfAsset(result.ElfFileToImport);

        ImportAppearanceAsset(result.OrcAppearanceFileToImport, "orc", Color.DarkOliveGreen);
        ImportAppearanceAsset(result.GoblinAppearanceFileToImport, "goblin", Color.SeaGreen);
        ImportAppearanceAsset(result.ZombieAppearanceFileToImport, "zombie", Color.SlateGray);
        ImportAppearanceAsset(result.WizardAppearanceFileToImport, "elderwizard", Color.MediumPurple);
        ImportAppearanceAsset(result.DragonAppearanceFileToImport, "dragon", Color.DarkRed);

        ReloadVisualAssets();
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
            var directionToPlayer = playerDistance >= 0 ? 1 : -1;
            var closeEnoughToChase = enemy.Type switch
            {
                EnemyType.Dragon => Math.Abs(playerDistance) < 520,
                EnemyType.Eagle => Math.Abs(playerDistance) < 680,
                EnemyType.ElderWizard => Math.Abs(playerDistance) < 380,
                _ => Math.Abs(playerDistance) < 320
            };

            var dir = closeEnoughToChase ? directionToPlayer : (enemy.MovingRight ? 1 : -1);
            var speed = closeEnoughToChase ? enemy.Speed * 1.15f : enemy.Speed;

            var newX = enemy.Bounds.X + (speed * dir);
            var newY = enemy.Bounds.Y;

            if (enemy.Type != EnemyType.Eagle)
            {
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

                if (enemy.Type == EnemyType.ElderWizard)
                {
                    // Czarodzieje biegają także po platformach: próbują trzymać się ich wysokości.
                    var targetPlatform = level.Platforms
                        .Where(p => p.Bounds.Width < 600 && newX + enemy.Bounds.Width / 2f >= p.Bounds.X && newX + enemy.Bounds.Width / 2f <= p.Bounds.Right)
                        .OrderBy(p => Math.Abs((p.Bounds.Y - enemy.Bounds.Height) - enemy.Bounds.Y))
                        .FirstOrDefault();

                    if (targetPlatform is not null)
                    {
                        var targetY = targetPlatform.Bounds.Y - enemy.Bounds.Height;
                        newY += Math.Clamp(targetY - enemy.Bounds.Y, -3.2f, 3.2f);
                    }
                }
            }
            else
            {
                // Orły atakują z powietrza i nurkują na elfa.
                var wave = (float)Math.Sin(enemy.AnimationPhase * 0.45f) * 2.2f;
                var dive = closeEnoughToChase ? Math.Clamp((_player.Bounds.Y - 80f) - enemy.Bounds.Y, -2.8f, 3.5f) : wave;
                newY += dive;

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

            enemy.Bounds = new RectangleF(newX, newY, enemy.Bounds.Width, enemy.Bounds.Height);
            enemy.AnimationPhase += enemy.Type switch
            {
                EnemyType.Dragon => 0.16f,
                EnemyType.Eagle => 0.30f,
                EnemyType.ElderWizard => 0.24f,
                _ => 0.22f + (closeEnoughToChase ? 0.08f : 0f)
            };

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

            var canShoot = enemy.Type is EnemyType.ElderWizard or EnemyType.Dragon or EnemyType.Goblin or EnemyType.Eagle;
            if (canShoot && closeEnoughToChase && enemy.AttackCooldown == 0)
            {
                var vx = enemy.Type switch
                {
                    EnemyType.Dragon => directionToPlayer * 6.8f,
                    EnemyType.Eagle => directionToPlayer * 6.2f,
                    EnemyType.Goblin => directionToPlayer * 7.2f,
                    _ => directionToPlayer * 5.2f
                };

                var shotBounds = enemy.Type switch
                {
                    EnemyType.Dragon => new RectangleF(enemy.Bounds.X + (directionToPlayer > 0 ? enemy.Bounds.Width - 18 : 8), enemy.Bounds.Y + 34, 18, 14),
                    EnemyType.Eagle => new RectangleF(enemy.Bounds.X + 18, enemy.Bounds.Y + 10, 11, 9),
                    EnemyType.Goblin => new RectangleF(enemy.Bounds.X + 16, enemy.Bounds.Y + 16, 13, 6),
                    _ => new RectangleF(enemy.Bounds.X + 18, enemy.Bounds.Y + 16, 10, 10)
                };

                _enemyProjectiles.Add(new EnemyProjectile
                {
                    Bounds = shotBounds,
                    VelocityX = vx,
                    VelocityY = enemy.Type == EnemyType.Eagle ? 0.4f : 0,
                    Damage = enemy.Type switch
                    {
                        EnemyType.Dragon => enemy.Damage + 8,
                        EnemyType.Eagle => enemy.Damage + 3,
                        EnemyType.Goblin => enemy.Damage + 2,
                        _ => enemy.Damage + 3
                    }
                });

                enemy.IsAttacking = true;
                enemy.AttackCooldown = enemy.Type switch
                {
                    EnemyType.Dragon => 34,
                    EnemyType.Goblin => 45,
                    EnemyType.Eagle => 52,
                    _ => 58
                };
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

            if (shot.Bounds.X < -30 || shot.Bounds.X > level.Width + 30 || shot.Bounds.Y > ClientSize.Height + 60)
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
        var activeBackground = GetBackgroundForCurrentLevel();
        if (activeBackground is not null)
        {
            DrawArtworkBackground(g, activeBackground);
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

    private void DrawArtworkBackground(Graphics g, Image background)
    {
        var src = new RectangleF(0, 0, background.Width, background.Height);
        var scale = Math.Max(ClientSize.Width / src.Width, ClientSize.Height / src.Height);
        var drawWidth = src.Width * scale;
        var drawHeight = src.Height * scale;
        var parallaxX = (_cameraX * 0.08f) % Math.Max(1f, drawWidth - ClientSize.Width + 1);
        var dest = new RectangleF(-parallaxX, (ClientSize.Height - drawHeight) / 2f, drawWidth, drawHeight);

        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(background, dest, src, GraphicsUnit.Pixel);

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

            using var flaskBody = new SolidBrush(pickup.Type == PickupType.Health ? Color.FromArgb(60, 170, 80) : Color.FromArgb(190, 42, 42));
            using var glass = new SolidBrush(Color.FromArgb(200, 220, 230, 235));
            using var cork = new SolidBrush(Color.SaddleBrown);

            // Fiolka laboratoryjna
            g.FillEllipse(glass, bounds.X + 4, bounds.Y + 2, bounds.Width - 8, bounds.Height - 2);
            g.FillRectangle(flaskBody, bounds.X + 7, bounds.Y + bounds.Height / 2.6f, bounds.Width - 14, bounds.Height / 2.2f);
            g.FillRectangle(cork, bounds.X + bounds.Width / 2f - 4, bounds.Y - 2, 8, 6);
            g.DrawEllipse(Pens.WhiteSmoke, bounds.X + 4, bounds.Y + 2, bounds.Width - 8, bounds.Height - 2);
        }
    }

    private void DrawEnemies(Graphics g)
    {
        foreach (var enemy in CurrentLevel.Enemies)
        {
            if (_enemySkinCache.TryGetValue(enemy.Type, out var skin) && skin is not null)
            {
                DrawEnemySkin(g, enemy, skin);
                continue;
            }

            if (enemy.Type == EnemyType.Dragon)
            {
                DrawDragon(g, enemy);
                continue;
            }

            if (enemy.Type == EnemyType.Eagle)
            {
                DrawEagle(g, enemy);
                continue;
            }

            DrawHumanoidEnemy(g, enemy);
        }

        foreach (var shot in _enemyProjectiles)
        {
            var shotColor = CurrentLevel.Enemies.Any(e => e.Type == EnemyType.Goblin) ? Color.BurlyWood : Color.MediumPurple;
            using var fire = new LinearGradientBrush(Rectangle.Round(shot.Bounds), shotColor, Color.OrangeRed, 0f);
            g.FillEllipse(fire, shot.Bounds);
        }
    }

    private void DrawHumanoidEnemy(Graphics g, Enemy enemy)
    {
        var bodyColor = enemy.Type switch
        {
            EnemyType.Orc => Color.FromArgb(104, 126, 72),
            EnemyType.Goblin => Color.FromArgb(76, 132, 88),
            EnemyType.Zombie => Color.FromArgb(112, 120, 124),
            _ => Color.FromArgb(126, 96, 144)
        };

        using var bodyBrush = new SolidBrush(bodyColor);
        using var faceBrush = new SolidBrush(Color.FromArgb(210, 180, 160));
        using var wrinklePen = new Pen(Color.FromArgb(95, 70, 60), 1.4f);
        using var mouthPen = new Pen(Color.FromArgb(90, 30, 30), 2.2f);
        using var limbPen = new Pen(Color.FromArgb(42, 34, 28), 4);

        var t = enemy.AnimationPhase;
        var limbSwing = (float)Math.Sin(t) * 5f;
        var armRaise = enemy.IsAttacking ? -8f : 0f;
        var mouthOpen = 2.4f + (float)Math.Abs(Math.Sin(t * 1.8f)) * 4.8f;

        g.FillEllipse(bodyBrush, enemy.Bounds.X + 4, enemy.Bounds.Y + 2, enemy.Bounds.Width - 8, enemy.Bounds.Height - 8);
        var head = new RectangleF(enemy.Bounds.X + 8, enemy.Bounds.Y - 15, 30, 28);
        g.FillEllipse(faceBrush, head);

        g.FillEllipse(Brushes.WhiteSmoke, head.X + 6, head.Y + 9, 5, 4);
        g.FillEllipse(Brushes.WhiteSmoke, head.X + 18, head.Y + 9, 5, 4);
        g.FillEllipse(Brushes.Black, head.X + 7.4f, head.Y + 10.1f, 2.1f, 2.1f);
        g.FillEllipse(Brushes.Black, head.X + 19.4f, head.Y + 10.1f, 2.1f, 2.1f);

        if (enemy.Type == EnemyType.Goblin)
        {
            // Ohydna twarz i wściekły wyraz + topór i łuk
            g.DrawArc(wrinklePen, head.X + 2, head.Y + 5, 9, 6, 200, 140);
            g.DrawArc(wrinklePen, head.X + 17, head.Y + 5, 9, 6, 210, 130);
            g.DrawLine(wrinklePen, head.X + 11, head.Y + 15, head.X + 18, head.Y + 17);
            g.DrawArc(mouthPen, head.X + 8, head.Y + 16, 14, mouthOpen + 1, 15, 160);
            using var axe = new Pen(Color.Silver, 3);
            g.DrawLine(axe, enemy.Bounds.Right + 2, enemy.Bounds.Y + 18, enemy.Bounds.Right + 14, enemy.Bounds.Y + 8);
            g.DrawLine(Pens.SaddleBrown, enemy.Bounds.Right, enemy.Bounds.Y + 22, enemy.Bounds.Right + 12, enemy.Bounds.Y + 34);
            using var bow = new Pen(Color.BurlyWood, 2);
            g.DrawArc(bow, enemy.Bounds.X - 8, enemy.Bounds.Y + 16, 10, 18, -90, 180);
        }
        else if (enemy.Type == EnemyType.ElderWizard)
        {
            // Siwe długie włosy, laska i bardziej ludzka sylwetka
            using var hair = new SolidBrush(Color.LightGray);
            g.FillEllipse(hair, head.X - 2, head.Y - 4, head.Width + 4, 14);
            g.FillRectangle(hair, head.X - 1, head.Y + 6, 6, 18);
            g.FillRectangle(hair, head.Right - 5, head.Y + 6, 6, 18);
            g.DrawArc(wrinklePen, head.X + 3, head.Y + 4, 10, 6, 200, 130);
            g.DrawArc(wrinklePen, head.X + 16, head.Y + 4, 10, 6, 210, 120);
            g.DrawArc(mouthPen, head.X + 9, head.Y + 17, 12, mouthOpen, 10, 160);
            using var staff = new Pen(Color.FromArgb(118, 83, 52), 4);
            g.DrawLine(staff, enemy.Bounds.X - 4, enemy.Bounds.Y + 10, enemy.Bounds.X - 4, enemy.Bounds.Bottom + 12);
            g.FillEllipse(Brushes.Cyan, enemy.Bounds.X - 8, enemy.Bounds.Y + 6, 8, 8);
        }
        else
        {
            g.DrawArc(wrinklePen, head.X + 3, head.Y + 4, 10, 6, 200, 130);
            g.DrawArc(wrinklePen, head.X + 16, head.Y + 4, 10, 6, 210, 120);
            g.DrawLine(wrinklePen, head.X + 14.5f, head.Y + 13, head.X + 14.5f, head.Y + 17);
            g.DrawArc(mouthPen, head.X + 9, head.Y + 16, 12, mouthOpen, 12, 160);
        }

        g.DrawLine(limbPen, enemy.Bounds.X + 14, enemy.Bounds.Bottom - 2, enemy.Bounds.X + 12 + limbSwing, enemy.Bounds.Bottom + 12);
        g.DrawLine(limbPen, enemy.Bounds.Right - 14, enemy.Bounds.Bottom - 2, enemy.Bounds.Right - 12 - limbSwing, enemy.Bounds.Bottom + 12);
        g.DrawLine(limbPen, enemy.Bounds.X + 5, enemy.Bounds.Y + 20, enemy.Bounds.X - 8, enemy.Bounds.Y + 28 + armRaise);
        g.DrawLine(limbPen, enemy.Bounds.Right - 5, enemy.Bounds.Y + 20, enemy.Bounds.Right + 8, enemy.Bounds.Y + 28 + armRaise);
    }

    private static void DrawEagle(Graphics g, Enemy eagle)
    {
        var b = eagle.Bounds;
        var wing = (float)Math.Sin(eagle.AnimationPhase * 0.8f) * 8f;
        using var feather = new SolidBrush(Color.FromArgb(86, 70, 52));
        using var belly = new SolidBrush(Color.FromArgb(205, 190, 168));
        using var beak = new SolidBrush(Color.Goldenrod);

        g.FillEllipse(feather, b.X + 8, b.Y + 4, b.Width - 16, b.Height - 8);
        g.FillEllipse(feather, b.X - 8, b.Y + 6 + wing, 20, 12);
        g.FillEllipse(feather, b.Right - 12, b.Y + 6 - wing, 20, 12);
        g.FillEllipse(belly, b.X + 20, b.Y + 10, 18, 14);
        g.FillPolygon(beak, [new PointF(b.Right - 6, b.Y + 12), new PointF(b.Right + 4, b.Y + 15), new PointF(b.Right - 6, b.Y + 18)]);
        g.FillEllipse(Brushes.White, b.X + 34, b.Y + 10, 4, 4);
        g.FillEllipse(Brushes.Black, b.X + 35, b.Y + 11, 2, 2);
    }

    private void DrawEnemySkin(Graphics g, Enemy enemy, Image skin)
    {
        var state = g.Save();
        var facingRight = enemy.MovingRight || _player.Bounds.X >= enemy.Bounds.X;
        if (!facingRight)
        {
            g.TranslateTransform(enemy.Bounds.X + enemy.Bounds.Width / 2f, 0);
            g.ScaleTransform(-1, 1);
            g.TranslateTransform(-(enemy.Bounds.X + enemy.Bounds.Width / 2f), 0);
        }

        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        var drawRect = new RectangleF(enemy.Bounds.X - 12, enemy.Bounds.Y - 22, enemy.Bounds.Width + 24, enemy.Bounds.Height + 30);
        g.DrawImage(skin, drawRect);
        g.Restore(state);
    }

    private static void DrawDragon(Graphics g, Enemy dragon)
    {
        var b = dragon.Bounds;
        var slither = (float)Math.Sin(dragon.AnimationPhase * 0.7f) * 10f;
        var jawOpen = 7f + (float)Math.Abs(Math.Sin(dragon.AnimationPhase * 1.6f)) * 10f;
        using var scaleA = new SolidBrush(Color.FromArgb(126, 44, 36));
        using var scaleB = new SolidBrush(Color.FromArgb(84, 28, 20));
        using var wing = new SolidBrush(Color.FromArgb(92, 36, 26));
        using var outline = new Pen(Color.FromArgb(28, 8, 6), 3);
        using var spike = new SolidBrush(Color.FromArgb(220, 210, 190));

        // Wężowe, bardziej mobilne segmenty ciała
        g.FillEllipse(scaleA, b.X + 10, b.Y + 42 + slither * 0.2f, b.Width * 0.55f, b.Height * 0.46f);
        g.FillEllipse(scaleB, b.X + b.Width * 0.32f, b.Y + 34 - slither * 0.15f, b.Width * 0.5f, b.Height * 0.44f);
        g.FillEllipse(scaleA, b.X + b.Width * 0.56f, b.Y + 26 + slither * 0.1f, b.Width * 0.36f, b.Height * 0.40f);

        // skrzydła
        g.FillEllipse(wing, b.X + 24, b.Y + 8 + slither * 0.1f, 96, 62);
        g.FillEllipse(wing, b.Right - 118, b.Y + 12 - slither * 0.1f, 96, 62);

        // głowa
        var head = new RectangleF(b.Right - 92, b.Y + 20, 90, 62);
        g.FillEllipse(scaleA, head);
        g.DrawArc(outline, head.X + 10, head.Y + 34, 64, jawOpen, -8, 170);
        g.DrawArc(outline, head.X + 8, head.Y + 20, 68, 34, -15, 150);
        g.FillEllipse(Brushes.Gold, head.X + 54, head.Y + 18, 8, 8);
        g.FillEllipse(Brushes.DarkOrange, head.X + 60, head.Y + 29, 10, 10);

        // smocze łapy
        g.DrawLine(outline, b.X + 30, b.Bottom - 6, b.X - 12, b.Bottom + 10);
        g.DrawLine(outline, b.X + 62, b.Bottom - 5, b.X + 24, b.Bottom + 14);
        g.DrawLine(outline, b.Right - 70, b.Bottom - 5, b.Right - 36, b.Bottom + 14);

        // kolce
        for (var i = 0; i < 9; i++)
        {
            var sx = b.X + 20 + i * 24;
            var sy = b.Y - 6 + (i % 2) * 4;
            g.FillPolygon(spike, [new PointF(sx, sy + 15), new PointF(sx + 7, sy), new PointF(sx + 14, sy + 15)]);
        }

        if (dragon.IsAttacking)
        {
            using var flame = new LinearGradientBrush(new RectangleF(head.Right - 4, head.Y + 36, 64, 20), Color.Yellow, Color.OrangeRed, 0f);
            g.FillEllipse(flame, head.Right - 4, head.Y + 36, 64, 20);
        }
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
        var walkSwing = (float)Math.Sin(_ambiencePhase * 4.2f) * (_onGround ? 1f : 0.2f);

        using var cape = new SolidBrush(Color.FromArgb(24, 68, 44));
        g.FillEllipse(cape, p.X + 6, p.Y + 16, 28, 48);

        using var tunic = new LinearGradientBrush(Rectangle.Round(new RectangleF(p.X + 7, p.Y + 18, 30, 42)), Color.FromArgb(118, 152, 110), Color.FromArgb(62, 98, 70), 90f);
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
        g.DrawArc(Pens.SaddleBrown, p.X + 18 + (dx < 0 ? 1 : 0), p.Y + 13, 8, 5 + Math.Abs(walkSwing) * 1.5f, 10, 160);

        using var pants = new SolidBrush(Color.FromArgb(72, 52, 42));
        g.FillRectangle(pants, p.X + 10 + walkSwing, p.Bottom - 18, 10, 18);
        g.FillRectangle(pants, p.X + 24 - walkSwing, p.Bottom - 18, 10, 18);
        g.FillRectangle(Brushes.Sienna, p.X + 9, p.Bottom - 4, 12, 4);
        g.FillRectangle(Brushes.Sienna, p.X + 23, p.Bottom - 4, 12, 4);

        using var arm = new SolidBrush(Color.FromArgb(244, 214, 188));
        g.FillRectangle(arm, p.X + (_facingRight ? 31 : 3), p.Y + 24 + walkSwing * 0.6f, 8, 16);

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
        var stride = (float)Math.Sin(_ambiencePhase * 4.4f) * (_onGround ? 4f : 1f);
        var scale = 1.9f;
        var width = _elfArtwork!.Width * scale * 0.18f;
        var height = _elfArtwork.Height * scale * 0.18f;
        var x = p.X - (width - p.Width) / 2f + stride * 0.2f;
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
        g.DrawString($"Poziom: {CurrentLevel.Number}/{_levels.Count}", _hudFont, Brushes.White, 18, 13);
        g.DrawString($"Zdrowie: {_player.Health}", _hudFont, Brushes.White, 18, 35);
        g.DrawString($"Pokonani: {_enemiesDefeatedTotal}", _hudFont, Brushes.White, 18, 57);
        g.DrawString($"Broń: {(_player.SelectedWeapon == WeaponType.Bow ? "Łuk" : "Ogień")} (Z)", _hudFont, Brushes.White, 18, 79);
        g.DrawString($"Jasność: {(int)(_brightness * 100)}%", _hudFont, Brushes.White, 240, 79);
        g.DrawString("ESC - menu (poziom, jasność, tła per-level, modele postaci i resety)", _hudFont, Brushes.White, 450, 79);

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
