using System.Collections.Generic;
using LARD.Monster;
using UnityEngine;

namespace LARD.Skill {
    public sealed class AeromancerSkill : SkillBase {
        public const int SkillId = 5;
        private const float GroundRadius = 1.5f;
        private const float GroundLifetime = 3f;
        private const float TickInterval = 1f;

        private readonly List<GroundLease> _zones = new();

        private readonly struct GroundLease {
            public readonly ObjSkillGround Zone;
            public readonly uint LeaseId;

            public GroundLease(ObjSkillGround zone) {
                Zone = zone;
                LeaseId = zone.LeaseId;
            }
        }

        public override bool TryUse() {
            if (Owner == null || Owner.Model == null || Model?.Effects == null ||
                Model.SkillObjectPrefab is not ObjSkillGround prefab || Factory == null) {
                return false;
            }

            List<IAttackTarget> targets = FindTargetUtil.FindTargets(Owner.transform.position, Owner.Model.Range);
            if (targets.Count == 0 || targets[0] is not MonsterObj monster || !monster.IsAlive) {
                return false;
            }

            Vector2 center = monster.transform.position;
            ObjSkillGround zone = Factory.SpawnObject(prefab, center, Quaternion.identity);
            zone.InitializeCircle(center, GroundRadius, GroundLifetime, new Color(1f, 0.85f, 0.1f, 0.6f));

            int ownerId = Owner.Id;
            uint leaseId = zone.LeaseId;
            zone.Ticked += () => OnGroundTick(zone, leaseId, ownerId, center);
            zone.StartTicks(TickInterval);

            _zones.RemoveAll(existing => existing.Zone == null || existing.Zone.IsInPool ||
                                         existing.Zone.LeaseId != existing.LeaseId);
            _zones.Add(new GroundLease(zone));
            return true;
        }

        private void OnGroundTick(ObjSkillGround zone, uint leaseId, int ownerId, Vector2 center) {
            if (zone == null || zone.IsInPool || zone.LeaseId != leaseId ||
                Owner == null || Owner.Id != ownerId || Owner.Model == null || Model?.Effects == null) {
                return;
            }

            foreach (IAttackTarget target in FindTargetUtil.FindTargets(center, GroundRadius)) {
                if (target is not MonsterObj monster || !monster.IsAlive) {
                    continue;
                }

                float healthBefore = monster.StatController.CurrentHealth;
                foreach (EffectData data in Model.Effects) {
                    if (data == null || data.Timing != EffectApplicationTiming.OnTick ||
                        data.Target != EffectTargetType.AreaEnemy ||
                        data.Effect != SkillEffectType.Damage || data.HitCount < 1) {
                        continue;
                    }

                    SkillEffectFactory.Create(data.Effect)?.Apply(Owner, data, monster);
                    break;
                }

                if (!monster.IsAlive || monster.StatController.CurrentHealth >= healthBefore) {
                    continue;
                }

                foreach (EffectData data in Model.Effects) {
                    if (data == null || data.Timing != EffectApplicationTiming.OnDamageDealt ||
                        data.Target != EffectTargetType.DamagedEnemy ||
                        data.Effect != SkillEffectType.MoveSpeedDecrease) {
                        continue;
                    }

                    SkillEffectFactory.Create(data.Effect)?.Apply(Owner, data, monster);
                }
            }
        }

        private void OnDisable() {
            foreach (GroundLease lease in _zones) {
                if (lease.Zone != null && !lease.Zone.IsInPool && lease.Zone.LeaseId == lease.LeaseId) {
                    lease.Zone.Release();
                }
            }
            _zones.Clear();
        }
    }
}
