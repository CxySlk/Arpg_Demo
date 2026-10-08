using System;
using System.Collections.Generic;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Input
{
    /// <summary>
    /// 输入控制器：挂在角色/物体上，集中配置一组输入节点（键盘/鼠标）并驱动它们。
    /// </summary>
    /// <remarks>
    /// 每个 <see cref="InputNodeBase"/> 节点代表一个按键输入源（触发回调/输入缓存由节点内部管理）。
    /// 在 Inspector 中通过 Odin 列表配置具体节点；需要在外部逐帧调用各节点的 <see cref="InputNodeBase.OnUpdate"/> 驱动检测。
    /// </remarks>
    public class InputController : SerializedMonoBehaviour
    {
        [BoxGroup("按键配置")]
        [LabelText("输入节点列表")]
        [ListDrawerSettings(ShowItemCount = true, AddCopiesLastElement = true, HideAddButton = false)]
        public List<InputNodeBase> buttonList = new();

        /// <summary>
        /// 开启输入检测
        /// </summary>
        public void StartInputCheck()
        {
            foreach (InputNodeBase inputNode in buttonList)
                inputNode.CanInput = true;
        }

        /// <summary>
        /// 关闭输入检测
        /// </summary>
        public void StopInputCheck()
        {
            foreach (InputNodeBase inputNode in buttonList)
                inputNode.CanInput = false;
        }

        /// <summary>
        /// 设置某个节点的状态
        /// </summary>
        /// <param name="index"></param>
        /// <param name="canInput"></param>
        public void SetInputCheckFlag(int index, bool canInput)
        {
            buttonList[index].CanInput = canInput;
        }

        private void Update()
        {
            foreach (InputNodeBase inputNode in buttonList)
            {
                inputNode.OnUpdate();
            }
        }
    }
}