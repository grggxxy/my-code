import random
import logic
from logic import CommandType

from network.configure import CONFIGURE

GENERATE_MAX_X = CONFIGURE["GENERATE_MAX_X"]
GENERATE_MIN_X = CONFIGURE["GENERATE_MIN_X"]
GENERATE_MAX_Z = CONFIGURE["GENERATE_MAX_Z"]
GENERATE_MIN_Z = CONFIGURE["GENERATE_MIN_Z"]
GENERATE_Y = CONFIGURE["GENERATE_Y"]


def all_player_die(server):
    for player in server.socket_map.itervalues():
        if player.hp > 0:
            return False
    return True


def wave_over(server):
    if server.wait_for_new_wave:
        return

    if not logic.EnemyInfo.get_alive_enemies():
        if server.waves > 0:
            server.wait_for_new_wave = True
            server.broadcast_join(logic.WaveOverCommand())
            server.damage_rate += 1
        else:
            server.broadcast_join(logic.GameResultCommand(True), None)


def find_aoe_players(server, position, radius):
    result = []
    for player in server.socket_map.itervalues():
        r = (player.position[0] - position[0]) * (player.position[0] - position[0])\
            + (player.position[1] - position[1]) * (player.position[1] - position[1])\
            + (player.position[2] - position[2]) * \
            (player.position[2] - position[2])

        if r < radius * radius and player.hp > 0:
            result.append(player)

    return result


def is_strongpoint_in_aoe(position, radius):
    strongpoint_pos = logic.StrongPointInfo.position

    r = (strongpoint_pos[0] - position[0]) * (position[0] - position[0])\
        + (strongpoint_pos[1] - position[1]) * (position[1] - position[1])\
        + (strongpoint_pos[2] - position[2]) * \
        (strongpoint_pos[2] - position[2])

    return r < radius * radius


def find_aoe_enemies(server, position, radius):
    result = []
    lived_enemies = logic.EnemyInfo.get_alive_enemies()

    for enemy in lived_enemies:
        r = (enemy.position[0] - position[0]) * (enemy.position[0] - position[0])\
            + (enemy.position[1] - position[1]) * (enemy.position[1] - position[1])\
            + (enemy.position[2] - position[2]) * \
            (enemy.position[2] - position[2])

        if r < radius * radius and enemy.hp > 0:
            result.append(enemy)

    return result


class Dispatcher(object):
    def __init__(self):
        self.dispach_map = {}

    def register_handler(self, command_type, call_back):
        self.dispach_map[command_type] = call_back

    def dispatch(self, command_type, command, server, sender):
        if self.dispach_map.has_key(command_type):
            self.dispach_map[command_type](command, server, sender)
        else:
            raise Exception("Invaild command type : {}".format(command_type))


commandDispatcher = Dispatcher()


def command_handler(command_type):
    def wrapper(func):
        commandDispatcher.register_handler(command_type, func)

        def wrapper2(*args, **kwargs):
            return func(args, kwargs)
        return wrapper2
    return wrapper


@command_handler(CommandType.MOVE)
def move_handler(command, server, sender):
    # update player info
    if server.socket_map.has_key(sender):
        info = server.socket_map[sender]
        info.position = command.position
        info.rotation = command.rotation

        server.broadcast_join(command, sender)


@command_handler(CommandType.LOGIN_REQUEST)
def login_request_handler(command, server, sender):
    username = command.username
    password = command.password

    result, database_id = server.sqlite.login_in(username, password)
    if result:
        # already login
        if server.database_id_map.has_key(database_id):
            server.send(logic.LoginResultCommand(False, 0), sender)
        else:
            server.database_id_map[sender] = database_id
            # alloc user id
            user_id = server.login(sender)
            server.send(logic.LoginResultCommand(True, user_id), sender)
            print "alloc {}".format(user_id)
    else:
        server.send(logic.LoginResultCommand(False, 0), sender)


@command_handler(CommandType.JOIN)
def join_handler(command, server, sender):
    player_count = len(server.socket_map)
    is_host = player_count == 0

    player_info = logic.PlayerInfo(sender, command.user_id)
    player_info.database_id = server.database_id_map[sender]

    server.sqlite.load_user_info(player_info)

    server.init_player(command.user_id, player_info)

    if player_info.hp <= 0:
        player_info.hp = CONFIGURE["PLAYER_REBORN_HP"]

    player_info.is_host = is_host
    init_cmd = logic.InitPlayerInfoCommand(player_info, is_host)

    server.send(init_cmd, sender)
    server.broadcast_join(
        logic.NewPlayerCommand(player_info, player_info.position, player_info.rotation), sender)

    # new game
    if is_host:
        server.host = sender


@command_handler(CommandType.PLAYER_LIST)
def player_list_handler(command, server, sender):
    cmd = logic.PlayerListResultCommand(server, sender)
    server.send(cmd, sender)


@command_handler(CommandType.SHOOT)
def shoot_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        info = server.socket_map[sender]
        # info.shoot(command.bullet_count)

        if command.is_granate:
            # no response
            if info.grenade_count <= 0:
                return
            info.lose_granate()

        cmd = logic.ShootResultCommand(
            info.cur_bullet, info.max_bullet, info.grenade_count)

        server.send(cmd, sender)
        server.broadcast_join(command, sender)


@command_handler(CommandType.BULLET_DESTROY)
def bullet_move_destroy_handler(command, server, sender):
    server.broadcast_join(command, sender)


@command_handler(CommandType.PLAYER_ROTATE)
def player_rotate_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        server.socket_map[sender].rotation = command.rotation
        server.broadcast_join(command, sender)


@command_handler(CommandType.ENEMY_MOVE)
def enemy_move_handler(command, server, sender):
    enemy = logic.EnemyInfo.get_enemy(command.enemy_id)
    if enemy:
        enemy.position = command.position
        enemy.rotation = command.rotation

    server.broadcast_join(command, sender)


@command_handler(CommandType.ENEMY_LIST)
def enemy_list_handler(command, server, sender):
    enemy_info = logic.EnemyInfo.get_alive_enemies()

    player_count = len(server.socket_map)
    is_host = player_count == 0

    cmd_result = logic.EnemyListResultCommand(enemy_info, is_host)
    server.send(cmd_result, sender)


@command_handler(CommandType.DEBUFF_ATTACH)
def debuff_attach_handler(command, server, sender):
    result_cmd = logic.DebuffAttachResultCommand(
        command.target_enemy_id,
        command.debuff_id,
        command.debuff_duration
    )

    server.send(result_cmd, sender)


@command_handler(CommandType.DEBUFF_REMOVE)
def debuff_remove_handler(command, server, sender):
    result_cmd = logic.DebuffRemoveResultCommand(
        command.target_enemy_id,
        command.debuff_id,
    )

    server.send(result_cmd, sender)


@command_handler(CommandType.RECHARGE)
def recharge_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        user_info = server.socket_map[sender]
        user_info.recharge()

        server.send(logic.RechargeResultCommand(
            user_info.cur_bullet, user_info.max_bullet), sender)
        server.broadcast_join(command, sender)


@command_handler(CommandType.PICK_UP_ITEM)
def pick_up_item_handler(command, server, sender):
    item = logic.ItemInfo.get_item(command.item_id)
    if item:
        player_info = server.socket_map[sender]
        item.apply(player_info)

        logic.ItemInfo.remove_item(command.item_id)

        server.broadcast_join(
            logic.PickUpItemResultCommand(item.item_id), None)
        if item.item_type == logic.iteminfo.RECOVER_HP:
            server.broadcast_join(logic.RecoverHpCommand(
                command.player_id, player_info.hp), None)
        elif item.item_type == logic.iteminfo.SUPPLY_BULLETS:
            server.broadcast_join(logic.SupplyBulletsCommand(
                command.player_id, player_info.cur_bullet, player_info.max_bullet), None)
        # elif item.item_type == logic.iteminfo.FAN_SHOOTING:
        #     server.broadcast_join(
        #         logic.FanShootingCommand(command.player_id), None)
        elif item.item_type == logic.iteminfo.RUNNING_SHOOTING:
            server.broadcast_join(
                logic.RunningShootingCommand(command.player_id), None)
        else:
            raise Exception("invaild item")
    else:
        server.send(logic.PickUpItemResultCommand(command.item_id), sender)


@command_handler(CommandType.ITEM_LIST)
def item_list_handler(command, server, sender):
    cmd_result = logic.ItemListResultCommand(logic.ItemInfo.get_all_items())
    server.send(cmd_result, sender)


@command_handler(CommandType.PLAYER_REBORN)
def player_reborn_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        player_info = server.socket_map[sender]

        if player_info.max_bullet <= 90:
            player_info.max_bullet = CONFIGURE["PLAYRE_REBORN_MAXBULLETS"]
            player_info.cur_bullet = CONFIGURE["PLAYER_REBORN_BULLETS"]

        if player_info.hp <= 0:
            player_info.hp = CONFIGURE["PLAYER_REBORN_HP"]

        cmd_result = logic.PlayerRebornResultCommand(
            player_info.user_id, player_info.hp, player_info.cur_bullet, player_info.max_bullet)
        server.broadcast_join(cmd_result, None)


@command_handler(CommandType.REGISTER)
def register_handler(command, server, sender):
    result = server.sqlite.sign_in(command.username, command.password)
    server.send(logic.RegisterResultCommand(result), sender)


@command_handler(CommandType.LEAVE)
def leave_handler(command, server, sender):
    player_info = server.socket_map[sender]
    server.sqlite.save_user_info(player_info)

    server.broadcast_join(command, sender)
    server.send(logic.LeaveResultCommand(), sender)
    server.player_leave(sender)


@command_handler(CommandType.GUN_SHOOT)
def gun_shoot_handler(command, server, sender):
    if not server.socket_map.has_key(sender):
        return

    player_info = server.socket_map[sender]
    player_info.shoot(command.bullet_count)

    server.broadcast_join(command, sender)

    if command.is_hit:
        enemy_info = logic.EnemyInfo.get_enemy(command.hit_enemy_id)

        if enemy_info:
            enemy_info.hp -= command.bullet_count * \
                CONFIGURE["PLAYER_NORMAL_SHOOT_DAMAGE"]

            server.broadcast_join(logic.GunShootResultCommand(
                command.shooter_id,
                command.hit_enemy_id,
                enemy_info.hp if enemy_info.hp >= 0 else 0,
                player_info.cur_bullet,
                player_info.max_bullet,
                True if command.is_hit == 1 else False), None)

            if enemy_info.hp <= 0:
                logic.EnemyInfo.remove_enmey(command.hit_enemy_id)
                server.broadcast_join(
                    logic.EnemyDieCommand(command.shooter_id, command.hit_enemy_id), None)

                player_info.increase_kills()
                server.send(logic.UpdatePlayerKillCountCommand(
                    player_info.kills), sender)

                player_info.gain_gold(CONFIGURE["ENMEY_GOLD"])
                server.send(logic.UpdateGoldCommand(player_info.gold), sender)

                # generate item
                item = logic.ItemInfo.random_generate_item()
                if item:
                    position = enemy_info.position
                    item.position = position
                    server.broadcast_join(logic.GenerateItemCommand(
                        item.item_id, item.item_type, position))

        else:
            server.send(
                logic.GunShootResultCommand(
                    command.shooter_id,
                    command.hit_enemy_id,
                    0,
                    player_info.cur_bullet,
                    player_info.max_bullet,
                    True if command.is_hit == 1 else False), sender)

        wave_over(server)

    else:
        server.send(
            logic.GunShootResultCommand(
                command.shooter_id,
                0,
                0,
                player_info.cur_bullet,
                player_info.max_bullet,
                False), sender)


@command_handler(CommandType.ENMEY_ATTACK)
def enemy_attack_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        player_info = server.get_player_info_by_user_id(
            command.attacked_user_id)
        if player_info:
            if logic.TankInfo.driver_id == player_info.user_id:
                if logic.TankInfo.hp >= 0:
                    logic.TankInfo.hp -= 5 * server.damage_rate
                    if logic.TankInfo.hp <= 0:
                        logic.TankInfo.driver_id = 0xff
                        cmd_destroy = logic.TankDestroyCommand()
                        server.broadcast_join(cmd_destroy, None)
                server.broadcast_join(command, sender)
                server.broadcast_join(logic.EnemyAttackResultCommand(
                    command.attacked_user_id, logic.TankInfo.hp), None)
            else:
                if player_info.hp >= 0:
                    player_info.hp -= CONFIGURE["ENMEY_ATTACK_DAMAGE"] * \
                        server.damage_rate
                    if player_info.hp <= 0:
                        cmd_die = logic.PlayerDieCommand(player_info.user_id)
                        server.broadcast_join(cmd_die, None)

                server.broadcast_join(command, sender)
                server.broadcast_join(logic.EnemyAttackResultCommand(
                    command.attacked_user_id, player_info.hp), None)

                if all_player_die(server):
                    server.broadcast_join(logic.GameResultCommand(False), None)
        else:
            server.send(logic.EnemyAttackResultCommand(
                command.attacked_user_id, 0), sender)

    else:
        server.send(logic.EnemyAttackResultCommand(
            command.attacked_user_id, 0), sender)


@command_handler(CommandType.ENEMY_GUN_SHOOT)
def enmey_gun_shoot_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        player_info = server.get_player_info_by_user_id(command.hit_target_id)
        if player_info:
            if command.is_hit:
                if logic.TankInfo.driver_id == player_info.user_id:
                    if logic.TankInfo.hp >= 0:
                        logic.TankInfo.hp -= 10 * server.damage_rate
                        if logic.TankInfo.hp <= 0:
                            logic.TankInfo.driver_id = 0xff
                            cmd_destroy = logic.TankDestroyCommand()
                            server.broadcast_join(cmd_destroy, None)

                    cmd_result = logic.EnmeyGunShootResultCommand(
                        command.shooter_id, player_info.user_id, command.is_hit, logic.TankInfo.hp)
                    server.broadcast_join(cmd_result, None)
                else:
                    if player_info.hp >= 0:
                        player_info.hp -= CONFIGURE["ENMEY_BULLET_DAMAGE"] * \
                            server.damage_rate
                        if player_info.hp <= 0:
                            cmd_die = logic.PlayerDieCommand(
                                player_info.user_id)
                            server.broadcast_join(cmd_die, None)

                    cmd_result = logic.EnmeyGunShootResultCommand(
                        command.shooter_id, player_info.user_id, command.is_hit, player_info.hp)
                    server.broadcast_join(cmd_result, None)

                    if all_player_die(server):
                        server.broadcast_join(
                            logic.GameResultCommand(False), None)
            else:
                cmd_result = logic.EnmeyGunShootResultCommand(
                    command.shooter_id, player_info.user_id, command.is_hit, 0)
                server.send(cmd_result, sender)
        else:
            cmd_result = logic.EnmeyGunShootResultCommand(
                command.shooter_id, command.hit_target_id, command.is_hit, 0)
            server.send(cmd_result, sender)


@command_handler(CommandType.SELF_DESTRUCT)
def self_destruct_command_handler(command, server, sender):
    players = find_aoe_players(server, command.position, 10.0)

    for player_info in players:
        if logic.TankInfo.driver_id == player_info.user_id:
            if logic.TankInfo.hp >= 0:
                logic.TankInfo.hp -= 10 * server.damage_rate
                if logic.TankInfo.hp <= 0:
                    logic.TankInfo.driver_id = 0xff
                    cmd_destroy = logic.TankDestroyCommand()
                    server.broadcast_join(cmd_destroy, None)
            server.broadcast_join(
                logic.SelfDestuctResultCommand(
                    player_info.user_id, logic.TankInfo.hp, command.enemy_id),
                None)
        else:
            if player_info.hp >= 0:
                player_info.hp -= 20 * server.damage_rate
                if player_info.hp <= 0:
                    cmd_die = logic.PlayerDieCommand(player_info.user_id)
                    server.broadcast_join(cmd_die, None)

            server.broadcast_join(
                logic.SelfDestuctResultCommand(
                    player_info.user_id, player_info.hp, command.enemy_id),
                None)

    logic.EnemyInfo.remove_enmey(command.enemy_id)
    server.broadcast_join(logic.EnemyDieCommand(0, command.enemy_id), None)

    if all_player_die(server):
        server.broadcast_join(logic.GameResultCommand(False), None)
        return

    if is_strongpoint_in_aoe(command.position, 10.0):
        logic.StrongPointInfo.hp -= 20 * server.damage_rate
        cmd_result = logic.StrongPointAttackedResultCommand(
            command.enemy_id,
            logic.StrongPointInfo.hp)
        server.broadcast_join(cmd_result, None)

        if logic.StrongPointInfo.hp <= 0:
            cmd_end = logic.GameResultCommand(False)
            server.broadcast_join(cmd_end, None)
        else:
            wave_over(server)
    else:
        wave_over(server)


@command_handler(CommandType.EXPLODE)
def explode_command_handler(command, server, sender):
    enemies = find_aoe_enemies(server, command.position, 10.0)

    for enemy_info in enemies:
        if enemy_info.hp >= 0:
            enemy_info.hp -= CONFIGURE["PLAYER_EXPLODE_DAMAGE"]
            server.broadcast_join(
                logic.ExplodeResultCommand(enemy_info.enemy_id, enemy_info.hp), None)

            if enemy_info.hp <= 0:
                logic.EnemyInfo.remove_enmey(enemy_info.enemy_id)
                server.broadcast_join(
                    logic.EnemyDieCommand(command.thrower, enemy_info.enemy_id), None)

                player_info = server.socket_map[sender]

                player_info.increase_kills()
                server.send(logic.UpdatePlayerKillCountCommand(
                    player_info.kills), sender)

                player_info.gain_gold(CONFIGURE["ENMEY_GOLD"])
                server.send(logic.UpdateGoldCommand(player_info.gold), sender)

                # generate item
                item = logic.ItemInfo.random_generate_item()
                if item:
                    position = enemy_info.position
                    item.position = position
                    server.broadcast_join(logic.GenerateItemCommand(
                        item.item_id, item.item_type, position))

    server.send(
        logic.BulletHitResultCommand(
            command.thrower,
            0,
            0,
            command.bullet_id), sender)

    wave_over(server)


@command_handler(CommandType.PUT_TRAP)
def put_trap_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        player_info = server.socket_map[sender]

        trap_type = command.trap_type

        if trap_type == logic.DAMAGE_TRAP:
            player_info.lose_damage_trap()
            count = player_info.damage_trap_count
        elif trap_type == logic.SLOW_DOWN_TRAP:
            player_info.lose_slow_trap()
            count = player_info.slow_trap_count

        new_trap = logic.TrapInfo(
            command.trap_type,
            command.position,
            command.rotation
        )

        logic.TrapInfo.add_new_trap(new_trap)

        cmd_result = logic.PutTrapResultCommand(
            command.builder_id,
            new_trap.position,
            new_trap.rotation,
            new_trap.trap_type,
            count
        )

        server.broadcast_join(cmd_result, None)


@command_handler(CommandType.ENMEY_DEBUFF_DAMAGED)
def enemy_debuff_damaged_handler(command, server, sender):
    enemy_info = logic.EnemyInfo.get_enemy(command.enemy_id)

    if enemy_info:
        if enemy_info.hp >= 0:
            enemy_info.hp -= 10
            server.broadcast_join(
                logic.EnemyDebuffDamagedResultCommand(enemy_info.enemy_id, enemy_info.hp), None)

            if enemy_info.hp <= 0:
                logic.EnemyInfo.remove_enmey(enemy_info.enemy_id)
                server.broadcast_join(
                    logic.EnemyDieCommand(0, enemy_info.enemy_id), None)

                # generate item
                item = logic.ItemInfo.random_generate_item()
                if item:
                    position = enemy_info.position
                    item.position = position
                    server.broadcast_join(logic.GenerateItemCommand(
                        item.item_id, item.item_type, position))

        wave_over(server)


@command_handler(CommandType.TRAP_LIST)
def trap_list_handler(command, server, sender):
    traps = logic.TrapInfo.get_traps()
    cmd_result = logic.TrapListResultCommand(traps)

    server.send(cmd_result, sender)


@command_handler(CommandType.TANK_MOVE)
def tank_move_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        logic.TankInfo.position = command.position

        server.broadcast_join(command, sender)


@command_handler(CommandType.TANK_BODY_ROTATE)
def tank_body_rotate_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        logic.TankInfo.body_rotation = command.body_rotation

        server.broadcast_join(command, sender)


@command_handler(CommandType.TANK_TURRET_ROTATE)
def tank_turret_rotate_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        logic.TankInfo.turret_rotation = command.turret_rotation

        server.broadcast_join(command, sender)


@command_handler(CommandType.DRIVE_TANK)
def drive_tank_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        if logic.TankInfo.is_driven:
            logic.TankInfo.driver_id = 0xff
            logic.TankInfo.is_driven = False
        else:
            logic.TankInfo.driver_id = command.driver_id
            logic.TankInfo.is_driven = True

        server.broadcast_join(
            logic.DriveTankResultCommand(logic.TankInfo.driver_id, logic.TankInfo.is_driven), None)


@command_handler(CommandType.REQUIRE_TANK_INFO)
def require_tank_info_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        cmd_result = logic.RequireTankInfoResultCommand(
            logic.TankInfo.driver_id,
            logic.TankInfo.is_driven,
            logic.TankInfo.position,
            logic.TankInfo.turret_rotation,
            logic.TankInfo.body_rotation,
            logic.TankInfo.hp
        )

        server.send(cmd_result, sender)


@command_handler(CommandType.NEW_WAVE)
def new_wave_handler(command, server, sender):
    server.wait_for_new_wave = False

    server.waves -= 1
    new_enemies = logic.EnemyInfo.random_generate_enemy()
    generate_enemy_cmd_h = logic.GenerateEnemyCommand(new_enemies, True)
    generate_enemy_cmd = logic.GenerateEnemyCommand(new_enemies, False)
    server.send(generate_enemy_cmd_h, server.host)
    server.broadcast_join(generate_enemy_cmd, server.host)


@command_handler(CommandType.NEW_GAME)
def new_game_handler(command, server, sender):
    server.reset()

    cmd_result = logic.NewGameResultCommand(
        logic.TankInfo.position,
        logic.TankInfo.turret_rotation,
        logic.TankInfo.body_rotation,
        logic.TankInfo.hp,
        logic.StrongPointInfo.hp
    )

    server.broadcast_join(cmd_result, None)

    # reborn dead player
    for player_info in server.socket_map.itervalues():
        if player_info.max_bullet <= 90:
            player_info.max_bullet = CONFIGURE["PLAYRE_REBORN_MAXBULLETS"]
            player_info.cur_bullet = CONFIGURE["PLAYER_REBORN_BULLETS"]

        if player_info.hp <= 0:
            player_info.hp = CONFIGURE["PLAYER_REBORN_HP"]

        cmd_result = logic.PlayerRebornResultCommand(
            player_info.user_id, player_info.hp, player_info.cur_bullet, player_info.max_bullet)
        server.broadcast_join(cmd_result, None)

    server.wait_for_new_wave = True
    server.broadcast_join(logic.WaveOverCommand(), None)


@command_handler(CommandType.REQUIRE_STRONG_POINT_INFO)
def require_strong_point_info_handler(command, server, sender):
    cmd_result = logic.RequireStrongPointInfoResultCommand(
        logic.StrongPointInfo.position,
        logic.StrongPointInfo.hp)

    server.send(cmd_result, sender)


@command_handler(CommandType.STRONG_POINT_ATTACKED)
def strong_point_attacked_handler(command, server, sender):
    if logic.StrongPointInfo.hp > 0:
        enemy = logic.EnemyInfo.get_enemy(command.enemy_id)
        if enemy:
            if enemy.enemy_type == logic.SHOOTING:
                logic.StrongPointInfo.hp -= CONFIGURE["ENMEY_BULLET_DAMAGE"] * \
                    server.damage_rate
            elif enemy.enemy_type == logic.MELEE:
                logic.StrongPointInfo.hp -= CONFIGURE["ENMEY_ATTACK_DAMAGE"] * \
                    server.damage_rate

            cmd_result = logic.StrongPointAttackedResultCommand(
                command.enemy_id,
                logic.StrongPointInfo.hp)
            server.broadcast_join(cmd_result, None)

        if logic.StrongPointInfo.hp <= 0:
            cmd_end = logic.GameResultCommand(False)
            server.broadcast_join(cmd_end, None)


@command_handler(CommandType.BUY_TRAP)
def buy_trap_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        player_info = server.socket_map[sender]

        if command.trap_type == logic.SLOW_DOWN_TRAP:
            player_info.buy_slow_trap()
        elif command.trap_type == logic.DAMAGE_TRAP:
            player_info.buy_damage_trap()

        server.send(logic.BuyTrapResultCommand(
            player_info.gold,
            player_info.damage_trap_count,
            player_info.slow_trap_count
        ), sender)


@command_handler(CommandType.BUY_GRENATE)
def but_grenate_handler(command, server, sender):
    if server.socket_map.has_key(sender):
        player_info = server.socket_map[sender]

        player_info.buy_grenade()

        server.send(
            logic.BuyGrenateResultCommand(
                player_info.grenade_count, player_info.gold),
            sender
        )
