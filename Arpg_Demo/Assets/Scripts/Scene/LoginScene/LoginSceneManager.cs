using LLKFrame;

public class LoginSceneManager : SingletonMonoBase<LoginSceneManager>
{
    private void Start()
    {
        UISystem.Show<UI_LoginSceneWindow>();
    }
}