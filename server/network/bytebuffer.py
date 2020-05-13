import struct

class TypeSize(object):
    Byte = 1
    Int16 = 2
    UInt16 = 2
    Int32 = 4
    UInt32 = 4
    Float = 4

    @classmethod
    def SizeOf(cls, data):
        if data is str:
            pass
        elif data is unicode:
            pass
        raise Exception("Unsupported type.")

class ByteBuffer(object):
    def __init__(self):
        self.offset = 0
        self.currentPointer = 0
        self.max_size = 4
        self.buffer = None

    def parse(self, byte_array):
        if not byte_array is None:
            # copy data into buffer
            self.buffer[self.currentPointer: self.currentPointer +
                        len(byte_array)] = byte_array
            self.currentPointer += len(byte_array)
            self.max_size = max(self.max_size, len(self.buffer))

        if self.currentPointer - self.offset <= 2:
            return None

        # get package length
        data_length = struct.unpack("<h", self.buffer[self.offset:self.offset+2])[0]

        # adjoin
        if data_length < self.currentPointer - self.offset:
            data = self.buffer[self.offset: self.offset + data_length]
            self.offset += data_length
        # half
        elif data_length > self.currentPointer - self.offset:
            # wait next package
            return None
        # exactly
        else:
            # reset
            data = self.buffer[self.offset:self.offset + data_length]
            self.offset = self.currentPointer

        if self.currentPointer >= self.max_size / 2:
            # move data
            remain = self.currentPointer - self.offset
            self.buffer[0: remain] = self.buffer[self.offset: self.currentPointer]

            self.currentPointer = remain
            self.offset = 0

        return data

    def generate_buffer_with_size(self, size):
        if size <= 0:
            raise Exception("Wrong max size.")

        self.buffer = bytearray(size)
        self.max_size = size
        self.currentPointer = 0

    def generate_buffer(self):
        if self.max_size <= 0:
            raise Exception("Wrong max size.")

        self.buffer = bytearray(self.max_size)
        self.currentPointer = 0

    def extends(self, size):
        self.max_size += size

    def put_byte(self, data):
        if self.currentPointer + 1 > self.max_size:
            raise Exception("Buffer over flow.")

        bytes_array = struct.pack("<B", data)
        self.buffer[self.currentPointer] = bytes_array
        self.currentPointer += 1

    def put_float(self, data):
        if self.currentPointer + 4 > self.max_size:
            raise Exception("Buffer over flow.")

        bytes_array = struct.pack("<f", data)
        self.buffer[self.currentPointer: self.currentPointer+4] = bytes_array
        self.currentPointer += 4

    def put_int32(self, data):
        if self.currentPointer + 4 > self.max_size:
            raise Exception("Buffer over flow.")

        bytes_array = struct.pack("<i", data)
        self.buffer[self.currentPointer: self.currentPointer+4] = bytes_array
        self.currentPointer += 4

    def put_int16(self, data):
        if self.currentPointer + 2 > self.max_size:
            raise Exception("Buffer over flow.")

        bytes_array = struct.pack("<h", data)
        self.buffer[self.currentPointer: self.currentPointer+2] = bytes_array
        self.currentPointer += 2

    def put_uint32(self, data):
        if self.currentPointer + 4 > self.max_size:
            raise Exception("Buffer over flow.")

        bytes_array = struct.pack("<I", data)
        self.buffer[self.currentPointer: self.currentPointer+4] = bytes_array
        self.currentPointer += 4

    def put_uint16(self, data):
        if self.currentPointer + 2 > self.max_size:
            raise Exception("Buffer over flow.")

        bytes_array = struct.pack("<H", data)
        self.buffer[self.currentPointer: self.currentPointer+2] = bytes_array
        self.currentPointer += 2

    def get_buffer(self):
        return self.buffer

    def putString(self):
        pass

    @classmethod
    def get_byte(cls, data, offset):
        v = data[offset]
        return v, offset + 1

    @classmethod
    def get_int16(cls, data, offset):
        v = struct.unpack("<h", data[offset: offset+2])[0]
        return v, offset + 2
    
    @classmethod
    def get_int32(cls, data, offset):
        v = struct.unpack("<i",data[offset: offset+4])[0]
        return v, offset + 4

    @classmethod
    def get_uint16(cls, data, offset):
        v = struct.unpack("<H", data[offset: offset+2])[0]
        return v, offset + 2
    
    @classmethod
    def get_uint32(cls, data, offset):
        v = struct.unpack("<I", data[offset: offset+4])[0]
        return v, offset + 4

    @classmethod
    def get_float(cls, data, offset):
        v = struct.unpack("<f", data[offset: offset+4])[0]
        return v, offset + 4
