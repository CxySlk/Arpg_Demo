using System.Collections.Generic;
using Animancer;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : SerializedMonoBehaviour
{
    [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.OneLine, KeyLabel = "摄像机Key", ValueLabel = "摄像机")]
    [LabelText("摄像机配置"), SerializeField]
    private Dictionary<string, CinemachineCamera> _cameraDic = new();

    [LabelText("默认摄像机"), Required, SerializeField]
    private string _defaultCameraKey;

    public string CurCameraKey { get; private set; }
    public CinemachineCamera CurCamera => _cameraDic.GetValueOrDefault(CurCameraKey);

    public void Init()
    {
        if (!_cameraDic.ContainsKey(_defaultCameraKey))
        {
            Debug.LogError("不存在该摄像机的Key：" + _defaultCameraKey);
            return;
        }

        // 默认关闭其他的摄像机
        foreach (var kv in _cameraDic) kv.Value.gameObject.SetActive(false);
        // 打开默认摄像机
        CurCameraKey = _defaultCameraKey;
        _cameraDic.Get(CurCameraKey).gameObject.SetActive(true);
    }

    public void StartCamera(string cameraKey)
    {
        if (!_cameraDic.ContainsKey(cameraKey))
        {
            Debug.LogError("不存在该摄像机的Key：" + cameraKey);
            return;
        }

        CurCamera?.gameObject.SetActive(false);
        CurCameraKey = cameraKey;
        _cameraDic[CurCameraKey].gameObject.SetActive(true);
    }

    public void StartCurCamera() => CurCamera?.gameObject.SetActive(true);

    public void StopCurCamera() => CurCamera?.gameObject.SetActive(false);
}