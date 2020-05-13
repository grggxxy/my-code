using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using System.Threading.Tasks;

public class EnemyController : MonoBehaviour
{
    public EnemyInfo EnemyInfo { get; set; }
    NavMeshAgent m_agent;
    float m_reRouteTime = 0;
    Animator m_animator;
    Vector3 m_targetPos;
    float m_attackTimeCount;
    float m_debuffDamageTimeCount;
    INetworkService m_networkService = RemoteNetworkServce.Instance;

    PlayerInfo m_currentTargetPlayer;

    // Debuff m_debuff = new Debuff();
    List<Debuff> m_debufList = new List<Debuff>();

    float m_normalSpeed = 0.0f;
    bool m_waitDebuffDamage = false;

    bool m_targetIsStrongPoint = false;

    bool m_gameEnd = false;

    PlayerPositionQueryResult m_query = new PlayerPositionQueryResult();

    private void OnExplodeResult(NetworkCommand cmd)
    {
        var cmdExplodeResult = cmd as ExplodeResultCommand;
        if (cmdExplodeResult.DamagedEnemyID == this.EnemyInfo.EnemyID)
        {
            var damage = this.EnemyInfo.Hp - cmdExplodeResult.Hp;
            this.EnemyInfo.Hp = cmdExplodeResult.Hp;
            m_animator.SetBool("Damaged", true);

            // update ui
            MessageDispacher.Instance.Send(UIMessage.EnmeyDamaged, new EnemyDamageMessage()
            {
                EnemyID = this.EnemyInfo.EnemyID,
                Damage = damage,
            });

            MessageDispacher.Instance.Send(UIMessage.UpdateEnemyHpSlider, new EnemyHpUpdateMessage()
            {
                EnemyID = this.EnemyInfo.EnemyID,
                Progress = (float)this.EnemyInfo.Hp / this.EnemyInfo.MaxHp,
            });

            MessageDispacher.Instance.Send(AudioMessage.PlayExplode, null);
        }
    }

    private void OnEnemyDie(NetworkCommand cmd)
    {
        var cmdEnemyDie = cmd as EnemyDieCommand;
        if (cmdEnemyDie.EnemyID == this.EnemyInfo.EnemyID)
        {
            m_agent = null;
            DestroyThis();
            m_animator.SetBool("Death", true);
        }
    }

    private void OnAttachDebuffResult(NetworkCommand cmd)
    {
        var cmdDebuff = cmd as DebuffAttachResultCommand;
        if (cmdDebuff.TargetEnemyID == this.EnemyInfo.EnemyID)
        {
            this.m_debufList.Add(new Debuff()
            {
                DeBuffType = cmdDebuff.DebuffType,
                Duration = cmdDebuff.DebuffDuration,
            });

            if (cmdDebuff.DebuffType == DebuffType.SlowDown)
            {
                m_agent.speed = 1.0f;
            }
            else if (cmdDebuff.DebuffType == DebuffType.Damage)
            {
                m_waitDebuffDamage = false;
                m_debuffDamageTimeCount = EnemyConfigure.DebuffDamageInterval;
            }
        }
    }

    private void OnRemoveDebuffResult(NetworkCommand cmd)
    {
        var cmdRemove = cmd as DebuffRemoveResultCommand;
        if (cmdRemove.TargetEnemyID == this.EnemyInfo.EnemyID)
        {
            var result = this.m_debufList.Find(debuff => debuff.DeBuffType == cmdRemove.DebuffType);
            this.m_debufList.Remove(result);

            if (cmdRemove.DebuffType == DebuffType.SlowDown)
            {
                m_agent.speed = m_normalSpeed;
            }
        }
    }

    private void OnGunShootResult(NetworkCommand cmd)
    {
        var cmdGunShootResult = cmd as GunShootResultCommand;
        if (cmdGunShootResult.EnemyID == this.EnemyInfo.EnemyID && cmdGunShootResult.IsHit)
        {
            var damage = this.EnemyInfo.Hp - cmdGunShootResult.EnemyHp;
            this.EnemyInfo.Hp = cmdGunShootResult.EnemyHp;
            m_animator.SetBool("Damaged", true);
            // Debug.Log($"enmey {this.EnemyInfo.EnemyID} hit, hp {this.EnemyInfo.Hp}");

            // update ui
            MessageDispacher.Instance.Send(UIMessage.EnmeyDamaged, new EnemyDamageMessage()
            {
                EnemyID = this.EnemyInfo.EnemyID,
                Damage = damage,
            });

            MessageDispacher.Instance.Send(UIMessage.UpdateEnemyHpSlider, new EnemyHpUpdateMessage
            {
                EnemyID = this.EnemyInfo.EnemyID,
                Progress = (float)this.EnemyInfo.Hp / this.EnemyInfo.MaxHp,
            });

            MessageDispacher.Instance.Send(AudioMessage.PlayEnemyShooting, null);
        }
    }

    private void OnDebuffDamageResult(NetworkCommand cmd)
    {
        var cmdDamageResult = cmd as EnemyDebuffDamagedResultCommand;
        if (cmdDamageResult.EnmeyID == this.EnemyInfo.EnemyID)
        {
            var damage = this.EnemyInfo.Hp - cmdDamageResult.Hp;
            this.EnemyInfo.Hp = cmdDamageResult.Hp;
            m_animator.SetBool("Damaged", true);

            // update ui
            MessageDispacher.Instance.Send(UIMessage.EnmeyDamaged, new EnemyDamageMessage()
            {
                EnemyID = this.EnemyInfo.EnemyID,
                Damage = damage,
            });

            MessageDispacher.Instance.Send(UIMessage.UpdateEnemyHpSlider, new EnemyHpUpdateMessage()
            {
                EnemyID = this.EnemyInfo.EnemyID,
                Progress = (float)this.EnemyInfo.Hp / this.EnemyInfo.MaxHp,
            });

            m_debuffDamageTimeCount = EnemyConfigure.DebuffDamageInterval;
            m_waitDebuffDamage = false;
        }
    }

    private void OnNewGameResult(NetworkCommand cmd)
    {
        DestroyThis();
        Destroy(this.gameObject);
    }

    private void OnGameResult(NetworkCommand cmd)
    {
        m_gameEnd = true;
    }

    private void DestroyThis()
    {
        m_networkService.Unregister(NetWorkCommandType.NewGameResult, OnNewGameResult);
        m_networkService.Unregister(NetWorkCommandType.EnemyDie, OnEnemyDie);
        m_networkService.Unregister(NetWorkCommandType.DebuffAttachResult, OnAttachDebuffResult);
        m_networkService.Unregister(NetWorkCommandType.DebuffRemoveResult, OnRemoveDebuffResult);
        m_networkService.Unregister(NetWorkCommandType.GunShootResult, OnGunShootResult);
        m_networkService.Unregister(NetWorkCommandType.ExplodeResult, this.OnExplodeResult);
        m_networkService.Unregister(NetWorkCommandType.EnemyDebuffDamagedResult, this.OnDebuffDamageResult);
        m_networkService.Unregister(NetWorkCommandType.GameResult, this.OnGameResult);
    }

    // Start is called before the first frame update
    void Start()
    {
        m_agent = GetComponent<NavMeshAgent>();
        m_animator = GetComponent<Animator>();

        m_normalSpeed = m_agent.speed;

        // this.m_gun = GameObject.Find("player/Bip001");
        // var result = this.transform
        //                 .Cast<Transform>()
        //                 .Select(t => t.gameObject)
        //                 .Where(gameObject => gameObject.name == "Bip001")
        //                 .ToList();

        m_networkService.Register(NetWorkCommandType.NewGameResult, OnNewGameResult);
        m_networkService.Register(NetWorkCommandType.EnemyDie, OnEnemyDie);
        m_networkService.Register(NetWorkCommandType.DebuffAttachResult, OnAttachDebuffResult);
        m_networkService.Register(NetWorkCommandType.DebuffRemoveResult, OnRemoveDebuffResult);
        m_networkService.Register(NetWorkCommandType.GunShootResult, OnGunShootResult);
        m_networkService.Register(NetWorkCommandType.ExplodeResult, OnExplodeResult);
        m_networkService.Register(NetWorkCommandType.EnemyDebuffDamagedResult, OnDebuffDamageResult);
        m_networkService.Register(NetWorkCommandType.GameResult, this.OnGameResult);

        if (this.EnemyInfo.Type == EnemyType.Shooting)
        {
            var shoottingOverBehaviour = m_animator.GetBehaviour<ShootingOverBehaviour>();
            shoottingOverBehaviour.CallBack = () =>
            {
                m_animator.SetBool("Shooting", false);
            };
        }
        else if (this.EnemyInfo.Type == EnemyType.Melee)
        {
            var attakingOverBehaviour = m_animator.GetBehaviour<AttackingOverBehaviour>();
            attakingOverBehaviour.CallBack = () =>
            {
                m_animator.SetBool("Attacking", false);
            };
        }

        var deathOverBehaviour = m_animator.GetBehaviour<DeathOverBehaviour>();
        deathOverBehaviour.CallBack = () =>
        {
            m_animator.SetBool("Death", false);
            Destroy(this.gameObject);
        };

        var damagedBehaviour = m_animator.GetBehaviour<DamangedOverBehaviour>();
        damagedBehaviour.CallBack = () =>
        {
            m_animator.SetBool("Damaged", false);
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (m_agent == null)
        {
            return;
        }

        if (m_gameEnd)
        {
            return;
        }

        HandleDebuff();


        /*
            if player to strongpoint distance < strongpoint radius -> attack player
            else -> attak strongpoint
        */

        if (PlayerInfo.PlayerInfos.Count > 0 && m_reRouteTime <= 0.0)
        {
            m_query.Clear();
            MessageDispacher.Instance.Send(GameObjectMessage.QueryPlayerPosition, m_query);

            var count = m_query.PlayerInfoList.Count;
            if (count > 0)
            {
                var index = Random.Range(0, count);
                m_currentTargetPlayer = m_query.PlayerInfoList[index];

                if (m_currentTargetPlayer.UserID == TankInfo.Instance.DriverID)
                {
                    if (Vector3.Distance(m_query.TankPosition, StrongPoint.Instance.Position) < EnemyConfigure.StrongPointAttackDistance)
                    {
                        m_targetPos = m_query.TankPosition;
                    }
                    else
                    {
                        m_targetPos = StrongPoint.Instance.Position;
                        m_targetIsStrongPoint = true;
                    }
                }
                else
                {
                    m_targetPos = m_query.PlayerPositionList[index];
                }

                m_targetIsStrongPoint = false;
            }
            else
            {
                m_targetPos = StrongPoint.Instance.Position;
                m_targetIsStrongPoint = true;
            }

            m_agent.SetDestination(m_targetPos);
        }

        if (this.EnemyInfo.Type == EnemyType.Shooting)
        {
            _Shooting();
        }
        else if (this.EnemyInfo.Type == EnemyType.Melee)
        {
            _Attacking();
        }
        else if (this.EnemyInfo.Type == EnemyType.SelfDestruct)
        {
            _SelfDestruct();
        }

        if (!m_agent.pathPending && m_agent.remainingDistance <= m_agent.stoppingDistance)
        {
            // face to position
            m_networkService.Send(
                new EnemyMoveCommand(
                    PlayerInfo.LocalPlayer.UserID,
                    this.EnemyInfo.EnemyID,
                    this.transform.position,
                    this.transform.rotation.eulerAngles.y)
            );

            m_animator.SetInteger("ForwardOrBack", 0);
        }
        else
        {
            m_animator.SetInteger("ForwardOrBack", 1);
        }

        if (!m_agent.isStopped && !(!m_agent.pathPending && m_agent.remainingDistance <= m_agent.stoppingDistance))
        {
            this.transform.LookAt(m_targetPos);
            m_networkService.Send(
                new EnemyMoveCommand(
                    PlayerInfo.LocalPlayer.UserID,
                    this.EnemyInfo.EnemyID,
                    m_agent.transform.position,
                    m_agent.transform.rotation.eulerAngles.y)
            );
        }

        if (m_reRouteTime <= 0)
        {
            this.transform.LookAt(m_targetPos);
            m_networkService.Send(
                new EnemyMoveCommand(
                    PlayerInfo.LocalPlayer.UserID,
                    this.EnemyInfo.EnemyID,
                    m_agent.transform.position,
                    m_agent.transform.rotation.eulerAngles.y)
            );
            m_reRouteTime = EnemyConfigure.RerouteTime;
        }
        else
        {
            m_reRouteTime -= Time.deltaTime;
        }
    }

    private void HandleDebuff()
    {
        foreach (var debuff in m_debufList)
        {
            if (debuff.DeBuffType == DebuffType.Dizzy)
            {
                debuff.Duration -= Time.deltaTime;
                if (debuff.Duration <= 0)
                {
                    debuff.Duration = 0;
                    debuff.DeBuffType = DebuffType.None;
                    m_agent.isStopped = false;

                    m_networkService.Send(
                        new DebuffRemoveCommand(PlayerInfo.LocalPlayer.UserID, this.EnemyInfo.EnemyID, DebuffType.Dizzy)
                    );
                    m_animator.SetInteger("ForwardOrBack", 1);
                }
                else
                {
                    m_animator.SetInteger("ForwardOrBack", 0);
                    m_agent.isStopped = true;
                }
                return;
            }
            else if (debuff.DeBuffType == DebuffType.Damage && !m_waitDebuffDamage)
            {
                if (m_debuffDamageTimeCount <= 0)
                {
                    m_networkService.Send(new EnemyDebuffDamagedCommand(PlayerInfo.LocalPlayer.UserID, this.EnemyInfo.EnemyID));
                    m_waitDebuffDamage = true;
                }
                else
                {
                    m_debuffDamageTimeCount -= Time.deltaTime;
                }
            }
            else if (debuff.DeBuffType == DebuffType.SlowDown)
            {
            }
        }

    }

    bool HasDebuff(DebuffType debuffType)
    {
        return m_debufList.Exists(debuff => debuff.DeBuffType == debuffType);
    }

    private void _SelfDestruct()
    {
        if (!_Attackable())
        {
            return;
        }

        if (Vector3.Distance(m_targetPos, this.transform.position) <= EnemyConfigure.SelfDestructDistance)
        {
            m_networkService.Send(new SelfDestructCommand(PlayerInfo.LocalPlayer.UserID, this.EnemyInfo.EnemyID, this.transform.position));
            this.m_agent.isStopped = true;
            MessageDispacher.Instance.Send(AudioMessage.PlayExplode, null);
        }
    }

    private void _Shooting()
    {
        if (!_Attackable())
        {
            return;
        }

        if (Vector3.Distance(m_targetPos, this.transform.position) <= EnemyConfigure.LongDistanceAttackDistance)
        {
            if (!m_animator.GetBool("Shooting") && m_attackTimeCount <= 0)
            {
                m_attackTimeCount = EnemyConfigure.LongDistanceAttackDuration;
                m_animator.SetBool("Shooting", true);

                RaycastHit hit;
                LayerMask mask = 1 << 8;
                var result = Physics.Raycast(this.transform.position, this.transform.forward, out hit, 1000.0f, mask);
                if (result)
                {
                    if (hit.transform.CompareTag("Player"))
                    {
                        var playerID = PlayerInfo.LocalPlayer.UserID;
                        m_networkService.Send(
                            new EnemyGunShootCommand(playerID, this.EnemyInfo.EnemyID, true, 0, playerID)
                        );
                    }
                    else if (hit.transform.CompareTag("PlayerSync"))
                    {
                        var playerID = hit.transform.GetComponent<PlayerControllerSync>().PlayerInfo.UserID;
                        m_networkService.Send(
                            new EnemyGunShootCommand(PlayerInfo.LocalPlayer.UserID, this.EnemyInfo.EnemyID, true, 0, playerID)
                        );
                    }
                    else if (hit.transform.CompareTag("Tank"))
                    {
                        if (TankInfo.Instance.DriverID != 0xff)
                        {
                            var playerID = TankInfo.Instance.DriverID;
                            m_networkService.Send(
                                new EnemyGunShootCommand(PlayerInfo.LocalPlayer.UserID, this.EnemyInfo.EnemyID, true, 0, playerID)
                            );
                        }
                        else
                        {
                            m_networkService.Send(
                                new EnemyGunShootCommand(0, this.EnemyInfo.EnemyID, false, 0, 0)
                            );
                        }
                    }
                    else if (hit.transform.CompareTag("StrongPoint"))
                    {
                        m_networkService.Send(
                            new StrongPointAttackedCommand(PlayerInfo.LocalPlayer.UserID, this.EnemyInfo.EnemyID)
                        );
                    }
                    else
                    {
                        m_networkService.Send(
                            new EnemyGunShootCommand(0, this.EnemyInfo.EnemyID, false, 0, 0)
                        );
                    }
                }
                else
                {
                    m_networkService.Send(
                        new EnemyGunShootCommand(0, this.EnemyInfo.EnemyID, false, 0, 0)
                    );
                }

                // play se
                MessageDispacher.Instance.Send(AudioMessage.PlayEnemyShooting, null);
            }
            else if (m_attackTimeCount >= 0)
            {
                m_attackTimeCount -= Time.deltaTime;
            }
        }
    }

    private void _Attacking()
    {
        if (!_Attackable())
        {
            return;
        }

        if (!m_animator.GetBool("Attacking") && m_attackTimeCount <= 0)
        {
            if (Vector3.Distance(m_targetPos, m_agent.transform.position) <= EnemyConfigure.ShortDistanceAttackDistance)
            {
                m_animator.SetBool("Attacking", true);

                if (m_targetIsStrongPoint)
                {
                    m_networkService.Send(new StrongPointAttackedCommand(PlayerInfo.LocalPlayer.UserID, this.EnemyInfo.EnemyID));
                }
                else
                {
                    m_networkService.Send(new EnemyAttackCommand(PlayerInfo.LocalPlayer.UserID, m_currentTargetPlayer.UserID, this.EnemyInfo.EnemyID));
                }

                m_attackTimeCount = EnemyConfigure.ShortDistanceAttackDuration;

                MessageDispacher.Instance.Send(AudioMessage.PlayKnife, null);
            }
        }
        else if (m_attackTimeCount >= 0)
        {
            m_attackTimeCount -= Time.deltaTime;
        }
    }

    bool _Attackable()
    {
        if (m_targetIsStrongPoint)
        {
            return true;
        }

        if (m_currentTargetPlayer.UserID == TankInfo.Instance.DriverID && TankInfo.Instance.Hp > 0)
        {
            return true;
        }
        else if (m_currentTargetPlayer.Hp > 0)
        {
            return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Trap"))
        {
            var trapObj = other.GetComponent<TrapController>();
            if (!trapObj.IsHandTrap)
            {
                if (trapObj.TrapInfo.TrapType == TrapType.SlowDown)
                {
                    if (!this.HasDebuff(DebuffType.SlowDown))
                    {
                        m_networkService.Send(new DebuffAttachCommand(PlayerInfo.LocalPlayer.UserID, this.EnemyInfo.EnemyID, DebuffType.SlowDown));
                    }
                }
                else if (trapObj.TrapInfo.TrapType == TrapType.Damage)
                {
                    if (!this.HasDebuff(DebuffType.Damage))
                    {
                        m_networkService.Send(new DebuffAttachCommand(PlayerInfo.LocalPlayer.UserID, this.EnemyInfo.EnemyID, DebuffType.Damage));
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Trap"))
        {
            var trapObj = other.GetComponent<TrapController>();
            if (!trapObj.IsHandTrap)
            {
                if (trapObj.TrapInfo.TrapType == TrapType.SlowDown)
                {
                    if (this.HasDebuff(DebuffType.SlowDown))
                    {
                        m_networkService.Send(new DebuffRemoveCommand(PlayerInfo.LocalPlayer.UserID, this.EnemyInfo.EnemyID, DebuffType.SlowDown));
                    }

                }
                else if (trapObj.TrapInfo.TrapType == TrapType.Damage)
                {
                    if (this.HasDebuff(DebuffType.Damage))
                    {
                        m_networkService.Send(new DebuffRemoveCommand(PlayerInfo.LocalPlayer.UserID, this.EnemyInfo.EnemyID, DebuffType.SlowDown));
                    }

                }
            }
        }
    }

}
