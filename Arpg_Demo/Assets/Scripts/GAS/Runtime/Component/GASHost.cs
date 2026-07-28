using System;
using UnityEngine;

namespace GAS
{
    public class GASHost : MonoBehaviour
    {
        private static GASHost _instance;
        public static GASHost Instance => _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(gameObject);
        }
        private void Update()
        {
            GASTimer.UpdateCurrentFrameCount();
            GameplayAbilitySystom.Update();
        }
    }
}