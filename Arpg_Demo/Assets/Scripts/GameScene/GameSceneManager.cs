using LLKFrame;
using UnityEngine;

public class GameSceneManager : SingletonMonoBase<GameSceneManager>
{
    [SerializeField]private Transform playerRoot;

    protected override void Awake()
    {
        base.Awake();
        // 创建角色

    }
}
