using LLKFrame;

public class BeginSceneManager : SingletonMonoBase<BeginSceneManager>
{
    private void Start()
    {
        // 显示窗口
        UISystem.Show<UI_BeginWindow>();
        // 初始化音量
        InitVolumeSetting();
    }

    private void InitVolumeSetting()
    {
        AudioSystem.SetBgMusicVolume(DataManager.VolumeData.bgVolume);
        AudioSystem.SetEffMusicVolume(DataManager.VolumeData.effVolume);
        AudioSystem.SetGlobalVolume(DataManager.VolumeData.globalVolume);
    }
}
