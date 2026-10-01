using LARD.Article;
using UnityEngine;

namespace LARD.Skill {
    public sealed class MagicMissile : MonoBehaviour {
        private const float HitDistance = 0.12f;
        private const float MaxLifetime = 10f;

        private static Sprite _sprite;

        private ArticleObj _owner;
        private IAttackTarget _target;
        private float _damage;
        private float _speed;
        private float _lifeTime;

        private void Awake() {
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSprite();
            renderer.color = new Color(0.45f, 0.85f, 1f);
            renderer.sortingOrder = 10;
            transform.localScale = Vector3.one * 0.22f;
        }

        public void Initialize(ArticleObj owner, IAttackTarget target, float damage, float speed) {
            _owner = owner;
            _target = target;
            _damage = damage;
            _speed = speed;
        }

        private void Update() {
            _lifeTime += Time.deltaTime;
            if (_lifeTime >= MaxLifetime) {
                Destroy(gameObject);
                return;
            }

            if (_owner == null || _owner.IsInPool || _owner.Model == null) {
                Destroy(gameObject);
                return;
            }

            if (!IsValid(_target)) {
                _target = FindNearestTarget();
                if (_target == null) {
                    Destroy(gameObject);
                    return;
                }
            }

            Vector3 destination = _target.TargetTransform.position;
            destination.z = transform.position.z;
            transform.position = Vector3.MoveTowards(transform.position, destination, _speed * Time.deltaTime);

            if ((destination - transform.position).sqrMagnitude <= HitDistance * HitDistance) {
                if (IsValid(_target)) {
                    _target.TakeDamage(_damage);
                }
                Destroy(gameObject);
            }
        }

        private IAttackTarget FindNearestTarget() {
            IAttackTarget nearest = null;
            float nearestDistance = float.PositiveInfinity;

            foreach (IAttackTarget candidate in FindTargetUtil.FindTargets(_owner.transform.position, _owner.Model.Range)) {
                float distance = (candidate.TargetTransform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance) {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private static bool IsValid(IAttackTarget target) {
            if (target == null || (target is Object unityObject && unityObject == null)) {
                return false;
            }

            return target.IsAlive && target.TargetTransform != null;
        }

        private static Sprite GetSprite() {
            if (_sprite != null) {
                return _sprite;
            }

            const int size = 16;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) {
                filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float radius = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(8f - radius)));
                }
            }
            texture.Apply();
            _sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _sprite;
        }
    }
}
