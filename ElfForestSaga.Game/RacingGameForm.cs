using System.Drawing.Drawing2D;

namespace ElfForestSaga.Game;

public enum PlayerCarStyle
{
    Sport,
    Muscle,
    Futuristic
}

internal enum TrafficType
{
    Blue,
    Black,
    ShooterOrange
}

internal sealed class TrafficCar
{
    public int Lane;
    public float Z;
    public float Speed;
    public TrafficType Type;
    public int ShootCooldown;
}

internal sealed class Bullet
{
    public int Lane;
    public float Z;
    public float Speed;
}

public sealed class RacingGameForm : Form
{
    private readonly Timer _timer = new() { Interval = 16 };
    private readonly Random _random = new();
    private readonly HashSet<Keys> _keys = new();
    private readonly List<TrafficCar> _traffic = [];
    private readonly List<Bullet> _bullets = [];
    private readonly Font _hud = new("Segoe UI", 12, FontStyle.Bold);
    private readonly Font _message = new("Segoe UI", 22, FontStyle.Bold);

    private readonly PlayerCarStyle _playerStyle;
    private float _playerLanePosition;
    private float _speedKmh = 90f;
    private double _distanceKm;
    private bool _paused;
    private bool _gameOver;

    private const float MaxSpeed = 250f;
    private const float MinSpeed = 40f;

    public RacingGameForm(PlayerCarStyle playerStyle)
    {
        _playerStyle = playerStyle;
        Text = "Highway Pursuit";
        Width = 1280;
        Height = 768;
        DoubleBuffered = true;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.Black;

        KeyDown += OnKeyDown;
        KeyUp += (_, e) => _keys.Remove(e.KeyCode);
        _timer.Tick += (_, _) => TickGame();
        _timer.Start();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _keys.Add(e.KeyCode);
        if (e.KeyCode == Keys.P)
        {
            _paused = !_paused;
        }

        if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
    }

    private void TickGame()
    {
        if (_paused || _gameOver)
        {
            Invalidate();
            return;
        }

        const float dt = 0.016f;

        if (_keys.Contains(Keys.Left))
        {
            _playerLanePosition -= 2.3f * dt;
        }

        if (_keys.Contains(Keys.Right))
        {
            _playerLanePosition += 2.3f * dt;
        }

        _playerLanePosition = Math.Clamp(_playerLanePosition, -1.05f, 1.05f);

        var acceleration = 8.2f;
        if (_keys.Contains(Keys.Up))
        {
            acceleration += 18f;
        }

        if (_keys.Contains(Keys.Down))
        {
            acceleration -= 28f;
        }

        _speedKmh = Math.Clamp(_speedKmh + (acceleration * dt), MinSpeed, MaxSpeed);
        _distanceKm += _speedKmh * dt / 3600f;

        SpawnTraffic();
        UpdateTraffic(dt);
        UpdateBullets(dt);
        CheckCollisions();

        Invalidate();
    }

    private void SpawnTraffic()
    {
        if (_traffic.Count(c => c.Z > 200) > 12)
        {
            return;
        }

        if (_random.NextDouble() > 0.12)
        {
            return;
        }

        _traffic.Add(new TrafficCar
        {
            Lane = _random.Next(-1, 2),
            Z = _random.Next(680, 1350),
            Speed = _random.Next(60, 170),
            Type = (TrafficType)_random.Next(0, 3),
            ShootCooldown = _random.Next(60, 180)
        });
    }

    private void UpdateTraffic(float dt)
    {
        foreach (var car in _traffic)
        {
            var relative = (_speedKmh - car.Speed) * 0.72f;
            car.Z -= relative * dt;

            if (car.Type != TrafficType.ShooterOrange)
            {
                continue;
            }

            car.ShootCooldown--;
            if (car.ShootCooldown <= 0 && car.Z > 80)
            {
                _bullets.Add(new Bullet
                {
                    Lane = car.Lane,
                    Z = car.Z - 35,
                    Speed = 165
                });
                car.ShootCooldown = _random.Next(95, 175);
            }
        }

        _traffic.RemoveAll(c => c.Z < -10 || c.Z > 1600);
    }

    private void UpdateBullets(float dt)
    {
        foreach (var bullet in _bullets)
        {
            var relative = (_speedKmh + bullet.Speed) * 0.66f;
            bullet.Z -= relative * dt;
        }

        _bullets.RemoveAll(b => b.Z < -30 || b.Z > 1700);
    }

    private void CheckCollisions()
    {
        var playerLane = (int)Math.Round(_playerLanePosition);

        foreach (var car in _traffic)
        {
            if (Math.Abs(car.Z) < 32 && car.Lane == playerLane)
            {
                _gameOver = true;
                return;
            }
        }

        foreach (var bullet in _bullets)
        {
            if (Math.Abs(bullet.Z) < 20 && bullet.Lane == playerLane)
            {
                _gameOver = true;
                return;
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        DrawSkyAndGround(g);
        DrawRoad(g);
        DrawTraffic(g);
        DrawPlayer(g);
        DrawHud(g);

        if (_paused)
        {
            DrawCenterMessage(g, "PAUZA (P)");
        }

        if (_gameOver)
        {
            DrawCenterMessage(g, "KONIEC JAZDY - ESC do menu");
        }
    }

    private void DrawSkyAndGround(Graphics g)
    {
        var phase = (float)((_distanceKm % 8.0) / 8.0);
        var skyTop = LerpColor(Color.MidnightBlue, Color.SteelBlue, phase);
        var skyBottom = LerpColor(Color.SlateBlue, Color.OrangeRed, phase * 0.85f);

        using var sky = new LinearGradientBrush(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height / 2), skyTop, skyBottom, 90f);
        g.FillRectangle(sky, 0, 0, ClientSize.Width, ClientSize.Height / 2);
        g.FillRectangle(Brushes.DarkOliveGreen, 0, ClientSize.Height / 2, ClientSize.Width, ClientSize.Height / 2);
    }

    private void DrawRoad(Graphics g)
    {
        var horizonY = 120f;
        var bottomY = ClientSize.Height;
        var centerX = ClientSize.Width / 2f;

        var roadTopHalf = 90f;
        var roadBottomHalf = 370f;

        var road = new[]
        {
            new PointF(centerX - roadTopHalf, horizonY),
            new PointF(centerX + roadTopHalf, horizonY),
            new PointF(centerX + roadBottomHalf, bottomY),
            new PointF(centerX - roadBottomHalf, bottomY)
        };

        g.FillPolygon(Brushes.DimGray, road);

        for (var z = 50; z < 1500; z += 110)
        {
            var y = ProjectY(z);
            var width = RoadHalfWidthAt(y) * 0.04f;
            var height = 10 + (1 - (y / ClientSize.Height)) * 12;
            g.FillRectangle(Brushes.WhiteSmoke, centerX - width / 2f, y - height / 2f, width, height);
        }
    }

    private void DrawTraffic(Graphics g)
    {
        foreach (var car in _traffic.OrderByDescending(c => c.Z))
        {
            if (car.Z < 0 || car.Z > 1500)
            {
                continue;
            }

            var y = ProjectY(car.Z);
            var x = ProjectX(car.Lane, y);
            var size = 25 + Perspective(car.Z) * 60;
            var rect = new RectangleF(x - size / 2f, y - size, size, size);

            var brush = car.Type switch
            {
                TrafficType.Blue => Brushes.DodgerBlue,
                TrafficType.Black => Brushes.Black,
                _ => Brushes.Orange
            };

            g.FillRectangle(brush, rect);
            g.FillRectangle(Brushes.White, rect.X + 4, rect.Bottom - 10, rect.Width - 8, 5);
        }

        foreach (var bullet in _bullets)
        {
            if (bullet.Z < 0 || bullet.Z > 1500)
            {
                continue;
            }

            var y = ProjectY(bullet.Z);
            var x = ProjectX(bullet.Lane, y);
            var radius = 4 + Perspective(bullet.Z) * 7;
            g.FillEllipse(Brushes.OrangeRed, x - radius, y - radius, radius * 2, radius * 2);
        }
    }

    private void DrawPlayer(Graphics g)
    {
        var y = ClientSize.Height - 85;
        var x = ProjectX((int)Math.Round(_playerLanePosition), y) + ((_playerLanePosition - (int)Math.Round(_playerLanePosition)) * 85f);
        var body = new RectangleF(x - 42, y - 22, 84, 44);

        g.FillRectangle(Brushes.LimeGreen, body);

        var accent = _playerStyle switch
        {
            PlayerCarStyle.Sport => Brushes.White,
            PlayerCarStyle.Muscle => Brushes.DarkRed,
            _ => Brushes.Cyan
        };

        g.FillRectangle(accent, body.X + 10, body.Y + 8, body.Width - 20, 8);
        g.FillEllipse(Brushes.Black, body.X - 6, body.Bottom - 10, 18, 18);
        g.FillEllipse(Brushes.Black, body.Right - 12, body.Bottom - 10, 18, 18);
    }

    private void DrawHud(Graphics g)
    {
        g.FillRectangle(new SolidBrush(Color.FromArgb(130, 0, 0, 0)), 0, 0, ClientSize.Width, 58);
        g.DrawString($"Kilometry: {_distanceKm:0.00} km", _hud, Brushes.White, 14, 16);
        var speedText = $"Prędkość: {_speedKmh:0} km/h (max {MaxSpeed:0})";
        var width = g.MeasureString(speedText, _hud).Width;
        g.DrawString(speedText, _hud, Brushes.White, ClientSize.Width - width - 14, 16);
    }

    private void DrawCenterMessage(Graphics g, string text)
    {
        var size = g.MeasureString(text, _message);
        var x = (ClientSize.Width - size.Width) / 2;
        var y = (ClientSize.Height - size.Height) / 2;
        g.FillRectangle(new SolidBrush(Color.FromArgb(170, 0, 0, 0)), x - 20, y - 16, size.Width + 40, size.Height + 32);
        g.DrawString(text, _message, Brushes.Gold, x, y);
    }

    private float Perspective(float z) => 1f / (1f + (z / 260f));

    private float ProjectY(float z)
    {
        var horizon = 120f;
        var bottom = ClientSize.Height - 40f;
        var p = Perspective(z);
        return horizon + ((bottom - horizon) * p);
    }

    private float RoadHalfWidthAt(float y)
    {
        var t = y / ClientSize.Height;
        return 90 + (t * 300);
    }

    private float ProjectX(int lane, float y)
    {
        var laneWidth = RoadHalfWidthAt(y) * 0.58f;
        return (ClientSize.Width / 2f) + (lane * laneWidth);
    }

    private static Color LerpColor(Color from, Color to, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        var r = from.R + ((to.R - from.R) * t);
        var g = from.G + ((to.G - from.G) * t);
        var b = from.B + ((to.B - from.B) * t);
        return Color.FromArgb((int)r, (int)g, (int)b);
    }
}
