using System.Collections.Generic;
using LAD.Article;
using LAD.Battle;
using LAD.Buff;
using LAD.Stat;
using UnityEngine;

namespace LAD.Skill {
    internal static class SkillUtility {
        private static readonly IReadOnlyList<string> AllyTags = new[] { "Article" };

        public static float DirectionTo(BattleObj owner, Component target) {
            float direction = Mathf.Sign(target.transform.position.x - owner.transform.position.x);
            return Mathf.Approximately(direction, 0f) ? 1f : direction;
        }

        public static bool DamageSingle(ArticleObj owner, IDamageable target, float damage, DamageType type,
                                        bool notifySkillHit) {
            if (target == null || damage <= 0f) return false;
            float applied = BattleLogic.ApplyDamage(damage, owner, target, type, notifySkillHit ? "스킬" : "기본 공격", notifySkillHit ? AttackCategory.Skill : AttackCategory.BasicAttack);
            if (!notifySkillHit && applied > 0f && !owner.IsDead)
                owner.BasicAttackEnhancements.ApplyHit(target);
            return applied > 0f;
        }

        public static int DamageCircle(ArticleObj owner, Vector2 center, float radius, float damage,
                                       DamageType type, bool notifySkillHit) {
            return DamageColliders(owner, Physics2D.OverlapCircleAll(center, radius), damage, type, notifySkillHit);
        }

        public static int DamageForwardBox(ArticleObj owner, float direction, float length, float width, float damage,
                                           DamageType type, bool notifySkillHit) {
            Vector2 center = (Vector2)owner.transform.position + Vector2.right * (direction * length * 0.5f);
            return DamageColliders(owner, Physics2D.OverlapBoxAll(center, new Vector2(length, width), 0f),
                                   damage, type, notifySkillHit);
        }

        public static int DamageCone(ArticleObj owner, Vector2 direction, float range, float angle,
                                     float damage, DamageType type, bool notifySkillHit) {
            if (owner == null || damage <= 0f) return 0;
            direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
            float halfAngle = Mathf.Clamp(angle, 0f, 360f) * 0.5f;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(owner.transform.position, Mathf.Max(0f, range));
            var targets = new HashSet<BattleObj>();
            int count = 0;
            for (int index = 0; index < colliders.Length; index++) {
                BattleObj target = colliders[index].GetComponentInParent<BattleObj>();
                if (target == null || target == owner || target.IsDead || !target.CompareTag("Monster") ||
                    !targets.Add(target)) continue;
                Vector2 toTarget = (Vector2)target.transform.position - (Vector2)owner.transform.position;
                if (toTarget.sqrMagnitude > range * range ||
                    Vector2.Angle(direction, toTarget) > halfAngle) continue;
                if (BattleLogic.ApplyDamage(damage, owner, target, type,
                    notifySkillHit ? "스킬" : "기본 공격",
                    notifySkillHit ? AttackCategory.Skill : AttackCategory.BasicAttack) <= 0f) continue;
                count++;
                if (!notifySkillHit && !owner.IsDead) owner.BasicAttackEnhancements.ApplyHit(target);
            }
            return count;
        }

        public static int BuffNearbyAllies(ArticleObj owner, float radius, float duration, StatType stat,
                                           float ratio, string buffName, float flatValue = 0f) {
            IReadOnlyList<BattleObj> allies = TargetSearchUtil.Search(owner.transform.position, radius, AllyTags);
            int count = 0;
            for (int i = 0; i < allies.Count; i++) {
                BattleObj ally = allies[i];
                if (ally?.BuffController == null || ally.IsDead) continue;
                ally.BuffController.AddBuff(new LAD.Buff.Buff(
                    $"{buffName}.Source{owner.GetEntityId()}", BuffType.Buff, duration,
                    new[] { new StatModifier(stat, flatValue, ratio) }));
                count++;
            }
            return count;
        }

        private static int DamageColliders(ArticleObj owner, Collider2D[] colliders, float damage,
                                           DamageType type, bool notifySkillHit) {
            var targets = new HashSet<BattleObj>();
            int damagedCount = 0;
            for (int i = 0; i < colliders.Length; i++) {
                BattleObj target = colliders[i].GetComponentInParent<BattleObj>();
                if (target == null || target == owner || target.IsDead ||
                    target.gameObject.tag != "Monster" || !targets.Add(target)) continue;
                float applied = BattleLogic.ApplyDamage(damage, owner, target, type, notifySkillHit ? "스킬" : "기본 공격", notifySkillHit ? AttackCategory.Skill : AttackCategory.BasicAttack);
                if (applied <= 0f) continue;
                damagedCount++;
                if (!notifySkillHit && !owner.IsDead) owner.BasicAttackEnhancements.ApplyHit(target);
            }
            return damagedCount;
        }
    }
}
