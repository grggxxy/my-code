import random

from network.configure import CONFIGURE

SHOOTING = 0
MELEE = 1
SELF_DESTRUCT = 2

GENERATE_MAX_X = CONFIGURE["GENERATE_MAX_X"]
GENERATE_MIN_X = CONFIGURE["GENERATE_MIN_X"]
GENERATE_MAX_Z = CONFIGURE["GENERATE_MAX_Z"]
GENERATE_MIN_Z = CONFIGURE["GENERATE_MIN_Z"]
GENERATE_Y = CONFIGURE["GENERATE_Y"]

class EnemyInfo(object):
    _enemy_count = 0
    _max_enemy_count = CONFIGURE["MAX_ENEMY_COUNT"]
    _generated_enemy_count = 0
    _lived_enemy_map = {}

    @classmethod
    def reset(cls):
        cls._max_enemy_count = CONFIGURE["MAX_ENEMY_COUNT"]
        cls._generated_enemy_count = 0
        cls._lived_enemy_map.clear()

    @classmethod
    def random_generate_enemy(cls):
        min_c = CONFIGURE["RANDOM_GENERATED_ENEMY_COUNT"][0]
        max_c = CONFIGURE["RANDOM_GENERATED_ENEMY_COUNT"][1]
        count = min(cls._max_enemy_count - cls._generated_enemy_count, random.randint(min_c, max_c))

        enemies = []
        for _ in range(count):
            enemy = EnemyInfo()
            enemy.initialize(
                random.randint(0, 2),
                # 1,
                # 2,
                [
                    random.uniform(GENERATE_MIN_X, GENERATE_MAX_X),
                    GENERATE_Y,
                    random.uniform(GENERATE_MIN_Z, GENERATE_MAX_Z)
                ],
                random.uniform(0.0, 1.0)
            )
            enemies.append(enemy)
            cls._lived_enemy_map[enemy.enemy_id] = enemy
        return enemies

    @classmethod
    def is_all_enemy_die(cls):
        return not cls._lived_enemy_map and cls._generated_enemy_count == cls._max_enemy_count

    @classmethod
    def get_alive_enemies(cls):
        return [v for _, v in cls._lived_enemy_map.items()]

    @classmethod
    def get_enemy(cls, enemy_id):
        if cls._lived_enemy_map.has_key(enemy_id):
            return cls._lived_enemy_map[enemy_id]
        else:
            return None

    @classmethod
    def remove_enmey(cls, enemy_id):
        if cls._lived_enemy_map.has_key(enemy_id):
            del cls._lived_enemy_map[enemy_id]

    def __init__(self):
        self.enemy_type = None
        self.hp = None
        self.position = None
        self.rotation = None

        self.enemy_id = EnemyInfo._enemy_count
        EnemyInfo._enemy_count += 1

    def initialize(self, enemy_type, position, rotation):
        self.enemy_type = enemy_type
        if enemy_type == 0:
            self.hp = CONFIGURE["ENMEY_SHOOTER_INIT_HP"]
        else:
            self.hp = CONFIGURE["ENMEY_ATTACKER_INIT_HP"]
        self.position = position
        self.rotation = rotation
