using System.Collections;
using System.Collections.Generic;
using GameConfigures;
using UnityEngine;

struct MoveTravelElem
{
    public Vector3 Position { get; set; }
    public float Rotation { get; set; }
    public KeyBoardMoveDescriptor KeyBoardMoveDescriptor { get; set; }
    public bool IsJump { get; set; }
    public bool OnlyRotate { get; set; }
}

public class PlayerControllerSync : MonoBehaviour
{
    INetworkService m_networkService = RemoteNetworkServce.Instance;
    Queue<MoveTravelElem> m_moveTravel = new Queue<MoveTravelElem>();
    public PlayerInfo PlayerInfo { get; set; }

    Animator m_animator = null;

    public GameObject m_bulletSync = null;

    // Start is called before the first frame update
    void Start()
    {
        m_animator = GetComponent<Animator>();

        var shoottingOverBehaviour = m_animator.GetBehaviour<ShootingOverBehaviour>();
        shoottingOverBehaviour.CallBack = () =>
        {
            m_animator.SetBool("Shooting", false);
        };

        var rechargeOverBehaiour = m_animator.GetBehaviour<RechargeOverBehaviour>();
        rechargeOverBehaiour.CallBack = () =>
        {
            m_animator.SetBool("Recharge", false);
        };

        var deathOverBehaviour = m_animator.GetBehaviour<DeathOverBehaviour>();
        deathOverBehaviour.CallBack = () =>
        {
            // m_animator.SetBool("Death", false);
        };

        var damagedBehaviour = m_animator.GetBehaviour<DamangedOverBehaviour>();
        damagedBehaviour.CallBack = () =>
        {
            m_animator.SetBool("Damaged", false);
        };

        m_networkService.Register(NetWorkCommandType.Move, OnMove);
        m_networkService.Register(NetWorkCommandType.PlayerRotate, OnRotate);
        m_networkService.Register(NetWorkCommandType.Shoot, OnShoot);
        m_networkService.Register(NetWorkCommandType.Recharge, OnRecharge);
        m_networkService.Register(NetWorkCommandType.EnemyBulletHitResult, OnEnemyBulletHitResult);
        m_networkService.Register(NetWorkCommandType.PlayerDie, OnPlayerDie);
        m_networkService.Register(NetWorkCommandType.PlayerRebornResult, OnPlayerRebornResult);
        m_networkService.Register(NetWorkCommandType.Leave, OnLeave);
        m_networkService.Register(NetWorkCommandType.GunShoot, OnGunShoot);
        m_networkService.Register(NetWorkCommandType.EnemyAttackResult, OnEnemyAttackResult);
        m_networkService.Register(NetWorkCommandType.EnemyGunShootResult, OnEnmeyGunShootResult);
        m_networkService.Register(NetWorkCommandType.SelfDestructResult, OnSelfDestructResult);
        m_networkService.Register(NetWorkCommandType.DriveTankResult, OnDriveTankResult);
        m_networkService.Register(NetWorkCommandType.Shoot, OnShoot);

        MessageDispacher.Instance.Register(GameObjectMessage.QueryPlayerPosition, OnQueryPlayerPosition);
    }

    void OnDriveTankResult(NetworkCommand cmd)
    {
        var cmdDriveResult = cmd as DriveTankResultCommand;
        if (cmdDriveResult.DriverID == this.PlayerInfo.UserID)
        {
            TankInfo.Instance.DriverID = cmdDriveResult.DriverID;
            TankInfo.Instance.IsDriven = true;

            MessageDispacher.Instance.Send(GameObjectMessage.HidePlayer, this.gameObject);

            m_networkService.Unregister(NetWorkCommandType.EnemyAttackResult, OnEnemyAttackResult);
            m_networkService.Unregister(NetWorkCommandType.EnemyGunShootResult, OnEnmeyGunShootResult);
            m_networkService.Unregister(NetWorkCommandType.SelfDestructResult, OnSelfDestructResult);
            m_networkService.Unregister(NetWorkCommandType.DriveTankResult, OnDriveTankResult);
            m_networkService.Unregister(NetWorkCommandType.Shoot, OnShoot);
        }
    }

    public void ResumeRegister()
    {
        m_networkService.Register(NetWorkCommandType.EnemyAttackResult, OnEnemyAttackResult);
        m_networkService.Register(NetWorkCommandType.EnemyGunShootResult, OnEnmeyGunShootResult);
        m_networkService.Register(NetWorkCommandType.SelfDestructResult, OnSelfDestructResult);
        m_networkService.Register(NetWorkCommandType.DriveTankResult, OnDriveTankResult);
        m_networkService.Register(NetWorkCommandType.Shoot, OnShoot);

        var shoottingOverBehaviour = m_animator.GetBehaviour<ShootingOverBehaviour>();
        shoottingOverBehaviour.CallBack = () =>
        {
            m_animator.SetBool("Shooting", false);
        };

        var rechargeOverBehaiour = m_animator.GetBehaviour<RechargeOverBehaviour>();
        rechargeOverBehaiour.CallBack = () =>
        {
            m_animator.SetBool("Recharge", false);
        };

        var deathOverBehaviour = m_animator.GetBehaviour<DeathOverBehaviour>();
        deathOverBehaviour.CallBack = () =>
        {
            // m_animator.SetBool("Death", false);
        };

        var damagedBehaviour = m_animator.GetBehaviour<DamangedOverBehaviour>();
        damagedBehaviour.CallBack = () =>
        {
            m_animator.SetBool("Damaged", false);
        };
    }

    private void OnEnemyAttackResult(NetworkCommand cmd)
    {
        var cmdAttacked = cmd as EnemyAttackResultCommand;

        if (cmdAttacked.AttackedUserID == this.PlayerInfo.UserID)
        {
            m_animator.SetBool("Damaged", true);
            this.PlayerInfo.Hp = cmdAttacked.Hp;
        }
    }

    private void OnSelfDestructResult(NetworkCommand cmd)
    {
        var cmdDestructResult = cmd as SelfDestructResultCommand;
        if (cmdDestructResult.DamagedPlayerID == this.PlayerInfo.UserID)
        {
            m_animator.SetBool("Damaged", true);
            this.PlayerInfo.Hp = cmdDestructResult.Hp;
        }
    }

    private void OnEnmeyGunShootResult(NetworkCommand cmd)
    {
        var cmdAttacked = cmd as EnemyGunShootResultCommand;

        if (cmdAttacked.IsHit && cmdAttacked.TargetID == this.PlayerInfo.UserID)
        {
            m_animator.SetBool("Damaged", true);
            this.PlayerInfo.Hp = cmdAttacked.Hp;
        }
    }

    private void OnGunShoot(NetworkCommand cmd)
    {
        var cmdGunShoot = cmd as GunShootCommand;
        if (cmdGunShoot.ShooterID == this.PlayerInfo.UserID)
        {
            m_animator.SetBool("Shooting", true);
            MessageDispacher.Instance.Send(AudioMessage.PlayPlayerSyncShooting, null);
        }
    }

    private void OnLeave(NetworkCommand cmd)
    {
        var cmdLeave = cmd as LeaveCommand;
        if (cmdLeave.PlayerID == this.PlayerInfo.UserID)
        {
            PlayerInfo.RemovePlayer(this.PlayerInfo);
            DestroyThis();
            Destroy(this.gameObject);
        }
    }

    private void OnPlayerRebornResult(NetworkCommand cmd)
    {
        var cmdReborn = cmd as PlayerRebornResultCommand;
        // Debug.Log($"reborn {cmdReborn.PlayerID} {this.PlayerInfo.UserID}");
        if (cmdReborn.PlayerID == this.PlayerInfo.UserID)
        {
            this.PlayerInfo.Hp = cmdReborn.Hp;
            this.PlayerInfo.MaxBulletCapacity = cmdReborn.MaxBulletCount;
            this.PlayerInfo.CurrentBulletCapacity = cmdReborn.CurBulletCount;

            m_animator.SetBool("Death", false);
        }
    }

    private void OnPlayerDie(NetworkCommand cmd)
    {
        var cmdDie = cmd as PlayerDieCommand;
        if (cmdDie.PlayerID == this.PlayerInfo.UserID)
        {
            m_animator.SetBool("Death", true);
        }
    }

    private void OnQueryPlayerPosition(object msg)
    {
        var result = msg as PlayerPositionQueryResult;
        if (Vector3.Distance(this.transform.position, StrongPoint.Instance.Position) < EnemyConfigure.StrongPointAttackDistance
            && this.PlayerInfo.Hp > 0)
        {
            result.PlayerInfoList.Add(this.PlayerInfo);
            result.PlayerPositionList.Add(this.transform.position);
        }
    }

    private void OnEnemyBulletHitResult(NetworkCommand cmd)
    {
        var cmdHit = cmd as EnemyBulletHitResultCommand;
        if (cmdHit.PlayerID == this.PlayerInfo.UserID)
        {
            m_animator.SetBool("Damaged", true);
        }
    }

    private void OnRecharge(NetworkCommand cmd)
    {
        var cmdRecharge = cmd as RechargeCommand;
        if (cmdRecharge.ShooterID == this.PlayerInfo.UserID)
        {
            m_animator.SetBool("Recharge", true);

            MessageDispacher.Instance.Send(AudioMessage.PlayRecharge, null);
        }
    }

    private void OnShoot(NetworkCommand cmd)
    {
        var cmdShoot = cmd as ShootCommand;
        if (cmdShoot.ShooterID == this.PlayerInfo.UserID)
        {
            for (int i = 0; i < cmdShoot.BulletInfos.Count; ++i)
            {
                var bulletSync = GameObject.Instantiate(m_bulletSync,
                    cmdShoot.BulletPositions[i],
                    Quaternion.AngleAxis(cmdShoot.BulletRotations[i], Vector3.up)
                );

                // Debug.Log(cmdShoot.BulletPositions[i]);

                var scriptComp = bulletSync.GetComponent<BulletControllerSync>();
                scriptComp.BulletInfo = cmdShoot.BulletInfos[i];
            }

            m_animator.SetBool("Shooting", true);
            // play se
            MessageDispacher.Instance.Send(AudioMessage.PlayPlayerSyncShooting, null);
        }
    }

    private void OnRotate(NetworkCommand cmd)
    {
        var cmdRotate = cmd as PlayerRotateCommand;
        if (cmdRotate.UserID == this.PlayerInfo.UserID)
        {
            lock (m_moveTravel)
            {
                m_moveTravel.Enqueue(new MoveTravelElem
                {
                    Position = this.transform.position,
                    Rotation = cmdRotate.Rotation,
                    KeyBoardMoveDescriptor = KeyBoardMoveDescriptor.None,
                    IsJump = false,
                    OnlyRotate = true
                });
            }
        }
    }

    private void OnMove(NetworkCommand cmd)
    {
        var cmdMove = cmd as MoveCommand;
        if (cmdMove.UserID == this.PlayerInfo.UserID)
        {
            lock (m_moveTravel)
            {
                m_moveTravel.Enqueue(new MoveTravelElem
                {
                    Position = cmdMove.TargetPosition,
                    Rotation = cmdMove.Rotation,
                    KeyBoardMoveDescriptor = cmdMove.KeyBoardMoveDescriptor,
                    IsJump = cmdMove.IsJump,
                    OnlyRotate = false
                });
            }
        }
    }

    void DestroyThis()
    {
        m_networkService.Unregister(NetWorkCommandType.Move, OnMove);
        m_networkService.Unregister(NetWorkCommandType.PlayerRotate, OnRotate);
        m_networkService.Unregister(NetWorkCommandType.Shoot, OnShoot);
        m_networkService.Unregister(NetWorkCommandType.Recharge, OnRecharge);
        m_networkService.Unregister(NetWorkCommandType.EnemyBulletHitResult, OnEnemyBulletHitResult);
        m_networkService.Unregister(NetWorkCommandType.EnemyAttackResult, OnEnemyAttackResult);
        m_networkService.Unregister(NetWorkCommandType.PlayerDie, OnPlayerDie);
        m_networkService.Unregister(NetWorkCommandType.PlayerRebornResult, OnPlayerRebornResult);
        m_networkService.Unregister(NetWorkCommandType.Leave, OnLeave);
        m_networkService.Unregister(NetWorkCommandType.GunShoot, OnGunShoot);
        m_networkService.Unregister(NetWorkCommandType.EnemyGunShootResult, OnEnmeyGunShootResult);
        m_networkService.Unregister(NetWorkCommandType.SelfDestructResult, OnSelfDestructResult);
        m_networkService.Unregister(NetWorkCommandType.DriveTankResult, OnDriveTankResult);

        MessageDispacher.Instance.Unregister(GameObjectMessage.QueryPlayerPosition, OnQueryPlayerPosition);
    }

    private void Move(KeyBoardMoveDescriptor keyBoardMoveDescriptor)
    {
        if (keyBoardMoveDescriptor == KeyBoardMoveDescriptor.None)
        {
            m_animator.SetInteger("ForwardOrBack", 0);
            return;
        }

        m_animator.SetInteger("ForwardOrBack", 1);

        if ((keyBoardMoveDescriptor & KeyBoardMoveDescriptor.Up) != 0)
        {
            m_animator.SetFloat("Direction", 1.0f, 0.0f, Time.deltaTime);
        }
        else if ((keyBoardMoveDescriptor & KeyBoardMoveDescriptor.Down) != 0)
        {
            m_animator.SetFloat("Direction", -1.0f, 0.0f, Time.deltaTime);
        }
        else
        {
            m_animator.SetFloat("Direction", 0.0f);
        }

        if ((keyBoardMoveDescriptor & KeyBoardMoveDescriptor.Left) != 0)
        {
            m_animator.SetFloat("LeftOrRight", -1.0f, 0.2f, Time.deltaTime);
        }
        else if ((keyBoardMoveDescriptor & KeyBoardMoveDescriptor.Right) != 0)
        {
            m_animator.SetFloat("LeftOrRight", 1.0f, 0.2f, Time.deltaTime);
        }
        else
        {
            m_animator.SetFloat("LeftOrRight", 0.0f, 0.2f, Time.deltaTime);
        }
    }

    // Update is called once per frame
    void Update()
    {
        lock (m_moveTravel)
        {
            if (m_moveTravel.Count > 0)
            {
                var moveTranvelElem = m_moveTravel.Dequeue();
                this.transform.rotation = Quaternion.AngleAxis(360.0f * moveTranvelElem.Rotation, Vector3.up);
                if (moveTranvelElem.OnlyRotate)
                {
                    Move(KeyBoardMoveDescriptor.None);
                }
                else
                {
                    var moveDirect = moveTranvelElem.Position - this.transform.position;
                    if (m_animator.GetInteger("Jump") == 1)
                    {
                        this.transform.Translate(new Vector3(moveDirect.x, 0, moveDirect.z), Space.World);
                    }
                    else
                    {
                        this.transform.Translate(moveDirect, Space.World);
                    }

                    Move(moveTranvelElem.KeyBoardMoveDescriptor);
                    if (moveTranvelElem.IsJump)
                    {
                        m_animator.SetInteger("Jump", 1);
                    }

                }
            }
            else
            {
                Move(KeyBoardMoveDescriptor.None);
            }
        }
    }
}
