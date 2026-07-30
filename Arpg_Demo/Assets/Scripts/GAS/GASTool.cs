using UnityEngine;

namespace GAS
{
    public static class GASTool
    {
        /// <summary>
        /// 计算水平面上，从source的forward到target的角度，忽略两者的高度差
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static float CalculateHorizontalAngle(Transform source, Transform target)
        {
            // 抹平高度差
            Vector3 pos = target.position - source.position;
            pos.y = source.position.y;
            // 计算角度
            return Vector3.Angle(source.forward, pos);
        }
    }
}
