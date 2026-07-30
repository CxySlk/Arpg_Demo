using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// GAS 宿主 Component（单例），持有全局 Timer 和 System 实例。
    /// 挂载在场景中的持久化 GameObject 上，驱动整个 GAS 的 Update 循环。
    /// </summary>
    public class GASHost : MonoBehaviour
    {
        public static GASHost Instance { get; private set; }

        /// <summary>全局帧计时器</summary>
        public GASTimer Timer { get; private set; }
        /// <summary>全局 Ability 系统实例（驱动所有 ASC 的 Tick）</summary>
        public GameplayAbilitySystem System { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Timer = new GASTimer();
            Timer.Init();
            System = new GameplayAbilitySystem(Timer);
        }

        private void Update()
        {
            Timer.UpdateCurrentFrameCount();
            System.Update();
        }
    }

    /// <summary>
    /// 驱动所有已注册 ASC 的每帧 Tick。
    /// 通过固定 60 FPS 基准帧计数追赶实时间，保证确定性。
    /// </summary>
    public class GameplayAbilitySystem
    {
        private readonly GASTimer _timer;
        private readonly List<AbilitySystemComponent> _activeASC = new List<AbilitySystemComponent>();
        /// <summary>系统已处理的帧数</summary>
        private int _currentFrameCount;

        public GameplayAbilitySystem(GASTimer timer)
        {
            _timer = timer;
        }

        /// <summary>注册一个 ASC 进入 Tick 列表</summary>
        public void RegisterActivateASC(AbilitySystemComponent asc) => _activeASC.Add(asc);
        /// <summary>从 Tick 列表移除一个 ASC</summary>
        public void UnRegisterActivateASC(AbilitySystemComponent asc) => _activeASC.Remove(asc);

        /// <summary>
        /// 追赶实时间帧计数，对每个错过的帧调用所有 ASC 的 Tick。
        /// </summary>
        public void Update()
        {
            int frameCount = _timer.CurrentFrameCount;
            for (; _currentFrameCount < frameCount; _currentFrameCount++)
            {
                for (int i = 0; i < _activeASC.Count; i++)
                    _activeASC[i].Tick(_currentFrameCount);
            }
        }

        /// <summary>暂停定时器（游戏暂停时不再推进帧计数）</summary>
        public void Pause() => _timer.Pause();
        /// <summary>恢复定时器</summary>
        public void UnPause() => _timer.UnPause();
    }
}
