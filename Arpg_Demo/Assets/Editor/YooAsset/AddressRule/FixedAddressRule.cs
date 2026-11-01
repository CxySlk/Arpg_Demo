using UnityEngine;
using YooAsset.Editor;

namespace Editor.YooAsset.AddressRule
{
    public class FixedAddressRule : IAddressRule
    {
        public string GetAssetAddress(AddressRuleData data)
        {
            if (string.IsNullOrEmpty(data.UserData))
            {
                Debug.Log(data+"寻址地址UserData为空");
                return null;
            }
            return data.UserData;
        }
    }
}