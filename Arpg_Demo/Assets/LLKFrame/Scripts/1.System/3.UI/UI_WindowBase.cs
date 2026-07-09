using UnityEngine;

namespace LLKFrame
{
    /// <summary>
    /// UI 窗口抽象基类，所有具体窗口脚本都应继承此类
    /// </summary>
    public abstract class UI_WindowBase : MonoBehaviour
    {
        /// <summary>当前所在层级索引</summary>
        public int layer { get; private set; }

        /// <summary>
        /// 窗口初始化，首次实例化时由 UISystem.Show 调用
        /// </summary>
        public virtual void Init() { }

        /// <summary>
        /// 显示窗口
        /// </summary>
        /// <param name=""layer"">目标层级</param>
        public virtual void Show(int layer)
        {
            this.layer = layer;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public virtual void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
