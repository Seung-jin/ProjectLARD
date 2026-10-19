namespace LARD.Skill {
    /// <summary>
    /// 범위형 스킬 오브젝트가 공유하는 풀링 수명과 대여 식별자를 관리한다.
    /// </summary>
    public abstract class SkillObjBase : PoolingObject<SkillObjBase> {
        public uint LeaseId { get; private set; }

        public override void OnGetFromPool() {
            LeaseId++;
            base.OnGetFromPool();
        }
    }
}
