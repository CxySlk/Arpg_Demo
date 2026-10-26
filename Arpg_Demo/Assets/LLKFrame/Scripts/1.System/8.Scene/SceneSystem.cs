using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LLKFrame
{
    public static class SceneSystem
    {
        public static readonly EventKey<int> LoadProgressEventKey = "LOAD_SCENE_PROGRESS";

        public static void LoadScene(string sceneName, Action action = null)
        {
            SceneManager.LoadScene(sceneName);
            action?.Invoke();
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="sceneName">场景名</param>
        /// <param name="action">加载完成的回调函数</param>
        public static void LoadSceneAsync(string sceneName, Action action = null) =>
            MonoSystem.Start_Coroutine(DoLoadSceneAsync(sceneName, action));

        private static IEnumerator DoLoadSceneAsync(string sceneName, Action action)
        {
            AsyncOperation ao = SceneManager.LoadSceneAsync(sceneName);
            ao.allowSceneActivation = false;
            int curProgress = 0;

            while (ao.progress < 0.9f)
            {
                while (curProgress < (int)(ao.progress * 100))
                {
                    EventCenter.Global.Publish(LoadProgressEventKey, curProgress);
                    curProgress++;
                    yield return null;
                }

                yield return null;
            }

            while (curProgress <= 100)
            {
                EventCenter.Global.Publish(LoadProgressEventKey, curProgress);
                curProgress++;
                yield return null;
            }

            ao.allowSceneActivation = true;
            // 等新场景真正激活、并先渲染出一帧,再执行完成回调(通常是关掉加载窗口)。
            // 顺序反了会出现"旧场景已无遮罩、新场景还没显示"的空档(LoginScene 那侧就是一片天空盒)。
            while (!ao.isDone)
                yield return null;
            yield return null;
            action?.Invoke();
        }
    }
}