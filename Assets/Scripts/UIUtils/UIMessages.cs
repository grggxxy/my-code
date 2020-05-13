using System;

enum UIMessage
{
    JoinGameClicked = 0,
    EnmeyDamaged,
    UpdateCoolDownProgressUI,
    UpdateHpUI,
    UpdateBulletUI,
    UpdateEnemyHpSlider,
    UpdateKills,
    UpdateRebornProgress,
    ShowRebornProgress,
    HideRebornProgress,

    ShowGameResult,
    HideGameResult,

    ShowTimeCountDown,
    HideTimeCountDown,
    UpdateTimeCountDown,

    StrongPointDamaged,
    UpdateStrongPointHpSlider,

    UpdateTrapCount,

    UpdateGold,

    UpdateGrenate,

    HideStartButton,
}


struct EnemyDamageMessage
{
    public uint EnemyID { get; set; }
    public int Damage { get; set; }
}

struct BulletUpdateMessage
{
    public Int16 CurrentBulletCount { get; set; }
    public Int16 MaxBulletCount { get; set; }
}

struct EnemyHpUpdateMessage
{
    public uint EnemyID { get; set; }
    public float Progress { get; set; }
}

struct TrapCountUpdateMessage
{
    public byte DamageTrapCount { get; set; }
    public byte SlowTrapCount { get; set; }
}
