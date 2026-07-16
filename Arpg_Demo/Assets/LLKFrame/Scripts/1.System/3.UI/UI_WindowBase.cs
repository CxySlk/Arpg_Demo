using Sirenix.OdinInspector;

namespace LLKFrame
{
    /// <summary>
    /// UI 窗口抽象基类，所有具体窗口脚本都应继承此类
    /// </summary>
    public abstract class UI_WindowBase : SerializedMonoBehaviour
    {
        //private string 
        /// <summary>当前所在层级索引</summary>
        public int layer { get; private set; }

        /// <summary>窗口初始化，`首次实例化时由 UISystem.Show 调用</summary>
        public virtual void Init() { }

        /// <summary>显示窗口</summary>
        public virtual void Show(int layer)
        {
            this.layer = layer;
            gameObject.SetActive(true);
            OnShow();
        }

        public virtual void OnShow() { }


        /// <summary>关闭窗口</summary>
        public virtual void Close()
        {
            OnClose();
            gameObject.SetActive(false);
        }

        /// <summary>窗口关闭时调用，子类在此清理资源（取消事件订阅等）</summary>
        protected virtual void OnClose() { }
    }
}
