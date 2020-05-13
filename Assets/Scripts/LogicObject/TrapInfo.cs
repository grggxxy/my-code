public enum TrapType
{
    SlowDown,
    Damage,
}


public class TrapInfo
{
    public uint ID { get; set; }
    public TrapType TrapType { get; set; }
}