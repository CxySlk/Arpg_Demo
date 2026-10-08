using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    [System.Serializable]
    public class GameplayCueInstantParticle : GameplayCueInstant
    {
        [LabelText("粒子预制体")] public GameObject particlePrefab;
        [LabelText("本地位置")] public Vector3 localPos;
        [LabelText("本地旋转")] public Vector3 localRot;

        
        protected override GameplayCueSpec CreateSpec()
        {
            return PoolSystem.Global.GetOrNewObject<GameplayCueInstantParticleSpec>();
        }
    }
}