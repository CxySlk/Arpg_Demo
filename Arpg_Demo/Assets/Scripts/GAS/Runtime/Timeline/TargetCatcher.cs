using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace GAS
{
    public abstract class TargetCatcherBase
    {
        [LabelText("捕捉层")] public LayerMask LayerMask;

        protected static readonly Collider[] Colliders = new Collider[20];

        public abstract Collider[] GetTargets(Transform checker);
    }

    [Serializable]
    public abstract class TargetShapeCatcherBase : TargetCatcherBase
    {
        [FormerlySerializedAs("_centerPos")] [LabelText("中心点")]
        public Vector3 centerPos;
    }

    /// <summary>
    /// 盒型监测
    /// </summary>
    public sealed class TargetBoxCatcher : TargetShapeCatcherBase
    {
        [LabelText("长宽高的一半")] public Vector3 HalfExtents;

        [LabelText("旋转"), OnValueChanged(nameof(OnRotationChanged))]
        public Vector3 Rotation;

        public override Collider[] GetTargets(Transform checker)
        {
            // 根据检测者计算中心点和旋转值
            Vector3 _centerPos = checker.TransformPoint(centerPos);
            Quaternion rotation = checker.rotation * Quaternion.Euler(Rotation);
            // 用盒体扫描所有碰撞体
            int colliderNum = Physics.OverlapBoxNonAlloc(_centerPos, HalfExtents, Colliders, rotation, LayerMask);
            // 把有效结果之后的槽位置空，防止脏数据
            for (; colliderNum < Colliders.Length && Colliders[colliderNum] != null; colliderNum++)
                Colliders[colliderNum] = null;
            return Colliders;
        }

#if UNITY_EDITOR
        private void OnRotationChanged()
        {
            if (Rotation.x > 180) Rotation.x -= 360;
            else if (Rotation.x < -180) Rotation.x += 360;
            if (Rotation.x % 180 != 0) Rotation.x %= 180;

            if (Rotation.y > 180) Rotation.y -= 360;
            else if (Rotation.y < -180) Rotation.y += 360;
            if (Rotation.y % 180 != 0) Rotation.y %= 180;

            if (Rotation.z > 180) Rotation.z -= 360;
            else if (Rotation.z < -180) Rotation.z += 360;
            if (Rotation.z % 180 != 0) Rotation.z %= 180;
        }
#endif
    }

    /// <summary>
    /// 球形检测
    /// </summary>
    public sealed class TargetSphereCatcher : TargetShapeCatcherBase
    {
        [LabelText("半径")] public float Radius;

        public override Collider[] GetTargets(Transform checker)
        {
            Vector3 _centerPos = checker.TransformPoint(centerPos);

            // 用球体扫描所有碰撞体
            int size = Physics.OverlapSphereNonAlloc(_centerPos, Radius, Colliders, LayerMask);
            for (; size < Colliders.Length && Colliders[size] != null; size++)
                Colliders[size] = null;
            return Colliders;
        }
    }

    /// <summary>
    /// 扇形检测
    /// </summary>
    public sealed class TargetSectorCatcher : TargetShapeCatcherBase
    {
        [LabelText("内半径"), MinValue(0), OnValueChanged(nameof(OnInsideRadiusValueChanged))]
        public float InsideRadius;

        [LabelText("外半径"), MinValue(0), OnValueChanged(nameof(OnOutsideRadiusValueChanged))]
        public float OutsideRadius;

        [LabelText("旋转"), OnValueChanged(nameof(OnRotationChanged))]
        public Vector3 Rotation;

        [LabelText("高度")] public float Height;
        [LabelText("角度")] public float Angle;

        public override Collider[] GetTargets(Transform checker)
        {
            // 根据检测者计算中心点和旋转值
            Vector3 _centerPos = checker.TransformPoint(centerPos);
            Quaternion rotation = checker.rotation * Quaternion.Euler(Rotation);

            // 先用盒体做粗筛（范围覆盖整个扇形可能区域）
            Vector3 halfExtents = new(OutsideRadius, Height / 2, OutsideRadius);
            int size = Physics.OverlapBoxNonAlloc(_centerPos, halfExtents,
                Colliders, Quaternion.Euler(Rotation), LayerMask);

            // 预计算扇形参数
            Vector3 forward = rotation * Vector3.forward; // 扇形的朝向
            Vector3 up = rotation * Vector3.up; // 扇形的上方
            float halfAngle = Angle * 0.5f;
            float minSqrDis = InsideRadius * InsideRadius;
            float maxSqrDis = OutsideRadius * OutsideRadius;
            float sqrDis;

            int curIndex = 0; // 符合条件的结果计数
            for (int i = 0; i < size; i++)
            {
                // 距离筛选：用 ClosestPoint 取碰撞体上离中心最近的点，算实际距离
                Vector3 closePoint = Colliders[i].ClosestPoint(_centerPos);
                Vector3 toTarget = closePoint - _centerPos;
                if (toTarget == default)
                {
                    toTarget = Colliders[i].transform.position - _centerPos;
                    sqrDis = toTarget.sqrMagnitude;
                    if (sqrDis >= minSqrDis && sqrDis <= maxSqrDis)
                    {
                        Colliders[curIndex++] = Colliders[i];
                    }

                    continue;
                }

                sqrDis = (closePoint - _centerPos).sqrMagnitude;
                if (sqrDis < minSqrDis || sqrDis > maxSqrDis) continue;

                float height = Vector3.Dot(toTarget, up);
                // 水平面投影：去掉高度分量，算水平夹角
                Vector3 onPlane = toTarget - up * height;
                if (onPlane.sqrMagnitude == 0) continue; // 目标恰好在正上方/下方，角度无意义

                float angle = Vector3.Angle(forward, onPlane);
                if (angle > halfAngle) continue;

                // 通过所有筛选，保留结果
                Colliders[curIndex++] = Colliders[i];
            }

            // 清理脏数据
            for (; curIndex < Colliders.Length && Colliders[curIndex]; curIndex++)
                Colliders[curIndex] = null;

            return Colliders;
        }

        #region Editor

#if UNITY_EDITOR
        private void OnInsideRadiusValueChanged() => OutsideRadius = Mathf.Max(OutsideRadius, InsideRadius);
        private void OnOutsideRadiusValueChanged() => InsideRadius = Mathf.Min(OutsideRadius, InsideRadius);

        private void OnRotationChanged()
        {
            if (Rotation.x > 180) Rotation.x -= 360;
            else if (Rotation.x < -180) Rotation.x += 360;
            if (Rotation.x % 180 != 0) Rotation.x %= 180;

            if (Rotation.y > 180) Rotation.y -= 360;
            else if (Rotation.y < -180) Rotation.y += 360;
            if (Rotation.y % 180 != 0) Rotation.y %= 180;

            if (Rotation.z > 180) Rotation.z -= 360;
            else if (Rotation.z < -180) Rotation.z += 360;
            if (Rotation.z % 180 != 0) Rotation.z %= 180;
        }
#endif

        #endregion
    }

    /// <summary>
    /// 开启武器检测
    /// </summary>
    public sealed class WeaponCatcher : TargetCatcherBase
    {
        [LabelText("武器(按位,0=全部)")] public int weaponKeys;

        public override Collider[] GetTargets(Transform checker)
        {
            return null;
        }
    }
}