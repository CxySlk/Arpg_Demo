using LLKFrame;

public class SelectLevelSceneMgr : SingletonMonoBase<SelectLevelSceneMgr>
{
    private void Start()
    {
        UISystem.Show<UI_LevelSelectWindow>();
    }
}
