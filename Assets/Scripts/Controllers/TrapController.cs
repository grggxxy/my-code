using UnityEngine;

public class TrapController : MonoBehaviour
{
    private int m_triggerCount = 0;
    public bool IsHandTrap { get; set; } = false;
    public TrapInfo TrapInfo { get; set; }

    public INetworkService m_networkService = RemoteNetworkServce.Instance;

    private void Start()
    {
        m_networkService.Register(NetWorkCommandType.NewGameResult, OnNewGameResult);
    }

    private void OnNewGameResult(NetworkCommand cmd)
    {
        if (this.IsHandTrap)
        {
            return;
        }
        m_networkService.Unregister(NetWorkCommandType.NewGameResult, OnNewGameResult);
        Destroy(this.gameObject);
    }

    private void Update()
    {
    }

    public bool CanSetTrap()
    {
        return m_triggerCount == 0;
    }

    public void Reset()
    {
        m_triggerCount = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsHandTrap)
        {
            if (other.CompareTag("Trap") || other.CompareTag("Untagged"))
            {
                ++m_triggerCount;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsHandTrap)
        {
            if (other.CompareTag("Trap") || other.CompareTag("Untagged"))
            {
                --m_triggerCount;
            }
        }
    }
}