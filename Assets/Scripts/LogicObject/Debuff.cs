public enum DebuffType
{
    None = 0x00,
    Dizzy = 0x01,
    SlowDown = 0x02,
    Damage = 0x03,
}

class Debuff
{
    public DebuffType DeBuffType { get; set; }
    public float Duration { get; set; }
}
