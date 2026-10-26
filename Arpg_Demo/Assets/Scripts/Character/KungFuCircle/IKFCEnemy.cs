using cfg;
using UnityEngine;

namespace Character
{
    public interface IKFCEnemy
    {
        public KFC_EnemyConfig kfcEnemyConfig { get; }
        public Transform selfTrans { get; }
    }
}