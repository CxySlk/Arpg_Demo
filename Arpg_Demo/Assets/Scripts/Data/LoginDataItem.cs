using MessagePack;

namespace Data
{
    [MessagePackObject]
    public class LoginDataItem
    {
        /// <summary>
        /// 存档的Key，相当于SaveSystem的SaveID。
        /// 每个账号单独一个文件夹存放数据内容
        /// </summary>
        [Key(0)] public int saveKey = -1;
        [Key(1)] public string userName;
        [Key(2)] public string passWord;
        [Key(3)] public bool autoLogin = false;
        [Key(4)] public bool rememberPwd = true;

        public bool Identity(string password)
        {
            return passWord == password;
        }
    }
}