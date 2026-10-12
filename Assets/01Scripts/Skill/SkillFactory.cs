using System;
using System.Collections.Generic;
using LARD.Article;
using UnityEngine;

namespace LARD.Skill {
    public sealed class SkillFactory {
        private readonly Dictionary<SkillObjBase, ObjectPooler<SkillObjBase>> _objectPools = new();

        public SkillBase Create(SkillModel model, ArticleObj owner) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }

            SkillBase skill = model.Id switch {
                ArtistSkill.SkillId => ScriptableObject.CreateInstance<ArtistSkill>(),
                AeromancerSkill.SkillId => ScriptableObject.CreateInstance<AeromancerSkill>(),
                _ => ScriptableObject.CreateInstance<SkillBase>(),
            };
            skill.SetFactory(this);
            skill.Init(owner, model);
            return skill;
        }

        public T SpawnObject<T>(T prefab, Vector3 position, Quaternion rotation) where T : SkillObjBase {
            if (prefab == null) {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (!_objectPools.TryGetValue(prefab, out ObjectPooler<SkillObjBase> pool)) {
                pool = new ObjectPooler<SkillObjBase>(prefab);
                _objectPools.Add(prefab, pool);
            }

            return (T)pool.Get(position, rotation);
        }
    }

}