using System;
using System.Collections.Generic;

public enum PlayerType
{
    Heavy = 0,
    // HandGun,
}

public enum ShoottingType
{
    Normal = 0,
    Running,
    // Fan,
}

public class PlayerInfo
{
    public static readonly PlayerInfo LocalPlayer = new PlayerInfo();
    public static readonly List<PlayerInfo> PlayerInfos = new List<PlayerInfo>();

    public static bool IsHost { get; set; } = false;

    public static void AddPlayer(PlayerInfo info)
    {
        PlayerInfos.Add(info);
    }

    public static void RemovePlayer(PlayerInfo info)
    {
        PlayerInfos.Remove(info);
    }

    public static void Clear()
    {
        PlayerInfos.Clear();
    }

    public byte UserID { get; set; } = 0;
    public PlayerType Type { get; set; } = PlayerType.Heavy;
    public Int16 CurrentBulletCapacity { get; set; } = 0;
    public Int16 MaxBulletCapacity { get; set; } = 0;
    public int Hp { get; set; } = 0;
    public ShoottingType ShootingType { get; set; } = ShoottingType.Normal;
    public uint Kills { get; set; } = 0;

    public int Gold { get; set; } = 0;
    public byte DamageTrapCount { get; set; } = 0;
    public byte SlowTrapCount { get; set; } = 0;

    public byte GrenadeCount { get; set; } = 0;
}
