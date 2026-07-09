using Sirenix.Utilities;
using System;
using System.Linq;
using UnityEngine;

namespace LLKFrame
{
    public static class LLKFrameExtension
    {
        #region 自定义特性
        public static TAttribute GetCustomAttribute<TAttribute>(this Type type) where TAttribute : Attribute
        {
            return type.GetAttribute<TAttribute>();
        }
        public static TAttribute[] GetCustomAttributes<TAttribute>(this Type type) where TAttribute : Attribute
        {
            return type.GetAttributes<TAttribute>().ToArray();
        }
        #endregion
        #region 对象池
        public static void PushObject(this object obj) => PoolSystem.PushObject(obj);
        public static void PushGameObject(this Component component) => PoolSystem.PushGameObject(component.gameObject);
        public static void PushGameObject(this GameObject obj) => PoolSystem.PushGameObject(obj);
        #endregion
    }
}
