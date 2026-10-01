using System;
using System.Collections.Generic;
using LARD.Skill;
using LSJ.Util;
using UnityEngine;

namespace LARD.Article {
    public sealed class ArticleFactory {
        private const string CounterKey = "ArticleObj";

        private readonly Dictionary<string, ObjectPooler<ArticleObj>> _pools = new();
        private readonly Dictionary<string, ArticleObj> _prefabs = new();
        private readonly SkillFactory _skillFactory = new();
        private readonly Transform _parent;

        public ArticleFactory(Transform parent = null) {
            _parent = parent;
        }

        public ArticleObj Create(ArticleModel model, Vector3 position, Quaternion rotation) {
            if (model == null || model.Prefab == null) {
                throw new ArgumentException("ArticleModel must reference an ArticleObj prefab.", nameof(model));
            }
            if (model.Skill != null && model.Skill.Skill == null) {
                throw new ArgumentException("ArticleModel's SkillModel must reference a SkillBase asset.", nameof(model));
            }

            ArticleObj prefab = model.Prefab;
            string key = prefab.name;

            if (!_pools.TryGetValue(key, out ObjectPooler<ArticleObj> pool)) {
                pool = new ObjectPooler<ArticleObj>(prefab, _parent);
                _pools.Add(key, pool);
                _prefabs.Add(key, prefab);
            } else if (_prefabs[key] != prefab) {
                throw new InvalidOperationException($"Different article prefabs share the pool key '{key}'.");
            }

            ArticleObj article = pool.Get(position, rotation);
            article.Initialize(model, Counter.Next(CounterKey));
            if (model.Skill != null) {
                article.SkillController.SetSkill(model.Skill, _skillFactory.Create(model.Skill, article));
            }
            return article;
        }

        public void Release(ArticleObj article) {
            if (article != null) {
                article.Release();
            }
        }
    }
}
