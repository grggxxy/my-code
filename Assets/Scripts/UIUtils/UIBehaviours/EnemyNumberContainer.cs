using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyNumberContainer : MonoBehaviour
{
    public GameObject m_hudText;

    public string m_targetType;

    private uint m_enmeyID;

    private bool m_isStrongPoint = false;

    // Start is called before the first frame update
    void Start()
    {
        if (m_targetType == "Local")
        {
            var controller = this.transform.parent.gameObject.GetComponent<EnemyController>();
            this.m_enmeyID = controller.EnemyInfo.EnemyID;
            m_isStrongPoint = false;
        }
        else if (m_targetType == "Sync")
        {
            var controller = this.transform.parent.gameObject.GetComponent<EnemyControllerSync>();
            this.m_enmeyID = controller.EnemyInfo.EnemyID;
            m_isStrongPoint = false;
        }
        else if (m_targetType == "StrongPoint")
        {
            m_isStrongPoint = true;
        }

        if (!m_isStrongPoint)
        {
            MessageDispacher.Instance.Register(UIMessage.EnmeyDamaged, OnDamaged);
        }
        else
        {
            MessageDispacher.Instance.Register(UIMessage.StrongPointDamaged, OnStrongPointDamaged);
        }
    }

    void OnDamaged(object msg)
    {
        var msgDamaged = (EnemyDamageMessage)msg;
        if (this.m_enmeyID == msgDamaged.EnemyID)
        {
            this.ShowDamageNumber(msgDamaged.Damage);
        }
    }

    void OnStrongPointDamaged(object msg)
    {
        var damage = (int)msg;
        this.ShowDamageNumber(damage);
    }

    private void OnDestroy()
    {
        if (!m_isStrongPoint)
        {
            MessageDispacher.Instance.Unregister(UIMessage.EnmeyDamaged, OnDamaged);
        }
        else
        {
            MessageDispacher.Instance.Unregister(UIMessage.StrongPointDamaged, OnStrongPointDamaged);
        }
    }

    private void Rotation()
    {
        this.transform.LookAt(Camera.main.transform);
    }

    // Update is called once per frame
    void Update()
    {
        Rotation();
    }

    void ShowDamageNumber(int damage)
    {
        var hud = GameObject.Instantiate(m_hudText, transform);
        hud.GetComponent<Text>().text = $"-{damage}";
    }
}
