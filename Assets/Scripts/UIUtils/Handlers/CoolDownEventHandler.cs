using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoolDownEventHandler : MonoBehaviour
{
    private Slider m_slider;
    // Start is called before the first frame update
    void Start()
    {
        m_slider = GetComponent<Slider>();

        MessageDispacher.Instance.Register(UIMessage.UpdateCoolDownProgressUI, OnUpdateProgress);
    }

    private void OnUpdateProgress(object msg)
    {
        m_slider.value = (float)msg;
    }

    private void OnDestroy() {
        MessageDispacher.Instance.Unregister(UIMessage.UpdateCoolDownProgressUI, OnUpdateProgress);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
