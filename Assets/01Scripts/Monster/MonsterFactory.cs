using System;
using System.Collections.Generic;
using LARD.Battle;
using UnityEngine;

namespace LARD.Monster {
    /// <summary>몬스터 프리팹별 풀을 통한 생성과 반환 관리.</summary>
    public sealed class MonsterFactory {
        private readonly Dictionary<MonsterObj, ObjectPooler<MonsterObj>> _pools = new();
        private readonly Transform _parent;

        public MonsterFactory(Transform parent = null) {
            _parent = parent;
        }

        public MonsterObj Create(MonsterModel model, Map map, float health) {
            if (model == null || model.Prefab == null) {
                throw new ArgumentException("MonsterModel must reference a MonsterObj prefab.", nameof(model));
            }
            if (map == null) {
                throw new ArgumentNullException(nameof(map));
            }

            MonsterObj prefab = model.Prefab;
            if (!_pools.TryGetValue(prefab, out ObjectPooler<MonsterObj> pool)) {
                pool = new ObjectPooler<MonsterObj>(prefab, _parent);
                _pools.Add(prefab, pool);
            }

            MonsterObj monster = pool.Get(map.MonsterSpawnPosition, Quaternion.identity);
            try {
                monster.Init(model, map, health);
                return monster;
            } catch {
                monster.Release();
                throw;
            }
        }

        public void Release(MonsterObj monster) {
            if (monster != null) {
                monster.Release();
            }
        }
    }
}
