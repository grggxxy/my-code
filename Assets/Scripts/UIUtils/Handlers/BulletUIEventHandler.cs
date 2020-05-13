using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BulletUIEventHandler : MonoBehaviour
{
    // Start is called before the first frame update
    private Text m_text;
    void Start()
    {
        MessageDispacher.Instance.Register(UIMessage.UpdateBulletUI, OnUpdateBullets);
        m_text = this.GetComponent<Text>();
    }

    void OnUpdateBullets(object msg)
    {
        var msgBullets = (BulletUpdateMessage)msg;
        var displayString = $"{msgBullets.CurrentBulletCount} / { msgBullets.MaxBulletCount - msgBullets.CurrentBulletCount}";

        m_text.text = displayString;
    }

    private void OnDestroy()
    {
        MessageDispacher.Instance.Unregister(UIMessage.UpdateBulletUI, OnUpdateBullets);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
