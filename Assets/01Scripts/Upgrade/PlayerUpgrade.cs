using System;
using LARD.Article;

namespace LARD.Upgrade {
    public sealed class PlayerUpgrade {
        public ArticleRace Race { get; }
        public int Level { get; private set; } = 0;

        public PlayerUpgrade(ArticleRace race) {
            Race = race;
        }

        public void SetLevel(int level) {
            if (level < 0) {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            Level = level;
        }
    }
}
