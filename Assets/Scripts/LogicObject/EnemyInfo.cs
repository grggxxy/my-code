public enum EnemyType
{
    Shooting = 0,
    Melee = 1,
    SelfDestruct = 2,
}

public class EnemyInfo
{
    public EnemyType Type { get; set; }
    public uint EnemyID { get; set; }
    private int m_hp;
    public int Hp
    {
        get => m_hp;

        set
        {
            if (MaxHp < 0) {
                MaxHp = value;
            }
            m_hp = value;
        }
    }
    public int MaxHp { get; set; } = -1;
}
