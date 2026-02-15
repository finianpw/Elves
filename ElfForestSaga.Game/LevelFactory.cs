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
                Width = 3200 + (i * 50)
            };

            level.Platforms.Add(new Platform { Bounds = new(0, 640, level.Width, 120) });
            for (var p = 0; p < 12; p++)
            {
                var x = 180 + (p * 250) + ((i * 23 + p * 41) % 120);
                var y = 520 - (p % 4) * 65;
                level.Platforms.Add(new Platform { Bounds = new(x, y, 170, 22) });
            }

            for (var e = 0; e < 8 + i / 2; e++)
            {
                var type = (EnemyType)((e + i) % 4);
                var x = 280 + (e * 260);
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
                    EnemyType.Orc => 1.7f,
                    EnemyType.Zombie => 1.2f,
                    _ => 2.2f
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

            level.Pickups.Add(new Pickup
            {
                Type = PickupType.Health,
                Bounds = new(500 + i * 30, 470, 24, 24)
            });
            level.Pickups.Add(new Pickup
            {
                Type = PickupType.RapidFire,
                Bounds = new(1200 + i * 20, 420, 24, 24)
            });

            levels.Add(level);
        }

        return levels;
    }
}
