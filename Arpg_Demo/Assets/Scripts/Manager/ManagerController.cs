using UnityEngine;

public class ManagerController : MonoBehaviour
{
    private static ManagerController _instance;

    protected void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        InitManager();
    }

    private void InitManager()
    {
        DataManager.Init();
    }
}