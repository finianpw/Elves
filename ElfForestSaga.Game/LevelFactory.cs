namespace ElfForestSaga.Game;

public static class LevelFactory
{
    public static List<Level> BuildCampaign()
    {
        var levels = new List<Level>();
        for (var i = 1; i <= 20; i++)
        {
            var level = new Level
            {
                Number = i,
                Width = 3400 + (i * 70)
            };

            level.Platforms.Add(new Platform { Bounds = new(0, 640, level.Width, 120) });

            for (var p = 0; p < 12; p++)
            {
                var x = 200 + (p * 270) + ((i * 31 + p * 37) % 120);
                var y = 520 - (p % 4) * 62;
                level.Platforms.Add(new Platform { Bounds = new(x, y, 180, 24) });

                if (p % 3 == 1)
                {
                    level.Platforms.Add(new Platform { Bounds = new(x + 80, y - 90, 140, 18) });
                }
            }

            for (var bridge = 0; bridge < 4; bridge++)
            {
                var bx = 820 + bridge * 760 + (i % 2) * 50;
                level.Platforms.Add(new Platform { Bounds = new(bx, 560, 220, 14) });
            }

            for (var e = 0; e < 9 + i / 2; e++)
            {
                var type = (EnemyType)((e + i) % 4);
                var x = 280 + (e * 250);
                var y = 590;
                var hp = type switch
                {
                    EnemyType.Orc => 26,
                    EnemyType.Goblin => 18,
                    EnemyType.Zombie => 24,
                    _ => 32
                };

                var speed = type switch
                {
                    EnemyType.Goblin => 2.6f,
                    EnemyType.Orc => 1.8f,
                    EnemyType.Zombie => 1.2f,
                    _ => 2.1f
                };

                level.Enemies.Add(new Enemy
                {
                    Type = type,
                    Bounds = new(x, y, 44, 50),
                    PatrolStart = x - 120,
                    PatrolEnd = x + 120,
                    Speed = speed,
                    Health = hp + (i / 3),
                    Damage = 5 + (i / 5)
                });
            }

            level.Pickups.Add(new Pickup { Type = PickupType.Health, Bounds = new(500 + i * 35, 470, 24, 24) });
            level.Pickups.Add(new Pickup { Type = PickupType.RapidFire, Bounds = new(1160 + i * 28, 420, 24, 24) });
            level.Pickups.Add(new Pickup { Type = PickupType.Health, Bounds = new(2400 + i * 18, 390, 24, 24) });

            levels.Add(level);
        }

        return levels;
    }
}
