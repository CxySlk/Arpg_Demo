using GAS;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class Test : SerializedMonoBehaviour
{
    public Transform A;
    public Transform B;

    [Button("计算角度")]
    private void ApplyEffect()
    {
        Debug.Log(GAS.GASTool.CalculateHorizontalAngle(A, B));
    }
}
