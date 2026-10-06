using UnityEngine;

namespace LARD.Monster {
    public sealed class UIMonsterObj : MonoBehaviour {
        private const float FillWidth = 0.56f;
        private const float FillHeight = 0.05f;

        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private SpriteRenderer _fill;

        private static Sprite _barSprite;
        private MonsterObj _monster;

        private void Awake() {
            if (_barSprite == null) {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _barSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }

            _background.sprite = _barSprite;
            _background.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            _background.sortingOrder = 20;
            _fill.sprite = _barSprite;
            _fill.color = new Color(0.2f, 0.85f, 0.25f, 1f);
            _fill.sortingOrder = 21;
        }

        private void OnEnable() {
            _monster = GetComponentInParent<MonsterObj>();
            if (_monster == null) {
                return;
            }

            _monster.MonsterObserver.AddListener(MonsterEventType.HealthChanged, Refresh);
            Refresh();
        }

        private void OnDisable() {
            if (_monster != null) {
                _monster.MonsterObserver.RemoveListener(MonsterEventType.HealthChanged, Refresh);
                _monster = null;
            }
        }

        private void Refresh() {
            if (_monster == null || _monster.StatController == null) {
                return;
            }

            float maxHealth = _monster.StatController.MaxHealth;
            float ratio = maxHealth > 0f
                ? Mathf.Clamp01(_monster.StatController.CurrentHealth / maxHealth)
                : 0f;
            _fill.transform.localScale = new Vector3(FillWidth * ratio, FillHeight, 1f);
            _fill.transform.localPosition = new Vector3(-FillWidth * (1f - ratio) * 0.5f, 0f, 0f);
        }
    }
}
