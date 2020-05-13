class TankInfo
{
    public byte DriverID { get; set; } = 0xff;
    public bool IsDriven { get; set; } = false;
    public int Hp { get; set; } = 0;

    public static readonly TankInfo Instance = new TankInfo();

    private TankInfo()
    {
    }
}
