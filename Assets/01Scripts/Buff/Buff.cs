using System.Threading;
using UnityEngine;

namespace LARD.Buff {
    /// <summary>
    /// 버프의 종류, 적용 주체, 효과 값과 만료 시각을 보관한다.
    /// </summary>
    public sealed class Buff {
        public BuffType Type { get; }
        public int SourceObjectId { get; }
        public float ValuePercent { get; private set; }
        public float ExpiresAt { get; private set; }

        internal CancellationTokenSource ExpirationCancellation { get; set; }

        internal Buff(BuffType type, int sourceObjectId, float valuePercent, float durationSeconds) {
            Type = type;
            SourceObjectId = sourceObjectId;
            Refresh(valuePercent, durationSeconds);
        }

        internal void Refresh(float valuePercent, float durationSeconds) {
            ValuePercent = valuePercent;
            ExpiresAt = Time.time + durationSeconds;
        }
    }
}
