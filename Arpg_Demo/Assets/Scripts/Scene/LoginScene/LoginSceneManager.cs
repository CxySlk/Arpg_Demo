using LLKFrame;

public class LoginSceneManager : SingletonMonoBase<LoginSceneManager>
{
    private void Start()
    {
        DataManager.ApplyVolumeSettings();
        UISystem.Show<UI_LoginSceneWindow>();
    }
}