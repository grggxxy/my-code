from network.configure import CONFIGURE

NORMAL_SHOOTING = 0
RUNNING_SHOOTING = 1
# FAN_SHOOTING = 2


class PlayerInfo(object):
    def __init__(self, socket, user_id):
        self.socket = socket
        self.user_id = user_id
        self.cur_bullet = 30
        self.max_bullet = 900
        self.player_type = 0
        self.position = [0, 0, 0]
        self.rotation = 0
        self.hp = 1000
        self.shooting_type = NORMAL_SHOOTING
        self.kills = 0
        self.database_id = -1
        self.is_host = False

        self.gold = 0
        self.slow_trap_count = 0
        self.damage_trap_count = 0
        self.grenade_count = 0

    def initialize(self, position, rotation):
        self.player_type = 0
        self.position = position
        self.rotation = rotation

    def shoot(self, count):
        if count >= self.cur_bullet:
            self.max_bullet -= self.cur_bullet
            self.cur_bullet = 0
        else:
            self.cur_bullet -= count
            self.max_bullet -= count

    def recharge(self):
        if self.max_bullet >= 30:
            self.cur_bullet = 30
        else:
            self.cur_bullet = self.max_bullet

    def recover(self, hp):
        self.hp += hp
        self.hp = min(1000, self.hp)

    def supply(self, bullets):
        remain = self.max_bullet - self.cur_bullet
        remain = min(900, remain + bullets)
        self.max_bullet = remain + self.cur_bullet

    def increase_kills(self):
        self.kills += 1

    def gain_gold(self, gold):
        self.gold += gold

    def lose_gold(self, gold):
        self.gold -= gold

    def buy_grenade(self):
        if self.grenade_count >= 16:
            return

        if self.gold >= CONFIGURE["GRENADE_PRICE"]:
            self.gold -= CONFIGURE["GRENADE_PRICE"]
            self.grenade_count += 1

    def buy_slow_trap(self):
        if self.slow_trap_count >= 64:
            return

        if self.gold >= CONFIGURE["SLOW_TRAP_PRICE"]:
            self.gold -= CONFIGURE["SLOW_TRAP_PRICE"]
            self.slow_trap_count += 1

    def buy_damage_trap(self):
        if self.damage_trap_count >= 64:
            return

        if self.gold >= CONFIGURE["DAMAGE_TRAP_PRICE"]:
            self.gold -= CONFIGURE["DAMAGE_TRAP_PRICE"]
            self.damage_trap_count += 1

    def lose_slow_trap(self):
        if self.slow_trap_count > 0:
            self.slow_trap_count -= 1

    def lose_damage_trap(self):
        if self.damage_trap_count > 0:
            self.damage_trap_count -= 1

    def lose_granate(self):
        if self.grenade_count > 0:
            self.grenade_count -= 1
