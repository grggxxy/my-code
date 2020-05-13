namespace GameConfigures
{
    public class HeaveyWeaponConfigure
    {
        public float BulletSpeed { get; } = 20.0f;
        public float BulletDuration { get; } = 5.0f;
        public float ShootDeltaTime { get; } = 0.1f;
        public uint MaxCapacityPerCharger { get; } = 30;
        public uint MaxCapacity { get; } = 900;
    }

    // public class HandWeaponConfigure
    // {
    // }

    public class WeaponConfigure
    {
        public static HeaveyWeaponConfigure HeaveyWeaponConfigure { get; } = new HeaveyWeaponConfigure();
        // public static HandWeaponConfigure HandWeaponConfigure { get; } = new HandWeaponConfigure();
    }
}
