using System;
using UnityEngine;

static class TypeSize
{
    static public int Byte { get; private set; } = 1;
    static public int Int16 { get; private set; } = 2;
    static public int Int32 { get; private set; } = 4;
    static public int UInt16 { get; private set; } = 2;
    static public int UInt32 { get; private set; } = 4;
    static public int Float { get; private set; } = 4;
    static public int LenthOf(string data) => System.Text.Encoding.UTF8.GetByteCount(data) + 2;
}


public class NetworkCommandBuffer
{
    public int Pointer { get; set; } = 0;
    public int Size { get; private set; } = 4;
    public int Remain => Size - Pointer;
    public int Offset { get; set; } = 0;
    public int WrittenDataLen => Pointer - Offset;

    private byte[] m_buffer = null;

    public bool Parse(Action<ArraySegment<byte>> callBack)
    {
        if (WrittenDataLen <= 2)
        {
            return false;
        }

        var cmdLen = BitConverter.ToInt16(m_buffer, Offset);

        if (cmdLen < WrittenDataLen)
        {
            var segment = new ArraySegment<byte>(m_buffer, Offset, WrittenDataLen);
            callBack(segment);
            Offset += cmdLen;
            // Pointer -= cmdLen;
        }
        else if (cmdLen > WrittenDataLen)
        {
            return false;
        }
        else
        {
            var segment = new ArraySegment<byte>(m_buffer, Offset, WrittenDataLen);
            callBack(segment);
            // Pointer = 0;
            Offset = Pointer;
        }

        // move data
        if (Offset >= Size / 2)
        {
            var dataLen = WrittenDataLen;
            Buffer.BlockCopy(m_buffer, Offset, m_buffer, 0, dataLen);
            Offset = 0;
            Pointer = dataLen;
        }

        return true;
    }

    public void Extends(int Length)
    {
        Size += Length;
    }

    public void GenerateBuffer()
    {
        if (Size <= 0)
        {
            throw new System.Exception("Invalid buffer size.");
        }

        m_buffer = new byte[Size];
        Pointer = 0;
        Offset = 0;
    }

    public void GenerateBufferWithSize(int size)
    {
        Size = size;
        if (Size <= 0)
        {
            throw new System.Exception("Invalid buffer size.");
        }

        m_buffer = new byte[Size];
        Pointer = 0;
        Offset = 0;
    }


    public void Put(Vector3 data)
    {
        if (Pointer + 4 * 3 > Size)
        {
            throw new System.Exception("Buffer overflow.");
        }

        System.Buffer.BlockCopy(BitConverter.GetBytes(data.x), 0, m_buffer, (int)Pointer, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(data.y), 0, m_buffer, (int)Pointer + 4, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(data.z), 0, m_buffer, (int)Pointer + 8, 4);

        Pointer += 4 * 3;
    }

    public void Put(byte data)
    {
        if (Pointer + 1 > Size)
        {
            throw new System.Exception("Buffer overflow.");
        }

        m_buffer[Pointer] = data;
        Pointer += 1;
    }

    public void Put(Int16 data)
    {
        if (Pointer + 2 > Size)
        {
            throw new System.Exception("Buffer overflow.");
        }

        System.Buffer.BlockCopy(BitConverter.GetBytes(data), 0, m_buffer, (int)Pointer, 2);

        Pointer += 2;
    }

    public void Put(Int32 data)
    {
        if (Pointer + 4 > Size)
        {
            throw new System.Exception("Buffer overflow.");
        }

        System.Buffer.BlockCopy(BitConverter.GetBytes(data), 0, m_buffer, (int)Pointer, 4);

        Pointer += 4;
    }

    public void Put(float data)
    {
        if (Pointer + 4 > Size)
        {
            throw new System.Exception("Buffer overflow.");
        }

        System.Buffer.BlockCopy(BitConverter.GetBytes(data), 0, m_buffer, (int)Pointer, 4);
        Pointer += 4;
    }

    public void Put(UInt16 data)
    {
        if (Pointer + 2 > Size)
        {
            throw new System.Exception("Buffer overflow.");
        }

        System.Buffer.BlockCopy(BitConverter.GetBytes(data), 0, m_buffer, (int)Pointer, 4);
        Pointer += 2;
    }

    public void Put(UInt32 data)
    {
        if (Pointer + 4 > Size)
        {
            throw new System.Exception("Buffer overflow.");
        }

        System.Buffer.BlockCopy(BitConverter.GetBytes(data), 0, m_buffer, (int)Pointer, 4);
        Pointer += 4;
    }


    public void Put(string data)
    {
        var buf = System.Text.Encoding.UTF8.GetBytes(data);

        if (Pointer + buf.Length + 2 > Size)
        {
            throw new System.Exception("Buffer overflow.");
        }

        System.Buffer.BlockCopy(BitConverter.GetBytes((Int16)buf.Length), 0, m_buffer, (int)Pointer, 2);
        System.Buffer.BlockCopy(buf, 0, m_buffer, (int)Pointer + 2, buf.Length);

        Pointer += buf.Length + 2;
    }

    public byte[] GetBuffer()
    {
        if (m_buffer == null)
        {
            throw new System.Exception("Buffer not initialized.");
        }

        return m_buffer;
    }

    public static Int32 GetInt32(byte[] bytes, ref int offset)
    {
        var result = BitConverter.ToInt32(bytes, offset);
        offset += 4;
        return result;
    }

    public static Int16 GetInt16(byte[] bytes, ref int offset)
    {
        var result = BitConverter.ToInt16(bytes, offset);
        offset += 2;
        return result;
    }

    public static UInt32 GetUInt32(byte[] bytes, ref int offset)
    {
        var result = BitConverter.ToUInt32(bytes, offset);
        offset += 4;
        return result;
    }

    public static UInt16 GetUInt16(byte[] bytes, ref int offset)
    {
        var result = BitConverter.ToUInt16(bytes, offset);
        offset += 2;
        return result;
    }

    public static byte GetByte(byte[] bytes, ref int offset)
    {
        var result = bytes[offset];
        offset += 1;
        return result;
    }

    public static float GetFloat(byte[] bytes, ref int offset)
    {
        var result = BitConverter.ToSingle(bytes, offset);
        offset += 4;
        return result;
    }

    public static String GetString(byte[] bytes, ref int offset)
    {
        var strLen = GetInt16(bytes, ref offset);
        var stringResult = BitConverter.ToString(bytes, offset, strLen);
        offset += strLen;

        return stringResult;

    }

    public static Vector3 GetVector3(byte[] bytes, ref int offset)
    {
        var result = new Vector3(
            BitConverter.ToSingle(bytes, offset),
            BitConverter.ToSingle(bytes, offset + 4),
            BitConverter.ToSingle(bytes, offset + 8)
        );
        offset += 12;
        return result;
    }
}
