import struct
from network.bytebuffer import ByteBuffer
from network.bytebuffer import TypeSize


class CommandType(object):
    MOVE = 0x00
    PLAYER_LIST = 0x01
    NEW_PLAYER = 0x02
    LEAVE = 0x03
    LOGIN_REQUEST = 0x04
    LOGIN_RESULT = 0x05
    JOIN = 0x06
    INIT_PLAYER_INFO = 0x07
    PLAYER_LIST_RESULT = 0x08
    SHOOT = 0x09
    SHOOT_RESULT = 0x0a
    BULLET_MOVE = 0x0b
    BULLET_DESTROY = 0x0c
    PLAYER_ROTATE = 0x0d
    GENERATE_ENEMY = 0x0e
    # ENEMY_SHOOT = 0x0f
    BULLET_HIT = 0x10
    # ENEMY_BULLET_HIT = 0x11
    # ENEMY_BULLET_DESTROY = 0x12
    GENERATE_ITEM = 0x13
    PICK_UP_ITEM = 0x14
    ENEMY_DIE = 0x15
    PLAYER_DIE = 0x16
    ENEMY_MOVE = 0x17
    ENEMY_LIST = 0x18
    ENEMY_LIST_RESULT = 0x19
    BULLET_HIT_RESULT = 0x1a
    RECHARGE = 0x1b
    RECHARGE_RESULT = 0x1c
    DEBUFF_ATTACH = 0x1d
    DEBUFF_REMOVE = 0x1e
    ENMEY_ATTACK = 0x1f
    ENMEY_ATTACK_RESULT = 0x20
    PICK_UP_ITEM_RESULT = 0x21
    RECOVER_HP = 0x22
    SUPPLY_BULLETS = 0x23
    RUNNING_SHOOTING = 0x24
    FAN_SHOOTING = 0x25
    ITEM_LIST = 0x26
    ITEM_LIST_RESULT = 0x27
    UPDATE_PLAYER_KILL_COUNT = 0x28
    ENMEY_BULLET_HIT_RESULT = 0x29
    PLAYER_REBORN = 0x2a
    PLAYER_REBORN_RESULT = 0x2b
    REGISTER = 0x2c
    REGISTER_RESULT = 0x2d
    LEAVE_RESULT = 0x2e

    # new commands
    GUN_SHOOT = 0x2f
    GUN_SHOOT_RESULT = 0x30
    ENEMY_GUN_SHOOT = 0x31
    ENEMY_GUN_SHOOT_RESULT = 0x32
    GAME_RESULT = 0x33
    SELF_DESTRUCT = 0x34
    SELF_DESTRUCT_RESULT = 0x35
    EXPLODE = 0x36
    EXPLODE_RESULT = 0x37

    PUT_TRAP = 0x38
    PUT_TRAP_RESULT = 0x39
    ENMEY_DEBUFF_DAMAGED = 0x3a
    ENMEY_DEBUFF_DAMAGED_RESULT = 0x3b

    DEBUFF_ATTACH_RESULT = 0x3c
    DEBUFF_REMOVE_RESULT = 0x3d

    TRAP_LIST = 0x3e
    TRAP_LIST_RESULT = 0x3f

    TANK_MOVE = 0x40
    TANK_BODY_ROTATE = 0x41
    TANK_TURRET_ROTATE = 0x42
    # TANK_SHOOT = 0x43
    # TANK_SHOOT_RESULT = 0x44
    DRIVE_TANK = 0x45

    REQUIRE_TANK_INFO = 0x46
    REQUIRE_TANK_INFO_RESULT = 0x47

    TANK_DESTROY = 0x48

    DRIVE_TANK_RESULT = 0x49

    WAVE_OVER = 0x4a
    NEW_WAVE = 0x4b

    NEW_GAME = 0x4c
    NEW_GAME_RESULT = 0x4d

    REQUIRE_STRONG_POINT_INFO = 0x4e
    REQUIRE_STRONG_POINT_INFO_RESULT = 0x4f

    STRONG_POINT_ATTACKED = 0x50
    STRONG_POINT_ATTACKED_RESULT = 0x51

    BUY_TRAP = 0x52
    BUY_TRAP_RESULT = 0x53

    UPDATE_GOLD = 0x54

    BUY_GRENATE = 0x55
    BUY_GRENATE_RESULT = 0x56


class CommandUsage(object):
    DualWay = 0x00
    SendOnly = 0x01
    RecieveOnly = 0x02


COMMAND_MAP = {}


def command_mapping(command_type, command_usage):
    def wrapper(cls):
        if command_usage == CommandUsage.DualWay\
                or command_usage == CommandUsage.RecieveOnly:
            COMMAND_MAP[command_type] = cls

        old_init = cls.__init__

        def costume_init(self, *args, **kwds):
            old_init(self, *args, **kwds)
            self.cmd_type = command_type

        cls.__init__ = costume_init
        cls.cmd_usage = command_usage

        return cls
    return wrapper


class NetworkCommand(object):
    cmd_usage = CommandUsage.DualWay

    @staticmethod
    def create_command_from_bytearray(byte_array):
        cmd_type = byte_array[3]
        cmd_cls = COMMAND_MAP[cmd_type]
        cmd_obj = cmd_cls()
        cmd_obj.parse(byte_array)
        return cmd_obj

    def parse(self, byte_array):
        # self.cmd_len = struct.unpack("<h", byte_array[0:2])[0]
        self.cmd_len, _ = ByteBuffer.get_int16(byte_array, 0)
        self.user_id = byte_array[2]
        self.cmd_type = byte_array[3]

    def format(self):
        self.buffer = ByteBuffer()
        self.calc_buffer_size(self.buffer)

        self.buffer.generate_buffer()

        self.buffer.put_int16(self.buffer.max_size)
        self.buffer.put_byte(self.user_id)
        self.buffer.put_byte(self.cmd_type)

        self.fill_buffer(self.buffer)

        return self.buffer.get_buffer()

    def __init__(self):
        self.cmd_len = 0
        self.user_id = 15
        self.cmd_type = None
        self.buffer = None

    def calc_buffer_size(self, byte_buffer):
        raise NotImplementedError

    def fill_buffer(self, byte_buffer):
        raise NotImplementedError


@command_mapping(CommandType.MOVE, CommandUsage.DualWay)
class MoveCommand(NetworkCommand):
    def __init__(self):
        super(MoveCommand, self).__init__()
        self.position = None
        self.rotation = None
        self.byte_array = None

    def parse(self, byte_array):
        self.byte_array = byte_array

        # position
        start = 5
        position_x, start = ByteBuffer.get_float(byte_array, start)
        position_y, start = ByteBuffer.get_float(byte_array, start)
        position_z, start = ByteBuffer.get_float(byte_array, start)
        # start += 4
        self.position = [position_x, position_y, position_z]

        # rotation
        self.rotation, start = ByteBuffer.get_float(byte_array, start)

    def format(self):
        return self.byte_array


@command_mapping(CommandType.LOGIN_REQUEST, CommandUsage.RecieveOnly)
class LoginRequestCommand(NetworkCommand):
    def __init__(self):
        super(LoginRequestCommand, self).__init__()
        self.username = None
        self.password = None

    def parse(self, byte_array):
        username_len = struct.unpack("<h", byte_array[4:6])[0]
        self.username = byte_array[6: 6+username_len].decode("utf-8")

        password_start = 6 + username_len
        password_len = struct.unpack(
            "<h", byte_array[password_start: password_start+2])[0]
        self.password = byte_array[password_start +
                                   2: password_start+2+password_len].decode("utf-8")


@command_mapping(CommandType.LOGIN_RESULT, CommandUsage.SendOnly)
class LoginResultCommand(NetworkCommand):
    def __init__(self, result, userID):
        super(LoginResultCommand, self).__init__()
        self.result = result
        self.userID = userID

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(1 if self.result else 0)
        byte_buffer.put_byte(self.userID)


@command_mapping(CommandType.JOIN, CommandUsage.RecieveOnly)
class JoinCommand(NetworkCommand):
    def __init__(self):
        super(JoinCommand, self).__init__()


@command_mapping(CommandType.INIT_PLAYER_INFO, CommandUsage.SendOnly)
class InitPlayerInfoCommand(NetworkCommand):
    def __init__(self, player_info, is_host):
        super(InitPlayerInfoCommand, self).__init__()
        self.player_info = player_info
        self.position = player_info.position
        self.rotation = player_info.rotation
        self.hp = player_info.hp
        self.shooting_type = player_info.shooting_type
        self.is_host = is_host
        self.gold = player_info.gold
        self.damage_trap_count = player_info.damage_trap_count
        self.slow_trap_count = player_info.slow_trap_count
        self.grenade_count = player_info.grenade_count

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Int16)
        byte_buffer.extends(TypeSize.Int16)
        byte_buffer.extends(TypeSize.Byte)

        byte_buffer.extends(TypeSize.Float)
        byte_buffer.extends(TypeSize.Float)
        byte_buffer.extends(TypeSize.Float)

        byte_buffer.extends(TypeSize.Float)

        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Byte)

        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Byte)

        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        # body
        byte_buffer.put_int16(self.player_info.cur_bullet)
        byte_buffer.put_int16(self.player_info.max_bullet)
        byte_buffer.put_byte(self.player_info.player_type)

        # position
        byte_buffer.put_float(self.position[0])
        byte_buffer.put_float(self.position[1])
        byte_buffer.put_float(self.position[2])

        # rotation
        byte_buffer.put_float(self.rotation)

        # hp
        byte_buffer.put_int32(self.hp)

        # shooting type
        byte_buffer.put_byte(self.shooting_type)

        # kills
        byte_buffer.put_int32(self.player_info.kills)

        # gold traps
        byte_buffer.put_int32(self.gold)
        byte_buffer.put_byte(self.damage_trap_count)
        byte_buffer.put_byte(self.slow_trap_count)

        # grenade
        byte_buffer.put_byte(self.grenade_count)

        # is host
        if self.is_host:
            byte_buffer.put_byte(1)
        else:
            byte_buffer.put_byte(0)


@command_mapping(CommandType.NEW_PLAYER, CommandUsage.SendOnly)
class NewPlayerCommand(NetworkCommand):
    def __init__(self, player_info, position, rotation):
        super(NewPlayerCommand, self).__init__()
        self.player_info = player_info
        self.position = position
        self.rotation = rotation

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Int16)
        byte_buffer.extends(TypeSize.Int16)
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Byte)

        byte_buffer.extends(TypeSize.Float * 3)

        byte_buffer.extends(TypeSize.Float)

        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        # body
        byte_buffer.put_int16(self.player_info.cur_bullet)
        byte_buffer.put_int16(self.player_info.max_bullet)
        byte_buffer.put_byte(self.player_info.player_type)
        byte_buffer.put_byte(self.player_info.user_id)

        # position
        byte_buffer.put_float(self.position[0])
        byte_buffer.put_float(self.position[1])
        byte_buffer.put_float(self.position[2])

        # rotation
        byte_buffer.put_float(self.rotation)

        # hp
        byte_buffer.put_int32(self.player_info.hp)

        # shooting type
        byte_buffer.put_byte(self.player_info.shooting_type)

        # kills
        byte_buffer.put_int32(self.player_info.kills)


@command_mapping(CommandType.PLAYER_LIST, CommandUsage.RecieveOnly)
class PlayerListCommand(NetworkCommand):
    def __init__(self):
        super(PlayerListCommand, self).__init__()


@command_mapping(CommandType.PLAYER_LIST_RESULT, CommandUsage.SendOnly)
class PlayerListResultCommand(NetworkCommand):
    def __init__(self, server, sender):
        super(PlayerListResultCommand, self).__init__()
        self.server = server
        self.sender = sender

    def calc_buffer_size(self, byte_buffer):
        player_count = len(self.server.socket_map)
        if self.server.socket_map.has_key(self.sender):
            player_count -= 1

        if player_count > 0:
            byte_buffer.extends(TypeSize.Byte)
            byte_buffer.extends(
                player_count *
                (TypeSize.Int16
                 + TypeSize.Int16
                 + TypeSize.Byte
                 + TypeSize.Byte
                 + TypeSize.Float * 3
                 + TypeSize.Float
                 + TypeSize.Int32
                 + TypeSize.Byte
                 + TypeSize.Int32)
            )
        else:
            byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        player_count = len(self.server.socket_map)
        if self.server.socket_map.has_key(self.sender):
            player_count -= 1

        if player_count > 0:
            # players
            byte_buffer.put_byte(player_count)

            for (s, p) in self.server.socket_map.items():
                if s != self.sender:
                    byte_buffer.put_int16(p.cur_bullet)
                    byte_buffer.put_int16(p.max_bullet)
                    byte_buffer.put_byte(p.player_type)
                    byte_buffer.put_byte(p.user_id)

                    # position
                    byte_buffer.put_float(p.position[0])
                    byte_buffer.put_float(p.position[1])
                    byte_buffer.put_float(p.position[2])

                    # print "position {} {} {}".format(
                    #     p.position[0], p.position[1], p.position[2])

                    # rotation
                    byte_buffer.put_float(p.rotation)

                    # hp
                    byte_buffer.put_int32(p.hp)

                    # shooting type
                    byte_buffer.put_byte(p.shooting_type)

                    # kills
                    byte_buffer.put_int32(p.kills)
        else:
            byte_buffer.put_byte(0)


@command_mapping(CommandType.SHOOT, CommandUsage.DualWay)
class ShootCommand(NetworkCommand):
    def __init__(self):
        super(ShootCommand, self).__init__()
        self.byte_array = None
        self.bullet_count = None
        self.is_granate = None

    def parse(self, byte_array):
        self.byte_array = byte_array
        self.bullet_count = byte_array[5]
        self.is_granate = byte_array[6] == 1

    def format(self):
        return self.byte_array


@command_mapping(CommandType.SHOOT_RESULT, CommandUsage.SendOnly)
class ShootResultCommand(NetworkCommand):
    def __init__(self, cur_bullet_count, max_bullet_count, grenade_count):
        super(ShootResultCommand, self).__init__()
        self.cur_bullet_count = cur_bullet_count
        self.max_bullet_count = max_bullet_count
        self.grenade_count = grenade_count

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Int16)
        byte_buffer.extends(TypeSize.Int16)
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_int16(self.cur_bullet_count)
        byte_buffer.put_int16(self.max_bullet_count)
        byte_buffer.put_byte(self.grenade_count)


@command_mapping(CommandType.BULLET_MOVE, CommandUsage.DualWay)
class BulletMoveCommand(NetworkCommand):
    def __init__(self):
        super(BulletMoveCommand, self).__init__()
        self.byte_array = None

    def parse(self, byte_array):
        self.byte_array = byte_array

    def format(self):
        return self.byte_array


@command_mapping(CommandType.BULLET_DESTROY, CommandUsage.DualWay)
class BulletDestroyCommand(NetworkCommand):
    def __init__(self):
        super(BulletDestroyCommand, self).__init__()
        self.byte_array = None

    def parse(self, byte_array):
        self.byte_array = byte_array

    def format(self):
        return self.byte_array


@command_mapping(CommandType.PLAYER_ROTATE, CommandUsage.DualWay)
class PlayerRotateCommand(NetworkCommand):
    def __init__(self):
        super(PlayerRotateCommand, self).__init__()
        self.byte_array = None
        self.rotation = None

    def parse(self, byte_array):
        self.byte_array = byte_array

        start = 5
        # self.rotation = struct.unpack("<f", byte_array[start: start+4])[0]
        self.rotation, start = ByteBuffer.get_float(byte_array, start)

    def format(self):
        return self.byte_array


@command_mapping(CommandType.GENERATE_ENEMY, CommandUsage.SendOnly)
class GenerateEnemyCommand(NetworkCommand):
    def __init__(self, enemy_infos, is_host):
        super(GenerateEnemyCommand, self).__init__()
        self.enemy_infos = enemy_infos
        self.is_host = is_host

    def calc_buffer_size(self, byte_buffer):
        count = len(self.enemy_infos)
        byte_buffer.extends(TypeSize.Byte)

        byte_buffer.extends(
            count * (
                TypeSize.Int32
                + TypeSize.Byte
                + TypeSize.Float * 3
                + TypeSize.Float
                + TypeSize.Int32
                + TypeSize.Byte
            )
        )

    def fill_buffer(self, byte_buffer):
        count = len(self.enemy_infos)

        byte_buffer.put_byte(count)
        for enemy in self.enemy_infos:
            byte_buffer.put_int32(enemy.enemy_id)
            byte_buffer.put_byte(enemy.enemy_type)
            byte_buffer.put_float(enemy.position[0])
            byte_buffer.put_float(enemy.position[1])
            byte_buffer.put_float(enemy.position[2])
            byte_buffer.put_float(enemy.rotation)
            byte_buffer.put_int32(enemy.hp)

        if self.is_host:
            byte_buffer.put_byte(1)
        else:
            byte_buffer.put_byte(0)


@command_mapping(CommandType.ENEMY_MOVE, CommandUsage.DualWay)
class EnemyMoveCommand(NetworkCommand):
    def __init__(self):
        super(EnemyMoveCommand, self).__init__()
        self.enemy_id = None
        self.position = None
        self.rotation = None
        self.byte_array = None

    def parse(self, byte_array):
        self.byte_array = byte_array

        start = 4
        self.enemy_id, start = ByteBuffer.get_uint32(byte_array, start)
        position_x, start = ByteBuffer.get_float(byte_array, start)
        position_y, start = ByteBuffer.get_float(byte_array, start)
        position_z, start = ByteBuffer.get_float(byte_array, start)
        self.position = [position_x, position_y, position_z]

        # rotation
        self.rotation, start = ByteBuffer.get_float(byte_array, start)

    def format(self):
        return self.byte_array


# @command_mapping(CommandType.ENEMY_SHOOT, CommandUsage.DualWay)
# class EnemyShootCommand(NetworkCommand):
#     def __init__(self):
#         super(EnemyShootCommand, self).__init__()
#         self.byte_array = None

#     def parse(self, byte_array):
#         self.byte_array = byte_array

#     def get_bytes(self):
#         return self.byte_array


# @command_mapping(CommandType.ENEMY_BULLET_DESTROY, CommandUsage.DualWay)
# class EnemyBulletDestroyCommand(NetworkCommand):
#     def __init__(self):
#         super(EnemyBulletDestroyCommand, self).__init__()
#         self.byte_array = None

#     def parse(self, byte_array):
#         self.byte_array = byte_array

#     def get_bytes(self):
#         return self.byte_array

@command_mapping(CommandType.ENEMY_LIST, CommandUsage.RecieveOnly)
class EnemyListCommand(NetworkCommand):
    def __init__(self):
        super(EnemyListCommand, self).__init__()


@command_mapping(CommandType.ENEMY_LIST_RESULT, CommandUsage.SendOnly)
class EnemyListResultCommand(NetworkCommand):
    def __init__(self, enemy_infos, is_host):
        super(EnemyListResultCommand, self).__init__()
        self.enemy_infos = enemy_infos
        self.is_host = is_host

    def calc_buffer_size(self, byte_buffer):
        count = len(self.enemy_infos)

        byte_buffer.extends(TypeSize.Byte)

        byte_buffer.extends(
            count *
            (
                TypeSize.Int32
                + TypeSize.Byte
                + TypeSize.Float * 3
                + TypeSize.Float
                + TypeSize.Int32
                + TypeSize.Byte
            )
        )

        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        count = len(self.enemy_infos)

        byte_buffer.put_byte(count)
        for enemy in self.enemy_infos:
            byte_buffer.put_int32(enemy.enemy_id)
            byte_buffer.put_byte(enemy.enemy_type)
            byte_buffer.put_float(enemy.position[0])
            byte_buffer.put_float(enemy.position[1])
            byte_buffer.put_float(enemy.position[2])
            byte_buffer.put_float(enemy.rotation)
            byte_buffer.put_int32(enemy.hp)

        if self.is_host:
            byte_buffer.put_byte(1)
        else:
            byte_buffer.put_byte(0)


@command_mapping(CommandType.BULLET_HIT, CommandUsage.RecieveOnly)
class BulletHitCommand(NetworkCommand):
    def __init__(self):
        super(BulletHitCommand, self).__init__()
        self.shooter_id = None
        self.bullet_type = None
        self.hit_enemy_id = None
        self.bullet_id = None

    def parse(self, byte_array):
        start = 4

        self.shooter_id, start = ByteBuffer.get_byte(byte_array, start)
        self.bullet_type, start = ByteBuffer.get_byte(byte_array, start)
        self.hit_enemy_id, start = ByteBuffer.get_uint32(byte_array, start)
        self.bullet_id, start = ByteBuffer.get_uint32(byte_array, start)


@command_mapping(CommandType.BULLET_HIT_RESULT, CommandUsage.SendOnly)
class BulletHitResultCommand(NetworkCommand):
    def __init__(self, shooter_id, enemy_id, enemy_hp, bullet_id):
        super(BulletHitResultCommand, self).__init__()
        self.shooter_id = shooter_id
        self.enemy_id = enemy_id
        self.enemy_hp = enemy_hp
        self.bullet_id = bullet_id

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.shooter_id)
        byte_buffer.put_int32(self.enemy_id)
        byte_buffer.put_int32(self.enemy_hp)
        byte_buffer.put_int32(self.bullet_id)


@command_mapping(CommandType.ENEMY_DIE, CommandUsage.SendOnly)
class EnemyDieCommand(NetworkCommand):
    def __init__(self, shooter_id, enmey_id):
        super(EnemyDieCommand, self).__init__()
        self.shooter_id = shooter_id
        self.enmey_id = enmey_id

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.shooter_id)
        byte_buffer.put_int32(self.enmey_id)


@command_mapping(CommandType.RECHARGE, CommandUsage.DualWay)
class RechargeCommand(NetworkCommand):
    def __init__(self):
        super(RechargeCommand, self).__init__()
        self.byte_array = None
        self.shooter_id = None

    def parse(self, byte_array):
        self.byte_array = byte_array

        start = 4
        self.shooter_id = byte_array[start]

    def format(self):
        return self.byte_array


@command_mapping(CommandType.RECHARGE_RESULT, CommandUsage.SendOnly)
class RechargeResultCommand(NetworkCommand):
    def __init__(self, cur_bullet_count, max_bullet_count):
        super(RechargeResultCommand, self).__init__()
        self.cur_bullet_count = cur_bullet_count
        self.max_bullet_count = max_bullet_count

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Int16)
        byte_buffer.extends(TypeSize.Int16)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_int16(self.cur_bullet_count)
        byte_buffer.put_int16(self.max_bullet_count)


@command_mapping(CommandType.ENMEY_ATTACK, CommandUsage.DualWay)
class EnemyAttackCommand(NetworkCommand):
    def __init__(self):
        super(EnemyAttackCommand, self).__init__()
        self.attacked_user_id = None
        self.attacking_enemy_id = None
        self.byte_array = None

    def parse(self, byte_array):
        self.byte_array = byte_array

        start = 4
        self.attacked_user_id, start = ByteBuffer.get_byte(byte_array, start)
        self.attacking_enemy_id, start = ByteBuffer.get_uint32(
            byte_array, start)

    def format(self):
        return self.byte_array


@command_mapping(CommandType.ENMEY_ATTACK_RESULT, CommandUsage.SendOnly)
class EnemyAttackResultCommand(NetworkCommand):
    def __init__(self, attacked_user_id, hp):
        super(EnemyAttackResultCommand, self).__init__()
        self.attacked_user_id = attacked_user_id
        self.hp = hp

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.attacked_user_id)
        byte_buffer.put_int32(self.hp)


@command_mapping(CommandType.GENERATE_ITEM, CommandUsage.SendOnly)
class GenerateItemCommand(NetworkCommand):
    def __init__(self, item_id, item_type, item_position):
        super(GenerateItemCommand, self).__init__()
        self.item_id = item_id
        self.item_type = item_type
        self.item_position = item_position

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Float * 3)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_int32(self.item_id)
        byte_buffer.put_byte(self.item_type)
        byte_buffer.put_float(self.item_position[0])
        byte_buffer.put_float(self.item_position[1])
        byte_buffer.put_float(self.item_position[2])


@command_mapping(CommandType.PICK_UP_ITEM, CommandUsage.RecieveOnly)
class PickUpItemCommand(NetworkCommand):
    def __init__(self):
        super(PickUpItemCommand, self).__init__()
        self.player_id = None
        self.item_id = None

    def parse(self, byte_array):
        start = 4
        self.player_id, start = ByteBuffer.get_byte(byte_array, start)
        self.item_id, start = ByteBuffer.get_uint32(byte_array, start)


@command_mapping(CommandType.PICK_UP_ITEM_RESULT, CommandUsage.SendOnly)
class PickUpItemResultCommand(NetworkCommand):
    def __init__(self, item_id):
        super(PickUpItemResultCommand, self).__init__()
        self.item_id = item_id

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_int32(self.item_id)


@command_mapping(CommandType.RECOVER_HP, CommandUsage.SendOnly)
class RecoverHpCommand(NetworkCommand):
    def __init__(self, user_id, hp):
        super(RecoverHpCommand, self).__init__()
        self.player_id = user_id
        self.hp = hp

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.player_id)
        byte_buffer.put_int32(self.hp)


@command_mapping(CommandType.SUPPLY_BULLETS, CommandUsage.SendOnly)
class SupplyBulletsCommand(NetworkCommand):
    def __init__(self, user_id, cur_bullets, max_bullets):
        super(SupplyBulletsCommand, self).__init__()
        self.player_id = user_id
        self.cur_bullet = cur_bullets
        self.max_bullet = max_bullets

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Int16)
        byte_buffer.extends(TypeSize.Int16)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.player_id)
        byte_buffer.put_int16(self.cur_bullet)
        byte_buffer.put_int16(self.max_bullet)


@command_mapping(CommandType.FAN_SHOOTING, CommandUsage.SendOnly)
class FanShootingCommand(NetworkCommand):
    def __init__(self, user_id):
        super(FanShootingCommand, self).__init__()
        self.player_id = user_id

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.player_id)


@command_mapping(CommandType.RUNNING_SHOOTING, CommandUsage.SendOnly)
class RunningShootingCommand(NetworkCommand):
    def __init__(self, user_id):
        super(RunningShootingCommand, self).__init__()
        self.player_id = user_id

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.player_id)


@command_mapping(CommandType.ITEM_LIST, CommandUsage.RecieveOnly)
class ItemListCommand(NetworkCommand):
    def __init__(self):
        super(ItemListCommand, self).__init__()


@command_mapping(CommandType.ITEM_LIST_RESULT, CommandUsage.SendOnly)
class ItemListResultCommand(NetworkCommand):
    def __init__(self, item_list):
        super(ItemListResultCommand, self).__init__()
        self.item_list = item_list

    def calc_buffer_size(self, byte_buffer):
        count = len(self.item_list)
        if count > 0:
            byte_buffer.extends(TypeSize.Byte)

            byte_buffer.extends(
                TypeSize.Int32
                + TypeSize.Byte
                + TypeSize.Float * 3
            )
        else:
            byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        count = len(self.item_list)

        if count > 0:
            byte_buffer.put_byte(count)

            for item in self.item_list:
                byte_buffer.put_int32(item.item_id)
                byte_buffer.put_byte(item.item_type)
                byte_buffer.put_float(item.position[0])
                byte_buffer.put_float(item.position[1])
                byte_buffer.put_float(item.position[2])
        else:
            byte_buffer.put_byte(0)


@command_mapping(CommandType.UPDATE_PLAYER_KILL_COUNT, CommandUsage.SendOnly)
class UpdatePlayerKillCountCommand(NetworkCommand):
    def __init__(self, count):
        super(UpdatePlayerKillCountCommand, self).__init__()
        self.count = count

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Float)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_int32(self.count)


# @command_mapping(CommandType.ENEMY_BULLET_HIT, CommandUsage.RecieveOnly)
# class EnemyBulletHitCommand(NetworkCommand):
#     def __init__(self):
#         super(EnemyBulletHitCommand, self).__init__()
#         self.player_id = None
#         self.enemy_id = None
#         self.bullet_id = None

#     def parse(self, byte_array):
#         start = 4
#         self.player_id, start = ByteBuffer.get_byte(byte_array, start)
#         self.enemy_id, start = ByteBuffer.get_uint32(byte_array, start)
#         self.bullet_id, start = ByteBuffer.get_uint32(byte_array, start)


# @command_mapping(CommandType.ENMEY_BULLET_HIT_RESULT, CommandUsage.SendOnly)
# class EnmeyBuleltHitResultCommand(NetworkCommand):
#     def __init__(self, player_id, enemy_id, player_hp, bullet_id):
#         super(EnmeyBuleltHitResultCommand, self).__init__()
#         self.player_id = player_id
#         self.enemy_id = enemy_id
#         self.player_hp = player_hp
#         self.bullet_id = bullet_id

#     def calc_buffer_size(self, byte_buffer):
#         byte_buffer.extends(TypeSize.Byte)
#         byte_buffer.extends(TypeSize.Int32)
#         byte_buffer.extends(TypeSize.Int32)
#         byte_buffer.extends(TypeSize.Int32)

#     def fill_buffer(self, byte_buffer):
#         byte_buffer.put_byte(self.player_id)
#         byte_buffer.put_int32(self.enemy_id)
#         byte_buffer.put_int32(self.player_hp)
#         byte_buffer.put_int32(self.bullet_id)


@command_mapping(CommandType.PLAYER_DIE, CommandUsage.SendOnly)
class PlayerDieCommand(NetworkCommand):
    def __init__(self, player_id):
        super(PlayerDieCommand, self).__init__()
        self.player_id = player_id

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.player_id)


@command_mapping(CommandType.PLAYER_REBORN, CommandUsage.RecieveOnly)
class PlayerRebornCommand(NetworkCommand):
    def __init__(self):
        super(PlayerRebornCommand, self).__init__()
        self.player_id = None

    def parse(self, byte_array):
        start = 4
        self.player_id = ByteBuffer.get_byte(byte_array, start)


@command_mapping(CommandType.PLAYER_REBORN_RESULT, CommandUsage.SendOnly)
class PlayerRebornResultCommand(NetworkCommand):
    def __init__(self, player_id, hp, cur_bullets, max_bullets):
        super(PlayerRebornResultCommand, self).__init__()
        self.player_id = player_id
        self.hp = hp
        self.cur_bullet = cur_bullets
        self.max_bullet = max_bullets

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Int16)
        byte_buffer.extends(TypeSize.Int16)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.player_id)
        byte_buffer.put_int32(self.hp)
        byte_buffer.put_int16(self.cur_bullet)
        byte_buffer.put_int16(self.max_bullet)


@command_mapping(CommandType.REGISTER, CommandUsage.RecieveOnly)
class RegisterCommand(NetworkCommand):
    def __init__(self):
        super(RegisterCommand, self).__init__()
        self.username = None
        self.password = None

    def parse(self, byte_array):
        username_len = struct.unpack("<h", byte_array[4:6])[0]
        self.username = byte_array[6: 6+username_len].decode("utf-8")

        password_start = 6 + username_len
        password_len = struct.unpack(
            "<h", byte_array[password_start: password_start+2])[0]
        self.password = byte_array[password_start +
                                   2: password_start+2+password_len].decode("utf-8")


@command_mapping(CommandType.REGISTER_RESULT, CommandUsage.SendOnly)
class RegisterResultCommand(NetworkCommand):
    def __init__(self, result):
        super(RegisterResultCommand, self).__init__()
        self.result = result

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(1 if self.result else 0)


@command_mapping(CommandType.LEAVE, CommandUsage.DualWay)
class LeaveCommand(NetworkCommand):
    def __init__(self):
        super(LeaveCommand, self).__init__()
        self.user_id = None
        self.byte_array = None

    def parse(self, byte_array):
        start = 4
        self.user_id, start = ByteBuffer.get_byte(byte_array, start)

        self.byte_array = byte_array

    def format(self):
        return self.byte_array


@command_mapping(CommandType.LEAVE_RESULT, CommandUsage.SendOnly)
class LeaveResultCommand(NetworkCommand):
    def __init__(self):
        super(LeaveResultCommand, self).__init__()

    def parse(self, byte_array):
        raise Exception("invalid command accpeted.")

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(0)


# ENEMY_GUN_SHOOT = 0x32
# ENEMY_GUN_SHOOT_RESULT = 0x33

@command_mapping(CommandType.GUN_SHOOT, CommandUsage.DualWay)
class GunShootCommand(NetworkCommand):
    def __init__(self):
        super(GunShootCommand, self).__init__()
        self.shooter_id = None
        self.bullet_count = None
        self.is_hit = None
        self.hit_enemy_id = None

        self.byte_array = None

    def parse(self, byte_array):
        self.byte_array = byte_array
        start = 4

        self.shooter_id, start = ByteBuffer.get_byte(byte_array, start)
        self.bullet_count, start = ByteBuffer.get_byte(byte_array, start)
        self.is_hit, start = ByteBuffer.get_byte(byte_array, start)
        self.hit_enemy_id, start = ByteBuffer.get_uint32(byte_array, start)

        self.is_hit = self.is_hit == 1

    def format(self):
        return self.byte_array


@command_mapping(CommandType.GUN_SHOOT_RESULT, CommandUsage.SendOnly)
class GunShootResultCommand(NetworkCommand):
    def __init__(self, shooter_id, enemy_id, enemy_hp, cur_bullet, max_bullet, is_hit):
        super(GunShootResultCommand, self).__init__()
        self.shooter_id = shooter_id
        self.enemy_id = enemy_id
        self.enemy_hp = enemy_hp
        self.cur_bullet = cur_bullet
        self.max_bullet = max_bullet
        self.is_hit = is_hit

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.UInt32)
        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Int16)
        byte_buffer.extends(TypeSize.Int16)
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.shooter_id)
        byte_buffer.put_uint32(self.enemy_id)
        byte_buffer.put_int32(self.enemy_hp)
        byte_buffer.put_int16(self.cur_bullet)
        byte_buffer.put_int16(self.max_bullet)
        byte_buffer.put_byte(1 if self.is_hit else 0)


@command_mapping(CommandType.ENEMY_GUN_SHOOT, CommandUsage.DualWay)
class EnemyGunShootCommand(NetworkCommand):
    def __init__(self):
        super(EnemyGunShootCommand, self).__init__()
        self.shooter_id = None
        self.is_hit = None
        self.hit_type = None
        self.hit_target_id = None

        self.byte_array = None

    def parse(self, byte_array):
        start = 4
        self.hit_target_id, start = ByteBuffer.get_byte(byte_array, start)
        self.is_hit, start = ByteBuffer.get_byte(byte_array, start)
        self.hit_type, start = ByteBuffer.get_byte(byte_array, start)
        self.shooter_id, start = ByteBuffer.get_uint32(byte_array, start)

        self.is_hit = self.is_hit == 1

        self.byte_array = byte_array

    def format(self):
        return self.byte_array


@command_mapping(CommandType.ENEMY_GUN_SHOOT_RESULT, CommandUsage.SendOnly)
class EnmeyGunShootResultCommand(NetworkCommand):
    def __init__(self, enemy_id, target_id, is_hit, hp):
        super(EnmeyGunShootResultCommand, self).__init__()
        self.enemy_id = enemy_id
        self.target_id = target_id
        self.is_hit = is_hit
        self.hp = hp

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.UInt32)
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_uint32(self.enemy_id)
        byte_buffer.put_byte(self.target_id)
        byte_buffer.put_byte(1 if self.is_hit else 0)
        byte_buffer.put_int32(self.hp)


@command_mapping(CommandType.SELF_DESTRUCT, CommandUsage.RecieveOnly)
class SelfDestuctCommand(NetworkCommand):
    def __init__(self):
        super(SelfDestuctCommand, self).__init__()
        self.enemy_id = None
        self.position = None

    def parse(self, byte_array):
        start = 4

        self.enemy_id, start = ByteBuffer.get_uint32(byte_array, start)
        x, start = ByteBuffer.get_float(byte_array, start)
        y, start = ByteBuffer.get_float(byte_array, start)
        z, start = ByteBuffer.get_float(byte_array, start)

        self.position = [x, y, z]


@command_mapping(CommandType.SELF_DESTRUCT_RESULT, CommandUsage.SendOnly)
class SelfDestuctResultCommand(NetworkCommand):
    def __init__(self, damaged_player_id, hp, enemy_id):
        super(SelfDestuctResultCommand, self).__init__()
        self.damaged_player_id = damaged_player_id
        self.hp = hp
        self.enemy_id = enemy_id

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.UInt32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.damaged_player_id)
        byte_buffer.put_int32(self.hp)
        byte_buffer.put_uint32(self.enemy_id)


@command_mapping(CommandType.EXPLODE, CommandUsage.DualWay)
class ExplodeCommand(NetworkCommand):
    def __init__(self):
        super(ExplodeCommand, self).__init__()
        self.byte_array = None
        self.thrower = None
        self.position = None
        self.bullet_id = None

    def parse(self, byte_array):
        self.byte_array = byte_array

        start = 4
        self.thrower, start = ByteBuffer.get_byte(byte_array, start)
        x, start = ByteBuffer.get_float(byte_array, start)
        y, start = ByteBuffer.get_float(byte_array, start)
        z, start = ByteBuffer.get_float(byte_array, start)
        self.position = [x, y, z]
        self.bullet_id, start = ByteBuffer.get_uint32(byte_array, start)

    def format(self):
        return self.byte_array


@command_mapping(CommandType.EXPLODE_RESULT, CommandUsage.SendOnly)
class ExplodeResultCommand(NetworkCommand):
    def __init__(self, damaged_enemy_id, hp):
        super(ExplodeResultCommand, self).__init__()
        self.damaged_enmey_id = damaged_enemy_id
        self.hp = hp

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.UInt32)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_uint32(self.damaged_enmey_id)
        byte_buffer.put_int32(self.hp)


@command_mapping(CommandType.PUT_TRAP, CommandUsage.RecieveOnly)
class PutTrapCommand(NetworkCommand):
    def __init__(self):
        super(PutTrapCommand, self).__init__()
        self.builder_id = None
        self.position = None
        self.rotation = None
        self.trap_type = None

    def parse(self, byte_array):
        start = 4

        self.builder_id, start = ByteBuffer.get_byte(byte_array, start)

        x, start = ByteBuffer.get_float(byte_array, start)
        y, start = ByteBuffer.get_float(byte_array, start)
        z, start = ByteBuffer.get_float(byte_array, start)
        self.position = [x, y, z]

        self.rotation, start = ByteBuffer.get_float(byte_array, start)
        self.trap_type, start = ByteBuffer.get_byte(byte_array, start)


@command_mapping(CommandType.PUT_TRAP_RESULT, CommandUsage.SendOnly)
class PutTrapResultCommand(NetworkCommand):
    def __init__(self, builder_id, position, rotation, trap_type, trap_count):
        super(PutTrapResultCommand, self).__init__()
        self.builder_id = builder_id
        self.position = position
        self.rotation = rotation
        self.trap_type = trap_type
        self.trap_count = trap_count

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Float * 3)
        byte_buffer.extends(TypeSize.Float)
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.builder_id)
        byte_buffer.put_float(self.position[0])
        byte_buffer.put_float(self.position[1])
        byte_buffer.put_float(self.position[2])
        byte_buffer.put_float(self.rotation)
        byte_buffer.put_byte(self.trap_type)
        byte_buffer.put_byte(self.trap_count)


@command_mapping(CommandType.DEBUFF_ATTACH, CommandUsage.RecieveOnly)
class DebuffAttachCommand(NetworkCommand):
    def __init__(self):
        super(DebuffAttachCommand, self).__init__()
        self.target_enemy_id = None
        self.debuff_id = None
        self.debuff_duration = None

    def parse(self, byte_array):
        start = 4
        self.target_enemy_id, start = ByteBuffer.get_uint32(byte_array, start)
        self.debuff_id, start = ByteBuffer.get_byte(byte_array, start)
        self.debuff_duration, start = ByteBuffer.get_float(byte_array, start)


@command_mapping(CommandType.DEBUFF_REMOVE, CommandUsage.RecieveOnly)
class DebuffRemoveCommand(NetworkCommand):
    def __init__(self):
        super(DebuffRemoveCommand, self).__init__()
        self.target_enemy_id = None
        self.debuff_id = None

    def parse(self, byte_array):
        start = 4
        self.target_enemy_id, start = ByteBuffer.get_uint32(byte_array, start)
        self.debuff_id, start = ByteBuffer.get_byte(byte_array, start)


@command_mapping(CommandType.DEBUFF_ATTACH_RESULT, CommandUsage.SendOnly)
class DebuffAttachResultCommand(NetworkCommand):
    def __init__(self, target_enemy_id, debuff_id, debuff_duration):
        super(DebuffAttachResultCommand, self).__init__()
        self.target_enemy_id = target_enemy_id
        self.debuff_id = debuff_id
        self.debuff_duration = debuff_duration

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Float)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_int32(self.target_enemy_id)
        byte_buffer.put_byte(self.debuff_id)
        byte_buffer.put_float(self.debuff_duration)


@command_mapping(CommandType.DEBUFF_REMOVE_RESULT, CommandUsage.RecieveOnly)
class DebuffRemoveResultCommand(NetworkCommand):
    def __init__(self, target_enemy_id, debuff_id):
        super(DebuffRemoveResultCommand, self).__init__()
        self.target_enemy_id = target_enemy_id
        self.debuff_id = debuff_id

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_int32(self.target_enemy_id)
        byte_buffer.put_byte(self.debuff_id)


@command_mapping(CommandType.ENMEY_DEBUFF_DAMAGED, CommandUsage.RecieveOnly)
class EnemyDebuffDamagedCommand(NetworkCommand):
    def __init__(self):
        super(EnemyDebuffDamagedCommand, self).__init__()
        self.enemy_id = None

    def parse(self, byte_array):
        start = 4
        self.enemy_id, start = ByteBuffer.get_uint32(byte_array, start)


@command_mapping(CommandType.ENMEY_DEBUFF_DAMAGED_RESULT, CommandUsage.SendOnly)
class EnemyDebuffDamagedResultCommand(NetworkCommand):
    def __init__(self, enemy_id, hp):
        super(EnemyDebuffDamagedResultCommand, self).__init__()
        self.enemy_id = enemy_id
        self.hp = hp

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.UInt32)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_uint32(self.enemy_id)
        byte_buffer.put_int32(self.hp)


@command_mapping(CommandType.TRAP_LIST, CommandUsage.RecieveOnly)
class TrapListCommand(NetworkCommand):
    def __init__(self):
        super(TrapListCommand, self).__init__()

    def parse(self, byte_array):
        pass


@command_mapping(CommandType.TRAP_LIST_RESULT, CommandUsage.SendOnly)
class TrapListResultCommand(NetworkCommand):
    def __init__(self, trap_list):
        super(TrapListResultCommand, self).__init__()
        self.trap_list = trap_list

    def calc_buffer_size(self, byte_buffer):
        count = len(self.trap_list)

        byte_buffer.extends(TypeSize.UInt32)

        byte_buffer.extends(
            count *
            (
                TypeSize.Byte
                + TypeSize.UInt32
                + TypeSize.Float * 3
                + TypeSize.Float
            )
        )

    def fill_buffer(self, byte_buffer):
        count = len(self.trap_list)

        byte_buffer.put_uint32(count)

        for trap in self.trap_list:
            byte_buffer.put_byte(trap.trap_type)
            byte_buffer.put_uint32(trap.trap_id)
            byte_buffer.put_float(trap.position[0])
            byte_buffer.put_float(trap.position[1])
            byte_buffer.put_float(trap.position[2])
            byte_buffer.put_float(trap.rotation)


@command_mapping(CommandType.TANK_MOVE, CommandUsage.DualWay)
class TankMoveCommand(NetworkCommand):
    def __init__(self):
        super(TankMoveCommand, self).__init__()
        self.driver_id = None
        self.position = None
        self.byte_array = None

    def parse(self, byte_array):
        self.byte_array = byte_array

        start = 4
        self.driver_id, start = ByteBuffer.get_byte(byte_array, start)
        x, start = ByteBuffer.get_float(byte_array, start)
        y, start = ByteBuffer.get_float(byte_array, start)
        z, start = ByteBuffer.get_float(byte_array, start)

        self.position = [x, y, z]

    def format(self):
        return self.byte_array


@command_mapping(CommandType.TANK_BODY_ROTATE, CommandUsage.DualWay)
class TankBodyRotateCommand(NetworkCommand):
    def __init__(self):
        super(TankBodyRotateCommand, self).__init__()
        self.driver_id = None
        self.body_rotation = None
        self.byte_array = None

    def parse(self, byte_array):
        self.byte_array = byte_array
        start = 4
        self.driver_id, start = ByteBuffer.get_byte(byte_array, start)
        self.body_rotation, start = ByteBuffer.get_float(byte_array, start)

    def format(self):
        return self.byte_array


@command_mapping(CommandType.TANK_TURRET_ROTATE, CommandUsage.DualWay)
class TankTurretRotateCommand(NetworkCommand):
    def __init__(self):
        super(TankTurretRotateCommand, self).__init__()
        self.driver_id = None
        self.turret_rotation = None
        self.byte_array = None

    def parse(self, byte_array):
        self.byte_array = byte_array

        start = 4
        self.driver_id, start = ByteBuffer.get_byte(byte_array, start)
        self.turret_rotation, start = ByteBuffer.get_float(byte_array, start)

    def format(self):
        return self.byte_array


@command_mapping(CommandType.DRIVE_TANK, CommandUsage.RecieveOnly)
class DriveTankCommand(NetworkCommand):
    def __init__(self):
        super(DriveTankCommand, self).__init__()
        self.driver_id = None

    def parse(self, byte_array):
        start = 4
        self.driver_id, start = ByteBuffer.get_byte(byte_array, start)


@command_mapping(CommandType.DRIVE_TANK_RESULT, CommandUsage.SendOnly)
class DriveTankResultCommand(NetworkCommand):
    def __init__(self, driver_id, is_drive):
        super(DriveTankResultCommand, self).__init__()
        self.driver_id = driver_id
        self.is_drive = is_drive

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.driver_id)
        byte_buffer.put_byte(1 if self.is_drive else 0)


@command_mapping(CommandType.REQUIRE_TANK_INFO, CommandUsage.RecieveOnly)
class RequireTankInfoCommand(NetworkCommand):
    def __init__(self):
        super(RequireTankInfoCommand, self).__init__()

    def parse(self, byte_array):
        pass


@command_mapping(CommandType.REQUIRE_TANK_INFO_RESULT, CommandUsage.RecieveOnly)
class RequireTankInfoResultCommand(NetworkCommand):
    def __init__(self, driver_id, is_driven, position, turret_rotation, body_rotation, hp):
        super(RequireTankInfoResultCommand, self).__init__()
        self.driver_id = driver_id
        self.is_driven = is_driven
        self.position = position
        self.turret_rotation = turret_rotation
        self.body_rotation = body_rotation
        self.hp = hp

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Float * 3)
        byte_buffer.extends(TypeSize.Float)
        byte_buffer.extends(TypeSize.Float)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.driver_id)
        byte_buffer.put_byte(1 if self.is_driven else 0)

        byte_buffer.put_float(self.position[0])
        byte_buffer.put_float(self.position[1])
        byte_buffer.put_float(self.position[2])

        byte_buffer.put_float(self.turret_rotation)
        byte_buffer.put_float(self.body_rotation)
        byte_buffer.put_int32(self.hp)


@command_mapping(CommandType.TANK_DESTROY, CommandUsage.SendOnly)
class TankDestroyCommand(NetworkCommand):
    def __init__(self):
        super(TankDestroyCommand, self).__init__()

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(0)


@command_mapping(CommandType.GAME_RESULT, CommandUsage.SendOnly)
class GameResultCommand(NetworkCommand):
    def __init__(self, result):
        super(GameResultCommand, self).__init__()
        self.result = result

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(1 if self.result else 0)


@command_mapping(CommandType.WAVE_OVER, CommandUsage.SendOnly)
class WaveOverCommand(NetworkCommand):
    def __init__(self):
        super(WaveOverCommand, self).__init__()

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(0)


@command_mapping(CommandType.NEW_WAVE, CommandUsage.RecieveOnly)
class NewWaveCommand(NetworkCommand):
    def __init__(self):
        super(NewWaveCommand, self).__init__()

    def calc_buffer_size(self, byte_buffer):
        pass

    def fill_buffer(self, byte_buffer):
        pass


@command_mapping(CommandType.NEW_GAME, CommandUsage.RecieveOnly)
class NewGameCommand(NetworkCommand):
    def __init__(self):
        super(NewGameCommand, self).__init__()

    def parse(self, byte_array):
        pass


@command_mapping(CommandType.NEW_GAME_RESULT, CommandUsage.SendOnly)
class NewGameResultCommand(NetworkCommand):
    def __init__(self, tank_position, tank_turret_rotation, tank_body_rotation, tank_hp, strongpoint_hp):
        super(NewGameResultCommand, self).__init__()
        self.tank_position = tank_position
        self.tank_turret_rotation = tank_turret_rotation
        self.tank_body_rotation = tank_body_rotation
        self.tank_hp = tank_hp
        self.strongpoint_hp = strongpoint_hp

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Float * 3)
        byte_buffer.extends(TypeSize.Float)
        byte_buffer.extends(TypeSize.Float)
        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_float(self.tank_position[0])
        byte_buffer.put_float(self.tank_position[1])
        byte_buffer.put_float(self.tank_position[2])

        byte_buffer.put_float(self.tank_turret_rotation)
        byte_buffer.put_float(self.tank_body_rotation)
        byte_buffer.put_int32(self.tank_hp)

        byte_buffer.put_uint32(self.strongpoint_hp)


@command_mapping(CommandType.REQUIRE_STRONG_POINT_INFO, CommandUsage.RecieveOnly)
class RequireStrongPointInfoCommand(NetworkCommand):
    def __init__(self):
        super(RequireStrongPointInfoCommand, self).__init__()

    def parse(self, byte_array):
        pass


@command_mapping(CommandType.REQUIRE_STRONG_POINT_INFO_RESULT, CommandUsage.SendOnly)
class RequireStrongPointInfoResultCommand(NetworkCommand):
    def __init__(self, position, hp):
        super(RequireStrongPointInfoResultCommand, self).__init__()
        self.position = position
        self.hp = hp

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Float * 3)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_float(self.position[0])
        byte_buffer.put_float(self.position[1])
        byte_buffer.put_float(self.position[2])

        byte_buffer.put_int32(self.hp)


@command_mapping(CommandType.STRONG_POINT_ATTACKED, CommandUsage.RecieveOnly)
class StrongPointAttackedCommand(NetworkCommand):
    def __init__(self):
        super(StrongPointAttackedCommand, self).__init__()
        self.enemy_id = None

    def parse(self, byte_array):
        start = 4

        self.enemy_id, start = ByteBuffer.get_uint32(byte_array, start)


@command_mapping(CommandType.STRONG_POINT_ATTACKED_RESULT, CommandUsage.SendOnly)
class StrongPointAttackedResultCommand(NetworkCommand):
    def __init__(self, enmey_id, hp):
        super(StrongPointAttackedResultCommand, self).__init__()
        self.enemy_id = enmey_id
        self.hp = hp

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.UInt32)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_uint32(self.enemy_id)
        byte_buffer.put_int32(self.hp)


@command_mapping(CommandType.BUY_TRAP, CommandUsage.RecieveOnly)
class BuyTrapCommand(NetworkCommand):
    def __init__(self):
        super(BuyTrapCommand, self).__init__()
        self.trap_type = None

    def parse(self, byte_array):
        start = 4

        self.trap_type, start = ByteBuffer.get_byte(byte_array, start)


@command_mapping(CommandType.BUY_TRAP_RESULT, CommandUsage.SendOnly)
class BuyTrapResultCommand(NetworkCommand):
    def __init__(self, gold, damage_trap_count, slow_trap_count):
        super(BuyTrapResultCommand, self).__init__()
        self.gold = gold
        self.damage_trap_count = damage_trap_count
        self.slow_trap_count = slow_trap_count

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Int32)
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Byte)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_int32(self.gold)
        byte_buffer.put_byte(self.damage_trap_count)
        byte_buffer.put_byte(self.slow_trap_count)


@command_mapping(CommandType.UPDATE_GOLD, CommandUsage.SendOnly)
class UpdateGoldCommand(NetworkCommand):
    def __init__(self, gold):
        super(UpdateGoldCommand, self).__init__()
        self.gold = gold

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_int32(self.gold)


@command_mapping(CommandType.BUY_GRENATE, CommandUsage.RecieveOnly)
class BuyGrenateCommand(NetworkCommand):
    def __init__(self):
        super(BuyGrenateCommand, self).__init__()

    def parse(self, byte_array):
        pass


@command_mapping(CommandType.BUY_GRENATE_RESULT, CommandUsage.SendOnly)
class BuyGrenateResultCommand(NetworkCommand):
    def __init__(self, grenate_count, gold):
        super(BuyGrenateResultCommand, self).__init__()
        self.grenate_count = grenate_count
        self.gold = gold

    def calc_buffer_size(self, byte_buffer):
        byte_buffer.extends(TypeSize.Byte)
        byte_buffer.extends(TypeSize.Int32)

    def fill_buffer(self, byte_buffer):
        byte_buffer.put_byte(self.grenate_count)
        byte_buffer.put_int32(self.gold)
