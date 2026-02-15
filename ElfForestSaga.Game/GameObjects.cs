using System.Drawing;

namespace ElfForestSaga.Game;

public enum WeaponType
{
    Bow,
    Fire
}

public enum EnemyType
{
    Orc,
    Goblin,
    Zombie,
    ElderWizard,
    Dragon
}

public enum PickupType
{
    RapidFire,
    Health
}

public sealed class Player
{
    public RectangleF Bounds;
    public float VelocityX;
    public float VelocityY;
    public int Health = 100;
    public WeaponType SelectedWeapon = WeaponType.Bow;
    public int FireCooldownTicks = 12;
    public int JumpPower = 18;
}

public sealed class Platform
{
    public RectangleF Bounds;
}

public sealed class Enemy
{
    public EnemyType Type;
    public RectangleF Bounds;
    public float PatrolStart;
    public float PatrolEnd;
    public float Speed;
    public bool MovingRight;
    public int Health;
    public int Damage;
    public float AnimationPhase;
    public int AttackCooldown;
    public bool IsAttacking;
}

public sealed class Projectile
{
    public RectangleF Bounds;
    public float VelocityX;
    public float VelocityY;
    public WeaponType Weapon;
    public int Damage;
}

public sealed class EnemyProjectile
{
    public RectangleF Bounds;
    public float VelocityX;
    public float VelocityY;
    public int Damage;
}

public sealed class Pickup
{
    public PickupType Type;
    public RectangleF Bounds;
    public bool Collected;
}

public sealed class Level
{
    public int Number;
    public int Width;
    public List<Platform> Platforms = new();
    public List<Enemy> Enemies = new();
    public List<Pickup> Pickups = new();
}
