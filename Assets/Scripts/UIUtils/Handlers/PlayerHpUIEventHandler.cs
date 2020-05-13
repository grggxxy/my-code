using UnityEngine;
using UnityEngine.UI;

public class PlayerHpUIEventHandler : MonoBehaviour
{
    private Slider m_slider;

    void Start()
    {
        m_slider = this.GetComponent<Slider>();

        MessageDispacher.Instance.Register(UIMessage.UpdateHpUI, OnHpUpdate);
    }

    void OnHpUpdate(object msg)
    {
        var progress = (float)msg;
        m_slider.value = progress;
    }

    private void OnDestroy()
    {
        MessageDispacher.Instance.Unregister(UIMessage.UpdateHpUI, OnHpUpdate);
    }

    void Update()
    {
    }
}
