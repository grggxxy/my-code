import socket
import select
import Queue

from network.configure import CONFIGURE
from network.bytebuffer import ByteBuffer
from network.dispatcher import commandDispatcher
from logic.enemyinfo import EnemyInfo
from logic.iteminfo import ItemInfo
from logic.trapinfo import TrapInfo
from logic.tankinfo import TankInfo
from logic.strongpointinfo import StrongPointInfo
from logic.commands import NetworkCommand, LeaveResultCommand
from databbase.sqlite import Sqlite


class Server(object):
    def __init__(self):
        self.socket = None
        self.database_id_map = {}
        self.user_id_map = [None for _ in range(64)]
        self.socket_map = {}
        self.sokect_msg_quque_map = {}
        self.bytes_buffer = ByteBuffer()
        self.bytes_buffer.generate_buffer_with_size(2048)

        self.host = None
        self.sqlite = Sqlite("game.db")
        self.leave_socket = []

        self.waves = 3

        self.wait_for_new_wave = False

        self.damage_rate = 1

    def initialize(self):
        self.socket = socket.socket()
        self.socket.bind((CONFIGURE["HOST"], CONFIGURE["PORT"]))
        self.socket.listen(500)

        self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        self.socket.setblocking(0)

        self.sqlite.initialize()

        self.waves = 3

    def broadcast(self, command, ruled_out=None):
        for (s, q) in self.sokect_msg_quque_map.items():
            if s != ruled_out and s not in self.leave_socket:
                q.put(command)

    def broadcast_join(self, command, ruled_out=None):
        for (s, q) in self.sokect_msg_quque_map.items():
            if s != ruled_out and self.socket_map.has_key(s) and s not in self.leave_socket:
                q.put(command)

    def send(self, command, target):
        if self.sokect_msg_quque_map.has_key(target) and target not in self.leave_socket:
            self.sokect_msg_quque_map[target].put(command)

    def login(self, conn):
        if self.socket_map.has_key(conn):
            return self.socket_map[conn].user_id
        else:
            index = -1
            for i in range(len(self.user_id_map)):
                if self.user_id_map[i] is None:
                    self.user_id_map[i] = conn
                    index = i
                    break
            return index

    def init_player(self, user_id, player_info):
        conn = self.user_id_map[user_id]
        self.socket_map[conn] = player_info

    def get_player_info_by_user_id(self, user_id):
        conn = self.user_id_map[user_id]
        if self.socket_map.has_key(conn):
            return self.socket_map[conn]
        else:
            return None

    def player_leave(self, conn):
        self.leave_socket.append(conn)

    def loop(self):
        inputs = []
        outputs = []

        inputs.append(self.socket)

        while True:
            readable, writable, exceptional = select.select(
                inputs, outputs, inputs)

            for r in readable:
                if r is self.socket:
                    # new connection
                    conn, addr = self.socket.accept()
                    print "new connection : {}".format(addr)

                    # pylint: disable=no-member
                    conn.setblocking(0)
                    inputs.append(conn)

                    self.sokect_msg_quque_map[conn] = Queue.Queue()
                else:
                    # data from remote
                    try:
                        data = r.recv(2048)
                        bytes_data = bytearray(data)

                        if data:
                            command_bytes = self.bytes_buffer.parse(bytes_data)

                            bytes_data = None
                            while command_bytes:
                                cmd = NetworkCommand.create_command_from_bytearray(
                                    command_bytes)

                                commandDispatcher.dispatch(
                                    cmd.cmd_type, cmd, self, r)

                                # try parse all the commands
                                command_bytes = self.bytes_buffer.parse(
                                    bytes_data)

                            if r not in outputs:
                                outputs.append(r)
                        else:
                            print "a connection disconnected"
                            if r in outputs:
                                outputs.remove(r)
                            inputs.remove(r)

                            self._lose_connection(r)

                    except socket.error:
                        pass

            for w in writable:
                if self.sokect_msg_quque_map.has_key(w):
                    queue = self.sokect_msg_quque_map[w]
                    if queue:
                        if not queue.empty():
                            send_msg = self.sokect_msg_quque_map[w].get()
                            try:
                                w.send(send_msg.format())
                            except socket.error:
                                self._lose_connection(w)
                        else:
                            leave_socket = self.leave_socket[:]
                            if w in self.leave_socket:
                                player_info = self.socket_map[w]

                                leave_socket.remove(w)
                                del self.socket_map[w]
                                del self.sokect_msg_quque_map[w]

                                if player_info.is_host:
                                    for c, _ in self.socket_map.items():
                                        if c != w:
                                            self.send(LeaveResultCommand(), c)
                                    self.reset()
                            self.leave_socket = leave_socket

                    else:
                        if w in outputs:
                            outputs.remove(w)

            for e in exceptional:
                if e in outputs:
                    outputs.remove(e)
                inputs.remove(e)
                e.close()

                self._lose_connection(e)

        self.socket.close()
        self.sqlite.close()

    def _lose_connection(self, conn):
        if self.socket_map.has_key(conn):
            player_info = self.socket_map[conn]
            self.user_id_map[player_info.user_id] = None
            self.sqlite.save_user_info(player_info)

            if player_info.is_host:
                for c, _ in self.socket_map.items():
                    if c != conn:
                        self.send(LeaveResultCommand(), c)
        else:
            for i in range(len(self.user_id_map)):
                # c = self.user_id_map[i]
                if conn == self.user_id_map[i]:
                    self.user_id_map[i] = None
                    break

        if self.socket_map.has_key(conn):
            del self.socket_map[conn]

        if self.sokect_msg_quque_map.has_key(conn):
            del self.sokect_msg_quque_map[conn]

        if self.database_id_map.has_key(conn):
            del self.database_id_map[conn]

        if not self.socket_map:
            print "reset world info."
            self.reset()

    def reset(self):
        EnemyInfo.reset()
        ItemInfo.reset()
        TrapInfo.reset()
        TankInfo.reset()
        StrongPointInfo.reset()
        self.waves = 3
        self.damage_rate = 1
