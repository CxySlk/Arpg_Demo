using System;
using System.Collections.Generic;
using cfg;
using Character;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

/// <summary>
/// GameScene 敌人生成器：按「生成点 + 敌人ID + 数量」生成敌人并完成初始化。
/// 预制体走 Addressables（cfg.EnemyConfig.PrefabPath = 地址），生成后调用
/// EnemyController.Init(config) 完成 ASC / 技能 / 武器初始化；
/// 敌人销毁时由 Enemy.prefab 上的 AutoReleaseOnDestroy 释放资源引用
/// （用实例名释放，所以 prefab 的资产名要和地址一致）。
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [LabelText("生成条目"), SerializeField] private List<EnemySpawnItem> _items = new();
    
    [LabelText("随机散布半径"), SerializeField, MinValue(0f)]
    private float _randomRadius = 2f;

    [LabelText("NavMesh 吸附半径"), SerializeField, MinValue(0f)]
    private float _navMeshSampleDistance = 10f;

    [LabelText("敌人父节点（空 = 场景根）"), SerializeField]
    private Transform _enemyRoot;

    [LabelText("进入场景自动生成"), SerializeField]
    private bool _spawnOnStart = true;

    private readonly List<EnemyController> _existMonster = new();    // 记录存活的怪物
    private EnemyMgr _enemyMgr;

    public void Init(EnemyMgr enemyMgr)
    {
        _enemyMgr = enemyMgr;
        if (_spawnOnStart) SpawnAll();
        EventCenter.Global.UnSubscribe(CharacterEventKey.EnemyDeathEventKey,OnEnemyDeath);
        EventCenter.Global.Subscribe(CharacterEventKey.EnemyDeathEventKey,OnEnemyDeath);
    }

    private void OnDisable()
    {
        EventCenter.Global.UnSubscribe(CharacterEventKey.EnemyDeathEventKey,OnEnemyDeath);
    }
    
    private void OnEnemyDeath(EnemyController enemy)
    {
        _existMonster.Remove(enemy);
        if (_existMonster.Count == 0)
        {
            _enemyMgr.RemoveSpawner(this);
        }
    }

    /// <summary>按配置生成全部敌人；编辑器里也可以点按钮反复调（调试用）。</summary>
    [Button("立即生成")]
    public void SpawnAll()
    {
        foreach (EnemySpawnItem item in _items)
        {
            for (int i = 0; i < item.Count; i++)
                Spawn(item);
        }
    }

    private void Spawn(EnemySpawnItem item)
    {
        EnemyConfig config = Data.ConfigManager.Tables.TbEnemyConfig.GetOrDefault(item.EnemyId);
        if (config == null)
        {
            Debug.LogError($"[EnemySpawner] 敌人表里没有 id = {item.EnemyId}");
            return;
        }

        Transform point = item.SpawnPoint ? item.SpawnPoint : transform;
        Vector3 position = GetSpawnPosition(point);
        bool onNavMesh = NavMesh.SamplePosition(position, out NavMeshHit hit, _navMeshSampleDistance, NavMesh.AllAreas);
        if (onNavMesh) position = hit.position;

        // LoadAsset<GameObject> 内部已 Instantiate，这里直接摆位
        GameObject enemyObj = ResSystem.LoadAsset<GameObject>(config.PrefabPath);
        enemyObj.transform.SetPositionAndRotation(position, point.rotation);
        if (_enemyRoot) enemyObj.transform.SetParent(_enemyRoot, true);

        // 没在 NavMesh 上时 NavMeshAgent 会拒收寻路请求，这里用 Warp 对齐一次
        NavMeshAgent agent = enemyObj.GetComponentInChildren<NavMeshAgent>();
        if (onNavMesh && agent) agent.Warp(position);

        var enemy = enemyObj.GetComponentInChildren<EnemyController>();
        enemy.Init(config);
        
        _existMonster.Add(enemy);
    }

    /// <summary>生成点 + 随机散布（水平面）。</summary>
    private Vector3 GetSpawnPosition(Transform point)
    {
        Vector2 offset = Random.insideUnitCircle * _randomRadius;
        return point.position + new Vector3(offset.x, 0f, offset.y);
    }
}

/// <summary>一条生成配置：在某个生成点生成 Count 个 EnemyId 的敌人。</summary>
[System.Serializable]
public class EnemySpawnItem
{
    [LabelText("生成点（空 = 生成器自身）"), SerializeField]
    private Transform _spawnPoint;

    [LabelText("敌人ID"), SerializeField, ValueDropdown(nameof(EnemyIds))]
    private int _enemyId;

    [LabelText("数量"), SerializeField, MinValue(1)]
    private int _count = 1;

    public Transform SpawnPoint => _spawnPoint;
    public int EnemyId => _enemyId;
    public int Count => _count;

    /// <summary>敌人ID 下拉：直接读 Luban 敌人表。</summary>
    private static IEnumerable<ValueDropdownItem<int>> EnemyIds()
    {
        foreach (EnemyConfig config in Data.ConfigManager.Tables.TbEnemyConfig.DataList)
            yield return new ValueDropdownItem<int>(config.Id + "  " + config.PrefabPath, config.Id);
    }
}