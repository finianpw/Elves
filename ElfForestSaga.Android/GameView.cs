using Android.Content;
using Android.Graphics;
using Android.Views;

namespace ElfForestSaga.Android;

public sealed class GameView : View
{
    private readonly Action _goToMenu;

    private readonly Paint _bgPaint = new() { AntiAlias = true };
    private readonly Paint _platformPaint = new() { Color = Color.Rgb(78, 66, 56), AntiAlias = true };
    private readonly Paint _heroPaint = new() { Color = Color.Rgb(230, 210, 160), AntiAlias = true };
    private readonly Paint _enemyPaint = new() { Color = Color.Rgb(95, 150, 90), AntiAlias = true };
    private readonly Paint _hudPaint = new() { Color = Color.White, TextSize = 44, AntiAlias = true };
    private readonly Paint _projectilePaint = new() { Color = Color.OrangeRed, AntiAlias = true };
    private readonly Paint _joystickPaint = new() { Color = Color.Argb(120, 255, 255, 255), AntiAlias = true };
    private readonly Paint _buttonPaint = new() { Color = Color.Argb(150, 220, 30, 30), AntiAlias = true };

    private readonly RectF _hero = new(120, 420, 190, 540);
    private readonly RectF _enemy = new(980, 430, 1050, 540);
    private readonly List<RectF> _shots = new();

    private float _cameraX;
    private float _heroVelocityY;
    private bool _onGround = true;
    private float _moveInput;
    private int _health = 100;
    private int _cooldown;

    private int _leftTouchId = -1;
    private int _rightTouchId = -1;
    private PointF _joyCenter;
    private PointF _joyKnob;
    private RectF _fireButton;

    private bool _running;
    private readonly Runnable _tickRunner;

    public GameView(Context context, Action goToMenu) : base(context)
    {
        _goToMenu = goToMenu;
        Focusable = true;
        FocusableInTouchMode = true;

        _tickRunner = new Runnable(() =>
        {
            if (!_running) return;
            Tick();
            Invalidate();
            PostDelayed(_tickRunner, 16);
        });

        StartLoop();
    }

    private void StartLoop()
    {
        _running = true;
        Post(_tickRunner);
    }

    protected override void OnDetachedFromWindow()
    {
        _running = false;
        base.OnDetachedFromWindow();
    }

    protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
    {
        base.OnSizeChanged(w, h, oldw, oldh);
        _joyCenter = new PointF(160, h - 160);
        _joyKnob = new PointF(_joyCenter.X, _joyCenter.Y);
        _fireButton = new RectF(w - 260, h - 230, w - 80, h - 50);
    }

    private void Tick()
    {
        _hero.Offset(_moveInput * 8.5f, 0);
        _heroVelocityY += 1.1f;
        _hero.Offset(0, _heroVelocityY);

        var groundY = Height - 110;
        if (_hero.Bottom >= groundY)
        {
            var correction = groundY - _hero.Bottom;
            _hero.Offset(0, correction);
            _heroVelocityY = 0;
            _onGround = true;
        }

        _cameraX = Math.Max(0, _hero.CenterX() - Width * 0.4f);

        var dir = _enemy.CenterX() > _hero.CenterX() ? -1.9f : 1.9f;
        _enemy.Offset(dir, 0);

        foreach (var s in _shots.ToList())
        {
            s.Offset(14, 0);
            if (s.Left > _cameraX + Width + 200)
            {
                _shots.Remove(s);
                continue;
            }

            if (RectF.Intersects(s, _enemy))
            {
                _shots.Remove(s);
                _enemy.Offset(220, 0);
            }
        }

        if (RectF.Intersects(_hero, _enemy))
        {
            _health = Math.Max(0, _health - 1);
        }

        _cooldown = Math.Max(0, _cooldown - 1);
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null) return false;

        var action = e.ActionMasked;
        var idx = e.ActionIndex;
        var id = e.GetPointerId(idx);

        switch (action)
        {
            case MotionEventActions.Down:
            case MotionEventActions.PointerDown:
            {
                var x = e.GetX(idx);
                var y = e.GetY(idx);

                if (x < Width * 0.45f && _leftTouchId == -1)
                {
                    _leftTouchId = id;
                    UpdateJoystick(x, y);
                }
                else if (_fireButton.Contains(x, y) && _rightTouchId == -1)
                {
                    _rightTouchId = id;
                    Fire();
                }

                break;
            }
            case MotionEventActions.Move:
            {
                for (var i = 0; i < e.PointerCount; i++)
                {
                    var pid = e.GetPointerId(i);
                    var x = e.GetX(i);
                    var y = e.GetY(i);

                    if (pid == _leftTouchId)
                    {
                        UpdateJoystick(x, y);
                    }

                    if (pid == _rightTouchId && _fireButton.Contains(x, y))
                    {
                        Fire();
                    }
                }

                break;
            }
            case MotionEventActions.Up:
            case MotionEventActions.PointerUp:
            case MotionEventActions.Cancel:
            {
                if (id == _leftTouchId)
                {
                    _leftTouchId = -1;
                    _moveInput = 0;
                    _joyKnob = new PointF(_joyCenter.X, _joyCenter.Y);
                }

                if (id == _rightTouchId)
                {
                    _rightTouchId = -1;
                }

                break;
            }
        }

        return true;
    }

    private void UpdateJoystick(float x, float y)
    {
        var dx = x - _joyCenter.X;
        var dy = y - _joyCenter.Y;
        var len = MathF.Sqrt(dx * dx + dy * dy);
        const float max = 90f;

        if (len > max)
        {
            dx = dx / len * max;
            dy = dy / len * max;
        }

        _joyKnob = new PointF(_joyCenter.X + dx, _joyCenter.Y + dy);
        _moveInput = Math.Clamp(dx / max, -1f, 1f);

        if (dy < -65 && _onGround)
        {
            _heroVelocityY = -20f;
            _onGround = false;
        }
    }

    private void Fire()
    {
        if (_cooldown > 0) return;
        _cooldown = 10;
        _shots.Add(new RectF(_hero.Right, _hero.CenterY() - 6, _hero.Right + 20, _hero.CenterY() + 6));
    }

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);

        _bgPaint.SetShader(new LinearGradient(0, 0, 0, Height, Color.Rgb(30, 60, 90), Color.Rgb(20, 80, 40), Shader.TileMode.Clamp));
        canvas.DrawRect(0, 0, Width, Height, _bgPaint);

        // Menu hint
        canvas.DrawText("Android: gałka po lewej, strzał po prawej, BACK = menu", 36, 54, _hudPaint);

        var saved = canvas.Save();
        canvas.Translate(-_cameraX, 0);

        canvas.DrawRect(0, Height - 110, 6000, Height, _platformPaint);
        canvas.DrawRect(600, Height - 250, 260, 24, _platformPaint);
        canvas.DrawRect(1200, Height - 320, 300, 24, _platformPaint);
        canvas.DrawRect(1800, Height - 280, 260, 24, _platformPaint);

        // hero (prosty humanoid 3D-like)
        canvas.DrawRoundRect(new RectF(_hero.Left + 8, _hero.Top + 38, _hero.Right - 8, _hero.Bottom), 14, 14, _heroPaint);
        var head = new RectF(_hero.Left + 14, _hero.Top, _hero.Right - 14, _hero.Top + 44);
        canvas.DrawOval(head, _heroPaint);
        var hair = new Paint { Color = Color.Rgb(220, 190, 95), AntiAlias = true };
        canvas.DrawArc(new RectF(head.Left - 4, head.Top - 4, head.Right + 4, head.Bottom - 10), 180, 180, true, hair);

        // enemy
        canvas.DrawRoundRect(new RectF(_enemy.Left + 8, _enemy.Top + 40, _enemy.Right - 8, _enemy.Bottom), 12, 12, _enemyPaint);
        canvas.DrawOval(new RectF(_enemy.Left + 12, _enemy.Top + 2, _enemy.Right - 12, _enemy.Top + 44), _enemyPaint);

        foreach (var s in _shots)
        {
            canvas.DrawOval(s, _projectilePaint);
        }

        canvas.RestoreToCount(saved);

        // HUD
        canvas.DrawText($"HP: {_health}", 36, 102, _hudPaint);

        // Controls
        canvas.DrawCircle(_joyCenter.X, _joyCenter.Y, 90, _joystickPaint);
        canvas.DrawCircle(_joyKnob.X, _joyKnob.Y, 44, _joystickPaint);
        canvas.DrawRoundRect(_fireButton, 30, 30, _buttonPaint);
        canvas.DrawText("STRZAŁ", _fireButton.Left + 24, _fireButton.CenterY() + 12, _hudPaint);
    }

    private sealed class Runnable : Java.Lang.Object, IRunnable
    {
        private readonly Action _action;
        public Runnable(Action action) => _action = action;
        public void Run() => _action();
    }
}
