using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class LocalNetworkService : INetworkService
{
    public static LocalNetworkService Instance { get; } = new LocalNetworkService();

    private Dictionary<NetWorkCommandType, ReigsterCallBackFunc> m_callbackMap = new Dictionary<NetWorkCommandType, ReigsterCallBackFunc>();

    public async Task<bool> Connect()
    {
        await Task.Run(() =>
        {
            System.Threading.Thread.Sleep(100);
            Debug.Log("Wait for connecting");
        });

        Debug.Log("after connecting");
        return true;
    }

    public async Task<bool> DisConnect()
    {
        await Task.Run(() =>
        {
            System.Threading.Thread.Sleep(100);
            Debug.Log("Wait for disconnecting");
        });

        Debug.Log("after disconnecting");
        return true;
    }

    public void Send(NetworkCommand command)
    {

    }

    public void Update()
    {

    }

    public bool Register(NetWorkCommandType type, ReigsterCallBackFunc callBackFunc)
    {
        if (m_callbackMap.ContainsKey(type))
        {
            m_callbackMap[type] += callBackFunc;
        }
        else
        {
            m_callbackMap[type] = callBackFunc;
        }

        return true;
    }

    void INetworkService.Register(NetWorkCommandType type, ReigsterCallBackFunc callBackFunc)
    {
        throw new System.NotImplementedException();
    }

    public void Unregister(NetWorkCommandType type, ReigsterCallBackFunc callBackFunc)
    {
        throw new System.NotImplementedException();
    }

    public void Clear()
    {
        throw new System.NotImplementedException();
    }
}
