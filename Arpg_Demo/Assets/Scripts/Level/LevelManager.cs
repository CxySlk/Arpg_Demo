using cfg;
using Data;
using LLKFrame;

namespace Level
{
    /// <summary>关卡管理器</summary>
    public static class LevelManager
    {
        public static int LevelID { get; private set; } = -1;

        public static void EnterLevel(int levelID)
        {
            // 加载关卡对应的场景
            LevelID = levelID;
            string sceneName = ConfigManager.Tables.TbLevelConfig.Get(levelID).SceneName;
            UISystem.Show<UI_LoadingSceneWindow>();
            SceneSystem.LoadSceneAsync(sceneName, UISystem.Close<UI_LoadingSceneWindow>);
        }

        public static void CompleteLevel()
        {
            // 获取下一个关卡
            LevelData levelData = DataManager.LoadLevelData();
            // 这个关卡如果本身就已经通关了的话，就直接返回
            if (levelData.IsUnlocked(LevelID))
                return;
            // 关卡通关，解锁下一关卡
            levelData.CompleteLevel(LevelID);
            LevelConfig config = ConfigManager.Tables.TbLevelConfig.Get(LevelID);
            // 解锁ID小于等于0不生效
            if (config.CompleteUnlockID <= 0)
                return;
            levelData.UnlockLevel(config.CompleteUnlockID);
        }

        public static bool IsUnlock(int levelID)
        {
            LevelConfig levelConfig = ConfigManager.Tables.TbLevelConfig.Get(levelID);
            if(levelConfig.DefaultUnlock)
                return true;
            LevelData levelData = DataManager.LoadLevelData();
            return levelData.IsUnlocked(levelID);
        }

        public static bool IsComplete(int levelID)
        {
            LevelData levelData = DataManager.LoadLevelData();
            return levelData.IsCompleted(levelID);
        }
    }
}