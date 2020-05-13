public class EnemyBulletInfo
{
    public uint BulletID { get; set; }

    uint m_bulletCount = 0;

    public EnemyBulletInfo()
    {
        this.BulletID = m_bulletCount++;
    }
}
