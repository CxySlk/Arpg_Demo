using System.Collections.Generic;
using MessagePack;

namespace Data
{
    [MessagePackObject]
    public class LoginData
    {
        [Key(0)]
        public Dictionary<string, LoginDataItem> loginDataDic = new();
        [Key(1)]
        public string lastLoginUserName;

        public bool TryGetLoginDataItem(string loginName, out LoginDataItem loginDataItem)
        {
            return loginDataDic.TryGetValue(loginName, out loginDataItem);
        }

        /// <summary>注册账号(仅写入登录信息字典);用户名已存在时返回 false</summary>
        public bool TryAddLoginData(string userName, string passWord)
        {
            if (loginDataDic.ContainsKey(userName)) return false;
            loginDataDic.Add(userName, new LoginDataItem { userName = userName, passWord = passWord });
            return true;
        }
    }
}