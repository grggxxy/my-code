using UnityEngine;

public class StrongPointController : MonoBehaviour
{
    public INetworkService m_networkService = RemoteNetworkServce.Instance;

    private void Start()
    {
        m_networkService.Register(NetWorkCommandType.StrongPointAttackedResult, cmd =>
        {
            var cmdResult = cmd as StrongPointAttackedResultCommand;

            var damage = StrongPoint.Instance.Hp - cmdResult.Hp;
            StrongPoint.Instance.Hp = cmdResult.Hp;

            // update ui
            MessageDispacher.Instance.Send(UIMessage.StrongPointDamaged, damage);
            MessageDispacher.Instance.Send(UIMessage.UpdateStrongPointHpSlider, (float)(StrongPoint.Instance.Hp / 1000.0f));
        });
    }

    private void Update()
    {
    }
}