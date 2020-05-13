using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasEventHandler : MonoBehaviour
{
    private INetworkService m_networkService = RemoteNetworkServce.Instance;
    // Start is called before the first frame update

    private GameObject m_rebornCountDown;
    private Slider m_rebornCountDownSlider;
    private GameObject m_gameResult;
    private GameObject m_gameResultButton;
    private Text m_gameResultText;

    private Text m_goldText;
    private Text m_trapText;
    private Text m_grenateText;

    private GameObject m_timeCountDown;
    private Text m_timeCountDownText;

    private GameObject m_startButtonObj;
    private Button m_startButton;

    void Start()
    {
        var button = GameObject.Find("UI/Leave");
        var btn = button.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            m_networkService.Send(new LeaveCommand(PlayerInfo.LocalPlayer.UserID));
            btn.interactable = false;
        });

        m_networkService.Register(NetWorkCommandType.LeaveResult, cmd =>
        {
            btn.interactable = true;
        });

        m_rebornCountDown = GameObject.Find("UI/RebornCountDown");
        m_rebornCountDownSlider = GameObject.Find("UI/RebornCountDown/RebornCountDownSlider").GetComponent<Slider>();
        m_rebornCountDown.SetActive(false);

        m_gameResult = GameObject.Find("UI/GameResult");
        m_gameResultButton = GameObject.Find("UI/GameResult/GameResultButton");
        m_gameResultText = GameObject.Find("UI/GameResult/GameResultText").GetComponent<Text>();
        m_gameResultButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (PlayerInfo.IsHost)
            {
                MessageDispacher.Instance.Send(GameObjectMessage.NewGame, null);
                m_gameResultButton.GetComponent<Button>().interactable = false;
            }
        });

        m_gameResult.SetActive(false);

        m_timeCountDown = GameObject.Find("UI/TimeCountDown");
        m_timeCountDownText = GameObject.Find("UI/TimeCountDown").GetComponent<Text>();
        m_timeCountDown.SetActive(false);

        m_goldText = GameObject.Find("UI/GoldText").GetComponent<Text>();
        m_trapText = GameObject.Find("UI/TrapText").GetComponent<Text>();
        m_grenateText = GameObject.Find("UI/GrenadeText").GetComponent<Text>();

        m_startButtonObj = GameObject.Find("UI/Start");
        m_startButton = m_startButtonObj.GetComponent<Button>();

        m_startButton.onClick.AddListener(() =>
        {
            MessageDispacher.Instance.Send(GameObjectMessage.StartGame, null);
            m_startButton.interactable = false;
        });

        MessageDispacher.Instance.Register(UIMessage.HideGameResult, msg =>
        {
            m_startButtonObj.SetActive(false);
        });

        MessageDispacher.Instance.Register(UIMessage.UpdateRebornProgress, msg =>
        {
            m_rebornCountDownSlider.value = (float)msg;
        });

        MessageDispacher.Instance.Register(UIMessage.ShowRebornProgress, msg =>
        {
            m_rebornCountDown.SetActive(true);
        });

        MessageDispacher.Instance.Register(UIMessage.HideRebornProgress, msg =>
        {
            m_rebornCountDown.SetActive(false);
        });

        MessageDispacher.Instance.Register(UIMessage.ShowGameResult, msg =>
        {
            var gameResult = (bool)msg;
            if (!PlayerInfo.IsHost)
            {
                m_gameResultButton.SetActive(false);
            }
            else
            {
                m_gameResultButton.GetComponent<Button>().interactable = true;
            }

            if (gameResult)
            {
                m_gameResultText.text = "You Win!";
            }
            else
            {
                m_gameResultText.text = "You Lose!";
            }

            m_gameResult.SetActive(true);
        });

        MessageDispacher.Instance.Register(UIMessage.HideGameResult, msg =>
        {
            m_gameResult.SetActive(false);
        });

        MessageDispacher.Instance.Register(UIMessage.ShowTimeCountDown, msg =>
        {
            m_timeCountDown.SetActive(true);
        });

        MessageDispacher.Instance.Register(UIMessage.HideTimeCountDown, msg =>
        {
            m_timeCountDown.SetActive(false);
        });

        MessageDispacher.Instance.Register(UIMessage.UpdateTimeCountDown, msg =>
        {
            var time = (int)(float)msg;
            m_timeCountDownText.text = $"Next Wave After {time} seconds ...";
        });

        MessageDispacher.Instance.Register(UIMessage.UpdateGold, msg =>
        {
            var gold = (int)msg;
            m_goldText.text = $"Gold: {gold}";
        });

        MessageDispacher.Instance.Register(UIMessage.UpdateTrapCount, msg =>
        {
            var trapCount = (TrapCountUpdateMessage)msg;

            m_trapText.text = $"Damage: {trapCount.DamageTrapCount}\nSlow: {trapCount.SlowTrapCount}";
        });

        MessageDispacher.Instance.Register(UIMessage.UpdateGrenate, msg =>
        {
            var grenateCount = (byte)msg;

            m_grenateText.text = $"Grenate: {grenateCount}";
        });
    }

    // Update is called once per frame
    void Update()
    {
    }
}
