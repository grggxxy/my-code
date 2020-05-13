using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/*
Command Memory Layout
    2byte - command length
    1byte - userid
    1byte - command id

    byte[] parameter body
*/

public enum NetWorkCommandType
{
    Move = 0x00,
    PlayerList = 0x01,
    NewPlayer = 0x02,
    Leave = 0x03,
    LogInRequest = 0x04,
    LogInResult = 0x05,
    Join = 0x06,
    InitPlayerInfo = 0x07,
    PlayerListResult = 0x08,
    Shoot = 0x09,
    ShootResult = 0x0a,
    BulletMove = 0x0b,
    BulletDestroy = 0x0c,
    PlayerRotate = 0x0d,
    GenerateEnemy = 0x0e,
    EnemyShoot = 0x0f,
    BulletHit = 0x10,
    EnemyBulletHit = 0x11,
    EnemyBulletDestroy = 0x12,
    GenerateItem = 0x13,
    PickUpItem = 0x14,
    EnemyDie = 0x15,
    PlayerDie = 0x16,
    EnemyMove = 0x17,
    EnemyList = 0x18,
    EnemyListResult = 0x19,
    BulletHitResult = 0x1a,
    Recharge = 0x1b,
    RechargeResult = 0x1c,
    DebuffAttach = 0x1d,
    DebuffRemove = 0x1e,
    EnemyAttack = 0x1f,
    EnemyAttackResult = 0x20,
    PickUpItemResult = 0x21,
    RecoverHp = 0x22,
    SupplyBullets = 0x23,
    RunningShooting = 0x24,
    FanShooting = 0x25,
    ItemList = 0x26,
    ItemListResult = 0x27,
    UpdatePlayerKillCount = 0x28,
    EnemyBulletHitResult = 0x29,
    PlayerReborn = 0x2a,
    PlayerRebornResult = 0x2b,
    Register = 0x2c,
    RegisterResult = 0x2d,
    LeaveResult = 0x2e,
    GunShoot = 0x2f,
    GunShootResult = 0x30,
    EnemyGunShoot = 0x31,
    EnemyGunShootResult = 0x32,
    GameResult = 0x33,
    SelfDestruct = 0x34,
    SelfDestructResult = 0x35,
    Explode = 0x36,
    ExplodeResult = 0x37,
    PutTrap = 0x38,
    PutTrapResult = 0x39,
    EnemyDebuffDamaged = 0x3a,
    EnemyDebuffDamagedResult = 0x3b,
    DebuffAttachResult = 0x3c,
    DebuffRemoveResult = 0x3d,
    TrapList = 0x3e,
    TrapListResult = 0x3f,

    TankMove = 0x40,
    TankBodyRotation = 0x41,
    TankTurretRotate = 0x42,
    // TankShoot = 0x43,
    // TankShootResult = 0x44,
    DriveTank = 0x45,
    RequireTankInfo = 0x46,
    RequireTankInfoResult = 0x47,
    TankDestroy = 0x48,
    DriveTankResult = 0x49,
    WaveOver = 0x4a,
    NewWave = 0x4b,

    NewGame = 0x4c,
    NewGameResult = 0x4d,

    RequireStrongPointInfo = 0x4e,
    RequireStrongPointInfoResult = 0x4f,

    StrongPointAttacked = 0x50,
    StrongPointAttackedResult = 0x51,

    BuyTrap = 0x52,
    BuyTrapResult = 0x53,

    UpdateGold = 0x54,

    BuyGrenate = 0x55,

    BuyGrenateResult = 0x56,
}

public abstract class NetworkCommand
{
    private static readonly Dictionary<NetWorkCommandType, Type> _CommandMap = new Dictionary<NetWorkCommandType, Type>();
    private static readonly Dictionary<int, NetWorkCommandType> _CommandMapReverse = new Dictionary<int, NetWorkCommandType>();
    private static readonly HashSet<NetWorkCommandType> _SendableCommandSet = new HashSet<NetWorkCommandType>();

    public static bool IsInitialized { get; private set; } = false;

    public NetWorkCommandType CommandType { get; private set; }

    private NetworkCommandBuffer m_buffer = new NetworkCommandBuffer();
    private byte m_userID;

    public NetworkCommand(byte userID)
    {
        m_userID = userID;
        var code = this.GetType().GetHashCode();
        this.CommandType = _CommandMapReverse[code];
    }

    public static NetworkCommand ParseFromByteArraySegment(ArraySegment<byte> buffer)
    {
        var cmdType = (NetWorkCommandType)buffer.Array[buffer.Offset + 3];

        if (_CommandMap.ContainsKey(cmdType))
        {
            var cls = _CommandMap[cmdType];

            var cmd = System.Activator.CreateInstance(cls) as NetworkCommand;
            cmd.Parse(buffer);
            return cmd;
        }

        throw new Exception($"Invalid command type recieved {cmdType}");
    }

    public static CommandUsage GetCommandUsage<T>() where T : NetworkCommand
    {
        Type type = typeof(T);
        if (type.IsDefined(typeof(NetworkCommandInfoAttribute), false))
        {
            var attribute = type.GetCustomAttributes(typeof(NetworkCommandInfoAttribute), false)[0] as NetworkCommandInfoAttribute;
            return attribute.Usage;
        }
        throw new Exception("Wrong network command");
    }

    public static NetWorkCommandType GetCommandType<T>() where T : NetworkCommand
    {
        Type type = typeof(T);
        if (type.IsDefined(typeof(NetworkCommandInfoAttribute), false))
        {
            var attribute = type.GetCustomAttributes(typeof(NetworkCommandInfoAttribute), false)[0] as NetworkCommandInfoAttribute;
            return attribute.CommandType;
        }
        throw new Exception("Wrong network command");
    }

    public static void Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        IsInitialized = true;

        System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
        System.Type[] types = asm.GetTypes();
        Func<Attribute[], NetworkCommandInfoAttribute> GetAttribute = o =>
        {
            foreach (Attribute a in o)
            {
                if (a is NetworkCommandInfoAttribute)
                    return a as NetworkCommandInfoAttribute;
            }
            return null;
        };

        foreach (var type in types)
        {
            var attr = GetAttribute(System.Attribute.GetCustomAttributes(type, false));
            if (attr != null)
            {
                if (_CommandMap.ContainsKey(attr.CommandType))
                {
                    throw new Exception("Different command has the same command type.");
                }
                else
                {
                    if (attr.Usage == CommandUsage.DualWay)
                    {
                        _CommandMap[attr.CommandType] = type;
                        _CommandMapReverse[type.GetHashCode()] = attr.CommandType;
                        _SendableCommandSet.Add(attr.CommandType);
                    }
                    else if (attr.Usage == CommandUsage.RecieveOnly)
                    {
                        _CommandMap[attr.CommandType] = type;
                        _CommandMapReverse[type.GetHashCode()] = attr.CommandType;
                    }
                    else if (attr.Usage == CommandUsage.SendOnly)
                    {
                        _SendableCommandSet.Add(attr.CommandType);
                        _CommandMapReverse[type.GetHashCode()] = attr.CommandType;
                    }
                }
            }
        }

        return;
    }

    public virtual void Parse(ArraySegment<byte> buffer)
    {
        throw new Exception("Invaild command recieved.");
    }

    public virtual void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        Debug.Log($"no impl {this.CommandType}");
        throw new NotImplementedException();
    }

    public virtual void FillBuffer(NetworkCommandBuffer buffer)
    {
        Debug.Log($"no impl {this.CommandType}");
        throw new NotImplementedException();
    }

    public byte[] Format()
    {
        if (_SendableCommandSet.Contains(this.CommandType))
        {
            m_buffer = new NetworkCommandBuffer();

            this.CalcBufferSize(m_buffer);

            m_buffer.GenerateBuffer();

            m_buffer.Put((short)(m_buffer.Size));
            m_buffer.Put(this.m_userID);
            m_buffer.Put((byte)this.CommandType);

            this.FillBuffer(m_buffer);

            return m_buffer.GetBuffer();
        }
        throw new Exception("Invaild command generated.");
    }
}


[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.Move)]
public class MoveCommand : NetworkCommand
{
    public Vector3 TargetPosition { get; private set; }
    public float Rotation { get; private set; }
    public byte UserID { get; private set; }
    public KeyBoardMoveDescriptor KeyBoardMoveDescriptor { get; private set; }
    public bool IsJump { get; private set; }

    public MoveCommand(byte userID, Vector3 targetPosition, float rotation, Int32 keyboardDirection, bool isJump)
    : base(userID)
    {
        this.UserID = userID;
        this.TargetPosition = targetPosition;
        this.Rotation = rotation;
        this.KeyBoardMoveDescriptor = (KeyBoardMoveDescriptor)keyboardDirection;
        this.IsJump = IsJump;
    }

    public MoveCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Float * 3);
        buffer.Extends(TypeSize.Float);
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.UserID);
        buffer.Put(this.TargetPosition);
        buffer.Put(this.Rotation);
        buffer.Put((byte)this.KeyBoardMoveDescriptor);
        buffer.Put((byte)(this.IsJump ? 1 : 0));
    }


    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.UserID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.TargetPosition = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
        this.Rotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
        this.KeyBoardMoveDescriptor = (KeyBoardMoveDescriptor)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.IsJump = NetworkCommandBuffer.GetByte(buffer.Array, ref offset) == 1 ? true : false;
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.PlayerList)]
public class PlayerListCommand : NetworkCommand
{
    public PlayerListCommand(byte userID) : base(userID)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put((byte)0);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.LogInRequest)]
public class LoginRequestCommand : NetworkCommand
{
    public string UserName { get; private set; }
    public string Password { get; private set; }

    public LoginRequestCommand(string userName, string password) :
        base(0)
    {
        this.UserName = userName;
        this.Password = password;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(2 + System.Text.Encoding.UTF8.GetByteCount(this.UserName));
        buffer.Extends(2 + System.Text.Encoding.UTF8.GetByteCount(this.Password));
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.UserName);
        buffer.Put(this.Password);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.LogInResult)]
public class LoginResultCommand : NetworkCommand
{
    public bool Result { get; private set; }
    public byte UserID { get; private set; }

    public LoginResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var result = buffer.Array[buffer.Offset + 4];
        var userID = buffer.Array[buffer.Offset + 5];

        this.Result = result == 0 ? false : true;
        this.UserID = userID;
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.Join)]
class JoinCommand : NetworkCommand
{
    public JoinCommand(byte userID) :
        base(userID)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put((byte)0);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.InitPlayerInfo)]
class InitPlayerInfoCommand : NetworkCommand
{
    public PlayerType Type { get; private set; } = PlayerType.Heavy;
    public Int16 CurrentBullet { get; private set; } = 0;
    public Int16 MaxBullet { get; private set; } = 0;
    public Vector3 InitialPosition { get; private set; }
    public float InitialRotation { get; private set; } = 0.0f;
    public int Hp { get; private set; } = 0;
    public ShoottingType ShoottingType { get; private set; }
    public uint Kills { get; private set; }
    public bool IsHost { get; private set; }

    public Int32 Gold { get; private set; }
    public byte DamageTrapCount { get; private set; }
    public byte SlowTrapCount { get; private set; }
    public byte GrenadeCount { get; private set; }

    public InitPlayerInfoCommand() :
        base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.CurrentBullet = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
        this.MaxBullet = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
        this.Type = (PlayerType)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);

        this.InitialPosition = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
        this.InitialRotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
        this.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.ShoottingType = (ShoottingType)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Kills = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);

        this.Gold = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.DamageTrapCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.SlowTrapCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);

        this.GrenadeCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);

        this.IsHost = NetworkCommandBuffer.GetByte(buffer.Array, ref offset) == 0 ? false : true;
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.NewPlayer)]
class NewPlayerCommand : NetworkCommand
{
    public PlayerType Type { get; private set; } = PlayerType.Heavy;
    public Int16 CurrentBullet { get; private set; } = 0;
    public Int16 MaxBullet { get; private set; } = 0;
    public Vector3 InitialPosition { get; private set; }
    public float InitialRotation { get; private set; } = 0.0f;
    public int Hp { get; private set; } = 0;
    public ShoottingType ShoottingType { get; private set; }
    public uint Kills { get; private set; } = 0;

    public byte UserID { get; private set; }

    public NewPlayerCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.CurrentBullet = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
        this.MaxBullet = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
        this.Type = (PlayerType)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.UserID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);

        this.InitialPosition = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
        this.InitialRotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
        this.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.ShoottingType = (ShoottingType)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Kills = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.PlayerListResult)]
class PlayerListResultCommand : NetworkCommand
{
    public List<PlayerInfo> PlayerInfo { get; private set; } = new List<PlayerInfo>();
    public List<Vector3> PlayerPositions { get; private set; } = new List<Vector3>();
    public List<float> PlayerRotations { get; private set; } = new List<float>();

    public PlayerListResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        int playerCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        for (int i = 0; i < playerCount; ++i)
        {
            var playerInfo = new PlayerInfo();
            playerInfo.CurrentBulletCapacity = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
            playerInfo.MaxBulletCapacity = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
            playerInfo.Type = (PlayerType)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
            playerInfo.UserID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);

            var pos = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
            PlayerPositions.Add(pos);

            var rot = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
            PlayerRotations.Add(rot);

            playerInfo.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
            playerInfo.ShootingType = (ShoottingType)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
            playerInfo.Kills = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
            PlayerInfo.Add(playerInfo);
        }
    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.Shoot)]
class ShootCommand : NetworkCommand
{
    public byte ShooterID { get; private set; }
    public List<BulletInfo> BulletInfos { get; private set; }
    public List<Vector3> BulletPositions { get; private set; }
    public List<float> BulletRotations { get; private set; }

    public byte UserID { get; private set; }

    public bool IsGranate { get; private set; }

    public ShootCommand(byte userID, List<Vector3> bulletPositions, List<float> bulletRotations, List<BulletInfo> bulletInfos, bool isGranate) :
        base(userID)
    {
        this.UserID = userID;
        this.BulletPositions = bulletPositions;
        this.BulletRotations = bulletRotations;
        this.BulletInfos = bulletInfos;
        this.IsGranate = isGranate;
    }

    public ShootCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        var count = this.BulletPositions.Count;
        buffer.Extends(TypeSize.Byte);

        buffer.Extends(TypeSize.Byte);

        buffer.Extends(
            count *
            (
                TypeSize.Float * 3
                + TypeSize.Float
                + TypeSize.Byte
                + TypeSize.UInt32
            )
        );
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.UserID);
        var count = this.BulletPositions.Count;
        buffer.Put((byte)count);

        buffer.Put((byte)(this.IsGranate ? 1 : 0));

        for (int i = 0; i < count; ++i)
        {
            buffer.Put(this.BulletPositions[i]);
            buffer.Put(this.BulletRotations[i]);
            buffer.Put((byte)this.BulletInfos[i].Type);
            buffer.Put(this.BulletInfos[i].BulletID);
        }

    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.ShooterID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        var count = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);

        this.IsGranate = NetworkCommandBuffer.GetByte(buffer.Array, ref offset) == 1;

        this.BulletInfos = new List<BulletInfo>();
        this.BulletPositions = new List<Vector3>();
        this.BulletRotations = new List<float>();

        for (int i = 0; i < count; ++i)
        {
            var pos = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
            var rot = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
            var type = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
            var id = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);

            this.BulletPositions.Add(pos);
            this.BulletRotations.Add(rot);
            this.BulletInfos.Add(new BulletInfo(id)
            {
                ShooterID = this.ShooterID,
                Type = (BulletType)type,
            });
        }
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.ShootResult)]
class ShootResultCommand : NetworkCommand
{
    public Int16 CurrentBulletCount { get; private set; }
    public Int16 MaxBulletCount { get; private set; }
    public byte GrenadeCount { get; private set; }

    public ShootResultCommand()
        : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.CurrentBulletCount = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
        this.MaxBulletCount = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
        this.GrenadeCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.BulletMove)]
class BulletMoveCommand : NetworkCommand
{
    public uint BulletID { get; private set; }
    public Vector3 Position { get; private set; }
    public float Rotation { get; private set; }
    public uint ShooterID { get; private set; }

    public BulletMoveCommand(byte userID, uint bulletID, Vector3 position, float rotation) :
        base(userID)
    {
        this.ShooterID = userID;
        this.BulletID = bulletID;
        this.Position = position;
        this.Rotation = rotation;
    }

    public BulletMoveCommand() :
        base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Int32);
        buffer.Extends(TypeSize.Float);
        buffer.Extends(TypeSize.Float);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.ShooterID);
        buffer.Put((Int32)this.BulletID);
        buffer.Put(this.Position);
        buffer.Put(this.Rotation);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.ShooterID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.BulletID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.Position = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
        this.Rotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.BulletDestroy)]
class BulletDestroyCommand : NetworkCommand
{
    public byte ShooterID { get; private set; }
    public uint BulletID { get; private set; }

    public BulletDestroyCommand(byte userID, uint bulletID) :
        base(userID)
    {
        this.ShooterID = userID;
        this.BulletID = bulletID;
    }

    public BulletDestroyCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Int32);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.ShooterID);
        buffer.Put((Int32)this.BulletID);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.ShooterID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.BulletID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.PlayerRotate)]
class PlayerRotateCommand : NetworkCommand
{
    public byte UserID { get; private set; }
    public float Rotation { get; private set; }

    public PlayerRotateCommand(byte userID, float rotation)
        : base(userID)
    {
        this.UserID = userID;
        this.Rotation = rotation;
    }

    public PlayerRotateCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Float);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.UserID);
        buffer.Put(this.Rotation);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.UserID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Rotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.GenerateEnemy)]
class GenerateEnemyCommand : NetworkCommand
{
    public List<uint> EnemyIDs { get; private set; }
    public List<EnemyType> EnemyTypes { get; private set; }
    public List<Vector3> EnemyPositions { get; private set; }
    public List<float> EnemyRotations { get; private set; }
    public List<int> Hps { get; private set; }
    public bool IsHost { get; private set; }

    public GenerateEnemyCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        this.EnemyIDs = new List<uint>();
        this.EnemyTypes = new List<EnemyType>();
        this.EnemyPositions = new List<Vector3>();
        this.EnemyRotations = new List<float>();
        this.Hps = new List<int>();

        var offset = buffer.Offset + 4;
        var enemyCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        for (int i = 0; i < enemyCount; ++i)
        {
            var enemyID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
            var enemyType = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
            var enemyPosition = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
            var enemyRotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
            var hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);

            this.EnemyIDs.Add(enemyID);
            this.EnemyTypes.Add((EnemyType)enemyType);
            this.EnemyPositions.Add(enemyPosition);
            this.EnemyRotations.Add(enemyRotation);
            this.Hps.Add(hp);
        }
        var isHost = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.IsHost = isHost == 0 ? false : true;
    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.EnemyMove)]
class EnemyMoveCommand : NetworkCommand
{
    public uint EnemyID { get; private set; }
    public Vector3 TargetPosition { get; private set; }
    public float Rotation { get; private set; }

    public EnemyMoveCommand(byte userID, uint enemyID, Vector3 targetPosition, float rotation) :
        base(userID)
    {
        this.EnemyID = enemyID;
        this.TargetPosition = targetPosition;
        this.Rotation = rotation;
    }

    public EnemyMoveCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.UInt32);
        buffer.Extends(TypeSize.Float * 3);
        buffer.Extends(TypeSize.Float);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.EnemyID);
        buffer.Put(this.TargetPosition);
        buffer.Put(this.Rotation);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.EnemyID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.TargetPosition = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
        this.Rotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.EnemyShoot)]
class EnemyShootCommand : NetworkCommand
{
    public uint EnemyID { get; private set; }
    public List<EnemyBulletInfo> BulletInfos { get; private set; }
    public List<Vector3> BulletPositions { get; private set; }
    public List<float> BulletRotations { get; private set; }

    public EnemyShootCommand(byte userID, uint enemyID, List<Vector3> bulletPositions, List<float> bulletRotations, List<EnemyBulletInfo> bulletInfos) :
        base(userID)
    {
        this.EnemyID = enemyID;
        this.BulletPositions = bulletPositions;
        this.BulletRotations = bulletRotations;
        this.BulletInfos = bulletInfos;
    }

    public EnemyShootCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.UInt32);
        buffer.Extends(TypeSize.Byte);
        var count = this.BulletPositions.Count;

        buffer.Extends(
            count *
            (
                TypeSize.Float * 3
                + TypeSize.Float
                + TypeSize.UInt32
            )
        );
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.EnemyID);
        buffer.Put((byte)this.BulletPositions.Count);
        for (int i = 0; i < this.BulletPositions.Count; ++i)
        {
            buffer.Put(this.BulletPositions[i]);
            buffer.Put(this.BulletRotations[i]);
            buffer.Put(this.BulletInfos[i].BulletID);
        }
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        this.BulletInfos = new List<EnemyBulletInfo>();
        this.BulletPositions = new List<Vector3>();
        this.BulletRotations = new List<float>();

        var offset = buffer.Offset + 4;

        this.EnemyID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        var count = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        for (int i = 0; i < count; ++i)
        {
            var position = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
            var rotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
            var bulletID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);

            this.BulletInfos.Add(new EnemyBulletInfo()
            {
                BulletID = bulletID,
            });
            this.BulletPositions.Add(position);
            this.BulletRotations.Add(rotation);
        }

    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.EnemyBulletDestroy)]
class EnemyBulletDestroyCommand : NetworkCommand
{
    public uint BulletID { get; private set; }

    public EnemyBulletDestroyCommand(byte userID, uint bulletID) :
        base(userID)
    {
        this.BulletID = bulletID;
    }

    public EnemyBulletDestroyCommand() :
        base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.UInt32);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.BulletID);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.BulletID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.EnemyList)]
class EnemyListCommand : NetworkCommand
{
    public EnemyListCommand(byte userID) : base(userID)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put((byte)0);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.EnemyListResult)]
class EnemyListResultCommand : NetworkCommand
{
    public List<uint> EnemyIDs { get; private set; }
    public List<EnemyType> EnemyTypes { get; private set; }
    public List<Vector3> EnemyPositions { get; private set; }
    public List<float> EnemyRotations { get; private set; }
    public List<int> Hps { get; private set; }
    public bool IsHost { get; private set; }

    public EnemyListResultCommand() :
        base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        this.EnemyIDs = new List<uint>();
        this.EnemyTypes = new List<EnemyType>();
        this.EnemyPositions = new List<Vector3>();
        this.EnemyRotations = new List<float>();
        this.Hps = new List<int>();

        var offset = buffer.Offset + 4;
        var enemyCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        for (int i = 0; i < enemyCount; ++i)
        {
            var enemyID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
            var enemyType = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
            var enemyPosition = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
            var enemyRotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
            var hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);

            this.EnemyIDs.Add(enemyID);
            this.EnemyTypes.Add((EnemyType)enemyType);
            this.EnemyPositions.Add(enemyPosition);
            this.EnemyRotations.Add(enemyRotation);
            this.Hps.Add(hp);
        }
        var isHost = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.IsHost = isHost == 0 ? false : true;
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.BulletHit)]
class BulletHitCommand : NetworkCommand
{
    public byte UserID { get; private set; }
    public BulletType BulletType { get; private set; }
    public uint BulletID { get; private set; }
    public uint HitEnemyID { get; private set; }

    public BulletHitCommand(byte userID, uint bulletID, BulletType bulletType, uint hitEnemyID) :
        base(userID)
    {
        this.UserID = userID;
        this.BulletID = bulletID;
        this.BulletType = bulletType;
        this.HitEnemyID = hitEnemyID;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.UInt32);
        buffer.Extends(TypeSize.UInt32);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.UserID);
        buffer.Put((byte)this.BulletType);
        buffer.Put(this.HitEnemyID);
        buffer.Put(this.BulletID);
    }

}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.BulletHitResult)]
class BulletHitResultCommand : NetworkCommand
{
    public byte ShooterID { get; private set; }
    public uint EnemyID { get; private set; }
    public int EnemyHp { get; private set; }
    public uint BuleltID { get; private set; }

    public BulletHitResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.ShooterID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.EnemyID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.EnemyHp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.BuleltID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.EnemyDie)]
class EnemyDieCommand : NetworkCommand
{
    public byte ShooterID { get; private set; }
    public uint EnemyID { get; private set; }

    public EnemyDieCommand() :
        base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.ShooterID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.EnemyID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.Recharge)]
class RechargeCommand : NetworkCommand
{
    public byte ShooterID { get; private set; }

    public RechargeCommand(byte userID)
        : base(userID)
    {
        this.ShooterID = userID;
    }

    public RechargeCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.ShooterID);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.ShooterID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.RechargeResult)]
class RechargeResultCommand : NetworkCommand
{
    public Int16 CurrentBulletCount { get; private set; }
    public Int16 MaxBulletCount { get; private set; }

    public RechargeResultCommand() :
        base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.CurrentBulletCount = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
        this.MaxBulletCount = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.EnemyAttack)]
class EnemyAttackCommand : NetworkCommand
{
    public byte AttackedUserID { get; private set; }
    public uint AttackingEnemyID { get; private set; }

    public EnemyAttackCommand(byte userID, byte attackedUserID, uint attackingEnemyID)
        : base(userID)
    {
        this.AttackedUserID = attackedUserID;
        this.AttackingEnemyID = attackingEnemyID;
    }

    public EnemyAttackCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.UInt32);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.AttackedUserID);
        buffer.Put(this.AttackingEnemyID);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.AttackedUserID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.AttackingEnemyID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.EnemyAttackResult)]
class EnemyAttackResultCommand : NetworkCommand
{
    public byte AttackedUserID { get; private set; }
    public int Hp { get; private set; }

    public EnemyAttackResultCommand() :
        base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.AttackedUserID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.GenerateItem)]
class GenerateItemCommand : NetworkCommand
{
    public uint ItemID { get; private set; }
    public ItemType ItemType { get; private set; }
    public Vector3 ItemPosition { get; private set; }

    public GenerateItemCommand() :
        base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.ItemID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.ItemType = (ItemType)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.ItemPosition = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.PickUpItem)]
class PickUpItemCommand : NetworkCommand
{
    public byte UserID { get; private set; }
    public uint ItemID { get; private set; }

    public PickUpItemCommand(byte userID, uint itemID) :
        base(userID)
    {
        this.UserID = userID;
        this.ItemID = itemID;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.UInt32);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.UserID);
        buffer.Put(this.ItemID);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.PickUpItemResult)]
class PickUpItemResultCommand : NetworkCommand
{
    public uint ItemID { get; private set; }

    public PickUpItemResultCommand() :
        base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.ItemID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.RecoverHp)]
class RecoverHpCommand : NetworkCommand
{
    public byte UserID { get; private set; }
    public Int32 Hp { get; private set; }

    public RecoverHpCommand() :
        base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.UserID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.SupplyBullets)]
class SupplyBulletsCommand : NetworkCommand
{
    public byte UserID { get; private set; }
    public Int16 CurrentBulletCapacity { get; private set; }
    public Int16 MaxBulletCapacity { get; private set; }

    public SupplyBulletsCommand() :
        base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.UserID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.CurrentBulletCapacity = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
        this.MaxBulletCapacity = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.FanShooting)]
class FanShootingCommand : NetworkCommand
{
    public byte UserID { get; private set; }
    public FanShootingCommand()
        : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.UserID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.RunningShooting)]
class RunningShootingCommand : NetworkCommand
{
    public byte UserID { get; private set; }
    public RunningShootingCommand() :
        base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.UserID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.ItemList)]
class ItemListCommand : NetworkCommand
{
    public ItemListCommand(byte userID) :
        base(userID)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put((byte)0);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.ItemListResult)]
class ItemListResultCommand : NetworkCommand
{
    public List<uint> ItemIDs { get; private set; }
    public List<ItemType> ItemTypes { get; private set; }
    public List<Vector3> ItemPositions { get; private set; }
    public ItemListResultCommand() :
        base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        var itemCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);

        this.ItemIDs = new List<uint>(itemCount);
        this.ItemTypes = new List<ItemType>(itemCount);
        this.ItemPositions = new List<Vector3>(itemCount);

        for (int i = 0; i < itemCount; ++i)
        {
            var id = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
            this.ItemIDs.Add(id);

            var type = (ItemType)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
            this.ItemTypes.Add(type);

            var pos = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
            this.ItemPositions.Add(pos);
        }
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.UpdatePlayerKillCount)]
class UpdatePlayerKillCountCommand : NetworkCommand
{
    // public byte UserID { get; private set; }
    public uint Kills { get; private set; }

    public UpdatePlayerKillCountCommand() :
        base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        // this.UserID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Kills = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.EnemyBulletHit)]
class EnemyBulletHitCommand : NetworkCommand
{
    public byte UserID { get; private set; }
    public uint BulletID { get; private set; }

    public uint EnemyID { get; private set; }

    public EnemyBulletHitCommand(byte userID, uint bulletID, uint enemyID)
        : base(userID)
    {
        this.UserID = userID;
        this.BulletID = bulletID;
        this.EnemyID = enemyID;
    }

    public EnemyBulletHitCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.UInt32);
        buffer.Extends(TypeSize.UInt32);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.UserID);
        buffer.Put(this.EnemyID);
        buffer.Put(this.BulletID);
    }

}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.EnemyBulletHitResult)]
class EnemyBulletHitResultCommand : NetworkCommand
{
    public byte PlayerID { get; private set; }
    public uint EnemyID { get; private set; }
    public int PlayerHp { get; private set; }
    public uint BuleltID { get; private set; }

    public EnemyBulletHitResultCommand()
        : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.PlayerID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.EnemyID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.PlayerHp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.BuleltID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.PlayerDie)]
class PlayerDieCommand : NetworkCommand
{
    public byte PlayerID { get; private set; }
    public PlayerDieCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.PlayerID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.PlayerReborn)]
class PlayerRebornCommand : NetworkCommand
{
    public byte PlayerID { get; private set; }

    public PlayerRebornCommand(byte userID) :
        base(userID)
    {
        this.PlayerID = userID;
    }

    public PlayerRebornCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.PlayerID);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.PlayerID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.PlayerRebornResult)]
class PlayerRebornResultCommand : NetworkCommand
{
    public byte PlayerID { get; private set; }
    public int Hp { get; private set; }
    public Int16 CurBulletCount { get; private set; }
    public Int16 MaxBulletCount { get; private set; }

    public PlayerRebornResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.PlayerID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.CurBulletCount = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
        this.MaxBulletCount = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.Register)]
class RegisterCommand : NetworkCommand
{
    public string UserName { get; private set; }
    public string Password { get; private set; }

    public RegisterCommand(string userName, string password) :
        base(0)
    {
        this.Password = password;
        this.UserName = userName;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(2 + System.Text.Encoding.UTF8.GetByteCount(this.UserName));
        buffer.Extends(2 + System.Text.Encoding.UTF8.GetByteCount(this.Password));
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.UserName);
        buffer.Put(this.Password);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.RegisterResult)]
class RegisterResultCommand : NetworkCommand
{
    public bool Result { get; private set; }

    public RegisterResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.Result = NetworkCommandBuffer.GetByte(buffer.Array, ref offset) == 0;
    }
}


[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.Leave)]
class LeaveCommand : NetworkCommand
{
    public byte PlayerID { get; private set; }
    public LeaveCommand(byte userID) :
        base(userID)
    {
        this.PlayerID = userID;
    }

    public LeaveCommand() :
        base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.PlayerID);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.PlayerID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.LeaveResult)]
class LeaveResultCommand : NetworkCommand
{
    public LeaveResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.GunShoot)]
class GunShootCommand : NetworkCommand
{
    public byte ShooterID { get; private set; }
    public byte BulletCount { get; private set; }
    public Boolean IsHit { get; private set; }
    public UInt32 HitEnemyID { get; private set; }

    public GunShootCommand(byte userID, byte bulletCount, bool isHit, uint hitEnemyID) : base(userID)
    {
        this.ShooterID = userID;
        this.BulletCount = bulletCount;
        this.IsHit = isHit;
        this.HitEnemyID = hitEnemyID;
    }

    public GunShootCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.UInt32);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.ShooterID);
        buffer.Put(this.BulletCount);
        buffer.Put(this.IsHit ? (byte)1 : (byte)0);
        buffer.Put(this.HitEnemyID);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.ShooterID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.BulletCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.IsHit = NetworkCommandBuffer.GetByte(buffer.Array, ref offset) == 0;
        this.HitEnemyID = NetworkCommandBuffer.GetUInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.GunShootResult)]
class GunShootResultCommand : NetworkCommand
{
    public byte ShooterID { get; private set; }
    public uint EnemyID { get; private set; }
    public int EnemyHp { get; private set; }
    public Int16 CurrentBullet { get; private set; }
    public Int16 MaxBullet { get; private set; }
    public bool IsHit { get; private set; }

    public GunShootResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.ShooterID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.EnemyID = NetworkCommandBuffer.GetUInt32(buffer.Array, ref offset);
        this.EnemyHp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.CurrentBullet = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
        this.MaxBullet = NetworkCommandBuffer.GetInt16(buffer.Array, ref offset);
        this.IsHit = NetworkCommandBuffer.GetByte(buffer.Array, ref offset) == 1;
    }
}


[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.EnemyGunShoot)]
class EnemyGunShootCommand : NetworkCommand
{
    public byte HitTargetID { get; private set; }
    public bool IsHit { get; private set; }
    public byte HitType { get; private set; }
    public uint EnemyID { get; private set; }

    public EnemyGunShootCommand(byte userID, uint enmeyID, bool isHit, byte hitType, byte hitTargetID) : base(userID)
    {
        this.HitTargetID = hitTargetID;
        this.IsHit = isHit;
        this.HitType = hitType;
        this.EnemyID = enmeyID;
    }

    public EnemyGunShootCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.UInt32);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.HitTargetID);
        buffer.Put(this.IsHit ? (byte)1 : (byte)0);
        buffer.Put(this.HitType);
        buffer.Put(this.EnemyID);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.HitTargetID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.IsHit = NetworkCommandBuffer.GetByte(buffer.Array, ref offset) == 1;
        this.HitType = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.EnemyID = NetworkCommandBuffer.GetUInt32(buffer.Array, ref offset);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.EnemyGunShootResult)]
class EnemyGunShootResultCommand : NetworkCommand
{
    public uint EnmeyID { get; private set; }
    public byte TargetID { get; private set; }
    public bool IsHit { get; private set; }
    public Int32 Hp { get; private set; }

    public EnemyGunShootResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.EnmeyID = NetworkCommandBuffer.GetUInt32(buffer.Array, ref offset);
        this.TargetID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.IsHit = NetworkCommandBuffer.GetByte(buffer.Array, ref offset) == 1;
        this.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }

}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.SelfDestruct)]
class SelfDestructCommand : NetworkCommand
{
    public uint EnmeyID { get; private set; }
    public Vector3 Position { get; private set; }

    public SelfDestructCommand(byte userID, uint enemyID, Vector3 position) : base(userID)
    {
        this.EnmeyID = enemyID;
        this.Position = position;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.UInt32);
        buffer.Extends(TypeSize.Float * 3);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.EnmeyID);
        buffer.Put(this.Position);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.SelfDestructResult)]
class SelfDestructResultCommand : NetworkCommand
{
    public byte DamagedPlayerID { get; private set; }
    public int Hp { get; private set; }
    public uint EnemyID { get; private set; }

    public SelfDestructResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.DamagedPlayerID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.EnemyID = NetworkCommandBuffer.GetUInt32(buffer.Array, ref offset);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.Explode)]
class ExplodeCommand : NetworkCommand
{
    public byte Thrower { get; private set; }
    public Vector3 Position { get; private set; }
    public uint BulletID { get; private set; }

    public ExplodeCommand(byte userID, Vector3 position, uint bulletID) : base(userID)
    {
        this.Thrower = userID;
        this.Position = position;
        this.BulletID = bulletID;
    }

    public ExplodeCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Float * 3);
        buffer.Extends(TypeSize.UInt32);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.Thrower);
        buffer.Put(this.Position);
        buffer.Put(this.BulletID);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.Thrower = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Position = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
        this.BulletID = NetworkCommandBuffer.GetUInt32(buffer.Array, ref offset);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.ExplodeResult)]
class ExplodeResultCommand : NetworkCommand
{
    public uint DamagedEnemyID { get; private set; }

    public int Hp { get; private set; }

    public ExplodeResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.DamagedEnemyID = NetworkCommandBuffer.GetUInt32(buffer.Array, ref offset);
        this.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.PutTrap)]
class PutTrapCommand : NetworkCommand
{
    public byte BuilderID { get; private set; }
    public Vector3 Position { get; private set; }
    public float Rotation { get; private set; }
    public TrapType TrapType { get; private set; }

    public PutTrapCommand(byte userID, Vector3 position, float rotation, TrapType trapType) : base(userID)
    {
        this.BuilderID = userID;
        this.Position = position;
        this.Rotation = rotation;
        this.TrapType = trapType;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Float * 3);
        buffer.Extends(TypeSize.Float);
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.BuilderID);
        buffer.Put(this.Position);
        buffer.Put(this.Rotation);
        buffer.Put((byte)this.TrapType);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.PutTrapResult)]
class PutTrapResultCommand : NetworkCommand
{
    public byte BuilderID { get; private set; }
    public Vector3 Position { get; private set; }
    public float Rotation { get; private set; }
    public TrapType TrapType { get; private set; }
    public byte TrapCount { get; private set; }
    public uint TrapID { get; private set; }

    public PutTrapResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;

        this.BuilderID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Position = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
        this.Rotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
        this.TrapType = (TrapType)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.TrapCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.TrapID = NetworkCommandBuffer.GetUInt32(buffer.Array, ref offset);
    }

}


[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.EnemyDebuffDamaged)]
class EnemyDebuffDamagedCommand : NetworkCommand
{
    public uint EnemyID { get; private set; }
    public EnemyDebuffDamagedCommand(byte userID, uint enemyID) : base(userID)
    {
        this.EnemyID = enemyID;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.UInt32);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.EnemyID);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.EnemyDebuffDamagedResult)]
class EnemyDebuffDamagedResultCommand : NetworkCommand
{
    public uint EnmeyID { get; private set; }
    public int Hp { get; private set; }

    public EnemyDebuffDamagedResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.EnmeyID = NetworkCommandBuffer.GetUInt32(buffer.Array, ref offset);
        this.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.DebuffAttach)]
class DebuffAttachCommand : NetworkCommand
{
    public uint TargetEnemyID { get; private set; }
    public DebuffType DebuffType { get; private set; }
    public float DebuffDuration { get; private set; }

    public DebuffAttachCommand(byte userID, uint targetEnemyID, DebuffType debuffType)
        : base(userID)
    {
        this.TargetEnemyID = targetEnemyID;
        this.DebuffType = debuffType;
    }

    public DebuffAttachCommand()
        : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.UInt32);
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Float);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.TargetEnemyID);
        buffer.Put((byte)this.DebuffType);
        buffer.Put(0.0f);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.DebuffRemove)]
class DebuffRemoveCommand : NetworkCommand
{
    public uint TargetEnemyID { get; private set; }
    public DebuffType DebuffType { get; private set; }

    public DebuffRemoveCommand(byte userID, uint targetEnemyID, DebuffType DebuffID) :
        base(userID)
    {
        this.TargetEnemyID = targetEnemyID;
        this.DebuffType = DebuffID;
    }

    public DebuffRemoveCommand() : base(0)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.UInt32);
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.TargetEnemyID);
        buffer.Put((byte)this.DebuffType);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.DebuffAttachResult)]
class DebuffAttachResultCommand : NetworkCommand
{
    public uint TargetEnemyID { get; private set; }
    public DebuffType DebuffType { get; private set; }
    public float DebuffDuration { get; private set; }

    public DebuffAttachResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.TargetEnemyID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.DebuffType = (DebuffType)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.DebuffDuration = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.DebuffRemoveResult)]
class DebuffRemoveResultCommand : NetworkCommand
{
    public uint TargetEnemyID { get; private set; }
    public DebuffType DebuffType { get; private set; }

    public DebuffRemoveResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.TargetEnemyID = (uint)NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.DebuffType = (DebuffType)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.TrapList)]
class TrapListCommand : NetworkCommand
{
    public TrapListCommand(byte userID) : base(userID)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put((byte)0);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.TrapListResult)]
class TrapListResultCommand : NetworkCommand
{
    public List<TrapInfo> TrapList { get; private set; }
    public List<Vector3> TrapPositions { get; private set; }
    public List<float> TrapRotations { get; private set; }

    public TrapListResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        this.TrapList = new List<TrapInfo>();
        this.TrapPositions = new List<Vector3>();
        this.TrapRotations = new List<float>();

        var offset = buffer.Offset + 4;

        var count = NetworkCommandBuffer.GetUInt32(buffer.Array, ref offset);

        for (uint i = 0; i < count; ++i)
        {
            var type = (TrapType)NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
            var id = NetworkCommandBuffer.GetUInt32(buffer.Array, ref offset);
            var position = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
            var rotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);

            TrapList.Add(new TrapInfo()
            {
                TrapType = type,
                ID = id,
            });

            TrapPositions.Add(position);
            TrapRotations.Add(rotation);
        }
    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.TankMove)]
class TankMoveCommand : NetworkCommand
{
    public byte DriverID { get; private set; }
    public Vector3 Position { get; private set; }

    public TankMoveCommand() : base(0)
    {
    }

    public TankMoveCommand(byte userID, Vector3 position) : base(userID)
    {
        this.DriverID = userID;
        this.Position = position;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Float * 3);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.DriverID);
        buffer.Put(this.Position);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;

        this.DriverID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Position = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.TankBodyRotation)]
class TankBodyRotationCommand : NetworkCommand
{
    public byte DriverID { get; private set; }
    public float Rotation { get; private set; }

    public TankBodyRotationCommand() : base(0)
    {
    }

    public TankBodyRotationCommand(byte userID, float rotation) : base(userID)
    {
        this.DriverID = userID;
        this.Rotation = rotation;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Float);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.DriverID);
        buffer.Put(this.Rotation);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;

        this.DriverID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Rotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
    }

}

[NetworkCommandInfo(Usage = CommandUsage.DualWay, CommandType = NetWorkCommandType.TankTurretRotate)]
class TankTurretRotateCommand : NetworkCommand
{
    public byte DriverID { get; private set; }
    public float Rotation { get; private set; }

    public TankTurretRotateCommand() : base(0)
    {
    }

    public TankTurretRotateCommand(byte userID, float rotation) : base(userID)
    {
        this.DriverID = userID;
        this.Rotation = rotation;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
        buffer.Extends(TypeSize.Float);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.DriverID);
        buffer.Put(this.Rotation);
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;

        this.DriverID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Rotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.DriveTank)]
class DriveTankCommand : NetworkCommand
{
    public byte DriverID { get; private set; }

    public DriveTankCommand(byte userID) : base(userID)
    {
        this.DriverID = userID;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.DriverID);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.DriveTankResult)]
class DriveTankResultCommand : NetworkCommand
{
    public byte DriverID { get; private set; }

    public DriveTankResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;

        this.DriverID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.RequireTankInfo)]
class RequireTankInfoCommand : NetworkCommand
{
    public RequireTankInfoCommand(byte userID) : base(userID)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put((byte)0);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.RequireTankInfoResult)]
class RequireTankInfoResultCommand : NetworkCommand
{
    public byte DriverID { get; private set; }
    public bool IsDriven { get; private set; }
    public Vector3 Position { get; private set; }
    public float TurretRotation { get; private set; }
    public float BodyRotation { get; private set; }
    public Int32 Hp { get; private set; }

    public RequireTankInfoResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;

        this.DriverID = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.IsDriven = NetworkCommandBuffer.GetByte(buffer.Array, ref offset) == 1;
        this.Position = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
        this.TurretRotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
        this.BodyRotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
        this.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.TankDestroy)]
class TankDestroyCommand : NetworkCommand
{
    public TankDestroyCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.GameResult)]
class GameResultCommand : NetworkCommand
{
    public bool IsWin { get; private set; }
    public GameResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;

        this.IsWin = NetworkCommandBuffer.GetByte(buffer.Array, ref offset) == 1;
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.WaveOver)]
class WaveOverCommand : NetworkCommand
{
    public WaveOverCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
    }
}


[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.NewWave)]
class NewWaveCommand : NetworkCommand
{
    public NewWaveCommand(byte userID) : base(userID)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put((byte)0);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.NewGame)]
class NewGameCommand : NetworkCommand
{
    public NewGameCommand(byte userID) : base(userID)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put((byte)0);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.NewGameResult)]
class NewGameResultCommand : NetworkCommand
{
    public Vector3 TankPosition { get; private set; }
    public float TankTurretRotation { get; private set; }
    public float TankBodyRotation { get; private set; }
    public int TankHp { get; private set; }

    public int StrpngPointHp { get; private set; }

    public NewGameResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;

        this.TankPosition = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
        this.TankTurretRotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
        this.TankBodyRotation = NetworkCommandBuffer.GetFloat(buffer.Array, ref offset);
        this.TankHp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);

        this.StrpngPointHp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.RequireStrongPointInfo)]
class RequireStrongPointInfoCommand : NetworkCommand
{
    public RequireStrongPointInfoCommand(byte userID) : base(userID)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put((byte)0);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.RequireStrongPointInfoResult)]
class RequireStrongPointInfoResultCommand : NetworkCommand
{
    public Vector3 Position { get; private set; }
    public Int32 Hp { get; private set; }

    public RequireStrongPointInfoResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.Position = NetworkCommandBuffer.GetVector3(buffer.Array, ref offset);
        this.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.StrongPointAttacked)]
class StrongPointAttackedCommand : NetworkCommand
{
    public uint EnmeyID { get; private set; }

    public StrongPointAttackedCommand(byte userID, uint enmeyID) : base(userID)
    {
        this.EnmeyID = enmeyID;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.UInt32);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(this.EnmeyID);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.StrongPointAttackedResult)]
class StrongPointAttackedResultCommand : NetworkCommand
{
    public uint EnmeyID { get; private set; }
    public int Hp { get; private set; }
    public StrongPointAttackedResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;
        this.EnmeyID = NetworkCommandBuffer.GetUInt32(buffer.Array, ref offset);
        this.Hp = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.BuyTrap)]
class BuyTrapCommand : NetworkCommand
{
    public TrapType TrapType { get; private set; }

    public BuyTrapCommand(byte userID, TrapType trapType) : base(userID)
    {
        this.TrapType = trapType;
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put((byte)this.TrapType);
    }
}

[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.BuyTrapResult)]
class BuyTrapResultCommand : NetworkCommand
{
    public Int32 Gold { get; private set; }
    public byte DamageTrapCount { get; private set; }
    public byte SlowTrapCount { get; private set; }

    public BuyTrapResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;

        this.Gold = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
        this.DamageTrapCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.SlowTrapCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.UpdateGold)]
class UpdateGoldCommand : NetworkCommand
{
    public Int32 Gold { get; private set; }

    public UpdateGoldCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;

        this.Gold = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}


[NetworkCommandInfo(Usage = CommandUsage.SendOnly, CommandType = NetWorkCommandType.BuyGrenate)]
class BuyGrenadeCommand : NetworkCommand
{
    public BuyGrenadeCommand(byte userID) : base(userID)
    {
    }

    public override void CalcBufferSize(NetworkCommandBuffer buffer)
    {
        buffer.Extends(TypeSize.Byte);
    }

    public override void FillBuffer(NetworkCommandBuffer buffer)
    {
        buffer.Put(((byte)0));
    }
}


[NetworkCommandInfo(Usage = CommandUsage.RecieveOnly, CommandType = NetWorkCommandType.BuyGrenateResult)]
class BuyGrenadeResultCommand : NetworkCommand
{
    public byte GrenateCount { get; private set; }
    public Int32 Gold { get; private set; }

    public BuyGrenadeResultCommand() : base(0)
    {
    }

    public override void Parse(ArraySegment<byte> buffer)
    {
        var offset = buffer.Offset + 4;

        this.GrenateCount = NetworkCommandBuffer.GetByte(buffer.Array, ref offset);
        this.Gold = NetworkCommandBuffer.GetInt32(buffer.Array, ref offset);
    }
}