SLOW_DOWN_TRAP = 0
DAMAGE_TRAP = 1


class TrapInfo(object):
    _trap_count = 0
    _trap_set = []

    @classmethod
    def add_new_trap(cls, trap):
        cls._trap_set.append(trap)

    @classmethod
    def reset(cls):
        cls._trap_count = 0
        del cls._trap_set[:]

    @classmethod
    def get_traps(cls):
        return cls._trap_set

    def __init__(self, trap_type, position, rotation):
        self.trap_type = trap_type
        self.position = position
        self.rotation = rotation
        self.trap_id = TrapInfo._trap_count
        TrapInfo._trap_count += 1
