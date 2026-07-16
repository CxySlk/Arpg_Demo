using LLKFrame;

public class ManagerController : SingletonMonoBase<ManagerController>
{
    protected override void Awake()
    {
        DataManager.Init();
    }
}
