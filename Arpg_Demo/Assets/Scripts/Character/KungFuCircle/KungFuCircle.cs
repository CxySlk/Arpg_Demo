using cfg;
using UnityEngine;

public class KungFuCircle
{
    public readonly int WeightLimit;
    /// <summary>
    /// 攻击权重限制
    /// </summary>
    public readonly int AtkLimit;
    /// <summary>
    /// 等待半径范围
    /// </summary>
    public readonly float WaitRadius;
    /// <summary>
    /// 气氛组距离
    /// </summary>
    public readonly float OutCircleDis;
    public readonly Vector3[] waitPosArr;
    public readonly bool[] waitPosFlags;

    public KungFuCircle(KfcConfig kfcConfig)
    {
        WeightLimit = kfcConfig.WeightLimit;
        AtkLimit =  kfcConfig.AtkLimit;
        WaitRadius = kfcConfig.WaitRadius;
        OutCircleDis = kfcConfig.OutCircleDis;
        
        waitPosArr = new Vector3[kfcConfig.WeightLimit];
        waitPosFlags = new bool[kfcConfig.WeightLimit];
    }
    
    public void PlaceCirclePos(Vector3 center, float radius, Vector3[] posArr)
    {
        float baseAngle = 360f / posArr.Length;
        for (int i = 0; i < posArr.Length; i++)
        {
            float rad = baseAngle * i * Mathf.Deg2Rad;

            posArr[i] = new Vector3(
                center.x + radius * Mathf.Cos(rad),
                center.y,
                center.z + radius * Mathf.Sin(rad)
            );
        }
    }
    
    /// <summary>
    /// 获取与curTrans位置最接近的等待圈点位置
    /// </summary>
    /// <param name="curPos">当前敌人位置</param>
    /// <returns>最接近的等待圈点位下标（无则为-1）</returns>
    public int GetClosestPosIndex(Vector3 curPos)
    {
        int posIndex = -1;
        float closestDis = float.MaxValue;
        for(int i = 0; i < waitPosArr.Length; ++i)
        {
            //如果i下标的点位已被占据，就找其他的
            if(waitPosFlags[i])
            {
                continue;
            }
            //计算与i下标点位置的距离平方（仅比较大小，可不开根号）
            float curDist = Vector3.SqrMagnitude(curPos - waitPosArr[i]);
            if(curDist < closestDis)
            {
                closestDis = curDist;
                posIndex = i;
            }
        }
        return posIndex;
    }
    
    /// <summary>
    /// 未有进圈机会的敌人在圈外徘徊
    /// </summary>
    /// <param name="enemyPos">敌人位置</param>
    /// <param name="circleCenter">功夫圈中心位置</param>
    /// <param name="patrolSpeed">徘徊速度</param>
    /// <returns>徘徊点位置</returns>
    public Vector3 CirclePatrol(Vector3 enemyPos, Vector3 circleCenter, float patrolSpeed)
    {
        Vector3 dirToCenter = circleCenter - enemyPos;
        dirToCenter.y = 0;
        dirToCenter.Normalize();
        //圈外徘徊半径距离(略大于等待圈半径 )
        float desiredDistance = WaitRadius + OutCircleDis + Random.Range(-0.5f, 0.5f);
        Vector3 targetPos = circleCenter - dirToCenter * desiredDistance;
        //沿徘徊半径所形成的圆切线方向移动
        Vector3 right = Vector3.Cross(Vector3.up, dirToCenter);
        return targetPos + right * patrolSpeed;
    }
}
