using System.Collections;
using System.Collections.Generic;
using GameConfigures;
using UnityEngine;

struct BulletMoveTravelElem
{
    public Vector3 Position { get; set; }
    public float Rotation { get; set; }
}

public class BulletControllerSync : MonoBehaviour
{
    public BulletInfo BulletInfo { get; set; } = null;
    public INetworkService m_networkService = RemoteNetworkServce.Instance;
    // Queue<BulletMoveTravelElem> m_moveTravel = new Queue<BulletMoveTravelElem>();

    Vector3 m_forward;
    Vector3 m_up;

    private const float g = 10.0f;
    private float m_bulletSpeed = 3.0f;
    private float m_verticalSpeed = 0;
    private float m_time = 0.0f;

    private Vector3 m_targetPostion;

    private float m_lifeTime = WeaponConfigure.HeaveyWeaponConfigure.BulletDuration;

    // Start is called before the first frame update
    void Start()
    {
        m_forward = this.transform.forward;
        m_up = this.transform.up;

        if (BulletInfo.Type == BulletType.Big)
        {
            m_targetPostion = this.transform.position + m_forward * 1.0f;
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

        m_networkService.Register(NetWorkCommandType.BulletDestroy, this.OnBulletDestroy);
    }

    private void OnBulletDestroy(NetworkCommand cmd)
    {
        var cmdDestroy = cmd as BulletDestroyCommand;
        Debug.Log($"{cmdDestroy.BulletID} == {this.BulletInfo.BulletID} && {cmdDestroy.ShooterID} == {this.BulletInfo.ShooterID}");
        if (cmdDestroy.BulletID == this.BulletInfo.BulletID && cmdDestroy.ShooterID == this.BulletInfo.ShooterID)
        {
            MessageDispacher.Instance.Send(AudioMessage.PlayExplode, null);

            // m_networkService.Unregister(NetWorkCommandType.BulletMove, this.OnBulletMove);
            DestroyThis();
            Destroy(this.gameObject);
        }
    }

    private void DestroyThis()
    {
        m_networkService.Unregister(NetWorkCommandType.BulletDestroy, this.OnBulletDestroy);
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y < m_targetPostion.y)
        {
            return;
        }

        m_time += Time.deltaTime;

        float test = m_verticalSpeed - g * m_time;
        transform.Translate(m_forward * m_bulletSpeed * Time.deltaTime, Space.World);
        transform.Translate(m_up * test * Time.deltaTime, Space.World);
    }
}
