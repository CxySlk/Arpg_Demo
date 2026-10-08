using System;
using LLKFrame;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class AutoReleaseOnDestroy : MonoBehaviour
{
    [LabelText("释放资源使用实例名（GameObject）"), SerializeField]
    private bool _isGameObjectName;

    [LabelText("释放资源的Key"), SerializeField, Sirenix.OdinInspector.ShowIf("@_isGameObjectName == false")]  
    private string _releaseKey;

    private void OnDestroy()
    {
        ResSystem.Release<GameObject>(_isGameObjectName ? gameObject.name : _releaseKey);
    }
}