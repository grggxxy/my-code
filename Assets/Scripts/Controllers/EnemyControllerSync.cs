using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct EnemyMoveTravelElem
{
    public Vector3 Position { get; set; }
    public float Rotation { get; set; }
}

public class EnemyControllerSync : MonoBehaviour
{
    public EnemyInfo EnemyInfo { get; set; }
    Queue<EnemyMoveTravelElem> m_moveTranvel = new Queue<EnemyMoveTravelElem>();
    INetworkService m_networkService = RemoteNetworkServce.Instance;
    Animator m_animator;

    // Start is called before the first frame update
    void Start()
    {
        m_animator = GetComponent<Animator>();

        m_networkService.Register(NetWorkCommandType.NewGameResult, this.OnNewGameResult);
        m_networkService.Register(NetWorkCommandType.EnemyMove, this.OnEnemyMove);
        m_networkService.Register(NetWorkCommandType.EnemyDie, this.OnEnemyDie);
        m_networkService.Register(NetWorkCommandType.ExplodeResult, this.OnExplodeResult);
        m_networkService.Register(NetWorkCommandType.EnemyShoot, this.OnEnemyShoot);
        m_networkService.Register(NetWorkCommandType.EnemyAttack, this.OnEnmeyAttack);
        m_networkService.Register(NetWorkCommandType.GunShootResult, this.OnGunShootResult);
        m_networkService.Register(NetWorkCommandType.EnemyDebuffDamagedResult, this.OnDebuffDamageResult);
        m_networkService.Register(NetWorkCommandType.StrongPointAttackedResult, this.OnStrongPointAttackedResult);
        m_networkService.Register(NetWorkCommandType.SelfDestructResult, this.OnSelfDestructResult);

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
    }

    private void OnSelfDestructResult(NetworkCommand command)
    {
        var cmdResult = command as SelfDestructResultCommand;
        if (cmdResult.EnemyID == this.EnemyInfo.EnemyID)
        {
            MessageDispacher.Instance.Send(AudioMessage.PlayExplode, null);
        }
    }

    private void OnStrongPointAttackedResult(NetworkCommand cmd)
    {
        var cmdResult = cmd as StrongPointAttackedResultCommand;
        if (cmdResult.EnmeyID == this.EnemyInfo.EnemyID)
        {
            if (this.EnemyInfo.Type == EnemyType.Melee)
            {
                MessageDispacher.Instance.Send(AudioMessage.PlayKnife, null);
                m_animator.SetBool("Attacking", true);
            }
            else if (this.EnemyInfo.Type == EnemyType.Shooting)
            {
                MessageDispacher.Instance.Send(AudioMessage.PlayEnemyShooting, null);
                m_animator.SetBool("Shooting", true);
            }
        }
    }

    private void OnNewGameResult(NetworkCommand cmd)
    {
        Destroy(this.gameObject);
    }

    private void OnEnemyDie(NetworkCommand command)
    {
        var cmdDie = command as EnemyDieCommand;
        if (cmdDie.EnemyID == this.EnemyInfo.EnemyID)
        {
            m_animator.SetBool("Death", true);
        }
    }

    private void OnEnmeyAttack(NetworkCommand command)
    {
        var cmdAttack = command as EnemyAttackCommand;
        if (cmdAttack.AttackingEnemyID == this.EnemyInfo.EnemyID)
        {
            MessageDispacher.Instance.Send(AudioMessage.PlayKnife, null);
            m_animator.SetBool("Attacking", true);
        }
    }

    private void OnEnemyShoot(NetworkCommand command)
    {
        var cmdShoot = command as EnemyShootCommand;
        if (cmdShoot.EnemyID == this.EnemyInfo.EnemyID)
        {
            // play se
            MessageDispacher.Instance.Send(AudioMessage.PlayEnemyShooting, null);
            m_animator.SetBool("Shooting", true);
        }
    }

    void OnEnemyMove(NetworkCommand cmd)
    {
        var cmdMove = cmd as EnemyMoveCommand;
        // Debug.Log($"Enemy move {this.EnemyInfo.EnemyID} {cmdMove.EnemyID}");
        if (cmdMove.EnemyID == this.EnemyInfo.EnemyID)
        {
            lock (m_moveTranvel)
            {
                m_moveTranvel.Enqueue(new EnemyMoveTravelElem
                {
                    Position = cmdMove.TargetPosition,
                    Rotation = cmdMove.Rotation,
                });
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

            MessageDispacher.Instance.Send(UIMessage.UpdateEnemyHpSlider, new EnemyHpUpdateMessage
            {
                EnemyID = this.EnemyInfo.EnemyID,
                Progress = (float)this.EnemyInfo.Hp / this.EnemyInfo.MaxHp,
            });

            MessageDispacher.Instance.Send(AudioMessage.PlayExplode, null);
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
        }
    }

    private void OnDestroy()
    {
        m_networkService.Unregister(NetWorkCommandType.NewGameResult, this.OnNewGameResult);
        m_networkService.Unregister(NetWorkCommandType.EnemyMove, this.OnEnemyMove);
        m_networkService.Unregister(NetWorkCommandType.EnemyDie, this.OnEnemyDie);
        m_networkService.Unregister(NetWorkCommandType.ExplodeResult, this.OnExplodeResult);
        m_networkService.Unregister(NetWorkCommandType.EnemyShoot, this.OnEnemyShoot);
        m_networkService.Unregister(NetWorkCommandType.EnemyAttack, this.OnEnmeyAttack);
        m_networkService.Unregister(NetWorkCommandType.GunShootResult, this.OnGunShootResult);
        m_networkService.Unregister(NetWorkCommandType.EnemyDebuffDamagedResult, this.OnDebuffDamageResult);
        m_networkService.Unregister(NetWorkCommandType.StrongPointAttackedResult, this.OnStrongPointAttackedResult);
    }

    // Update is called once per frame
    void Update()
    {
        lock (m_moveTranvel)
        {
            if (m_moveTranvel.Count > 0)
            {
                var moveTranvelElem = m_moveTranvel.Dequeue();
                this.transform.rotation = Quaternion.AngleAxis(moveTranvelElem.Rotation, Vector3.up);
                var moveDirect = moveTranvelElem.Position - this.transform.position;
                this.transform.Translate(moveDirect, Space.World);

                m_animator.SetFloat("Direction", 1.0f, 0.0f, Time.deltaTime);
                m_animator.SetInteger("ForwardOrBack", 1);
            }
            else
            {
                m_animator.SetInteger("ForwardOrBack", 0);
            }
        }
    }
}
