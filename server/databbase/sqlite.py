import sqlite3
import random

from network.configure import CONFIGURE

GENERATE_MAX_X = CONFIGURE["GENERATE_MAX_X"]
GENERATE_MIN_X = CONFIGURE["GENERATE_MIN_X"]
GENERATE_MAX_Z = CONFIGURE["GENERATE_MAX_Z"]
GENERATE_MIN_Z = CONFIGURE["GENERATE_MIN_Z"]
GENERATE_Y = CONFIGURE["GENERATE_Y"]


class Sqlite(object):
    def __init__(self, database_name):
        self.database_name = database_name
        self.conn = None

    def initialize(self):
        self.conn = sqlite3.connect(self.database_name)
        print "Opened database successfully"
        c = self.conn.cursor()

        cursor = c.execute('''
            SELECT COUNT(*) from sqlite_master where type='table' and name = 'PLAYER';
        ''')

        first = cursor.fetchone()
        if first and first[0] == 0:
            c.execute('''CREATE TABLE PLAYER
                (id         INT     PRIMARY KEY     NOT NULL,
                username   TEXT    NOT NULL,
                password   TEXT     NOT NULL,
                hp         INT     NOT NULL,
                shoottype  BYTE     NOT NULL,
                kills      INT     NOT NULL,
                curbullets INT     NOT NULL,
                maxbullets INT     NOT NULL,
                posx       FLOAT   NOT NULL,
                posy       FLOAT   NOT NULL,
                posz       FLOAT   NOT NULL,
                rotation   FLOAT   NOT NULL,
                gold       INT     NOT NULL,
                damagetrap INT     NOT NULL,
                slowtrap   INT     NOT NULL,
                grenade    INT     NOT NULL,
                );''')
            print "Table created successfully"
            self.conn.commit()

    def close(self):
        self.conn.close()

    def save_user_info(self, playerinfo):
        c = self.conn.cursor()

        c.execute('''
        UPDATE PLAYER set hp = ?, shoottype = ?, kills = ?, curbullets = ?,
                          maxbullets = ?, posx = ?, posy = ?, posz = ?, rotation = ?,
                          gold = ?, damagetrap = ?, slowtrap = ?, grenade = ?
                          where id = ?
        ''', (playerinfo.hp, playerinfo.shooting_type, playerinfo.kills, playerinfo.cur_bullet,
              playerinfo.max_bullet, playerinfo.position[0], playerinfo.position[1], playerinfo.position[2],
              playerinfo.rotation, playerinfo.gold, playerinfo.damage_trap_count, playerinfo.slow_trap_count,
              playerinfo.grenade_count, playerinfo.database_id))

        self.conn.commit()

        print "save playerinfo."

    def load_user_info(self, player_info):
        c = self.conn.cursor()

        cursor = c.execute('''
        SELECT hp, shoottype, kills, curbullets, maxbullets, posx, posy, posz, rotation, gold, damagetrap, slowtrap, grenade FROM PLAYER WHERE id = ?
        ''', (player_info.database_id,))

        first = cursor.fetchone()
        player_info.hp = first[0]
        player_info.shooting_type = first[1]
        player_info.kills = first[2]
        player_info.cur_bullet = first[3]
        player_info.max_bullet = first[4]
        player_info.position = [first[5], first[6], first[7]]
        player_info.rotation = first[8]
        player_info.gold = first[9]
        player_info.damage_trap_count = first[10]
        player_info.slow_trap_count = first[11]
        player_info.grenade_count = first[12]

        print "load playerinfo."

    def login_in(self, username, password):
        c = self.conn.cursor()

        cursor = c.execute('''
            SELECT id, password FROM PLAYER WHERE username = ?
        ''', (username,))

        first = cursor.fetchone()
        if first:
            real_password = first[1]

            if real_password == password:
                return True, first[0]

        return False, -1

    def sign_in(self, username, password):
        c = self.conn.cursor()

        cursor = c.execute('''
            SELECT id FROM PLAYER WHERE username = ?
        ''', (username,))

        first = cursor.fetchone()
        if first:
            return False

        cursor = c.execute('''
            SELECT COUNT(*) from PLAYER;
        ''')

        count = cursor.fetchone()[0]

        position = [
            random.uniform(GENERATE_MIN_X, GENERATE_MAX_X),
            GENERATE_Y,
            random.uniform(GENERATE_MIN_Z, GENERATE_MAX_Z),
        ]

        rotation = random.uniform(0.0, 1.0)

        c.execute('''
            INSERT INTO PLAYER
            (id, username, password, hp, shoottype, kills, curbullets, maxbullets,
             posx, posy, posz, rotation, gold, damagetrap, slowtrap, grenade) VALUES(?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        ''', (count, username, password,
              CONFIGURE["PLAYER_INIT_HP"], 0, 0,
              CONFIGURE["PLAYER_INIT_BULLETS"],
              CONFIGURE["PLAYER_INIT_MAXBULLETS"],
              position[0], position[1], position[2], rotation, 0, 0, 0, 0))

        self.conn.commit()

        return True
