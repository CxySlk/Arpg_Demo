using LLKFrame;
using UnityEngine;

public class ChooseCharacterSceneManager : MonoBehaviour
{
    private void Start()
    {
        // 显示创建角色窗口
        UISystem.Show<UI_ChooseCharacterWindow>();
    }
}
