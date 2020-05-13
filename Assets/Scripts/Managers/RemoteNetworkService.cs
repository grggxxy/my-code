using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;

class RemoteNetworkServce : INetworkService
{
    public static RemoteNetworkServce Instance = new RemoteNetworkServce();
    private Socket m_socket;
    private NetworkCommandBuffer m_buffer = new NetworkCommandBuffer();

    private ConcurrentQueue<NetworkCommand> m_commandReadQueue = new ConcurrentQueue<NetworkCommand>();
    private ConcurrentQueue<NetworkCommand> m_commandWriteQueue = new ConcurrentQueue<NetworkCommand>();

    private Dictionary<NetWorkCommandType, ReigsterCallBackFunc> m_callbackMap = new Dictionary<NetWorkCommandType, ReigsterCallBackFunc>();

    public async Task<bool> Connect()
    {
        m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        m_socket.NoDelay = true;

        try
        {
            m_socket.Connect(NetworkConfigure.IP, NetworkConfigure.Port);
            Debug.Log("Connected.");
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }

        // m_buffer.SetSize(2048);
        // m_buffer.GenerateBuffer();
        m_buffer.GenerateBufferWithSize(2048);

        new Thread(async () =>
        {
            List<Socket> socketReadList = new List<Socket>();
            try
            {
                while (m_socket.Connected)
                {
                    socketReadList.Clear();

                    socketReadList.Add(m_socket);

                    try
                    {
                        Socket.Select(socketReadList, null, null, 0);
                    }
                    catch (SocketException e)
                    {
                        Debug.Log(e.Message);
                        Debug.Log(e.StackTrace);
                        Debug.Log(e.ErrorCode);
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.ToString());
                    }

                    // readable
                    if (socketReadList.Count > 0)
                    {
                        var seg = new ArraySegment<byte>(m_buffer.GetBuffer(), m_buffer.Pointer, m_buffer.Remain);
                        var recvLen = await m_socket.ReceiveAsync(seg, SocketFlags.None);
                        m_buffer.Pointer += recvLen;

                        bool result = false;
                        do
                        {
                            result = m_buffer.Parse(
                                segment =>
                                {
                                    var cmd = NetworkCommand.ParseFromByteArraySegment(segment);
                                    m_commandReadQueue.Enqueue(cmd);
                                    // Debug.Log($"recieved cmd : {cmd.CommandType}");
                                }
                            );
                        } while (result);
                    }

                    // writable
                    if (!m_commandWriteQueue.IsEmpty)
                    {
                        NetworkCommand command;
                        var result = m_commandWriteQueue.TryDequeue(out command);
                        if (result)
                        {
                            var buf = command.Format();
                            await m_socket.SendAsync(new ArraySegment<byte>(buf), SocketFlags.None);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e.StackTrace);
                throw e;
            }
        }).Start();

        // this.Send(new LoginRequestCommand("player1", "123456"));

        return true;
    }

    public async Task<bool> DisConnect()
    {
        if (m_socket != null && m_socket.Connected)
        {
            m_socket.Disconnect(false);
            Debug.Log("DisConnected.");
        }
        return true;
    }

    public void Send(NetworkCommand command)
    {
        if (!m_socket.Connected)
        {
            return;
        }

        m_commandWriteQueue.Enqueue(command);
    }

    public void Update()
    {
        if (!m_commandReadQueue.IsEmpty)
        {
            var readQueueCount = m_commandReadQueue.Count;
            int count = readQueueCount > 10 ? 10 : readQueueCount;

            while (count > 0)
            {
                NetworkCommand cmd;
                var result = m_commandReadQueue.TryDequeue(out cmd);
                if (result)
                {
                    this.Dispatch(cmd.CommandType, cmd);
                }
                --count;
            }
        }
    }

    private void Dispatch(NetWorkCommandType type, NetworkCommand cmd)
    {
        if (m_callbackMap.ContainsKey(type))
        {
            m_callbackMap[type].Invoke(cmd);
        }
    }

    public void Register(NetWorkCommandType type, ReigsterCallBackFunc callBackFunc)
    {
        if (m_callbackMap.ContainsKey(type))
        {
            m_callbackMap[type] += callBackFunc;
        }
        else
        {
            m_callbackMap[type] = callBackFunc;
        }
    }

    public void Unregister(NetWorkCommandType type, ReigsterCallBackFunc callBackFunc)
    {
        if (m_callbackMap.ContainsKey(type))
        {
            m_callbackMap[type] -= callBackFunc;
            if (m_callbackMap[type] is null)
            {
                m_callbackMap.Remove(type);
            }
        }
    }

    public void Clear()
    {
        m_callbackMap.Clear();
    }
}
