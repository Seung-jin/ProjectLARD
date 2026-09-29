namespace LAD.Skill {
    public enum EffectTargetType {
        Self,
        Enemy,
        Ally,
        NearbyEnemies,
        LowestHealthAlly,
    }

    /// <summary>같은 StackKey를 가진 효과의 수치 중첩 방식입니다.</summary>
    public enum EffectStackPolicy {
        /// <summary>각 적용을 별개의 효과로 처리합니다. 즉시 피해나 개별 DoT에 적합합니다.</summary>
        Independent,
        /// <summary>가장 수치가 큰 효과 하나만 활성화합니다.</summary>
        StrongestOnly,
        /// <summary>하나만 유지하며 재적용 방식은 DurationPolicy를 따릅니다.</summary>
        Single,
        /// <summary>MaxStacks까지만 중첩합니다.</summary>
        Limited,
        /// <summary>중첩 수에 제한을 두지 않습니다.</summary>
        Unlimited,
    }

    /// <summary>이미 적용된 지속 효과가 다시 들어왔을 때의 시간 처리 방식입니다.</summary>
    public enum EffectDurationPolicy {
        /// <summary>지속시간을 새 효과의 Duration으로 갱신합니다.</summary>
        Refresh,
        /// <summary>현재 남은 지속시간을 유지합니다.</summary>
        Keep,
        /// <summary>새 효과의 Duration을 남은 시간에 더합니다.</summary>
        Extend,
        /// <summary>각 중첩이 자신만의 지속시간을 갖습니다.</summary>
        Independent,
    }

    /// <summary>서로 다른 시전자가 같은 효과를 적용할 때의 중첩 범위입니다.</summary>
    public enum EffectStackScope {
        /// <summary>대상에게 적용된 모든 동일 효과를 하나의 중첩 그룹으로 취급합니다.</summary>
        SharedOnTarget,
        /// <summary>시전자마다 별도의 중첩 그룹으로 취급합니다.</summary>
        PerSource,
    }
}
