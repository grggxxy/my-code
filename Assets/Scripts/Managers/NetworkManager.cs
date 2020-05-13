using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class NetworkManager : MonoBehaviour
{
    public INetworkService m_networkService = RemoteNetworkServce.Instance;
    public GameObject m_playerObject;
    public GameObject m_playerSyncObject;
    public GameObject m_bulletBigSyncObject;
    public GameObject m_shootingEnemyObject;
    public GameObject m_shootingEnemySyncObject;
    public GameObject m_meleeEnemyObject;
    public GameObject m_meleeEnmeySyncObject;
    public GameObject m_selfConstructEnemyObject;
    public GameObject m_selfConstructEnmeySyncObject;
    public GameObject m_itemObject;
    public GameObject m_trapDamage;
    public GameObject m_trapSlowDown;
    public GameObject m_tank;
    private GameObject m_hidePlayer;

    public bool m_cursorState = false;
    public bool m_gameEnd = false;

    public float m_newWaveTimeCount = 0;


    // Start is called before the first frame update
    void Start()
    {
        // await m_networkService.Connect();

        m_networkService.Register(NetWorkCommandType.UpdateGold, cmd =>
        {
            var cmdGold = cmd as UpdateGoldCommand;
            PlayerInfo.LocalPlayer.Gold = cmdGold.Gold;

            MessageDispacher.Instance.Send(UIMessage.UpdateGold, PlayerInfo.LocalPlayer.Gold);
        });

        m_networkService.Register(NetWorkCommandType.InitPlayerInfo, cmd =>
        {
            var cmdInit = cmd as InitPlayerInfoCommand;
            PlayerInfo.LocalPlayer.Type = cmdInit.Type;
            PlayerInfo.LocalPlayer.CurrentBulletCapacity = cmdInit.CurrentBullet;
            PlayerInfo.LocalPlayer.MaxBulletCapacity = cmdInit.MaxBullet;

            PlayerInfo.LocalPlayer.ShootingType = cmdInit.ShoottingType;
            PlayerInfo.LocalPlayer.Kills = cmdInit.Kills;

            PlayerInfo.LocalPlayer.Gold = cmdInit.Gold;
            PlayerInfo.LocalPlayer.DamageTrapCount = cmdInit.DamageTrapCount;
            PlayerInfo.LocalPlayer.SlowTrapCount = cmdInit.SlowTrapCount;

            PlayerInfo.LocalPlayer.GrenadeCount = cmdInit.GrenadeCount;

            // update ui
            MessageDispacher.Instance.Send(UIMessage.UpdateGrenate, PlayerInfo.LocalPlayer.GrenadeCount);

            MessageDispacher.Instance.Send(UIMessage.UpdateTrapCount, new TrapCountUpdateMessage
            {
                DamageTrapCount = PlayerInfo.LocalPlayer.DamageTrapCount,
                SlowTrapCount = PlayerInfo.LocalPlayer.SlowTrapCount
            });

            MessageDispacher.Instance.Send(UIMessage.UpdateGold, PlayerInfo.LocalPlayer.Gold);

            MessageDispacher.Instance.Send(UIMessage.UpdateBulletUI, new BulletUpdateMessage
            {
                CurrentBulletCount = cmdInit.CurrentBullet,
                MaxBulletCount = cmdInit.MaxBullet,
            });

            PlayerInfo.LocalPlayer.Hp = cmdInit.Hp;
            MessageDispacher.Instance.Send(UIMessage.UpdateHpUI, cmdInit.Hp / 1000.0f);
            MessageDispacher.Instance.Send(UIMessage.UpdateKills, cmdInit.Kills);

            PlayerInfo.AddPlayer(PlayerInfo.LocalPlayer);

            m_playerObject.transform.position = cmdInit.InitialPosition;
            var rotate = Quaternion.AngleAxis(cmdInit.InitialRotation, Vector3.up);
            m_playerObject.transform.rotation = rotate;

            PlayerInfo.IsHost = cmdInit.IsHost;

            if (!PlayerInfo.IsHost)
            {
                Debug.Log("current player is not host.");
                m_networkService.Send(new PlayerListCommand(PlayerInfo.LocalPlayer.UserID));

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Debug.Log("current player is host.");
                m_networkService.Send(new RequireTankInfoCommand(
                    PlayerInfo.LocalPlayer.UserID
                ));
            }
        });

        m_networkService.Register(NetWorkCommandType.RequireTankInfoResult, cmd =>
        {
            var cmdResult = cmd as RequireTankInfoResultCommand;

            TankInfo.Instance.DriverID = cmdResult.DriverID;
            TankInfo.Instance.Hp = cmdResult.Hp;
            TankInfo.Instance.IsDriven = cmdResult.IsDriven;

            var tank = GameObject.Instantiate(m_tank,
                cmdResult.Position,
                Quaternion.AngleAxis(0, Vector3.up)
            );

            var scriptComp = tank.GetComponent<TankController>();
            scriptComp.SetTurretRotation(cmdResult.TurretRotation);
            scriptComp.SetBodyRotation(cmdResult.BodyRotation);

            m_networkService.Send(new RequireStrongPointInfoCommand(PlayerInfo.LocalPlayer.UserID));
        });

        m_networkService.Register(NetWorkCommandType.RequireStrongPointInfoResult, cmd =>
        {
            var cmdResult = cmd as RequireStrongPointInfoResultCommand;

            StrongPoint.Instance.Hp = cmdResult.Hp;

            // update ui
            MessageDispacher.Instance.Send(UIMessage.UpdateStrongPointHpSlider, (float)(StrongPoint.Instance.Hp / 1000.0f));

            MessageDispacher.Instance.Send(AudioMessage.PlayFightingBgm, null);
            m_playerObject.SetActive(true);
        });

        m_networkService.Register(NetWorkCommandType.PlayerListResult, cmd =>
        {
            var cmdPlayerListResult = cmd as PlayerListResultCommand;
            for (int i = 0; i < cmdPlayerListResult.PlayerInfo.Count; ++i)
            {
                var initPos = cmdPlayerListResult.PlayerPositions[i];
                var initRot = cmdPlayerListResult.PlayerRotations[i];

                var syncObj = GameObject.Instantiate(this.m_playerSyncObject, initPos, Quaternion.AngleAxis(initRot, Vector3.up));
                var scriptComp = syncObj.GetComponent<PlayerControllerSync>();
                scriptComp.PlayerInfo = cmdPlayerListResult.PlayerInfo[i];

                PlayerInfo.AddPlayer(scriptComp.PlayerInfo);
            }

            Debug.Log($"Sync {cmdPlayerListResult.PlayerInfo.Count} players.");

            m_networkService.Send(new EnemyListCommand(PlayerInfo.LocalPlayer.UserID));
        });

        m_networkService.Register(NetWorkCommandType.EnemyListResult, cmd =>
        {
            var cmdEnemyListResult = cmd as EnemyListResultCommand;

            Debug.Log($"Sync {cmdEnemyListResult.EnemyIDs.Count} enemies.");

            _InstantiateSyncEnemies(
                cmdEnemyListResult.EnemyTypes,
                cmdEnemyListResult.EnemyIDs,
                cmdEnemyListResult.EnemyPositions,
                cmdEnemyListResult.EnemyRotations,
                cmdEnemyListResult.Hps,
                1.0f);

            // m_playerObject.SetActive(true);
            m_networkService.Send(new ItemListCommand(PlayerInfo.LocalPlayer.UserID));
        });

        m_networkService.Register(NetWorkCommandType.ItemListResult, cmd =>
        {
            var cmdItemListResult = cmd as ItemListResultCommand;

            Debug.Log($"Sync {cmdItemListResult.ItemIDs.Count} items.");

            for (int i = 0; i < cmdItemListResult.ItemIDs.Count; ++i)
            {
                var itemObj = GameObject.Instantiate(
                    m_itemObject, cmdItemListResult.ItemPositions[i], Quaternion.AngleAxis(0, Vector3.up));

                var scriptComp = itemObj.gameObject.transform.GetChild(0).GetComponent<ItemController>();
                scriptComp.ItemInfo = new ItemInfo()
                {
                    ID = cmdItemListResult.ItemIDs[i],
                    Type = cmdItemListResult.ItemTypes[i],
                };
            }

            m_networkService.Send(new TrapListCommand(PlayerInfo.LocalPlayer.UserID));

        });

        m_networkService.Register(NetWorkCommandType.TrapListResult, cmd =>
        {
            var cmdTrapListResult = cmd as TrapListResultCommand;

            Debug.Log($"Sync {cmdTrapListResult.TrapPositions.Count} traps.");

            for (int i = 0; i < cmdTrapListResult.TrapList.Count; ++i)
            {
                var trapInfo = cmdTrapListResult.TrapList[i];
                GameObject trapObject = null;
                if (trapInfo.TrapType == TrapType.Damage)
                {
                    trapObject = m_trapDamage;
                }
                else if (trapInfo.TrapType == TrapType.SlowDown)
                {
                    trapObject = m_trapSlowDown;
                }

                var itemObj = GameObject.Instantiate(
                    trapObject, cmdTrapListResult.TrapPositions[i], Quaternion.AngleAxis(cmdTrapListResult.TrapRotations[i], Vector3.up));
                var scriptComp = itemObj.GetComponent<TrapController>();
                scriptComp.TrapInfo = trapInfo;
                scriptComp.IsHandTrap = false;

            }
            m_networkService.Send(new RequireTankInfoCommand(PlayerInfo.LocalPlayer.UserID));
        });

        m_networkService.Register(NetWorkCommandType.NewPlayer, cmd =>
        {
            var cmdNewPlayer = cmd as NewPlayerCommand;
            var initPos = cmdNewPlayer.InitialPosition;
            var initRot = cmdNewPlayer.InitialRotation;

            Debug.Log($"New player join the game.");
            var syncObj = GameObject.Instantiate(this.m_playerSyncObject, initPos, Quaternion.AngleAxis(initRot, new Vector3(0.0f, 1.0f, 0.0f)));
            var scriptComp = syncObj.GetComponent<PlayerControllerSync>();
            scriptComp.PlayerInfo = new PlayerInfo()
            {
                CurrentBulletCapacity = cmdNewPlayer.CurrentBullet,
                MaxBulletCapacity = cmdNewPlayer.MaxBullet,
                Type = cmdNewPlayer.Type,
                UserID = cmdNewPlayer.UserID,
                Hp = cmdNewPlayer.Hp,
                ShootingType = cmdNewPlayer.ShoottingType,
            };

            PlayerInfo.AddPlayer(scriptComp.PlayerInfo);
        });

        m_networkService.Register(NetWorkCommandType.GenerateEnemy, cmd =>
        {
            var cmdGen = cmd as GenerateEnemyCommand;
            Debug.Log($"{cmdGen.EnemyIDs.Count} enemies generated");

            if (cmdGen.IsHost)
            {
                _InstantiateEnemies(cmdGen);
            }
            else
            {
                _InstantiateSyncEnemies(cmdGen.EnemyTypes, cmdGen.EnemyIDs,
                    cmdGen.EnemyPositions, cmdGen.EnemyRotations, cmdGen.Hps, 360.0f);
            }
        });

        m_networkService.Register(NetWorkCommandType.GenerateItem, cmd =>
        {
            var cmdItem = cmd as GenerateItemCommand;
            var insObj = GameObject.Instantiate(m_itemObject, cmdItem.ItemPosition, Quaternion.AngleAxis(0, Vector3.up));
            var scriptComp = insObj.gameObject.transform.GetChild(0).GetComponent<ItemController>();
            scriptComp.ItemInfo = new ItemInfo()
            {
                ID = cmdItem.ItemID,
                Type = cmdItem.ItemType,
            };
        });

        m_networkService.Register(NetWorkCommandType.UpdatePlayerKillCount, cmd =>
        {
            var cmdUpdate = cmd as UpdatePlayerKillCountCommand;

            PlayerInfo.LocalPlayer.Kills = cmdUpdate.Kills;
            MessageDispacher.Instance.Send(UIMessage.UpdateKills, cmdUpdate.Kills);
        });

        m_networkService.Register(NetWorkCommandType.LeaveResult, async cmd =>
        {
            m_networkService.Clear();
            MessageDispacher.Instance.Clear();
            PlayerInfo.Clear();
            await m_networkService.DisConnect();
            SceneManager.LoadScene("OpenScene");
        });

        m_networkService.Register(NetWorkCommandType.PutTrapResult, cmd =>
        {
            var cmdPutTrapResult = cmd as PutTrapResultCommand;
            if (cmdPutTrapResult.BuilderID != PlayerInfo.LocalPlayer.UserID)
            {
                GameObject trapObject = null;
                if (cmdPutTrapResult.TrapType == TrapType.Damage)
                {
                    trapObject = m_trapDamage;
                }
                else
                {
                    trapObject = m_trapSlowDown;
                }

                var newTrap = GameObject.Instantiate(trapObject,
                    cmdPutTrapResult.Position,
                    Quaternion.AngleAxis(cmdPutTrapResult.Rotation, Vector3.up));
                var scriptComp = newTrap.GetComponent<TrapController>();
                scriptComp.IsHandTrap = false;
                scriptComp.TrapInfo = new TrapInfo()
                {
                    TrapType = cmdPutTrapResult.TrapType,
                    ID = cmdPutTrapResult.TrapID,
                };
            }
        });

        m_networkService.Register(NetWorkCommandType.GameResult, cmd =>
        {
            var cmdResult = cmd as GameResultCommand;
            // m_cursorState = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            m_gameEnd = true;
            MessageDispacher.Instance.Send(GameObjectMessage.SwitchCursorState, m_cursorState);
            MessageDispacher.Instance.Send(UIMessage.ShowGameResult, cmdResult.IsWin);
        });

        m_networkService.Register(NetWorkCommandType.WaveOver, cmd =>
        {
            m_newWaveTimeCount = 10.0f;
            MessageDispacher.Instance.Send(UIMessage.ShowTimeCountDown, null);
        });

        m_networkService.Register(NetWorkCommandType.NewGameResult, cmd =>
        {
            var cmdNewGameResult = cmd as NewGameResultCommand;

            m_gameEnd = false;
            // m_cursorState = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            MessageDispacher.Instance.Send(GameObjectMessage.SwitchCursorState, m_cursorState);
            MessageDispacher.Instance.Send(UIMessage.HideGameResult, null);

            TankInfo.Instance.Hp = cmdNewGameResult.TankHp;
            // TankInfo.Instance.DriverID = 0xff;
            // TankInfo.Instance.IsDriven = false;

            var tank = GameObject.Instantiate(m_tank,
                cmdNewGameResult.TankPosition,
                Quaternion.AngleAxis(0, Vector3.up)
            );

            var scriptComp = tank.GetComponent<TankController>();
            scriptComp.SetTurretRotation(cmdNewGameResult.TankTurretRotation);
            scriptComp.SetBodyRotation(cmdNewGameResult.TankBodyRotation);

            StrongPoint.Instance.Hp = cmdNewGameResult.StrpngPointHp;

            // update ui
            MessageDispacher.Instance.Send(UIMessage.UpdateStrongPointHpSlider, (float)(StrongPoint.Instance.Hp / 1000.0f));
        });

        m_networkService.Send(new JoinCommand(PlayerInfo.LocalPlayer.UserID));

        MessageDispacher.Instance.Register(GameObjectMessage.StartGame, msg =>
        {
            if (PlayerInfo.IsHost)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                m_networkService.Send(new NewGameCommand(PlayerInfo.LocalPlayer.UserID));
            }
        });

        MessageDispacher.Instance.Register(GameObjectMessage.HidePlayer, msg =>
        {
            var gameObj = msg as GameObject;
            m_hidePlayer = gameObj;

            m_hidePlayer.SetActive(false);
        });

        MessageDispacher.Instance.Register(GameObjectMessage.ShowPlayer, msg =>
        {
            var position = (Vector3)msg;

            if (this.m_hidePlayer != null)
            {
                m_hidePlayer.SetActive(true);
                m_hidePlayer.transform.position = position;

                m_hidePlayer.GetComponent<PlayerController>().ResumeRegister();

                m_hidePlayer = null;
            }
        });

        MessageDispacher.Instance.Register(GameObjectMessage.ShowPlayerSync, msg =>
        {
            var position = (Vector3)msg;

            if (this.m_hidePlayer != null)
            {
                m_hidePlayer.SetActive(true);
                m_hidePlayer.transform.position = position;

                m_hidePlayer.GetComponent<PlayerControllerSync>().ResumeRegister();

                m_hidePlayer = null;
            }
        });

        MessageDispacher.Instance.Register(GameObjectMessage.NewGame, msg =>
        {
            if (PlayerInfo.IsHost)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                m_networkService.Send(new NewGameCommand(PlayerInfo.LocalPlayer.UserID));
            }
        });
    }

    private void _InstantiateEnemies(GenerateEnemyCommand cmdGen)
    {
        for (int i = 0; i < cmdGen.EnemyIDs.Count; ++i)
        {
            GameObject enemyObj;
            if (cmdGen.EnemyTypes[i] == EnemyType.Shooting)
            {
                enemyObj = this.m_shootingEnemyObject;
            }
            else if (cmdGen.EnemyTypes[i] == EnemyType.Melee)
            {
                enemyObj = this.m_meleeEnemyObject;
            }
            else
            {
                enemyObj = this.m_selfConstructEnemyObject;
            }

            var insEnemyObj = GameObject.Instantiate(enemyObj,
                cmdGen.EnemyPositions[i],
                Quaternion.AngleAxis(360.0f * cmdGen.EnemyRotations[i], Vector3.up)
            );
            var scriptComp = insEnemyObj.GetComponent<EnemyController>();
            scriptComp.EnemyInfo = new EnemyInfo()
            {
                Type = cmdGen.EnemyTypes[i],
                EnemyID = cmdGen.EnemyIDs[i],
                Hp = cmdGen.Hps[i],
            };
        }
    }

    private void _InstantiateSyncEnemies(
        List<EnemyType> enemyTypes,
        List<uint> enmeyIDs,
        List<Vector3> enmeyPositions,
        List<float> enemyRotations,
        List<int> enemyHps,
        float rotationFactor)
    {
        for (int i = 0; i < enmeyIDs.Count; ++i)
        {
            GameObject enemyObj;
            if (enemyTypes[i] == EnemyType.Shooting)
            {
                enemyObj = m_shootingEnemySyncObject;
            }
            else if (enemyTypes[i] == EnemyType.Melee)
            {
                enemyObj = m_meleeEnmeySyncObject;
            }
            else
            {
                enemyObj = m_selfConstructEnmeySyncObject;
            }

            var insEnemyObj = GameObject.Instantiate(enemyObj,
                enmeyPositions[i],
                Quaternion.AngleAxis(rotationFactor * enemyRotations[i], Vector3.up)
            );
            var scriptComp = insEnemyObj.GetComponent<EnemyControllerSync>();
            scriptComp.EnemyInfo = new EnemyInfo()
            {
                Type = enemyTypes[i],
                EnemyID = enmeyIDs[i],
                Hp = enemyHps[i],
            };
        }
    }

    async private void OnApplicationQuit()
    {
        m_networkService.Send(new LeaveCommand(PlayerInfo.LocalPlayer.UserID));
        await m_networkService.DisConnect();

#if UNITY_EDITOR
#else
    System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
    }

    // Update is called once per frame
    void Update()
    {
        if (m_newWaveTimeCount > 0)
        {
            m_newWaveTimeCount -= Time.deltaTime;

            if (m_newWaveTimeCount <= 0)
            {
                if (PlayerInfo.IsHost)
                {
                    m_networkService.Send(new NewWaveCommand(PlayerInfo.LocalPlayer.UserID));
                }
                MessageDispacher.Instance.Send(UIMessage.HideTimeCountDown, null);
            }
            else
            {
                MessageDispacher.Instance.Send(UIMessage.UpdateTimeCountDown, m_newWaveTimeCount);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !m_gameEnd)
        {
            // m_cursorState = !m_cursorState;
            //Cursor.lockState = CursorLockMode.Confined;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            MessageDispacher.Instance.Send(GameObjectMessage.SwitchCursorState, m_cursorState);
        }

        m_networkService.Update();
    }
}
