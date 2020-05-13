public enum ItemType
{
    Recover = 0,
    SupplyBullets = 1,
    RunningShooting = 2,
    // FanShooting,
}

public class ItemInfo
{
    public uint ID { get; set; }
    public ItemType Type { get; set; }
}
