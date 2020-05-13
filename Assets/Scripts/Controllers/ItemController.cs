using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemController : MonoBehaviour
{
    public ItemInfo ItemInfo { get; set; }

    ParticleSystem m_particalSystem;

    INetworkService m_networkService = RemoteNetworkServce.Instance;
    // Start is called before the first frame update
    void Start()
    {
        m_networkService.Register(NetWorkCommandType.PickUpItemResult, OnPickUpItemResult);
        m_networkService.Register(NetWorkCommandType.NewGameResult, OnNewGameResult);

        m_particalSystem = this.gameObject.transform.parent.GetChild(1).gameObject.GetComponent<ParticleSystem>();

        var main = m_particalSystem.main;
        if (this.ItemInfo.Type == ItemType.Recover)
        {
            main.startColor = new Color(254 / 255.0f, 67 / 255.0f, 101 / 255.0f);
        }
        else if (this.ItemInfo.Type == ItemType.SupplyBullets)
        {
            main.startColor = new Color(252 / 255.0f, 157 / 255.0f, 154 / 255.0f);
        }
        // else if (this.ItemInfo.Type == ItemType.FanShooting)
        // {
        //     main.startColor = new Color(249 / 255.0f, 205 / 255.0f, 173 / 255.0f);
        // }
        else if (this.ItemInfo.Type == ItemType.RunningShooting)
        {
            main.startColor = new Color(131 / 255.0f, 175 / 255.0f, 155 / 255.0f);
        }
    }

    private void OnNewGameResult(NetworkCommand cmd)
    {
        DestroyThis();
        Destroy(this.transform.parent.gameObject);
    }

    void OnPickUpItemResult(NetworkCommand cmd)
    {
        var cmdPickUp = cmd as PickUpItemResultCommand;
        if (cmdPickUp.ItemID == this.ItemInfo.ID)
        {
            DestroyThis();
            Destroy(this.gameObject.transform.parent.gameObject);
        }
    }

    private void DestroyThis()
    {
        m_networkService.Unregister(NetWorkCommandType.PickUpItemResult, OnPickUpItemResult);
        m_networkService.Unregister(NetWorkCommandType.NewGameResult, OnNewGameResult);
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            m_networkService.Send(new PickUpItemCommand(PlayerInfo.LocalPlayer.UserID, this.ItemInfo.ID));
        }
    }
}
