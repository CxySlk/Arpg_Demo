using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace GAS
{
    public abstract class TargetCatcherBase
    {
        [LabelText("捕捉层")]
        public LayerMask _layerMask;

        protected static Collider[] _colliders = new Collider[20];
        protected static Collider[] _workspace = new Collider[20];

        public abstract Collider[] GetTargets();
    }

    public abstract class TargetShapeCatcherBase : TargetCatcherBase
    {
        [LabelText("中心点")]
        public Vector3 _centerPos;
    }

    /// <summary>
    /// 盒型监测
    /// </summary>
    public sealed class TargetBoxCatcher : TargetShapeCatcherBase
    {
        [LabelText("长宽高的一半")]
        public Vector3 HalfExtents;
        [LabelText("旋转")]
        public Vector3 Rotation;

        public override Collider[] GetTargets()
        {
            int colliderNum = Physics.OverlapBoxNonAlloc(_centerPos, HalfExtents, _colliders, Quaternion.Euler(Rotation), _layerMask);
            for (; colliderNum < _colliders.Length && _colliders[colliderNum] != null; colliderNum++)
                _colliders[colliderNum] = null;
            return _colliders;
        }
    }

    /// <summary>
    /// 球形检测
    /// </summary>
    public sealed class TargetSphereCatcher : TargetShapeCatcherBase
    {
        [LabelText("半径")]
        public float Radius;

        public override Collider[] GetTargets()
        {
            int size = Physics.OverlapSphereNonAlloc(_centerPos, Radius, _colliders, _layerMask);
            for (; size < _colliders.Length && _colliders[size] != null; size++)
                _colliders[size] = null;
            return _colliders;
        }
    }
    /// <summary>
    /// 扇形检测
    /// </summary>
    public sealed class TargetSectorCatcher : TargetShapeCatcherBase
    {
        [LabelText("内半径"), MinValue(0),OnValueChanged(nameof(OnInsideRadiusValueChanged))]
        public float InsideRadius;
        [LabelText("内半径"), MinValue(0), OnValueChanged(nameof(OnOutsideRadiusValueChanged))]
        public float OutsideRadius;
        [LabelText("旋转")]
        public Vector3 Rotation;
        [LabelText("高度")]
        public float Height;
        [LabelText("角度")]
        public float Angle;

        public override Collider[] GetTargets()
        {
            Vector3 halfExtents = new Vector3(OutsideRadius, Height / 2, OutsideRadius);
            int size = Physics.OverlapBoxNonAlloc(_centerPos, halfExtents,
               _colliders, Quaternion.Euler(Rotation), _layerMask);

            // 筛选掉不符合条件的
            int curIndex = 0;
            float minSqrDis = InsideRadius * InsideRadius;
            float maxSqrDis = OutsideRadius * OutsideRadius;
            float halfAngle = Angle / 2;
            for (int i = 0; i < size; i++)
            {
                Vector3 pos = _colliders[i].transform.position;
                float sqrDic = Vector3.SqrMagnitude(_centerPos - pos);
                // 筛选距离不合适的
                if (sqrDic < minSqrDis || sqrDic > maxSqrDis)
                    continue;

                // 筛选角度


                // 剩下的符合条件
                _colliders[curIndex] = _colliders[i]; 
                curIndex++;
            }

            // 把后面的置空
            for (; curIndex < size; curIndex++)
                _colliders[curIndex] = null;

            return _colliders;
        }

        #region Editor
#if UNITY_EDITOR
        private void OnInsideRadiusValueChanged() => OutsideRadius = Mathf.Max(OutsideRadius, InsideRadius);
        private void OnOutsideRadiusValueChanged() => InsideRadius = Mathf.Min(OutsideRadius, InsideRadius);
#endif
        #endregion
    }
}
