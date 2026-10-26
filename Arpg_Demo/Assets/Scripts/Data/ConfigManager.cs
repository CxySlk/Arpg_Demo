using cfg;
using Luban.SimpleJSON;
using UnityEngine;

namespace Data
{
    public static class ConfigManager
    {
        public static Tables Tables { get; private set; }
        
        static ConfigManager()
        {
            Tables = new Tables(name=>JSON.Parse(Resources.Load<TextAsset>("Data/" + name).text));
        }
    }
}