using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUIEventHandler : MonoBehaviour
{
    private Dropdown m_dropDown;
    public GameObject m_register;
    public GameObject m_login;
    private Button m_loginButton = null;
    private InputField m_loginUserName = null;
    private InputField m_loginPassword = null;
    private Button m_registerButton = null;

    private InputField m_registerUserName = null;
    private InputField m_registerPassword = null;
    private InputField m_registerPasswordAgain = null;
    public GameObject m_gameUI;
    public Text m_hint;

    private INetworkService m_networkService = RemoteNetworkServce.Instance;

    private void LoginResultCallBack(NetworkCommand cmd)
    {
        var cmdResult = cmd as LoginResultCommand;
        Debug.Log($"login success, user id : {cmdResult.Result} {cmdResult.UserID}");
        if (cmdResult.Result)
        {
            PlayerInfo.LocalPlayer.UserID = cmdResult.UserID;

            m_loginPassword.text = "";
            m_loginUserName.text = "";

            SceneManager.LoadScene("MainScene");
        }
        else
        {
            m_hint.text = "Wrong username or password !";
        }

        m_loginButton.interactable = true;
    }

    private void RegisterResultCallBack(NetworkCommand cmd)
    {
        var cmdResult = cmd as RegisterResultCommand;
        if (cmdResult.Result)
        {
            m_registerPasswordAgain.text = "";
            m_registerPassword.text = "";
            m_registerUserName.text = "";

            m_hint.text = "Register success !";
            m_dropDown.value = 0;
        }
        else
        {
            m_hint.text = "Register faild !";
        }
        m_loginButton.interactable = true;
    }

    void Start()
    {
        m_networkService.Register(NetWorkCommandType.LogInResult, this.LoginResultCallBack);
        m_networkService.Register(NetWorkCommandType.RegisterResult, this.RegisterResultCallBack);

        // login ui
        m_login = GameObject.Find("LoginUI/LoginUI");
        m_register = GameObject.Find("LoginUI/RegisterUI");

        m_loginButton = GameObject.Find("LoginUI/LoginUI/LoginButton").GetComponent<Button>();
        m_loginUserName = GameObject.Find("LoginUI/LoginUI/UserNameInput").GetComponent<InputField>();
        m_loginPassword = GameObject.Find("LoginUI/LoginUI/PasswordInput").GetComponent<InputField>();

        m_loginButton.onClick.AddListener(() =>
        {
            var username = m_loginUserName.text;
            var password = m_loginPassword.text;

            if (username.Trim() == "" || password == "")
            {
                m_hint.text = "User name or password is empty !";
                return;
            }

            m_networkService.Send(new LoginRequestCommand(username.Trim(), password));
            m_loginButton.interactable = false;
        });

        // register ui
        m_registerButton = GameObject.Find("LoginUI/RegisterUI/RegisterButton").GetComponent<Button>();
        m_registerPassword = GameObject.Find("LoginUI/RegisterUI/PasswordInput").GetComponent<InputField>();
        m_registerPasswordAgain = GameObject.Find("LoginUI/RegisterUI/AgainPasswordInput").GetComponent<InputField>();
        m_registerUserName = GameObject.Find("LoginUI/RegisterUI/UserNameInput").GetComponent<InputField>();

        m_registerButton.onClick.AddListener(() =>
        {
            var username = m_registerUserName.text;
            var password = m_registerPassword.text;
            var passwordAgain = m_registerPasswordAgain.text;

            if (username.Trim() == "" || password == "" || passwordAgain == "")
            {
                m_hint.text = "Empty user name or empty password !";
                return;
            }

            if (password != passwordAgain)
            {
                m_hint.text = "Password is not same !";
                return;
            }

            m_loginButton.interactable = false;
            m_networkService.Send(new RegisterCommand(username.Trim(), password));
        });

        // drop down
        m_dropDown = GameObject.Find("LoginUI/Dropdown").GetComponent<Dropdown>();
        m_dropDown.onValueChanged.AddListener(value =>
        {
            if (value == 0)
            {
                m_login.gameObject.SetActive(true);
                m_register.gameObject.SetActive(false);
            }
            else
            {
                m_login.gameObject.SetActive(false);
                m_register.gameObject.SetActive(true);
            }
        });

        m_register.SetActive(false);
    }

    void Update()
    {
    }
}