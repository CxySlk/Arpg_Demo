using Bullet;
using Character;
using GAS;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skill
{
    public class MagicAttackBehavior : SkillBehaviorBase
    {
        [LabelText("魔法球预制体")] public GameObject orbPrefab;
        [LabelText("魔法球预制体")] public BulletData bulletData = new();
        private static Transform _rootTrans;

        static MagicAttackBehavior()
        {
            _rootTrans = new GameObject().transform;
            _rootTrans.name = "OrbBullet";
        }

        public override SkillBehaviorBase DeepCopy()
        {
            return new MagicAttackBehavior
            {
                orbPrefab = orbPrefab,
                bulletData = bulletData,
            };
        }

        protected override void OnCustomEvent(string eventName, FrameEvent frameEvent)
        {
            if (eventName == "ReleaseOrb")
            {
                // 只有魔法师才起作用
                if (skillBrain.characterController is not EnemyController_Wizard controller)
                    return;
                Transform target = controller.Target;
                // 计算方向
                Vector3 rightHandPos = controller.RightHandTrans.position;
                Vector3 dir = target.position - rightHandPos;
                dir.y = 0;
                // 生成法球
                GameObject orbObj = PoolSystem.Global.GetGameObject(orbPrefab,_rootTrans);
                orbObj.transform.position = rightHandPos;
                orbObj.transform.rotation = Quaternion.LookRotation(dir);
                var bullet = orbObj.GetComponent<BulletBase>();
                bullet.Init(bulletData,Owner);
            }
        }
    }
}