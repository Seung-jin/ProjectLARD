namespace LARD.Skill {
    public abstract class SkillObjBase : PoolingObject<SkillObjBase> {
        public uint LeaseId { get; private set; }

        public override void OnGetFromPool() {
            LeaseId++;
            base.OnGetFromPool();
        }
    }
}
