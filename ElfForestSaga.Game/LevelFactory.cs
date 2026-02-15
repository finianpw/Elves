namespace ElfForestSaga.Game;

public static class LevelFactory
{
    public static List<Level> BuildCampaign()
    {
        var levels = new List<Level>();
        for (var i = 1; i <= 21; i++)
        {
            if (i == 21)
            {
                levels.Add(BuildDragonFinaleLevel(i));
                continue;
            }

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

            var enemiesToSpawn = i == 20 ? 7 : 9 + i / 2;
            for (var e = 0; e < enemiesToSpawn; e++)
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

            if (i == 20)
            {
                level.Enemies.Add(new Enemy
                {
                    Type = EnemyType.Dragon,
                    Bounds = new(level.Width - 520, 532, 160, 108),
                    PatrolStart = level.Width - 700,
                    PatrolEnd = level.Width - 280,
                    Speed = 1.8f,
                    Health = 340,
                    Damage = 16,
                    MovingRight = false,
                    AttackCooldown = 40
                });
            }

            level.Pickups.Add(new Pickup { Type = PickupType.Health, Bounds = new(500 + i * 35, 470, 24, 24) });
            level.Pickups.Add(new Pickup { Type = PickupType.RapidFire, Bounds = new(1160 + i * 28, 420, 24, 24) });
            level.Pickups.Add(new Pickup { Type = PickupType.Health, Bounds = new(2400 + i * 18, 390, 24, 24) });

            if (i == 20)
            {
                level.Pickups.Add(new Pickup { Type = PickupType.Health, Bounds = new(level.Width - 860, 500, 28, 28) });
                level.Pickups.Add(new Pickup { Type = PickupType.RapidFire, Bounds = new(level.Width - 910, 500, 28, 28) });
            }

            levels.Add(level);
        }

        return levels;
    }

    private static Level BuildDragonFinaleLevel(int number)
    {
        var level = new Level
        {
            Number = number,
            Width = 4200
        };

        level.Platforms.Add(new Platform { Bounds = new(0, 640, level.Width, 120) });
        level.Platforms.Add(new Platform { Bounds = new(500, 560, 3200, 22) });
        level.Platforms.Add(new Platform { Bounds = new(1000, 500, 800, 20) });
        level.Platforms.Add(new Platform { Bounds = new(2400, 500, 900, 20) });

        level.Enemies.Add(new Enemy
        {
            Type = EnemyType.Dragon,
            Bounds = new(level.Width - 920, 470, 260, 170),
            PatrolStart = level.Width - 1400,
            PatrolEnd = level.Width - 420,
            Speed = 2.2f,
            Health = 680,
            Damage = 22,
            AttackCooldown = 25,
            MovingRight = false
        });

        level.Pickups.Add(new Pickup { Type = PickupType.Health, Bounds = new(level.Width - 1500, 450, 30, 30) });
        level.Pickups.Add(new Pickup { Type = PickupType.RapidFire, Bounds = new(level.Width - 1560, 450, 30, 30) });
        return level;
    }
}
