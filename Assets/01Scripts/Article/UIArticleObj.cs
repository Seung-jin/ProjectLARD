using UnityEngine;

namespace LARD.Article {
    public sealed class UIArticleObj : MonoBehaviour {
        [SerializeField] private TextMesh _raceText;
        [SerializeField] private TextMesh _tierText;

        public void SetInfo(ArticleRace race, int tier) {
            _raceText.text = race.ToString();
            _tierText.text = $"Tier {tier}";
        }

        public void Clear() {
            _raceText.text = string.Empty;
            _tierText.text = string.Empty;
        }
    }
}
