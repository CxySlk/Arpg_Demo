using LLKFrame;
using UnityEngine;

public class BeginSceneManager : SingletonMonoBase<BeginSceneManager>
{
    private void Start()
    {
        // 加载存档
        DataManager.LoadArchive();
        // 显示窗口
        UISystem.Show<UI_BeginWindow>();
        // 初始化音量
        InitVolumeSetting();
    }

    private void InitVolumeSetting()
    {
        DataManager.ApplyVolumeSettings();
    }
}