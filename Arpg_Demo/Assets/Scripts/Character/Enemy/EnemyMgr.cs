using System.Collections;
using System.Collections.Generic;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Character
{
    public class EnemyMgr : SingletonMonoBase<EnemyMgr>
    {
        [LabelText("敌人生成器"), SerializeField] private List<EnemySpawner> _spawners = new();

        private IEnumerator Start()
        {
            yield return null;

            foreach (EnemySpawner spawner in _spawners)
            {
                spawner.Init(this);
            }
        }

        public void RemoveSpawner(EnemySpawner enemySpawner)
        {
            _spawners.Remove(enemySpawner);
            if (_spawners.Count == 0)
            {
                EventCenter.Global.Publish(CharacterEventKey.EnemyAllDeathEventKey);
            }
        }
        
#if UNITY_EDITOR
        [Button("加载场景中所有的生成器")]
        private void AutoLoadEnemySpawner()
        {
            Undo.RecordObject(this,"Auto Load Enemy Spawners");
            var spawners = FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            _spawners .Clear();
            _spawners.AddRange(spawners);
            
            EditorUtility.SetDirty(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}