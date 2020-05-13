using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHpSliderUI : MonoBehaviour
{
    public string m_targetType;

    private uint m_enmeyID;

    private Slider m_slider;

    private bool m_isStrongPoint = false;

    // Start is called before the first frame update
    void Start()
    {
        if (m_targetType == "Local")
        {
            var controller = this.transform.parent.gameObject.transform.parent.GetComponent<EnemyController>();
            this.m_enmeyID = controller.EnemyInfo.EnemyID;
            m_isStrongPoint = false;
        }
        else if (m_targetType == "Sync")
        {
            var controller = this.transform.parent.gameObject.transform.parent.GetComponent<EnemyControllerSync>();
            this.m_enmeyID = controller.EnemyInfo.EnemyID;
            m_isStrongPoint = false;
        }
        else if (m_targetType == "StrongPoint")
        {
            m_isStrongPoint = true;
        }


        m_slider = this.GetComponent<Slider>();

        if (m_isStrongPoint)
        {
            MessageDispacher.Instance.Register(UIMessage.UpdateStrongPointHpSlider, OnUpdateStrongPointHp);
        }
        else
        {
            MessageDispacher.Instance.Register(UIMessage.UpdateEnemyHpSlider, OnUpdateEnmeyHp);
        }
    }

    private void OnUpdateStrongPointHp(object msg)
    {
        var update = (float)msg;
        m_slider.value = update;
    }

    private void OnUpdateEnmeyHp(object msg)
    {
        var msgUpdate = (EnemyHpUpdateMessage)msg;
        if (this.m_enmeyID == msgUpdate.EnemyID)
        {
            m_slider.value = msgUpdate.Progress;
        }
    }

    private void OnDestroy()
    {
        if (m_isStrongPoint)
        {
            MessageDispacher.Instance.Unregister(UIMessage.UpdateStrongPointHpSlider, OnUpdateStrongPointHp);
        }
        else
        {
            MessageDispacher.Instance.Unregister(UIMessage.UpdateEnemyHpSlider, OnUpdateEnmeyHp);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // this.transform.LookAt(Camera.main.transform);
    }
}
