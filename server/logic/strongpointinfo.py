from network.configure import CONFIGURE

class StrongPointInfo(object):
    hp = CONFIGURE["STRONG_POINT_INIT_HP"]
    position = CONFIGURE["STRONG_POINT_POSITION"]

    @classmethod
    def reset(cls):
        cls.hp = CONFIGURE["STRONG_POINT_INIT_HP"]
