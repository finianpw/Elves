using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace ElfForestSaga.Android;

[Activity(
    Label = "Elf Forest Saga Mobile",
    MainLauncher = true,
    Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
    ScreenOrientation = ScreenOrientation.Landscape,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
public sealed class MainActivity : Activity
{
    private FrameLayout? _root;
    private GameView? _gameView;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        _root = new FrameLayout(this)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        var background = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical,
            Gravity = GravityFlags.Center,
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        background.SetBackgroundColor(global::Android.Graphics.Color.Rgb(14, 26, 20));

        var title = new TextView(this)
        {
            Text = "ELF FOREST SAGA\nANDROID EDITION",
            Gravity = GravityFlags.Center,
            TextSize = 30
        };
        title.SetTextColor(global::Android.Graphics.Color.Rgb(235, 229, 198));

        var play = new Button(this) { Text = "Start Gry" };
        play.Click += (_, _) => StartGame();

        var exit = new Button(this) { Text = "Wyjście" };
        exit.Click += (_, _) => Finish();

        background.AddView(title);
        background.AddView(play, new LinearLayout.LayoutParams(420, ViewGroup.LayoutParams.WrapContent) { TopMargin = 24 });
        background.AddView(exit, new LinearLayout.LayoutParams(420, ViewGroup.LayoutParams.WrapContent) { TopMargin = 12 });

        _root.AddView(background);
        SetContentView(_root);
    }

    private void StartGame()
    {
        _gameView = new GameView(this, ShowMainMenu);
        SetContentView(_gameView);
    }

    public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent? e)
    {
        if (keyCode == Keycode.Back && _gameView is not null)
        {
            ShowMainMenu();
            return true;
        }

        return base.OnKeyDown(keyCode, e);
    }
}
