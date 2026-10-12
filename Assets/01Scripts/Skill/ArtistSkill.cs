using System.Collections.Generic;
using LARD.Monster;
using UnityEngine;

namespace LARD.Skill {
    public sealed class ArtistSkill : SkillBase {
        public const int SkillId = 4;
        private const float GroundLength = 3f;
        private const float StartOffset = 0.5f;
        private const float GroundLifetime = 3f;

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
            if (Owner == null || Owner.Model == null || Model == null || Model.Effects == null ||
                Model.SkillObjectPrefab is not ObjSkillGround prefab || Factory == null) {
                return false;
            }

            foreach (IAttackTarget candidate in FindTargetUtil.FindTargets(Owner.transform.position, Owner.Model.Range)) {
                if (candidate is not MonsterObj monster || !monster.IsAlive ||
                    monster.MoveComponent == null ||
                    !monster.MoveComponent.TryGetStraightDirection(GroundLength + StartOffset,
                        out Vector2 direction, out float width)) {
                    continue;
                }

                Vector2 center = (Vector2)monster.transform.position +
                                 direction * (StartOffset + GroundLength * 0.5f);
                ObjSkillGround zone = Factory.SpawnObject(prefab, center,
                    Quaternion.FromToRotation(Vector3.right, direction));
                zone.Initialize(center, direction, GroundLength, width, GroundLifetime, Color.white);
                int ownerId = Owner.Id;
                HashSet<int> hitMonsterIds = new();
                zone.Entered += other => OnGroundEnter(other, ownerId, hitMonsterIds);
                _zones.RemoveAll(existing => existing.Zone == null || existing.Zone.IsInPool ||
                                             existing.Zone.LeaseId != existing.LeaseId);
                _zones.Add(new GroundLease(zone));
                return true;
            }

            return false;
        }

        private void OnGroundEnter(Collider2D other, int ownerId, HashSet<int> hitMonsterIds) {
            MonsterObj monster = other.GetComponentInParent<MonsterObj>();
            if (monster == null || !monster.IsAlive || monster.Id == 0 ||
                Owner == null || Owner.Id != ownerId || Owner.Model == null ||
                Model?.Effects == null || !hitMonsterIds.Add(monster.Id)) {
                return;
            }

            float healthBefore = monster.StatController.CurrentHealth;
            foreach (EffectData data in Model.Effects) {
                if (data == null || data.Timing != EffectApplicationTiming.OnEnter ||
                    data.Target != EffectTargetType.EnteredEnemy ||
                    data.Effect != SkillEffectType.Damage || data.HitCount < 1) {
                    continue;
                }

                SkillEffectFactory.Create(data.Effect)?.Apply(Owner, data, monster);
                break; // A monster takes damage once from each ground object.
            }

            if (!monster.IsAlive || monster.StatController.CurrentHealth >= healthBefore) {
                return;
            }

            foreach (EffectData data in Model.Effects) {
                if (data == null || data.Timing != EffectApplicationTiming.OnDamageDealt ||
                    data.Target != EffectTargetType.DamagedEnemy ||
                    data.Effect != SkillEffectType.DamageTakenIncrease) {
                    continue;
                }

                SkillEffectFactory.Create(data.Effect)?.Apply(Owner, data, monster);
            }
        }

        public override void Cleanup() {
            foreach (GroundLease lease in _zones) {
                if (lease.Zone != null && !lease.Zone.IsInPool && lease.Zone.LeaseId == lease.LeaseId) {
                    lease.Zone.Release();
                }
            }
            _zones.Clear();
        }

        private void OnDisable() => Cleanup();
    }
}
