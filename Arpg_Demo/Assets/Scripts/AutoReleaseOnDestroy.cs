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

    /// <summary>
    /// 释放本对象占用的资源引用。
    /// 只处理场景中的实例:Addressables 引用计数归零时会卸载 prefab 资产,
    /// 资产对象被销毁时同样会触发 OnDestroy(它没有有效场景),那份引用不属于它。
    /// </summary>
    private void OnDestroy()
    {
        if (!gameObject.scene.IsValid()) return;
        ResSystem.Release<GameObject>(_isGameObjectName ? gameObject.name : _releaseKey);
    }
}