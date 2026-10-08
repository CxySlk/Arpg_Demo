using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

public class ChooseCharacterSceneManager : SingletonMonoBase<ChooseCharacterSceneManager>
{
    [LabelText("角色模型纹理"), SerializeField] private RenderTexture _modelRT;

    private void Start()
    {
        // 显示创建角色窗口
        UISystem.Show<UI_ChooseCharacterWindow>().SetModelImage(_modelRT);
    }
}