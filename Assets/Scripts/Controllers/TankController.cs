using System;
using System.Collections.Generic;
using GameConfigures;
using UnityEngine;
using UnityEngine.UI;

enum TankMoveType
{
    Move,
    TurretRotate,
    BodyRotate,
}

struct TankMoveTravelElem
{
    public Vector3 Position { get; set; }
    public float TurretRotation { get; set; }
    public float BodyRotation { get; set; }
    public TankMoveType MoveType { get; set; }
}


public class TankController : MonoBehaviour
{
    public GameObject m_turret;
    public GameObject m_body;
    private Camera m_camera;
    private float m_theta = Mathf.PI;
    private float m_phi = 0.0f;
    private float m_targetPhi = 0.0f;
    private float m_cameraPhi = 0.0f;

    private float m_bodyRotation = 0.0f;
    private bool m_waitForResult = false;

    private float m_reloadDuration = 0;

    private INetworkService m_networkService = RemoteNetworkServce.Instance;

    Queue<TankMoveTravelElem> m_moveTravel = new Queue<TankMoveTravelElem>();

    private List<BulletInfo> m_bulletInfoBuffer = new List<BulletInfo>();
    private List<float> m_bulletRotationBuffer = new List<float>();
    private List<Vector3> m_bulletPositionBuffer = new List<Vector3>();
    public GameObject m_bullet;
    public GameObject m_bulletSync;
    private bool m_cursorState;

    private Vector3 m_cameraForward;

    public Image m_shootSign;
    public GameObject m_shootSignCanvasObject;

    // Rigidbody m_rigidBody;

    private void Start()
    {
        m_camera = Camera.main;

        m_cameraForward = m_turret.transform.forward;

        m_shootSignCanvasObject.SetActive(false);

        m_networkService.Register(NetWorkCommandType.DriveTankResult, OnDriveTankResult);
        m_networkService.Register(NetWorkCommandType.TankMove, OnTankMove);
        m_networkService.Register(NetWorkCommandType.TankTurretRotate, OnTurretRotate);
        m_networkService.Register(NetWorkCommandType.TankBodyRotation, OnBodyRotate);
        m_networkService.Register(NetWorkCommandType.Shoot, OnShoot);
        m_networkService.Register(NetWorkCommandType.ShootResult, OnShootResult);

        m_networkService.Register(NetWorkCommandType.EnemyGunShootResult, OnEnmeyGunShootResult);
        m_networkService.Register(NetWorkCommandType.SelfDestructResult, OnSelfDestructResult);
        m_networkService.Register(NetWorkCommandType.EnemyAttackResult, OnEnemyAttackResult);

        m_networkService.Register(NetWorkCommandType.TankDestroy, OnTankDesroy);

        m_networkService.Register(NetWorkCommandType.NewGameResult, OnNewGameResult);

        MessageDispacher.Instance.Register(GameObjectMessage.QueryPlayerPosition, OnQueryPosition);
        MessageDispacher.Instance.Register(GameObjectMessage.SwitchCursorState, OnSwitchCursorState);
    }

    private void OnNewGameResult(NetworkCommand cmd)
    {
        if (TankInfo.Instance.DriverID == PlayerInfo.LocalPlayer.UserID)
        {
            MessageDispacher.Instance.Send(GameObjectMessage.ShowPlayer,
                    this.transform.position + this.m_body.transform.forward * 2.0f);

            MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, PlayerInfo.LocalPlayer.Hp / 1000.0f);
        }
        else if (TankInfo.Instance.DriverID != 0xff)
        {
            MessageDispacher.Instance.Send(GameObjectMessage.ShowPlayerSync,
                    this.transform.position + this.m_body.transform.forward * 2.0f);
        }

        TankInfo.Instance.DriverID = 0xff;
        TankInfo.Instance.IsDriven = false;

        DestroyThis();
        Destroy(this.gameObject);
    }

    private void DestroyThis()
    {
        m_networkService.Unregister(NetWorkCommandType.NewGameResult, OnNewGameResult);

        m_networkService.Unregister(NetWorkCommandType.DriveTankResult, OnDriveTankResult);
        m_networkService.Unregister(NetWorkCommandType.TankMove, OnTankMove);
        m_networkService.Unregister(NetWorkCommandType.TankTurretRotate, OnTurretRotate);
        m_networkService.Unregister(NetWorkCommandType.TankBodyRotation, OnBodyRotate);
        m_networkService.Unregister(NetWorkCommandType.Shoot, OnShoot);
        m_networkService.Unregister(NetWorkCommandType.ShootResult, OnShootResult);

        m_networkService.Unregister(NetWorkCommandType.EnemyGunShootResult, OnEnmeyGunShootResult);
        m_networkService.Unregister(NetWorkCommandType.SelfDestructResult, OnSelfDestructResult);
        m_networkService.Unregister(NetWorkCommandType.EnemyAttackResult, OnEnemyAttackResult);

        m_networkService.Unregister(NetWorkCommandType.TankDestroy, OnTankDesroy);

        MessageDispacher.Instance.Unregister(GameObjectMessage.QueryPlayerPosition, OnQueryPosition);
        MessageDispacher.Instance.Unregister(GameObjectMessage.SwitchCursorState, OnSwitchCursorState);
    }

    private void OnSwitchCursorState(object msg)
    {
        var state = (bool)msg;
        // m_cursorState = state;
    }

    private void OnTankDesroy(NetworkCommand cmd)
    {
        if (TankInfo.Instance.DriverID == PlayerInfo.LocalPlayer.UserID)
        {
            MessageDispacher.Instance.Send(GameObjectMessage.ShowPlayer,
                    this.transform.position + this.m_body.transform.forward * 2.0f);

            MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, PlayerInfo.LocalPlayer.Hp / 1000.0f);
        }
        else
        {
            MessageDispacher.Instance.Send(GameObjectMessage.ShowPlayerSync,
                    this.transform.position + this.m_body.transform.forward * 2.0f);
        }

        TankInfo.Instance.DriverID = 0xff;
        TankInfo.Instance.IsDriven = false;

        DestroyThis();
        Destroy(this.gameObject);
    }

    private void OnEnemyAttackResult(NetworkCommand cmd)
    {
        var cmdAttacked = cmd as EnemyAttackResultCommand;
        if (cmdAttacked.AttackedUserID == TankInfo.Instance.DriverID)
        {
            // update ui
            TankInfo.Instance.Hp = cmdAttacked.Hp;

            MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, TankInfo.Instance.Hp / 1000.0f);
        }
    }

    private void OnSelfDestructResult(NetworkCommand cmd)
    {
        var cmdDestructResult = cmd as SelfDestructResultCommand;
        if (cmdDestructResult.DamagedPlayerID == TankInfo.Instance.DriverID)
        {
            TankInfo.Instance.Hp = cmdDestructResult.Hp;

            MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, TankInfo.Instance.Hp / 1000.0f);
        }
    }

    private void OnEnmeyGunShootResult(NetworkCommand cmd)
    {
        var cmdAttacked = cmd as EnemyGunShootResultCommand;
        if (cmdAttacked.IsHit && cmdAttacked.TargetID == TankInfo.Instance.DriverID)
        {
            TankInfo.Instance.Hp = cmdAttacked.Hp;

            MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, TankInfo.Instance.Hp / 1000.0f);
        }
    }

    private void OnQueryPosition(object msg)
    {
        var result = msg as PlayerPositionQueryResult;
        result.TankPosition = this.transform.position;
    }

    private void OnTankMove(NetworkCommand cmd)
    {
        var cmdMove = cmd as TankMoveCommand;
        lock (m_moveTravel)
        {
            m_moveTravel.Enqueue(new TankMoveTravelElem()
            {
                Position = cmdMove.Position,
                MoveType = TankMoveType.Move,
            });
        }
    }

    private void OnTurretRotate(NetworkCommand cmd)
    {
        var cmdMove = cmd as TankTurretRotateCommand;
        lock (m_moveTravel)
        {
            m_moveTravel.Enqueue(new TankMoveTravelElem()
            {
                TurretRotation = cmdMove.Rotation,
                MoveType = TankMoveType.TurretRotate,
            });
        }
    }

    private void OnBodyRotate(NetworkCommand cmd)
    {
        var cmdMove = cmd as TankBodyRotationCommand;
        lock (m_moveTravel)
        {
            m_moveTravel.Enqueue(new TankMoveTravelElem()
            {
                BodyRotation = cmdMove.Rotation,
                MoveType = TankMoveType.BodyRotate,
            });
        }
    }

    private void OnDriveTankResult(NetworkCommand cmd)
    {
        var cmdDriveResult = cmd as DriveTankResultCommand;
        // leave tank
        if (cmdDriveResult.DriverID == 0xff)
        {
            if (TankInfo.Instance.DriverID == PlayerInfo.LocalPlayer.UserID)
            {
                MessageDispacher.Instance.Send(GameObjectMessage.ShowPlayer,
                        this.transform.position + this.m_body.transform.forward * 2.0f);

                MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, PlayerInfo.LocalPlayer.Hp / 1000.0f);
            }
            else
            {
                MessageDispacher.Instance.Send(GameObjectMessage.ShowPlayerSync,
                        this.transform.position + this.m_body.transform.forward * 2.0f);
            }

            TankInfo.Instance.DriverID = 0xff;
            TankInfo.Instance.IsDriven = false;

            m_shootSignCanvasObject.SetActive(false);
        }
        else
        {
            m_moveTravel.Clear();
            MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, TankInfo.Instance.Hp / 1000.0f);

            m_shootSignCanvasObject.SetActive(true);
        }
        m_waitForResult = false;
    }

    private void OnShootResult(NetworkCommand cmd)
    {
        var cmdShoot = cmd as ShootResultCommand;
        if (TankInfo.Instance.DriverID == PlayerInfo.LocalPlayer.UserID)
        {
            m_waitForResult = false;

            var cmdShootResult = cmd as ShootResultCommand;

            // play se
            MessageDispacher.Instance.Send(AudioMessage.PlayPlayerShooting, null);

            for (int i = 0; i < m_bulletInfoBuffer.Count; ++i)
            {
                if (m_bulletInfoBuffer[i].Type == BulletType.TankShell)
                {
                    var gameObj = GameObject.Instantiate(this.m_bullet,
                        m_bulletPositionBuffer[i],
                        Quaternion.AngleAxis(360.0f * m_bulletRotationBuffer[i],
                        Vector3.up));
                    var scriptComp = gameObj.GetComponent<BulletController>();
                    scriptComp.BulletInfo = m_bulletInfoBuffer[i];
                }
            }

            m_reloadDuration = PlayerConfigure.BigSkillCoolDownDuration;
        }
    }

    private void OnShoot(NetworkCommand cmd)
    {
        var cmdShoot = cmd as ShootCommand;
        // sync tank shooter
        if (TankInfo.Instance.IsDriven && TankInfo.Instance.DriverID == cmdShoot.ShooterID)
        {
            // play se
            MessageDispacher.Instance.Send(AudioMessage.PlayPlayerShooting, null);

            for (int i = 0; i < cmdShoot.BulletInfos.Count; ++i)
            {
                if (cmdShoot.BulletInfos[i].Type == BulletType.TankShell)
                {
                    var gameObj = GameObject.Instantiate(this.m_bulletSync,
                        cmdShoot.BulletPositions[i],
                        Quaternion.AngleAxis(360.0f * cmdShoot.BulletRotations[i],
                        Vector3.up));
                    var scriptComp = gameObj.GetComponent<BulletControllerSync>();
                    scriptComp.BulletInfo = cmdShoot.BulletInfos[i];
                }
            }
        }
    }

    void Sync()
    {
        lock (m_moveTravel)
        {
            var handleCount = Math.Min(m_moveTravel.Count, 3);

            for (int i = 0; i < handleCount; i++)
            {
                var moveTranvelElem = m_moveTravel.Dequeue();

                switch (moveTranvelElem.MoveType)
                {
                    case TankMoveType.BodyRotate:
                        this.m_body.transform.rotation = Quaternion.AngleAxis(moveTranvelElem.BodyRotation, Vector3.up);
                        break;
                    case TankMoveType.Move:
                        var dir = moveTranvelElem.Position - this.transform.position;
                        this.transform.Translate(dir, Space.World);
                        break;
                    case TankMoveType.TurretRotate:
                        this.m_turret.transform.rotation = Quaternion.AngleAxis(moveTranvelElem.TurretRotation, Vector3.up);
                        break;
                }
            }

        }
    }

    private void Update()
    {
        if (!TankInfo.Instance.IsDriven || TankInfo.Instance.DriverID != PlayerInfo.LocalPlayer.UserID)
        {
            Sync();
            return;
        }

        if (m_waitForResult)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Move();
        UpDown();
        Shoot();
    }

    public void Shoot()
    {
        // update ui
        MessageDispacher.Instance.Send(
            UIMessage.UpdateCoolDownProgressUI,
            m_reloadDuration <= 0 ? 1.0f : 1.0f - m_reloadDuration / PlayerConfigure.BigSkillCoolDownDuration);

        if (m_reloadDuration > 0)
        {
            m_reloadDuration -= Time.deltaTime;
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                m_bulletInfoBuffer.Clear();
                m_bulletRotationBuffer.Clear();
                m_bulletPositionBuffer.Clear();

                var shootingPos = this.m_turret.transform.position + this.m_turret.transform.forward * 2.0f + this.transform.up * 3.0f;

                m_bulletInfoBuffer.Add(new BulletInfo { Type = BulletType.TankShell, ShooterID = PlayerInfo.LocalPlayer.UserID });
                m_bulletRotationBuffer.Add(this.m_phi);
                m_bulletPositionBuffer.Add(shootingPos);

                m_networkService.Send(new ShootCommand(PlayerInfo.LocalPlayer.UserID,
                    m_bulletPositionBuffer,
                    m_bulletRotationBuffer,
                    m_bulletInfoBuffer,
                    false
                ));

                m_waitForResult = true;
            }
        }
    }

    public void UpDown()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            m_networkService.Send(new DriveTankCommand(PlayerInfo.LocalPlayer.UserID));
            m_waitForResult = true;
        }
    }

    public void SetTurretRotation(float rotation)
    {
        m_turret.transform.rotation = Quaternion.AngleAxis(rotation, Vector3.up);
    }

    public void SetBodyRotation(float rotation)
    {
        m_turret.transform.rotation = Quaternion.AngleAxis(rotation, Vector3.up);
    }

    private void Move()
    {
        bool turretRotate = false;
        bool bodyRotate = false;
        bool moved = false;

        float dx = Input.GetAxis("Mouse X");
        var rate = PlayerConfigure.CameraMouseMoveRateX;

        if (Mathf.Abs(dx) >= 1e-4)
        {
            m_targetPhi += dx * rate / 4;
            turretRotate = true;

            if (Input.GetMouseButton(1))
            {
                m_cameraPhi += dx * rate / 2;
                m_cameraForward = Quaternion.AngleAxis(360.0f * m_cameraPhi, Vector3.up) * Vector3.forward;
            }
        }

        if (Math.Abs(m_phi - m_targetPhi) >= 1e-2)
        {
            m_phi -= Math.Sign(m_phi - m_targetPhi) * 0.004f;

            var rotate = Quaternion.AngleAxis(360.0f * m_phi, Vector3.up);
            m_turret.transform.rotation = Quaternion.AngleAxis(360.0f * m_phi, Vector3.up);


            // shoot sign
            var alpha = m_phi - m_cameraPhi;

            var screenWidth = Screen.width;
            var screenHeight = Screen.height;

            var x = screenWidth / 2 * (alpha / 0.2f);
            if (Math.Abs(x - m_shootSign.rectTransform.anchoredPosition.x) >= 2)
            {
                m_shootSign.rectTransform.anchoredPosition =
                    new Vector2(x, m_shootSign.rectTransform.anchoredPosition.y);
            }
        }

        if (Input.GetKey(KeyCode.A))
        {
            m_bodyRotation -= Time.deltaTime * 45.0f;
            m_body.transform.rotation = Quaternion.AngleAxis(m_bodyRotation, Vector3.up);
            bodyRotate = true;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            m_bodyRotation += Time.deltaTime * 45.0f;
            m_body.transform.rotation = Quaternion.AngleAxis(m_bodyRotation, Vector3.up);
            bodyRotate = true;
        }

        Vector3 moveVector;

        if (Input.GetKey(KeyCode.W))
        {
            moveVector = m_body.transform.forward * Time.deltaTime * PlayerConfigure.MoveSpeed;
            transform.Translate(moveVector, Space.World);
            moved = true;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            moveVector = -m_body.transform.forward * Time.deltaTime * PlayerConfigure.MoveSpeed;
            transform.Translate(moveVector, Space.World);
            moved = true;
        }

        if (moved)
        {
            m_networkService.Send(new TankMoveCommand(PlayerInfo.LocalPlayer.UserID, transform.position));
        }

        if (turretRotate)
        {
            m_networkService.Send(new TankTurretRotateCommand(PlayerInfo.LocalPlayer.UserID, 360.0f * m_phi));
        }

        if (bodyRotate)
        {
            m_networkService.Send(new TankBodyRotationCommand(PlayerInfo.LocalPlayer.UserID, m_bodyRotation));
        }
    }

    private void LateUpdate()
    {
        if (!TankInfo.Instance.IsDriven || TankInfo.Instance.DriverID != PlayerInfo.LocalPlayer.UserID)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float rate = PlayerConfigure.CameraMouseMoveRateY;
        const float distance = 10.0f;

        float dy = Input.GetAxis("Mouse Y");
        m_theta -= (dy * rate);
        m_theta = Mathf.Clamp(m_theta, Mathf.PI, Mathf.PI / 2 * 3);

        Vector3 pos = -distance * Mathf.Cos(m_theta) * m_cameraForward.normalized + distance * Mathf.Sin(m_theta) * Vector3.up;

        m_camera.transform.position = this.transform.position - pos;
        m_camera.transform.LookAt(this.transform.position + new Vector3(0.0f, 4.0f, 0.0f), Vector3.up);
    }
}
