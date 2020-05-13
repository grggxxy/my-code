public enum BulletType
{
    Normal = 0,
    Big = 1,
    TankShell = 2,
}

public class BulletInfo
{
    public uint BulletID { get; private set; } = 0;
    public BulletType Type { get; set; } = BulletType.Normal;
    public byte ShooterID { get; set; } = 0;

    static uint sm_bulletCount = 0;

    public BulletInfo()
    {
        BulletID = sm_bulletCount++;
    }

    public BulletInfo(uint bulletID)
    {
        BulletID = bulletID;
    }
}
