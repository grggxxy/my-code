using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KillsUIEventHandler : MonoBehaviour
{
    private Text m_text;
    // Start is called before the first frame update
    void Start()
    {
        m_text = this.GetComponent<Text>();

        MessageDispacher.Instance.Register(UIMessage.UpdateKills, OnKillsUpdate);
    }

    void OnKillsUpdate(object msg)
    {
        var kills = (uint)msg;
        m_text.text = $"Kills : {kills}";
    }

    private void OnDestroy() {
        MessageDispacher.Instance.Unregister(UIMessage.UpdateKills, OnKillsUpdate);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
