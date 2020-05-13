import random

from network.configure import CONFIGURE
import logic.playerinfo as playerinfo

RECOVER_HP = 0
SUPPLY_BULLETS = 1
RUNNING_SHOOTING = 2
# FAN_SHOOTING = 3

ITEM_COLLECTION = [
    RECOVER_HP,
    SUPPLY_BULLETS,
    RUNNING_SHOOTING,
]


class ItemInfo(object):
    _item_count = 0
    _generated_item_map = {}

    @classmethod
    def reset(cls):
        cls._item_count = 0
        cls._generated_item_map.clear()

    @classmethod
    def random_generate_item(cls):
        if len(cls._generated_item_map) > CONFIGURE["MAX_GENERABLE_ITEM"]:
            return None
        item_type = random.sample(ITEM_COLLECTION, 1)[0]
        item = ItemInfo(item_type)
        cls._generated_item_map[item.item_id] = item
        return item

    @classmethod
    def get_all_items(cls):
        return [v for _, v in cls._generated_item_map.items()]

    @classmethod
    def get_item(cls, index):
        if cls._generated_item_map.has_key(index):
            return cls._generated_item_map[index]
        else:
            return None

    @classmethod
    def remove_item(cls, index):
        del cls._generated_item_map[index]

    def __init__(self, item_type):
        self.item_id = ItemInfo._item_count
        self.item_type = item_type
        ItemInfo._item_count += 1
        self.position = None

    def apply(self, player_info):
        if self.item_id == RECOVER_HP:
            player_info.recover(CONFIGURE["ITEM_RECOVER_HP"])
        elif self.item_id == SUPPLY_BULLETS:
            player_info.supply(CONFIGURE["ITEM_SUPPLY"])
        # elif self.item_id == FAN_SHOOTING:
        #     player_info.shooting_type = playerinfo.FAN_SHOOTING
        elif self.item_id == RUNNING_SHOOTING:
            player_info.shooting_type = playerinfo.RUNNING_SHOOTING
