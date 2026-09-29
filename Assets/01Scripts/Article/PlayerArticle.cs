namespace LAD.Article {
	/// <summary>
	/// 플레이어가 소유한 개별 기물의 런타임 데이터입니다.
	/// </summary>
	public sealed class PlayerArticle {
		public const int MaxGrade = 3;

		public int InstanceId { get; }
		public int ArticleId { get; }
		public int Grade { get; private set; } = 1;
		public ArticleLocation Location { get; private set; } = ArticleLocation.Bench;
		public int BenchSlotIndex { get; private set; } = -1;

		internal PlayerArticle(int instanceId, int articleId) {
			InstanceId = instanceId;
			ArticleId = articleId;
		}

		internal void SetLocation(ArticleLocation location) {
			Location = location;
		}

		internal void SetBenchSlot(int benchSlotIndex) {
			BenchSlotIndex = benchSlotIndex;
		}

		internal void SetGrade(int grade) {
			Grade = grade;
		}
	}
}
