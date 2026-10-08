using System;
using UnityEngine;

namespace LARD.Skill {
    [RequireComponent(typeof(SpriteRenderer))]
    public class ObjSkillGround : SkillObjBase {
        public event Action<Collider2D> Entered;
        public event Action<Collider2D> Stayed;
        public event Action<Collider2D> Exited;
        public event Action Ticked;

        private float _expiresAt;
        private float _tickInterval;
        private float _nextTickAt;
        private int _ticksRemaining;

        public virtual void Initialize(Vector2 center, Vector2 direction, float length, float width,
                                       float lifetime, Color color, int sortingOrder = -19) {
            ResetLifetime(lifetime);
            transform.position = center;
            transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);
            transform.localScale = new Vector3(length, width, 1f);

            BoxCollider2D trigger = GetComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = Vector2.one;

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        public virtual void InitializeCircle(Vector2 center, float radius, float lifetime,
                                             Color color, int sortingOrder = -19) {
            ResetLifetime(lifetime);
            transform.position = center;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * (radius * 2f);

            CircleCollider2D trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.5f;

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        public void StartTicks(float interval) {
            if (interval <= 0f) {
                throw new ArgumentOutOfRangeException(nameof(interval));
            }

            _tickInterval = interval;
            _nextTickAt = Time.time + interval;
            _ticksRemaining = Mathf.FloorToInt((_expiresAt - Time.time) / interval + 0.0001f);
        }

        public override void OnReleaseToPool() {
            _expiresAt = 0f;
            _tickInterval = 0f;
            _nextTickAt = 0f;
            _ticksRemaining = 0;
            ClearCallbacks();
            base.OnReleaseToPool();
        }

        private void Update() {
            if (_expiresAt <= 0f) {
                return;
            }

            float now = Time.time;
            while (_ticksRemaining > 0 && (_nextTickAt <= now || now >= _expiresAt)) {
                _ticksRemaining--;
                _nextTickAt += _tickInterval;
                Ticked?.Invoke();
                if (IsInPool) {
                    return;
                }
            }

            if (now >= _expiresAt) {
                Release();
            }
        }

        private void OnTriggerEnter2D(Collider2D other) {
            Entered?.Invoke(other);
            HandleEnter(other);
        }

        private void OnTriggerStay2D(Collider2D other) {
            Stayed?.Invoke(other);
            HandleStay(other);
        }

        private void OnTriggerExit2D(Collider2D other) {
            Exited?.Invoke(other);
            HandleExit(other);
        }

        protected virtual void HandleEnter(Collider2D other) { }
        protected virtual void HandleStay(Collider2D other) { }
        protected virtual void HandleExit(Collider2D other) { }

        protected virtual void OnDestroy() {
            ClearCallbacks();
        }

        private void ResetLifetime(float lifetime) {
            _expiresAt = Time.time + Mathf.Max(0f, lifetime);
            _tickInterval = 0f;
            _nextTickAt = 0f;
            _ticksRemaining = 0;
        }

        private void ClearCallbacks() {
            Entered = null;
            Stayed = null;
            Exited = null;
            Ticked = null;
        }
    }
}
