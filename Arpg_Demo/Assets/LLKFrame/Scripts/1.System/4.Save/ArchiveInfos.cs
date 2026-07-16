using MessagePack;
using System.Collections.Generic;
using UnityEngine;

namespace LLKFrame
{
    [MessagePackObject]
    public class ArchiveInfos
    {
        [Key(0)] public Dictionary<int, ArchiveItem> archiveItemDic = new Dictionary<int, ArchiveItem>();    // 存档列表
        [Key(1)] public int createSaveID;    // 当前用于创建存档的ID

        public ArchiveItem CreateArchiveItem()
        {
            ArchiveItem saveItem = new ArchiveItem(createSaveID, Time.time);
            archiveItemDic.Add(createSaveID, saveItem);
            createSaveID++;
            return saveItem;
        }
        public void DeleteArchiveItem(int saveID) => archiveItemDic.Remove(saveID);
        public void CleanAllArchive()
        {
            archiveItemDic.Clear();
            createSaveID = 0;
        }
    }
}
