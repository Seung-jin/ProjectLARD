using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Battle;
using UnityEngine;

namespace LARD.Stage {
    /// <summary>기물 조작과 스테이지 전투 로직의 진입점을 제공한다.</summary>
    public static class StageLogic {
        public static ArticleFactory ArticleFactory => StageArticleLogic.ArticleFactory;
        public static int AliveMonsterCount => StageBattleLogic.AliveMonsterCount;

        public static void Init(Map map, float formationGap, List<ArticleObj> selectedArticles,
                                Action<StageModel> startStage, Action<int> stageTimeChanged,
                                Action completeVictory, Func<bool, bool> stageTimeExpired, Action monsterDied) {
            if (selectedArticles == null) throw new ArgumentNullException(nameof(selectedArticles));
            if (startStage == null) throw new ArgumentNullException(nameof(startStage));
            if (stageTimeChanged == null) throw new ArgumentNullException(nameof(stageTimeChanged));
            if (completeVictory == null) throw new ArgumentNullException(nameof(completeVictory));
            if (stageTimeExpired == null) throw new ArgumentNullException(nameof(stageTimeExpired));
            if (monsterDied == null) throw new ArgumentNullException(nameof(monsterDied));

            Dispose();
            StageArticleLogic.Init(map, formationGap, selectedArticles);
            StageBattleLogic.Init(map, startStage, stageTimeChanged, completeVictory, stageTimeExpired, monsterDied);
        }

        public static void Select(ArticleObj article) => StageArticleLogic.Select(article);
        public static void Deselect(ArticleObj article) => StageArticleLogic.Deselect(article);
        public static void ClearSelection() => StageArticleLogic.ClearSelection();
        public static void SelectAt(Vector2 position) => StageArticleLogic.SelectAt(position);
        public static void SelectArea(Vector2 start, Vector2 end) => StageArticleLogic.SelectArea(start, end);

        public static bool TrySummonArticle(int articleId, out ArticleObj article) =>
            StageArticleLogic.TrySummonArticle(articleId, out article);

        public static void MoveSelectedArticles(Vector2 destination) =>
            StageArticleLogic.MoveSelectedArticles(destination);

        public static UniTask RunAsync(CancellationToken cancellationToken) =>
            StageBattleLogic.RunAsync(cancellationToken);

        public static void Dispose() {
            StageBattleLogic.Dispose();
            StageArticleLogic.Dispose();
        }
    }
}
