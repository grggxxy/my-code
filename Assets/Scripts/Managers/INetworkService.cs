using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public delegate void ReigsterCallBackFunc(NetworkCommand command);

public interface INetworkService
{
    Task<bool> Connect();
    Task<bool> DisConnect();
    void Update();
    void Send(NetworkCommand command);
    void Register(NetWorkCommandType type, ReigsterCallBackFunc callBackFunc);
    void Unregister(NetWorkCommandType type, ReigsterCallBackFunc callBackFunc);
    void Clear();
}
