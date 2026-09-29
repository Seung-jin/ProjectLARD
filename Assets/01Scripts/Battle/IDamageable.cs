using LAD.Stat;

namespace LAD.Battle {
    /// <summary>
    /// 전투 중 대미지를 받을 수 있는 대상이 구현하는 계약입니다.
    /// </summary>
    public interface IDamageable {
        /// <summary>
        /// 버프와 디버프가 반영된 대상의 스탯을 관리합니다.
        /// </summary>
        StatController StatController { get; }

        /// <summary>
        /// 계산이 완료된 대미지를 대상에게 적용합니다.
        /// </summary>
        void TakeDamage(float damage);
    }
}
