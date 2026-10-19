using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LARD.Buff {
    /// <summary>
    /// 버프를 종류와 적용 주체별로 관리하고 갱신, 합산 및 만료를 처리한다.
    /// </summary>
    public sealed class BuffController {
        private readonly Dictionary<(BuffType Type, int SourceObjectId), Buff> _buffs = new();

        /// <summary>같은 종류와 적용 주체의 버프를 갱신하고 새 만료 시간을 예약한다.</summary>
        public void AddOrRefresh(BuffType type, int sourceObjectId, float valuePercent,
                                 float durationSeconds) {
            if (sourceObjectId <= 0 || valuePercent <= 0f || durationSeconds <= 0f) return;

            var key = (type, sourceObjectId);
            if (!_buffs.TryGetValue(key, out Buff buff)) {
                buff = new Buff(type, sourceObjectId, valuePercent, durationSeconds);
                _buffs.Add(key, buff);
            } else {
                buff.Refresh(valuePercent, durationSeconds);
                buff.ExpirationCancellation?.Cancel();
            }

            var cancellation = new CancellationTokenSource();
            buff.ExpirationCancellation = cancellation;
            ExpireAsync(key, buff, durationSeconds, cancellation).Forget();
        }

        public bool Remove(BuffType type, int sourceObjectId) {
            var key = (type, sourceObjectId);
            if (!_buffs.TryGetValue(key, out Buff buff)) return false;
            _buffs.Remove(key);
            buff.ExpirationCancellation?.Cancel();
            return true;
        }

        /// <summary>만료되지 않은 같은 종류의 버프 효과 값을 합산한다.</summary>
        public float GetTotalValue(BuffType type) {
            float total = 0f;
            foreach (Buff buff in _buffs.Values) {
                if (buff.Type == type && Time.time < buff.ExpiresAt) {
                    total += buff.ValuePercent;
                }
            }
            return total;
        }

        /// <summary>모든 버프와 만료 대기 작업을 정리한다.</summary>
        public void Clear() {
            var buffs = new List<Buff>(_buffs.Values);
            _buffs.Clear();
            foreach (Buff buff in buffs) {
                buff.ExpirationCancellation?.Cancel();
            }
        }

        private async UniTask ExpireAsync((BuffType Type, int SourceObjectId) key, Buff buff,
                                          float durationSeconds, CancellationTokenSource cancellation) {
            try {
                await UniTask.Delay(TimeSpan.FromSeconds(durationSeconds),
                                    cancellationToken: cancellation.Token);
                if (_buffs.TryGetValue(key, out Buff current) && ReferenceEquals(current, buff) &&
                    ReferenceEquals(buff.ExpirationCancellation, cancellation)) {
                    _buffs.Remove(key);
                }
            } catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
                // A refresh, removal, or pool release superseded this expiration.
            } finally {
                if (ReferenceEquals(buff.ExpirationCancellation, cancellation)) {
                    buff.ExpirationCancellation = null;
                }
                cancellation.Dispose();
            }
        }
    }
}
