using System.Collections.Generic;
using GAS;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// SceneView 捕捉范围可视化：编辑器里选中的事件若带捕捉器/粒子手柄，则绘制其范围与操作手柄。
    /// </summary>
    public class SceneGizmoView
    {
        private AbilityAsset _asset;
        private readonly PreviewSceneManager _preview;
        private bool _registered;

        /// <summary>当前选中的标记事件（由主窗口在选中变化时更新）；只有带手柄的事件才会被绘制</summary>
        public MarkEventBase SelectedMark { get; set; }

        /// <summary>粒子 Cue 位置/旋转手柄变化 → 刷新预览粒子实例</summary>
        public event System.Action<DurationCueMarkEvent> ParticleTransformChanged;

        public SceneGizmoView(AbilityAsset asset, PreviewSceneManager preview)
        {
            _asset = asset;
            _preview = preview;
        }

        public void BindAsset(AbilityAsset asset) => _asset = asset;

        public void Register()
        {
            if (_registered) return;
            SceneView.duringSceneGui -= OnSceneGizmos;
            SceneView.duringSceneGui += OnSceneGizmos;
            _registered = true;
            SceneView.RepaintAll();
        }

        public void Unregister()
        {
            SceneView.duringSceneGui -= OnSceneGizmos;
            _registered = false;
        }

        private void OnSceneGizmos(SceneView sceneView)
        {
            if (_asset == null || SelectedMark == null) return;

            // 只绘制"选中"的事件（不再按当前帧匹配）
            MarkEventBase mark = SelectedMark;
            if (mark is ReleaseGEMarkEvent geMark && geMark.targetCatcher != null)
            {
                DrawCatcherGizmos(geMark.targetCatcher);
                DrawCatcherHandles(geMark.targetCatcher);
            }
            else if (mark is DurationCueMarkEvent cueMark &&
                     cueMark.DurationCue is GameplayCueDurationalParticle particleCue &&
                     particleCue.ParticlePrefab != null)
            {
                DrawParticleHandles(cueMark, particleCue);
            }
        }

        private void DrawCatcherGizmos(TargetCatcherBase catcher)
        {
            Transform previewModel = _preview.PreviewModel;
            Vector3 center = previewModel != null
                ? previewModel.TransformPoint(GetCatcherCenterOffset(catcher))
                : GetCatcherCenterOffset(catcher);

            Handles.color = new Color(1f, 0.6f, 0.2f, 0.9f);
            Quaternion modelRotation = previewModel != null ? previewModel.rotation : Quaternion.identity;
            switch (catcher)
            {
                case TargetBoxCatcher box:
                    // 旋转顺序与运行时一致：模型(角色)旋转在前、偏移在后 → 偏移在角色局部坐标生效
                    DrawBox(center, box.HalfExtents, modelRotation * Quaternion.Euler(NormalizeRotation(box.Rotation)));
                    break;
                case TargetSphereCatcher sphere:
                    DrawSphere(center, sphere.Radius);
                    break;
                case TargetSectorCatcher sector:
                    DrawSector(center, sector, modelRotation);
                    break;
            }
        }

        /// <summary>欧拉角归一到 (-180, 180]：手柄与绘制统一用它，避免 0~360 与 ±180 混用造成跳变</summary>
        private static Vector3 NormalizeRotation(Vector3 rotation)
        {
            return new Vector3(
                Mathf.DeltaAngle(0f, rotation.x),
                Mathf.DeltaAngle(0f, rotation.y),
                Mathf.DeltaAngle(0f, rotation.z));
        }

        private static Vector3 GetCatcherCenterOffset(TargetCatcherBase catcher)
        {
            if (catcher is TargetShapeCatcherBase shape)
                return shape.centerPos;
            return Vector3.zero;
        }

        private static void DrawSphere(Vector3 center, float radius)
        {
            Handles.DrawWireDisc(center, Vector3.up, radius);
            Handles.DrawWireDisc(center, Vector3.right, radius);
            Handles.DrawWireDisc(center, Vector3.forward, radius);
        }

        private static void DrawBox(Vector3 center, Vector3 halfExtents, Quaternion rotation)
        {
            Vector3[] corners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3 local = new(
                    (i & 1) == 0 ? -halfExtents.x : halfExtents.x,
                    (i & 2) == 0 ? -halfExtents.y : halfExtents.y,
                    (i & 4) == 0 ? -halfExtents.z : halfExtents.z);
                corners[i] = center + rotation * local;
            }

            Handles.DrawLine(corners[0], corners[1]);
            Handles.DrawLine(corners[1], corners[3]);
            Handles.DrawLine(corners[3], corners[2]);
            Handles.DrawLine(corners[2], corners[0]);
            Handles.DrawLine(corners[4], corners[5]);
            Handles.DrawLine(corners[5], corners[7]);
            Handles.DrawLine(corners[7], corners[6]);
            Handles.DrawLine(corners[6], corners[4]);
            Handles.DrawLine(corners[0], corners[4]);
            Handles.DrawLine(corners[1], corners[5]);
            Handles.DrawLine(corners[2], corners[6]);
            Handles.DrawLine(corners[3], corners[7]);
        }

        private static void DrawSector(Vector3 center, TargetSectorCatcher sector, Quaternion modelRotation)
        {
            Quaternion rot = modelRotation * Quaternion.Euler(NormalizeRotation(sector.Rotation));
            Vector3 forward = rot * Vector3.forward;
            Vector3 up = rot * Vector3.up;
            Vector3 right = rot * Vector3.right;
            float halfAngle = sector.Angle * 0.5f;
            int segments = 32;

            Vector3 prevOuter = GetSectorPoint(center, forward, right, up, sector.OutsideRadius, -halfAngle,
                sector.Height * 0.5f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / segments);
                Vector3 curOuter = GetSectorPoint(center, forward, right, up, sector.OutsideRadius, angle,
                    sector.Height * 0.5f);
                Handles.DrawLine(prevOuter, curOuter);
                prevOuter = curOuter;
            }

            if (sector.InsideRadius > 0)
            {
                Vector3 prevInner = GetSectorPoint(center, forward, right, up, sector.InsideRadius, -halfAngle,
                    sector.Height * 0.5f);
                for (int i = 1; i <= segments; i++)
                {
                    float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / segments);
                    Vector3 curInner = GetSectorPoint(center, forward, right, up, sector.InsideRadius, angle,
                        sector.Height * 0.5f);
                    Handles.DrawLine(prevInner, curInner);
                    prevInner = curInner;
                }
            }

            Vector3 topCenter = center + up * sector.Height * 0.5f;
            Handles.DrawLine(topCenter,
                GetSectorPoint(center, forward, right, up, sector.OutsideRadius, -halfAngle, sector.Height * 0.5f));
            Handles.DrawLine(topCenter,
                GetSectorPoint(center, forward, right, up, sector.OutsideRadius, halfAngle, sector.Height * 0.5f));

            Vector3 bottomCenter = center - up * sector.Height * 0.5f;
            Vector3 prevOuterB = GetSectorPoint(center, forward, right, up, sector.OutsideRadius, -halfAngle,
                -sector.Height * 0.5f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / segments);
                Vector3 curOuterB = GetSectorPoint(center, forward, right, up, sector.OutsideRadius, angle,
                    -sector.Height * 0.5f);
                Handles.DrawLine(prevOuterB, curOuterB);
                prevOuterB = curOuterB;
            }

            Handles.DrawLine(bottomCenter,
                GetSectorPoint(center, forward, right, up, sector.OutsideRadius, -halfAngle, -sector.Height * 0.5f));
            Handles.DrawLine(bottomCenter,
                GetSectorPoint(center, forward, right, up, sector.OutsideRadius, halfAngle, -sector.Height * 0.5f));

            Handles.DrawLine(
                GetSectorPoint(center, forward, right, up, sector.OutsideRadius, -halfAngle, sector.Height * 0.5f),
                GetSectorPoint(center, forward, right, up, sector.OutsideRadius, -halfAngle, -sector.Height * 0.5f));
            Handles.DrawLine(
                GetSectorPoint(center, forward, right, up, sector.OutsideRadius, halfAngle, sector.Height * 0.5f),
                GetSectorPoint(center, forward, right, up, sector.OutsideRadius, halfAngle, -sector.Height * 0.5f));
        }

        private static Vector3 GetSectorPoint(Vector3 center, Vector3 forward, Vector3 right, Vector3 up,
            float radius, float angle, float halfHeight)
        {
            Vector3 dir = (forward * Mathf.Cos(angle * Mathf.Deg2Rad)) + (right * Mathf.Sin(angle * Mathf.Deg2Rad));
            return center + dir * radius + up * halfHeight;
        }

        #region 交互手柄

        private void DrawCatcherHandles(TargetCatcherBase catcher)
        {
            Transform previewModel = _preview.PreviewModel;
            Vector3 center = previewModel != null
                ? previewModel.TransformPoint(GetCatcherCenterOffset(catcher))
                : GetCatcherCenterOffset(catcher);
            Quaternion modelRotation = previewModel != null ? previewModel.rotation : Quaternion.identity;

            EditorGUI.BeginChangeCheck();
            switch (catcher)
            {
                case TargetBoxCatcher box:
                    DrawBoxHandles(box, ref center, modelRotation);
                    break;
                case TargetSphereCatcher sphere:
                    DrawSphereHandles(sphere, ref center, modelRotation);
                    break;
                case TargetSectorCatcher sector:
                    DrawSectorHandles(sector, ref center, modelRotation);
                    break;
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (previewModel != null && catcher is TargetShapeCatcherBase shape)
                    shape.centerPos = previewModel.InverseTransformPoint(center);
                EditorUtility.SetDirty(_asset);
                SceneView.RepaintAll();
            }
        }

        private void DrawParticleHandles(DurationCueMarkEvent cueMark, GameplayCueDurationalParticle particleCue)
        {
            Transform previewModel = _preview.PreviewModel;
            Vector3 worldPos = previewModel != null
                ? previewModel.TransformPoint(particleCue.Position)
                : particleCue.Position;
            Quaternion modelRotation = previewModel != null ? previewModel.rotation : Quaternion.identity;

            EditorGUI.BeginChangeCheck();

            // 位置
            Vector3 newPos = Handles.PositionHandle(worldPos, modelRotation);

            // 旋转（顺序与运行时一致：模型旋转在前、偏移在后 → 偏移在角色局部坐标生效）
            Quaternion worldRot = modelRotation * Quaternion.Euler(NormalizeRotation(particleCue.Rotation));
            Quaternion newRot = Handles.RotationHandle(worldRot, newPos);
            if (newRot != worldRot)
                particleCue.Rotation = NormalizeRotation((Quaternion.Inverse(modelRotation) * newRot).eulerAngles);

            if (EditorGUI.EndChangeCheck())
            {
                if (previewModel != null)
                    particleCue.Position = previewModel.InverseTransformPoint(newPos);
                EditorUtility.SetDirty(_asset);
                SceneView.RepaintAll();
                ParticleTransformChanged?.Invoke(cueMark);
            }
        }

        private static void DrawBoxHandles(TargetBoxCatcher box, ref Vector3 center, Quaternion modelRotation)
        {
            Quaternion worldRot = modelRotation * Quaternion.Euler(NormalizeRotation(box.Rotation));

            // 位置
            center = Handles.PositionHandle(center, worldRot);

            // 旋转
            Quaternion newRot = Handles.RotationHandle(worldRot, center);
            if (newRot != worldRot)
                box.Rotation = NormalizeRotation((Quaternion.Inverse(modelRotation) * newRot).eulerAngles);

            // 三轴缩放
            Vector3 newScale = Handles.ScaleHandle(box.HalfExtents, center, worldRot, HandleUtility.GetHandleSize(center));
            if (newScale != box.HalfExtents)
                box.HalfExtents = new Vector3(Mathf.Max(0.01f, newScale.x), Mathf.Max(0.01f, newScale.y), Mathf.Max(0.01f, newScale.z));
        }

        private static void DrawSphereHandles(TargetSphereCatcher sphere, ref Vector3 center, Quaternion modelRotation)
        {
            // 位置
            center = Handles.PositionHandle(center, modelRotation);

            // 半径：球面一点沿半径方向拖动
            Vector3 gripPos = center + Vector3.up * sphere.Radius;
            float handleSize = HandleUtility.GetHandleSize(gripPos);
            EditorGUI.BeginChangeCheck();
            Vector3 newGrip = Handles.Slider(gripPos, Vector3.up, handleSize * 0.12f, Handles.SphereHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
                sphere.Radius = Mathf.Max(0.01f, (newGrip - center).magnitude);
        }

        private static void DrawSectorHandles(TargetSectorCatcher sector, ref Vector3 center, Quaternion modelRotation)
        {
            Quaternion worldRot = modelRotation * Quaternion.Euler(NormalizeRotation(sector.Rotation));
            Vector3 forward = worldRot * Vector3.forward;
            Vector3 right = worldRot * Vector3.right;
            Vector3 up = worldRot * Vector3.up;
            float halfAngle = sector.Angle * 0.5f;
            float handleSize = HandleUtility.GetHandleSize(center);
            float halfHeight = sector.Height * 0.5f;

            // 位置
            center = Handles.PositionHandle(center, modelRotation);

            // 旋转
            Quaternion newRot = Handles.RotationHandle(worldRot, center);
            if (newRot != worldRot)
                sector.Rotation = NormalizeRotation((Quaternion.Inverse(modelRotation) * newRot).eulerAngles);

            // 外半径：外弧中点沿水平半径方向拖动
            Vector3 outerGrip = GetSectorPoint(center, forward, right, up, sector.OutsideRadius, 0f, 0f);
            EditorGUI.BeginChangeCheck();
            Vector3 newOuter = Handles.Slider(outerGrip, forward, handleSize * 0.12f, Handles.SphereHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
                sector.OutsideRadius = Mathf.Max(0.01f, Vector3.Dot(newOuter - center, forward));

            // 内半径：内弧中点沿水平半径方向拖动
            if (sector.InsideRadius > 0)
            {
                Vector3 innerGrip = GetSectorPoint(center, forward, right, up, sector.InsideRadius, 0f, 0f);
                EditorGUI.BeginChangeCheck();
                Vector3 newInner = Handles.Slider(innerGrip, forward, handleSize * 0.1f, Handles.SphereHandleCap, 0f);
                if (EditorGUI.EndChangeCheck())
                    sector.InsideRadius = Mathf.Clamp(Vector3.Dot(newInner - center, forward), 0f, sector.OutsideRadius);
            }

            // 高度：顶部中点沿 up 轴拖动
            Vector3 topGrip = center + up * halfHeight;
            EditorGUI.BeginChangeCheck();
            Vector3 newTop = Handles.Slider(topGrip, up, handleSize * 0.12f, Handles.SphereHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
                sector.Height = Mathf.Max(0.01f, Vector3.Dot(newTop - center, up) * 2f);

            // 张开角度：外弧两端手柄，拖动任一端对称张开/收拢
            Vector3 leftEdge = GetSectorPoint(center, forward, right, up, sector.OutsideRadius, -halfAngle, 0f);
            Vector3 rightEdge = GetSectorPoint(center, forward, right, up, sector.OutsideRadius, halfAngle, 0f);
            Vector3 leftNew = Handles.Slider(leftEdge, forward, handleSize * 0.12f, Handles.SphereHandleCap, 0f);
            Vector3 rightNew = Handles.Slider(rightEdge, forward, handleSize * 0.12f, Handles.SphereHandleCap, 0f);
            if (leftNew != leftEdge)
                sector.Angle = Mathf.Clamp(GetSectorHalfAngle(center, forward, right, leftNew, sector.OutsideRadius) * 2f, 0f, 360f);
            else if (rightNew != rightEdge)
                sector.Angle = Mathf.Clamp(GetSectorHalfAngle(center, forward, right, rightNew, sector.OutsideRadius) * 2f, 0f, 360f);
        }

        /// <summary>计算一个点在水平面上相对 forward 的半角（弧度→度）</summary>
        private static float GetSectorHalfAngle(Vector3 center, Vector3 forward, Vector3 right, Vector3 point, float radius)
        {
            Vector3 toPoint = point - center;
            float dot = Vector3.Dot(toPoint, forward);
            float side = Vector3.Dot(toPoint, right);
            return Mathf.Atan2(side, dot) * Mathf.Rad2Deg;
        }

        #endregion
    }
}