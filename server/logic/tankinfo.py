from network.configure import CONFIGURE


class TankInfo(object):
    position = CONFIGURE["TANK_INIT_POSITION"]
    turret_rotation = 0.0
    body_rotation = 0.0
    driver_id = 0xff
    is_driven = False
    hp = CONFIGURE["TANK_INIT_HP"]

    @classmethod
    def reset(cls):
        cls.position = CONFIGURE["TANK_INIT_POSITION"]
        cls.turret_rotation = 0.0
        cls.body_rotation = 0.0
        cls.driver_id = 0xff
        cls.is_driven = False
        cls.hp = CONFIGURE["TANK_INIT_HP"]
