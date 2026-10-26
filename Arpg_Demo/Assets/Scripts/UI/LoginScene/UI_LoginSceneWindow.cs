using Data;
using LLKFrame;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIWindow(nameof(UI_LoginSceneWindow), nameof(UI_LoginSceneWindow), false, 0)]
public class UI_LoginSceneWindow : UI_WindowBase
{
    [LabelText("用户名"), SerializeField] private TMP_InputField _inputUserName;
    [LabelText("密码"), SerializeField] private TMP_InputField _inputPassword;
    [LabelText("登录按钮"), SerializeField] private Button _btnLogin;
    [LabelText("注册按钮"), SerializeField] private Button _btnRegister;
    [LabelText("自动登录"), SerializeField] private Toggle _togAutoLogin;
    [LabelText("记住密码"), SerializeField] private Toggle _togRememberPassword;
    [LabelText("显示密码"), SerializeField] private Toggle _togShowPassword;

    private static bool _isFirstLogin = true;

    protected override void OnShow()
    {
        // 按钮事件注册
        _btnLogin.onClick.AddListener(OnLoginClicked);
        _btnRegister.onClick.AddListener(OnRegisterClicked);

        // Toggle事件注册
        _togShowPassword.onValueChanged.AddListener(OnShowPasswordValueChanged);

        // UI内容初始化
        LoginData loginData = DataManager.LoginData;
        if (!string.IsNullOrEmpty(loginData.lastLoginUserName))
        {
            LoginDataItem item = loginData.loginDataDic[loginData.lastLoginUserName];
            // 获取用户名密码
            _inputUserName.text = loginData.lastLoginUserName;
            _inputPassword.text = item.rememberPwd ? item.passWord : "";
            // 获取输入设置
            _togAutoLogin.isOn = item.autoLogin;
            _togRememberPassword.isOn = item.rememberPwd;
        }

        if (_isFirstLogin && _togAutoLogin.isOn && _togRememberPassword.isOn)
        {
            OnLoginClicked();
        }

        _isFirstLogin = false;
    }

    protected override void OnClose()
    {
        _btnLogin.onClick.RemoveListener(OnLoginClicked);
        _btnRegister.onClick.RemoveListener(OnRegisterClicked);
        _togShowPassword.onValueChanged.RemoveListener(OnShowPasswordValueChanged);
    }

    #region 按钮事件

    /// <summary>
    /// 登录按钮
    /// </summary>
    private void OnLoginClicked()
    {
        if (!_isFirstLogin) AudioSystem.PlayEffectMusic("ButtonClick");
        if (!TryGetAccountInput(out string userName, out string password)) return;

        // 获取登录信息
        if (!DataManager.LoginData.TryGetLoginDataItem(userName, out LoginDataItem item))
        {
            UISystem.Show<UI_TipWindow>().ShowTip("提示", "用户不存在");
            return;
        }

        // 验证用户
        if (item.Identity(password))
        {
            // 保存登录数据
            LoginData loginData = DataManager.LoginData;
            loginData.lastLoginUserName = userName;
            item.autoLogin = _togAutoLogin.isOn;
            item.rememberPwd = _togRememberPassword.isOn;
            DataManager.SaveLoginData();
            DataManager.SetLoginData(item);
            
            // 提示登陆成功
            UI_TipWindow window = UISystem.Show<UI_TipWindow>();
            window.SetCallBack(OnLoginSuccess);
            window.ShowTip("提示", "登录成功");
        }
        else UISystem.Show<UI_TipWindow>().ShowTip("提示", "用户名或密码错误！");
    }

    /// <summary>
    /// 登录成功
    /// </summary>
    private void OnLoginSuccess()
    {
        UISystem.Show<UI_LoadingSceneWindow>();
        SceneSystem.LoadSceneAsync("BeginScene", UISystem.Close<UI_LoadingSceneWindow>);
        UISystem.Close<UI_LoginSceneWindow>();
    }

    /// <summary>
    /// 注册按钮:只注册账号(写入登录信息字典),不创建存档
    /// </summary>
    private void OnRegisterClicked()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        if (!TryGetAccountInput(out string userName, out string password)) return;

        // 用户名已存在,不允许重复注册
        if (!DataManager.LoginData.TryAddLoginData(userName, password))
        {
            UISystem.Show<UI_TipWindow>().ShowTip("提示", "该用户已存在");
            return;
        }

        DataManager.SaveLoginData();
        UI_TipWindow window = UISystem.Show<UI_TipWindow>();
        window.ShowTip("提示", "注册成功");
    }

    /// <summary>取用户名与密码输入(去掉首尾空白);任一为空时弹提示并返回 false</summary>
    private bool TryGetAccountInput(out string userName, out string password)
    {
        userName = _inputUserName.text.Trim();
        password = _inputPassword.text.Trim();
        if (userName.Length == 0 || password.Length == 0)
        {
            UISystem.Show<UI_TipWindow>().ShowTip("提示", "用户名或密码不能为空");
            return false;
        }

        return true;
    }

    #endregion

    #region Toggle事件

    /// <summary>
    /// 显示密码
    /// </summary>
    private void OnShowPasswordValueChanged(bool value)
    {
        _inputPassword.contentType = value ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        _inputPassword.ForceLabelUpdate();
    }

    #endregion
}