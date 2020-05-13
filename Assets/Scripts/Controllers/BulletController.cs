using System.Collections;
using System.Collections.Generic;
using GameConfigures;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    public BulletInfo BulletInfo { get; set; }
    public INetworkService m_networkService = RemoteNetworkServce.Instance;

    bool m_waitHitResult = false;

    Vector3 m_forward;
    Vector3 m_up;

    private const float g = 10.0f;
    private float m_bulletSpeed = 3.0f;
    private float m_verticalSpeed = 0;
    private float m_time = 0.0f;

    private float m_lifeTime = WeaponConfigure.HeaveyWeaponConfigure.BulletDuration;

    private Vector3 m_targetPostion;

    // Start is called before the first frame update
    void Start()
    {
        m_forward = this.transform.forward.normalized;
        m_up = Vector3.up;
        if (BulletInfo.Type == BulletType.Big)
        {
            m_targetPostion = this.transform.position + m_forward;
            m_bulletSpeed = 3.0f;
        }
        else if (BulletInfo.Type == BulletType.TankShell)
        {
            m_targetPostion = this.transform.position + m_forward * 5.0f;
            m_bulletSpeed = 8.0f;
        }
        m_targetPostion.y = 0.0f;

        float tempDistance = Vector3.Distance(transform.position, m_targetPostion);
        float tempTime = tempDistance / m_bulletSpeed;

        float riseTime, downTime;
        riseTime = downTime = tempTime / 2;
        m_verticalSpeed = g * riseTime;

        m_networkService.Register(NetWorkCommandType.BulletHitResult, OnBulletHitResult);
    }

    // Update is called once per frame
    void Update()
    {
        if (m_waitHitResult)
        {
            return;
        }

        if (transform.position.y < m_targetPostion.y)
        {
            return;
        }

        m_time += Time.deltaTime;

        float test = m_verticalSpeed - g * m_time;

        transform.Translate(m_forward * m_bulletSpeed * Time.deltaTime, Space.World);
        transform.Translate(m_up * test * Time.deltaTime, Space.World);        
    }

    private void OnBulletHitResult(NetworkCommand cmd)
    {
        var cmdHitResult = cmd as BulletHitResultCommand;
        if (cmdHitResult.BuleltID == this.BulletInfo.BulletID)
        {
            MessageDispacher.Instance.Send(AudioMessage.PlayExplode, null);

            m_waitHitResult = false;
            m_networkService.Send(new BulletDestroyCommand(PlayerInfo.LocalPlayer.UserID, this.BulletInfo.BulletID));
            DestroyThis();
            Destroy(this.gameObject);
        }
    }

    private void DestroyThis()
    {
        this.m_networkService.Unregister(NetWorkCommandType.BulletHitResult, this.OnBulletHitResult);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("PlayerSync"))
        {
            return;
        }

        var position = this.transform.position;
        m_networkService.Send(
            new ExplodeCommand(PlayerInfo.LocalPlayer.UserID, position, this.BulletInfo.BulletID)
        );

        m_waitHitResult = true;
    }
}
