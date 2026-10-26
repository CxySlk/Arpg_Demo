using System.Collections.Generic;
using MessagePack;

namespace Data
{
    [MessagePackObject]
    public class LevelData
    {
        /// <summary>
        /// 解锁关卡id
        /// </summary>
        /// <关卡ID，是否通关>
        [Key(0)] public Dictionary<int, bool> unlockDic = new();

        public void UnlockLevel(int levelID) => unlockDic.Add(levelID, false);

        public bool IsUnlocked(int levelID) => unlockDic.ContainsKey(levelID);

        public bool IsCompleted(int levelID)
        {
            unlockDic.TryGetValue(levelID, out bool isCompleted);
            return isCompleted;
        }

        public void CompleteLevel(int levelID) => unlockDic[levelID] = true;
    }
}