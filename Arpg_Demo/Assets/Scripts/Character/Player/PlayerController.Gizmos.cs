using UnityEngine;

public partial class PlayerController
{
    #region Unity Editor

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.TransformPoint(posOffset);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + Vector3.down * maxDis);
    }

#endif

    #endregion
}
