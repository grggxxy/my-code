using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GameConfigures;

public enum KeyBoardMoveDescriptor
{
    None = 0b0000,
    Up = 0b0001,
    Down = 0b0010,
    Left = 0b0100,
    Right = 0b1000,
}

enum ShootingMode
{
    Fixed = 0,
    Running,
}

enum PlayerMode
{
    Normal,
    SetTrap,
}

public class PlayerController : MonoBehaviour
{
    public Camera m_camera;
    private Animator m_animator;
    public GameObject m_bulletBig;
    private KeyBoardMoveDescriptor m_keyBoardDirection = KeyBoardMoveDescriptor.None;
    private float m_theta = Mathf.PI;
    private float m_phi = 0.0f;
    private float m_shootTimeCount = 0.0f;
    private ShootingMode m_shootingMode = ShootingMode.Fixed;
    public PlayerInfo m_playerInfo;
    private INetworkService m_networkService = RemoteNetworkServce.Instance;

    private bool m_isWaitingResult = false;
    private List<BulletInfo> m_bulletInfoBuffer = new List<BulletInfo>();
    private List<float> m_bulletRotationBuffer = new List<float>();
    private List<Vector3> m_bulletPositionBuffer = new List<Vector3>();

    private float m_bigSkillCoolDownDuration = 0;

    private bool m_isRotate = false;

    // private float m_deathCount = 0.0f;

    private PlayerMode m_currentPlayerMode = PlayerMode.Normal;

    public GameObject m_trap;
    public GameObject m_slowDownTrap;
    public GameObject m_damageTrap;

    private GameObject m_handTrap;
    private TrapController m_handTrapController;

    // private bool m_cursorState = false;

    // Start is called before the first frame update
    void Start()
    {
        // initialize handtrap
        var pos = this.transform.position + this.transform.forward * 4.0f;
        pos.y = 1.5f;
        m_handTrap = GameObject.Instantiate(m_trap, pos, this.transform.rotation);
        m_handTrapController = m_handTrap.GetComponent<TrapController>();
        m_handTrapController.IsHandTrap = true;
        m_handTrap.SetActive(false);

        m_animator = GetComponent<Animator>();

        MessageDispacher.Instance.Register(GameObjectMessage.QueryPlayerPosition, msg =>
        {
            var result = msg as PlayerPositionQueryResult;
            if (Vector3.Distance(this.transform.position, StrongPoint.Instance.Position) < EnemyConfigure.StrongPointAttackDistance
                && PlayerInfo.LocalPlayer.Hp > 0)
            {
                result.PlayerInfoList.Add(PlayerInfo.LocalPlayer);
                result.PlayerPositionList.Add(this.transform.position);
            }
        });

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

        var throwBehaviour = m_animator.GetBehaviour<ThrowOverBehaviour>();
        throwBehaviour.CallBack = () =>
        {
            m_animator.SetBool("Throw", false);
        };


        m_networkService.Register(NetWorkCommandType.RechargeResult, cmd =>
        {
            m_animator.SetBool("Recharge", true);

            var cmdRechargeResult = cmd as RechargeResultCommand;

            PlayerInfo.LocalPlayer.CurrentBulletCapacity = cmdRechargeResult.CurrentBulletCount;
            PlayerInfo.LocalPlayer.MaxBulletCapacity = cmdRechargeResult.MaxBulletCount;

            // update ui
            MessageDispacher.Instance.Send(UIMessage.UpdateBulletUI, new BulletUpdateMessage
            {
                CurrentBulletCount = cmdRechargeResult.CurrentBulletCount,
                MaxBulletCount = cmdRechargeResult.MaxBulletCount,
            });

            m_isWaitingResult = false;
        });

        m_networkService.Register(NetWorkCommandType.RecoverHp, cmd =>
        {
            var cmdRecover = cmd as RecoverHpCommand;

            // update ui
            if (cmdRecover.UserID == PlayerInfo.LocalPlayer.UserID)
            {
                PlayerInfo.LocalPlayer.Hp = cmdRecover.Hp;
                MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, cmdRecover.Hp / 1000.0f);
            }
        });

        m_networkService.Register(NetWorkCommandType.SupplyBullets, cmd =>
        {
            var cmdSupply = cmd as SupplyBulletsCommand;
            if (cmdSupply.UserID == PlayerInfo.LocalPlayer.UserID)
            {
                PlayerInfo.LocalPlayer.CurrentBulletCapacity = cmdSupply.CurrentBulletCapacity;
                PlayerInfo.LocalPlayer.MaxBulletCapacity = cmdSupply.MaxBulletCapacity;

                // update ui
                MessageDispacher.Instance.Send(UIMessage.UpdateBulletUI, new BulletUpdateMessage
                {
                    CurrentBulletCount = cmdSupply.CurrentBulletCapacity,
                    MaxBulletCount = cmdSupply.MaxBulletCapacity,
                });
            }
        });

        m_networkService.Register(NetWorkCommandType.RunningShooting, cmd =>
        {
            var cmdFan = cmd as RunningShootingCommand;
            if (cmdFan.UserID == PlayerInfo.LocalPlayer.UserID)
            {
                PlayerInfo.LocalPlayer.ShootingType = ShoottingType.Running;
            }
        });

        m_networkService.Register(NetWorkCommandType.EnemyBulletHitResult, cmd =>
        {
            var cmdHit = cmd as EnemyBulletHitResultCommand;
            if (cmdHit.PlayerID == PlayerInfo.LocalPlayer.UserID)
            {
                PlayerInfo.LocalPlayer.Hp = cmdHit.PlayerHp;

                // update ui
                MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, cmdHit.PlayerHp / 1000.0f);
                m_animator.SetBool("Damaged", true);
            }
        });

        m_networkService.Register(NetWorkCommandType.PlayerDie, cmd =>
        {
            var cmdDie = cmd as PlayerDieCommand;
            if (cmdDie.PlayerID == PlayerInfo.LocalPlayer.UserID)
            {
                m_animator.SetBool("Death", true);
                // m_deathCount = PlayerConfigure.RebornWaitingTime;

                // MessageDispacher.Instance.Send(UIMessage.ShowRebornProgress, null);
                // MessageDispacher.Instance.Send(UIMessage.UpdateRebornProgress, 1.0f);
            }
        });

        m_networkService.Register(NetWorkCommandType.PlayerRebornResult, cmd =>
        {
            var cmdReborn = cmd as PlayerRebornResultCommand;
            if (cmdReborn.PlayerID == PlayerInfo.LocalPlayer.UserID)
            {
                PlayerInfo.LocalPlayer.Hp = cmdReborn.Hp;
                PlayerInfo.LocalPlayer.MaxBulletCapacity = cmdReborn.MaxBulletCount;
                PlayerInfo.LocalPlayer.CurrentBulletCapacity = cmdReborn.CurBulletCount;

                m_animator.SetBool("Death", false);

                // update ui
                MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, cmdReborn.Hp / 1000.0f);
                MessageDispacher.Instance.Send(UIMessage.UpdateBulletUI, new BulletUpdateMessage
                {
                    CurrentBulletCount = cmdReborn.CurBulletCount,
                    MaxBulletCount = cmdReborn.MaxBulletCount,
                });

                MessageDispacher.Instance.Send(
                    UIMessage.HideRebornProgress, null);
            }
        });

        m_networkService.Register(NetWorkCommandType.GunShootResult, cmd =>
        {
            var cmdGunShootResult = cmd as GunShootResultCommand;
            if (cmdGunShootResult.ShooterID == PlayerInfo.LocalPlayer.UserID)
            {
                PlayerInfo.LocalPlayer.MaxBulletCapacity = cmdGunShootResult.MaxBullet;
                PlayerInfo.LocalPlayer.CurrentBulletCapacity = cmdGunShootResult.CurrentBullet;

                // update ui
                MessageDispacher.Instance.Send(UIMessage.UpdateBulletUI, new BulletUpdateMessage
                {
                    CurrentBulletCount = cmdGunShootResult.CurrentBullet,
                    MaxBulletCount = cmdGunShootResult.MaxBullet,
                });

                m_isWaitingResult = false;
            }
        });

        m_networkService.Register(NetWorkCommandType.PutTrapResult, cmd =>
        {
            var cmdPutTrapResult = cmd as PutTrapResultCommand;
            if (cmdPutTrapResult.BuilderID == PlayerInfo.LocalPlayer.UserID)
            {
                GameObject trapObject = null;
                if (cmdPutTrapResult.TrapType == TrapType.Damage)
                {
                    trapObject = m_damageTrap;
                    PlayerInfo.LocalPlayer.DamageTrapCount = cmdPutTrapResult.TrapCount;
                }
                else if (cmdPutTrapResult.TrapType == TrapType.SlowDown)
                {
                    trapObject = m_slowDownTrap;
                    PlayerInfo.LocalPlayer.SlowTrapCount = cmdPutTrapResult.TrapCount;
                }

                // update ui
                MessageDispacher.Instance.Send(UIMessage.UpdateTrapCount, new TrapCountUpdateMessage
                {
                    DamageTrapCount = PlayerInfo.LocalPlayer.DamageTrapCount,
                    SlowTrapCount = PlayerInfo.LocalPlayer.SlowTrapCount
                });

                var newTrap = GameObject.Instantiate(trapObject, cmdPutTrapResult.Position, Quaternion.AngleAxis(cmdPutTrapResult.Rotation, Vector3.up));
                var scriptComp = newTrap.GetComponent<TrapController>();
                scriptComp.IsHandTrap = false;
                scriptComp.TrapInfo = new TrapInfo()
                {
                    TrapType = cmdPutTrapResult.TrapType,
                    ID = cmdPutTrapResult.TrapID,
                };

                m_isWaitingResult = false;
            }
        });

        m_networkService.Register(NetWorkCommandType.EnemyAttackResult, OnEnemyAttackResult);

        m_networkService.Register(NetWorkCommandType.EnemyGunShootResult, OnEnemyGunShootResult);

        m_networkService.Register(NetWorkCommandType.SelfDestructResult, OnSelfDestructResult);

        m_networkService.Register(NetWorkCommandType.DriveTankResult, OnDriveTankResult);

        m_networkService.Register(NetWorkCommandType.ShootResult, OnShootResult);

        m_networkService.Register(NetWorkCommandType.BuyTrapResult, cmd =>
        {
            var cmdResult = cmd as BuyTrapResultCommand;

            PlayerInfo.LocalPlayer.DamageTrapCount = cmdResult.DamageTrapCount;
            PlayerInfo.LocalPlayer.SlowTrapCount = cmdResult.SlowTrapCount;
            PlayerInfo.LocalPlayer.Gold = cmdResult.Gold;

            MessageDispacher.Instance.Send(UIMessage.UpdateGold, PlayerInfo.LocalPlayer.Gold);
            MessageDispacher.Instance.Send(UIMessage.UpdateTrapCount, new TrapCountUpdateMessage
            {
                DamageTrapCount = PlayerInfo.LocalPlayer.DamageTrapCount,
                SlowTrapCount = PlayerInfo.LocalPlayer.SlowTrapCount
            });
            m_isWaitingResult = false;
        });

        m_networkService.Register(NetWorkCommandType.BuyGrenateResult, cmd =>
        {
            var cmdResult = cmd as BuyGrenadeResultCommand;

            PlayerInfo.LocalPlayer.GrenadeCount = cmdResult.GrenateCount;
            PlayerInfo.LocalPlayer.Gold = cmdResult.Gold;

            MessageDispacher.Instance.Send(UIMessage.UpdateGold, PlayerInfo.LocalPlayer.Gold);
            MessageDispacher.Instance.Send(UIMessage.UpdateGrenate, PlayerInfo.LocalPlayer.GrenadeCount);

            m_isWaitingResult = false;
        });

        MessageDispacher.Instance.Register(GameObjectMessage.SwitchCursorState, msg =>
        {
            var state = (bool)msg;
            // m_cursorState = state;
        });
    }

    void OnShootResult(NetworkCommand cmd)
    {
        m_isWaitingResult = false;

        if (PlayerInfo.LocalPlayer.ShootingType == ShoottingType.Running)
        {
            m_shootTimeCount = 0;
        }
        else
        {
            m_shootTimeCount = WeaponConfigure.HeaveyWeaponConfigure.ShootDeltaTime;
        }

        var cmdShootResult = cmd as ShootResultCommand;

        PlayerInfo.LocalPlayer.CurrentBulletCapacity = cmdShootResult.CurrentBulletCount;
        PlayerInfo.LocalPlayer.MaxBulletCapacity = cmdShootResult.MaxBulletCount;
        PlayerInfo.LocalPlayer.GrenadeCount = cmdShootResult.GrenadeCount;

        // update ui
        MessageDispacher.Instance.Send(UIMessage.UpdateBulletUI, new BulletUpdateMessage
        {
            CurrentBulletCount = cmdShootResult.CurrentBulletCount,
            MaxBulletCount = cmdShootResult.MaxBulletCount,
        });

        MessageDispacher.Instance.Send(UIMessage.UpdateGrenate, PlayerInfo.LocalPlayer.GrenadeCount);

        for (int i = 0; i < m_bulletInfoBuffer.Count; ++i)
        {
            if (m_bulletInfoBuffer[i].Type == BulletType.Big)
            {
                var gameObj = GameObject.Instantiate(this.m_bulletBig,
                    m_bulletPositionBuffer[i],
                    Quaternion.AngleAxis(360.0f * m_bulletRotationBuffer[i],
                    Vector3.up));
                var scriptComp = gameObj.GetComponent<BulletController>();
                scriptComp.BulletInfo = m_bulletInfoBuffer[i];

                // Debug.Log(m_bulletPositionBuffer[i]);
            }
        }

        m_animator.SetBool("Throw", true);
    }

    void OnEnemyAttackResult(NetworkCommand cmd)
    {
        var cmdAttacked = cmd as EnemyAttackResultCommand;

        // update ui
        if (cmdAttacked.AttackedUserID == PlayerInfo.LocalPlayer.UserID)
        {
            m_animator.SetBool("Damaged", true);
            PlayerInfo.LocalPlayer.Hp = cmdAttacked.Hp;
            MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, cmdAttacked.Hp / 1000.0f);
        }
    }

    void OnSelfDestructResult(NetworkCommand cmd)
    {
        var cmdDestructResult = cmd as SelfDestructResultCommand;
        if (cmdDestructResult.DamagedPlayerID == PlayerInfo.LocalPlayer.UserID)
        {
            PlayerInfo.LocalPlayer.Hp = cmdDestructResult.Hp;

            // update ui
            MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, cmdDestructResult.Hp / 1000.0f);
            m_animator.SetBool("Damaged", true);
        }
    }

    void OnEnemyGunShootResult(NetworkCommand cmd)
    {
        var cmdHit = cmd as EnemyGunShootResultCommand;
        if (cmdHit.TargetID == PlayerInfo.LocalPlayer.UserID)
        {
            if (cmdHit.IsHit)
            {
                PlayerInfo.LocalPlayer.Hp = cmdHit.Hp;

                // update ui
                MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, cmdHit.Hp / 1000.0f);
                m_animator.SetBool("Damaged", true);
            }
        }
    }

    void OnDriveTankResult(NetworkCommand cmd)
    {
        var cmdDriveResult = cmd as DriveTankResultCommand;
        if (cmdDriveResult.DriverID == PlayerInfo.LocalPlayer.UserID)
        {
            TankInfo.Instance.DriverID = cmdDriveResult.DriverID;
            TankInfo.Instance.IsDriven = true;

            MessageDispacher.Instance.Send(GameObjectMessage.HidePlayer, this.gameObject);

            m_networkService.Unregister(NetWorkCommandType.DriveTankResult, OnDriveTankResult);
            m_networkService.Unregister(NetWorkCommandType.EnemyAttackResult, OnEnemyAttackResult);
            m_networkService.Unregister(NetWorkCommandType.SelfDestructResult, OnSelfDestructResult);
            m_networkService.Unregister(NetWorkCommandType.EnemyGunShootResult, OnEnemyGunShootResult);
            m_networkService.Unregister(NetWorkCommandType.ShootResult, OnShootResult);

            m_isWaitingResult = false;
        }
    }

    public void ResumeRegister()
    {
        m_networkService.Register(NetWorkCommandType.DriveTankResult, OnDriveTankResult);
        m_networkService.Register(NetWorkCommandType.EnemyAttackResult, OnEnemyAttackResult);
        m_networkService.Register(NetWorkCommandType.SelfDestructResult, OnSelfDestructResult);
        m_networkService.Register(NetWorkCommandType.EnemyGunShootResult, OnEnemyGunShootResult);
        m_networkService.Register(NetWorkCommandType.ShootResult, OnShootResult);

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

        var throwBehaviour = m_animator.GetBehaviour<ThrowOverBehaviour>();
        throwBehaviour.CallBack = () =>
        {
            m_animator.SetBool("Throw", false);
        };
    }

    // Update is called once per frame
    void Update()
    {
        // player dying
        if (m_animator.GetBool("Death"))
        {
            // if (m_deathCount > 0)
            // {
            //     m_deathCount -= Time.deltaTime;
            //     MessageDispacher.Instance.Send(
            //         UIMessage.UpdateRebornProgress, 1.0f - m_deathCount / PlayerConfigure.RebornWaitingTime);
            //     if (m_deathCount <= 0)
            //     {
            //         m_deathCount = 0;
            //         m_networkService.Send(new PlayerRebornCommand(PlayerInfo.LocalPlayer.UserID));
            //     }
            // }
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        // move
        m_keyBoardDirection = KeyBoardMoveDescriptor.None;

        float dx = Input.GetAxis("Mouse X");
        var rate = PlayerConfigure.CameraMouseMoveRateX;

        m_phi += dx * rate;

        if (Mathf.Abs(dx) > 1e-5f)
        {
            m_isRotate = true;
        }

        var rotate = Quaternion.AngleAxis(360.0f * m_phi, Vector3.up);
        this.transform.rotation = rotate;

        if (Input.GetKey(KeyCode.W))
        {
            m_keyBoardDirection |= KeyBoardMoveDescriptor.Up;
            m_animator.SetFloat("Direction", 1.0f, 0.0f, Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            m_keyBoardDirection |= KeyBoardMoveDescriptor.Down;
            m_animator.SetFloat("Direction", -1.0f, 0.0f, Time.deltaTime);
        }
        else
        {
            m_animator.SetFloat("Direction", 0.0f);
        }

        if (Input.GetKey(KeyCode.A))
        {
            m_keyBoardDirection |= KeyBoardMoveDescriptor.Left;
            m_animator.SetFloat("LeftOrRight", -1.0f, 0.2f, Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            m_keyBoardDirection |= KeyBoardMoveDescriptor.Right;
            m_animator.SetFloat("LeftOrRight", 1.0f, 0.2f, Time.deltaTime);
        }
        else
        {
            m_animator.SetFloat("LeftOrRight", 0.0f, 0.2f, Time.deltaTime);
        }

        Move();

        if (m_keyBoardDirection != KeyBoardMoveDescriptor.None || Input.GetKey(KeyCode.Space))
        {
            m_networkService.Send(
                new MoveCommand(
                    PlayerInfo.LocalPlayer.UserID,
                    transform.position,
                    m_phi,
                    (byte)m_keyBoardDirection,
                    Input.GetKey(KeyCode.Space)
                )
            );
        }
        else
        {
            if (m_isRotate)
            {
                m_networkService.Send(new PlayerRotateCommand(PlayerInfo.LocalPlayer.UserID, m_phi));
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            m_currentPlayerMode = PlayerMode.Normal;
            m_handTrap.SetActive(false);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            m_currentPlayerMode = PlayerMode.SetTrap;
            m_handTrap.SetActive(true);
            m_handTrap.GetComponent<TrapController>().Reset();
        }

        if (m_currentPlayerMode == PlayerMode.Normal)
        {
            // switch shooting mode
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (m_shootingMode == ShootingMode.Fixed)
                {
                    m_shootingMode = ShootingMode.Running;
                }
                else if (m_shootingMode == ShootingMode.Running)
                {
                    m_shootingMode = ShootingMode.Fixed;
                }
            }

            // recharge
            if (Input.GetKeyDown(KeyCode.R))
            {
                Recharge();
            }

            // shoot
            Shoot();
        }
        // set trap
        else
        {
            SetTrap();
        }

        BuyThings();
        DriveTank();

        m_isRotate = false;
    }

    private void BuyThings()
    {
        if (m_isWaitingResult)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Z) && PlayerInfo.LocalPlayer.Gold >= 200)
        {
            m_networkService.Send(new BuyTrapCommand(PlayerInfo.LocalPlayer.UserID, TrapType.Damage));
            m_isWaitingResult = true;
        }
        else if (Input.GetKeyDown(KeyCode.X) && PlayerInfo.LocalPlayer.Gold >= 200)
        {
            m_networkService.Send(new BuyTrapCommand(PlayerInfo.LocalPlayer.UserID, TrapType.SlowDown));
            m_isWaitingResult = true;
        }
        else if (Input.GetKeyDown(KeyCode.C) && PlayerInfo.LocalPlayer.Gold >= 250)
        {
            m_networkService.Send(new BuyGrenadeCommand(PlayerInfo.LocalPlayer.UserID));
            m_isWaitingResult = true;
        }
    }

    private void DriveTank()
    {
        if (m_isWaitingResult)
        {
            return;
        }

        // press F key to get into the tank :)
        if (Input.GetKeyDown(KeyCode.F))
        {
            var pos = this.transform.position;
            pos.y = 1.0f;
            var ray = new Ray(pos, this.transform.forward);
            RaycastHit hit;
            LayerMask mask = 1 << 8;
            var result = Physics.Raycast(ray, out hit, 5.0f, mask);

            if (result)
            {
                if (hit.transform.CompareTag("Tank"))
                {
                    if (!TankInfo.Instance.IsDriven)
                    {
                        m_networkService.Send(new DriveTankCommand(PlayerInfo.LocalPlayer.UserID));
                        m_isWaitingResult = true;
                    }
                }
            }
        }
    }

    private void SetTrap()
    {
        var pos = this.transform.position + this.transform.forward * 4.0f;
        pos.y = 0.1f;
        m_handTrap.transform.position = pos;

        if (m_isWaitingResult)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (PlayerInfo.LocalPlayer.DamageTrapCount > 0 && m_handTrapController.CanSetTrap())
            {
                m_networkService.Send(new PutTrapCommand(
                    PlayerInfo.LocalPlayer.UserID,
                    m_handTrap.transform.position,
                    m_handTrap.transform.rotation.eulerAngles.y,
                    TrapType.Damage)
                );
                m_isWaitingResult = true;
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            if (PlayerInfo.LocalPlayer.SlowTrapCount > 0 && m_handTrapController.CanSetTrap())
            {
                m_networkService.Send(new PutTrapCommand(
                    PlayerInfo.LocalPlayer.UserID,
                    m_handTrap.transform.position,
                    m_handTrap.transform.rotation.eulerAngles.y,
                    TrapType.SlowDown)
                );
                m_isWaitingResult = true;
            }
        }
    }

    private void Recharge()
    {
        if (PlayerInfo.LocalPlayer.CurrentBulletCapacity < 30
            && PlayerInfo.LocalPlayer.MaxBulletCapacity > PlayerInfo.LocalPlayer.CurrentBulletCapacity
            && !m_isWaitingResult
            && !m_animator.GetBool("Recharge"))
        {
            m_networkService.Send(new RechargeCommand(PlayerInfo.LocalPlayer.UserID));
            m_isWaitingResult = true;

            MessageDispacher.Instance.Send(AudioMessage.PlayRecharge, null);
        }
    }

    void Shoot()
    {
        // update ui
        MessageDispacher.Instance.Send(
            UIMessage.UpdateCoolDownProgressUI,
            m_bigSkillCoolDownDuration <= 0 ? 1.0f : 1.0f - m_bigSkillCoolDownDuration / PlayerConfigure.BigSkillCoolDownDuration);

        if (m_isWaitingResult)
        {
            return;
        }

        if (Input.GetMouseButtonDown(1)
            && !m_animator.GetBool("Shooting")
            && !m_animator.GetBool("Throw")
            && !m_animator.GetBool("Recharge"))
        {
            if (PlayerInfo.LocalPlayer.GrenadeCount > 0)
            {
                _BigShoot();
            }
        }

        if (m_bigSkillCoolDownDuration > 0)
        {
            m_bigSkillCoolDownDuration -= Time.deltaTime;
        }

        if (Input.GetMouseButtonDown(0)
            && m_shootTimeCount <= 0
            && !m_animator.GetBool("Shooting")
            && !m_animator.GetBool("Recharge")
            && !m_animator.GetBool("Throw"))
        {
            if (PlayerInfo.LocalPlayer.ShootingType == ShoottingType.Normal)
            {
                _NormalShoot();
            }
            else if (PlayerInfo.LocalPlayer.ShootingType == ShoottingType.Running)
            {
                _RunningShoot();
            }
        }
        else if (m_shootTimeCount > 0)
        {
            m_shootTimeCount -= Time.deltaTime;
        }
    }

    private void _RunningShoot()
    {
        if (PlayerInfo.LocalPlayer.CurrentBulletCapacity >= 1)
        {
            var ray = m_camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;
            LayerMask mask = 1 << 9;
            var result = Physics.Raycast(ray, out hit, 1000.0f, mask);
            if (result)
            {
                if (hit.transform.CompareTag("Enemy"))
                {
                    var enmey = hit.transform.GetComponent<EnemyController>().EnemyInfo;
                    m_networkService.Send(new GunShootCommand(PlayerInfo.LocalPlayer.UserID, 1, true, enmey.EnemyID));
                }
                else if (hit.transform.CompareTag("EnemySync"))
                {
                    var enmey = hit.transform.GetComponent<EnemyControllerSync>().EnemyInfo;
                    m_networkService.Send(new GunShootCommand(PlayerInfo.LocalPlayer.UserID, 1, true, enmey.EnemyID));
                }
                else
                {
                    m_networkService.Send(new GunShootCommand(PlayerInfo.LocalPlayer.UserID, 1, false, 0));
                }
            }
            else
            {
                m_networkService.Send(new GunShootCommand(PlayerInfo.LocalPlayer.UserID, 1, false, 0));
            }

            m_isWaitingResult = true;

            MessageDispacher.Instance.Send(AudioMessage.PlayPlayerShooting, null);
        }
        else
        {
            Recharge();
        }
    }

    private void _NormalShoot()
    {
        if (PlayerInfo.LocalPlayer.CurrentBulletCapacity >= 1)
        {
            var ray = m_camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;
            LayerMask mask = 1 << 9;
            var result = Physics.Raycast(ray, out hit, 1000.0f, mask);
            if (result)
            {
                if (hit.transform.CompareTag("Enemy"))
                {
                    var enmey = hit.transform.GetComponent<EnemyController>().EnemyInfo;
                    m_networkService.Send(new GunShootCommand(PlayerInfo.LocalPlayer.UserID, 1, true, enmey.EnemyID));
                }
                else if (hit.transform.CompareTag("EnemySync"))
                {
                    var enmey = hit.transform.GetComponent<EnemyControllerSync>().EnemyInfo;
                    m_networkService.Send(new GunShootCommand(PlayerInfo.LocalPlayer.UserID, 1, true, enmey.EnemyID));
                }
                else
                {
                    m_networkService.Send(new GunShootCommand(PlayerInfo.LocalPlayer.UserID, 1, false, 0));
                }
            }
            else
            {
                m_networkService.Send(new GunShootCommand(PlayerInfo.LocalPlayer.UserID, 1, false, 0));
            }

            // play se
            MessageDispacher.Instance.Send(AudioMessage.PlayPlayerShooting, null);

            m_isWaitingResult = true;
        }
        else
        {
            Recharge();
        }
    }

    private void _BigShoot()
    {
        if (m_bigSkillCoolDownDuration <= 0)
        {
            m_bigSkillCoolDownDuration = PlayerConfigure.BigSkillCoolDownDuration;
        }
        else
        {
            return;
        }

        m_bulletInfoBuffer.Clear();
        m_bulletRotationBuffer.Clear();
        m_bulletPositionBuffer.Clear();

        var shootingPos = this.transform.position + this.transform.forward * 2.0f + this.transform.up * 3.0f;

        m_bulletInfoBuffer.Add(new BulletInfo { Type = BulletType.Big, ShooterID = PlayerInfo.LocalPlayer.UserID });
        m_bulletRotationBuffer.Add(this.m_phi);
        m_bulletPositionBuffer.Add(shootingPos);

        m_networkService.Send(new ShootCommand(PlayerInfo.LocalPlayer.UserID,
            m_bulletPositionBuffer,
            m_bulletRotationBuffer,
            m_bulletInfoBuffer,
            true
        ));

        m_isWaitingResult = true;
    }

    private void Move()
    {
        if (m_keyBoardDirection == KeyBoardMoveDescriptor.None)
        {
            m_animator.SetInteger("ForwardOrBack", 0);
            return;
        }

        m_animator.SetInteger("ForwardOrBack", 1);

        Vector3 moveVector;

        if ((m_keyBoardDirection & KeyBoardMoveDescriptor.Down) != 0)
        {
            moveVector = -this.transform.forward * Time.deltaTime * PlayerConfigure.MoveSpeed;
        }
        else
        {
            moveVector = this.transform.forward * Time.deltaTime * PlayerConfigure.MoveSpeed;
        }

        if (((m_keyBoardDirection & KeyBoardMoveDescriptor.Up) != 0)
            || ((m_keyBoardDirection & KeyBoardMoveDescriptor.Down) != 0))
        {
            if ((m_keyBoardDirection & KeyBoardMoveDescriptor.Left) != 0)
            {
                moveVector = Quaternion.AngleAxis(-45.0f, Vector3.up) * moveVector;
            }
            else if ((m_keyBoardDirection & KeyBoardMoveDescriptor.Right) != 0)
            {
                moveVector = Quaternion.AngleAxis(45.0f, Vector3.up) * moveVector;
            }
        }
        else
        {
            if ((m_keyBoardDirection & KeyBoardMoveDescriptor.Left) != 0)
            {
                moveVector = Quaternion.AngleAxis(-90.0f, Vector3.up) * moveVector;
            }
            else if ((m_keyBoardDirection & KeyBoardMoveDescriptor.Right) != 0)
            {
                moveVector = Quaternion.AngleAxis(90.0f, Vector3.up) * moveVector;
            }
        }

        transform.Translate(moveVector, Space.World);
    }

    private void LateUpdate()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float rate = PlayerConfigure.CameraMouseMoveRateY;
        const float distance = 5.0f;

        float dy = Input.GetAxis("Mouse Y");
        m_theta -= (dy * rate);
        m_theta = Mathf.Clamp(m_theta, Mathf.PI, Mathf.PI / 2 * 3);

        Vector3 pos = -distance * Mathf.Cos(m_theta) * this.transform.forward.normalized + distance * Mathf.Sin(m_theta) * Vector3.up;

        m_camera.transform.position = this.transform.position - pos;
        m_camera.transform.LookAt(this.transform.position + new Vector3(1.0f, 2.0f, 0.0f), Vector3.up);
    }
}
