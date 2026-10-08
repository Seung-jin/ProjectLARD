using UnityEngine;

namespace LARD.Battle {
    public sealed class Map : MonoBehaviour {
        public const int MonsterWaypointCount = 4;

        [SerializeField, Min(0.1f)] private float _articleAreaSize = 5f;
        [SerializeField, Min(0.1f)] private float _monsterPathOffset = 0.75f;
        [SerializeField, Min(0.1f)] private float _monsterPathWidth = 0.75f;
        [SerializeField] private Color _articleAreaColor = new(0.18f, 0.33f, 0.29f, 1f);
        [SerializeField] private Color _monsterPathColor = new(0.35f, 0.39f, 0.44f, 1f);

        private static Sprite _tileSprite;

        public float ArticleAreaSize => _articleAreaSize;
        public float MonsterPathWidth => _monsterPathWidth;
        public Vector2 ArticleSpawnPosition => transform.position;
        public Vector2 MonsterSpawnPosition => GetMonsterWaypoint(0);

        private void Awake() {
            CreateVisuals();
        }

        public Vector2 GetMonsterWaypoint(int index) {
            float half = _articleAreaSize * 0.5f + _monsterPathOffset;
            Vector2 center = transform.position;
            return ((index % MonsterWaypointCount + MonsterWaypointCount) % MonsterWaypointCount) switch {
                0 => center + new Vector2(-half, half),
                1 => center + new Vector2(-half, -half),
                2 => center + new Vector2(half, -half),
                _ => center + new Vector2(half, half),
            };
        }

        public Vector2 GetNextMonsterWaypoint(int currentIndex) {
            return GetMonsterWaypoint(currentIndex + 1);
        }

        public Vector2 ClampArticlePosition(Vector2 position, Vector2 halfExtents) {
            Vector2 center = transform.position;
            float half = _articleAreaSize * 0.5f;
            float xLimit = Mathf.Max(0f, half - Mathf.Max(0f, halfExtents.x));
            float yLimit = Mathf.Max(0f, half - Mathf.Max(0f, halfExtents.y));
            Vector2 local = position - center;
            return center + new Vector2(
                Mathf.Clamp(local.x, -xLimit, xLimit),
                Mathf.Clamp(local.y, -yLimit, yLimit));
        }

        private void CreateVisuals() {
            float routeHalf = _articleAreaSize * 0.5f + _monsterPathOffset;
            float routeLength = routeHalf * 2f + _monsterPathWidth;
            float sideLength = Mathf.Max(0f, routeHalf * 2f - _monsterPathWidth);

            CreateRectangle("Monster Path Top", new Vector2(0f, routeHalf), new Vector2(routeLength, _monsterPathWidth), _monsterPathColor, -20);
            CreateRectangle("Monster Path Bottom", new Vector2(0f, -routeHalf), new Vector2(routeLength, _monsterPathWidth), _monsterPathColor, -20);
            CreateRectangle("Monster Path Left", new Vector2(-routeHalf, 0f), new Vector2(_monsterPathWidth, sideLength), _monsterPathColor, -20);
            CreateRectangle("Monster Path Right", new Vector2(routeHalf, 0f), new Vector2(_monsterPathWidth, sideLength), _monsterPathColor, -20);
            CreateRectangle("Article Area", Vector2.zero, Vector2.one * _articleAreaSize, _articleAreaColor, -10);
        }

        private void CreateRectangle(string name, Vector2 position, Vector2 size, Color color, int sortingOrder) {
            GameObject rectangle = new GameObject(name);
            rectangle.transform.SetParent(transform, false);
            rectangle.transform.localPosition = position;
            rectangle.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = rectangle.AddComponent<SpriteRenderer>();
            renderer.sprite = GetTileSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        private static Sprite GetTileSprite() {
            if (_tileSprite != null) {
                return _tileSprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _tileSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return _tileSprite;
        }

        private void OnDrawGizmos() {
            Vector2 center = transform.position;
            Gizmos.color = _articleAreaColor;
            Gizmos.DrawWireCube(center, Vector3.one * _articleAreaSize);

            Gizmos.color = _monsterPathColor;
            for (int i = 0; i < MonsterWaypointCount; i++) {
                Vector2 from = GetMonsterWaypoint(i);
                Vector2 to = GetMonsterWaypoint(i + 1);
                Gizmos.DrawLine(from, to);
                Gizmos.DrawSphere(from, 0.08f);
            }
        }
    }
}
