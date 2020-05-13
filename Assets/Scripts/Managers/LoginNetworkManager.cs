using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginNetworkManager : MonoBehaviour
{
    public INetworkService m_networkService = RemoteNetworkServce.Instance;

    // Start is called before the first frame update
    async void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Screen.fullScreen = false;
        NetworkCommand.Initialize();
        await m_networkService.Connect();
    }

    async private void OnApplicationQuit()
    {
        await m_networkService.DisConnect();

#if UNITY_EDITOR
#else
    System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
    }

    // Update is called once per frame
    void Update()
    {
        m_networkService.Update();
    }
}
