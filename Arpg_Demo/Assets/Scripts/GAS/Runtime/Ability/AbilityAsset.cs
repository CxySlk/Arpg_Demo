using System.Collections.Generic;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Ability 资产。承载时间轴驱动的技能系统数据：
    /// 技能总帧数 + 按帧组织的事件字典，用于编辑器制作和运行时驱动。
    /// </summary>
    [CreateAssetMenu(fileName = "Ability", menuName = "GAS/Ability")]
    public class AbilityAsset : ConfigBase
    {
        [Header("技能信息")] [LabelText("技能名称")] public string abilityName = "新技能";

        [LabelText("技能总帧数"), MinValue(1)] public int totalFrames = 60;

        [LabelText("效果字典")] public Dictionary<int, ReleaseGEMarkEvent> effectDic = new();

        [LabelText("Cue显示字典")] public Dictionary<int, List<DurationCueMarkEvent>> cueDic = new();

        [LabelText("事件字典")] public Dictionary<int, List<FrameEvent>> eventDic = new();

        [LabelText("RootMotion距离限制"), SerializeField]
        public float rootMotionDis = -1; // 小于等于0默认cue的OnRootMotion不进行距离检查

        #region 编辑器

#if UNITY_EDITOR
        /// <summary>按帧升序合并三部字典，返回全部事件（编辑器统一遍历入口）</summary>
        public List<MarkEventBase> GetAllMarkEvents()
        {
            List<MarkEventBase> result = new();
            foreach (KeyValuePair<int, ReleaseGEMarkEvent> kv in effectDic)
                if (kv.Value != null)
                    result.Add(kv.Value);
            foreach (KeyValuePair<int, List<DurationCueMarkEvent>> kv in cueDic)
                if (kv.Value != null)
                    foreach (DurationCueMarkEvent cue in kv.Value)
                        if (cue != null)
                            result.Add(cue);
            foreach (KeyValuePair<int, List<FrameEvent>> kv in eventDic)
                if (kv.Value != null)
                    foreach (FrameEvent frameEvent in kv.Value)
                        if (frameEvent != null)
                            result.Add(frameEvent);

            result.RemoveAll(m => m == null);
            result.Sort((a, b) => a.StartFrame.CompareTo(b.StartFrame));
            return result;
        }

        /// <summary>新增一个标记事件到指定帧（按类型分发到对应字典）</summary>
        public T AddMarkEvent<T>(int frame) where T : MarkEventBase, new()
        {
            T mark = new();
            mark.StartFrame = Mathf.Max(0, frame);

            if (mark is DurationCueMarkEvent cue)
            {
                if (!cueDic.TryGetValue(frame, out List<DurationCueMarkEvent> list))
                {
                    list = new List<DurationCueMarkEvent>();
                    cueDic[frame] = list;
                }

                list.Add(cue);
            }
            else if (mark is ReleaseGEMarkEvent ge)
            {
                effectDic[frame] = ge;
            }
            else if (mark is FrameEvent frameEvent)
            {
                if (!eventDic.TryGetValue(frame, out List<FrameEvent> list))
                {
                    list = new List<FrameEvent>();
                    eventDic[frame] = list;
                }

                list.Add(frameEvent);
            }

            return mark;
        }

        /// <summary>移除指定标记事件（按类型从对应字典移除）</summary>
        public void RemoveMarkEvent(MarkEventBase mark)
        {
            if (mark == null) return;

            if (mark is ReleaseGEMarkEvent)
            {
                int frame = mark.StartFrame;
                if (effectDic.TryGetValue(frame, out ReleaseGEMarkEvent ge) && ge == mark)
                    effectDic.Remove(frame);
                return;
            }

            if (mark is DurationCueMarkEvent cue)
            {
                if (cueDic.TryGetValue(mark.StartFrame, out List<DurationCueMarkEvent> list))
                {
                    list.Remove(cue);
                    if (list.Count == 0) cueDic.Remove(mark.StartFrame);
                }

                return;
            }

            if (mark is FrameEvent frameEvent)
            {
                if (eventDic.TryGetValue(mark.StartFrame, out List<FrameEvent> list))
                {
                    list.Remove(frameEvent);
                    if (list.Count == 0) eventDic.Remove(mark.StartFrame);
                }
            }
        }

        /// <summary>
        /// 移动标记事件到新帧（同步更新字典 key 与 StartFrame）。
        /// GE 目标帧已被其他 GE 占用时返回 false（禁止覆盖）。
        /// </summary>
        public bool MoveMarkEvent(MarkEventBase mark, int newFrame)
        {
            if (mark == null) return false;
            newFrame = Mathf.Max(0, newFrame);
            if (newFrame == mark.StartFrame) return true;

            if (mark is ReleaseGEMarkEvent ge)
            {
                if (effectDic.ContainsKey(newFrame)) return false;
                effectDic.Remove(mark.StartFrame);
                effectDic[newFrame] = ge;
                ge.StartFrame = newFrame;
                return true;
            }

            if (mark is DurationCueMarkEvent cue)
            {
                if (cueDic.TryGetValue(mark.StartFrame, out List<DurationCueMarkEvent> oldList))
                {
                    oldList.Remove(cue);
                    if (oldList.Count == 0) cueDic.Remove(mark.StartFrame);
                }

                if (!cueDic.TryGetValue(newFrame, out List<DurationCueMarkEvent> newList))
                {
                    newList = new List<DurationCueMarkEvent>();
                    cueDic[newFrame] = newList;
                }

                newList.Add(cue);
                cue.StartFrame = newFrame;
                return true;
            }

            if (mark is FrameEvent frameEvent)
            {
                if (eventDic.TryGetValue(mark.StartFrame, out List<FrameEvent> oldList))
                {
                    oldList.Remove(frameEvent);
                    if (oldList.Count == 0) eventDic.Remove(mark.StartFrame);
                }

                if (!eventDic.TryGetValue(newFrame, out List<FrameEvent> newList))
                {
                    newList = new List<FrameEvent>();
                    eventDic[newFrame] = newList;
                }

                newList.Add(frameEvent);
                frameEvent.StartFrame = newFrame;
                return true;
            }

            return false;
        }
#endif

        #endregion
    }
}