using MessagePack;

namespace LLKFrame
{
    [MessagePackObject]
    public class ArchiveItem
    {
        [Key(0)] public int saveID;              // 存档ID
        [Key(1)] public float lastModifyTime;    // 上次修改时间
        public ArchiveItem() { }
        public ArchiveItem(int saveID, float lastModifyTime)
        {
            this.saveID = saveID;
            this.lastModifyTime = lastModifyTime;
        }
        public void UpdateModifyTime(float modifyTime) => lastModifyTime = modifyTime;
    }
}
