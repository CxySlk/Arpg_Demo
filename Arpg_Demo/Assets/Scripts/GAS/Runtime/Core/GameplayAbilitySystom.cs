using System.Collections.Generic;

namespace GAS
{
    public static class GameplayAbilitySystom
    {
        private static List<AbilitySystemComponent> _activeASC = new List<AbilitySystemComponent>();

        private static int _currentFrameCount;

        static GameplayAbilitySystom() => GASTimer.InitStartTimestamp();

        public static void Update()
        {
            // 时间戳驱动GAS
            TickFrameCount(GASTimer.CurrentFrameCount);
        }

        private static void TickFrameCount(int frameCount)
        {
            for (; _currentFrameCount < frameCount; _currentFrameCount++)
            {
                for (int i = 0; i < _activeASC.Count; i++)
                    _activeASC[i].Tick(_currentFrameCount);
            }
        }

        /// <summary>注册活跃的ASC</summary>
        public static void RegisterActivateASC(AbilitySystemComponent abilitySystemComponent) => _activeASC.Add(abilitySystemComponent);
        /// <summary>取消注册活跃的ASC</summary>
        public static void UnRegisterActivateASC(AbilitySystemComponent abilitySystemComponent) => _activeASC.Remove(abilitySystemComponent);

        public static void Pause()
        {
            GASTimer.Pause();
            GASHost.Instance.enabled = false;
        }
        public static void UnPause()
        {
            GASTimer.UnPause();
            GASHost.Instance.enabled = true;
        }
    }
}