using Sirenix.OdinInspector;
using UnityEngine;

namespace Character
{
    public class EnemyController_Wizard : EnemyController
    {
        [LabelText("右手部施法位置"), SerializeField] private Transform _rightHandTrans;
        public Transform RightHandTrans => _rightHandTrans;
    }
}