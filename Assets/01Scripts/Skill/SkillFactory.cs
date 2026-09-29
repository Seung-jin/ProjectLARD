using System.Collections.Generic;
using LAD.Article;
using UnityEngine;

namespace LAD.Skill {
	/// <summary>스킬 실행 인스턴스 생성과 스킬 오브젝트 풀을 관리합니다.</summary>
	public sealed class SkillFactory {
		private readonly Dictionary<string, ObjectPooler<SkillObjBase>> objectPools = new(System.StringComparer.Ordinal);
		private readonly Dictionary<string, SkillObjBase> registeredPrefabs = new(System.StringComparer.Ordinal);
		private readonly Transform poolRoot;

		public SkillFactory(Transform poolRoot = null) {
			this.poolRoot = poolRoot;
		}

		public SkillBase Create(
			ArticleObj owner,
			ArticleAttackComponent attackComponent,
			SkillModel model,
			int articleGrade) {
			if (owner == null || model == null || !model.TryGetGrade(articleGrade, out SkillGradeData gradeData)) {
				return null;
			}

			SkillBase skill = CreateImplementation(model.Id);
			if (skill == null) {
				Debug.LogError($"SkillFactory에 Skill {model.Id} 구현이 등록되지 않았습니다.", owner);
				return null;
			}

			skill.Initialize(owner, attackComponent, model, gradeData, this);
			return skill;
		}

		private static SkillBase CreateImplementation(int skillId) {
			return skillId switch {
				1 => new Article1NiaSkill(),
				2 => new Article2SashaSkill(),
				3 => new Article3AdeleSkill(),
				4 => new Article4MysticSkill(),
				5 => new Article5NeriaSkill(),
				6 => new Article6ZakraSkill(),
				7    => new Article7LumerusSkill(),
				8    => new Article8GinSkill(),
				11   => new Article11InannaSkill(),
				10   => new Article10GingerwhaleSkill(),
				9    => new Article9WeiSkill(),
				12   => new Article12DohwagaSkill(),
				13   => new Article13LangeSkill(),
				14   => new Article14AkatesSkill(),
				15   => new Article15PriunaSkill(),
				16   => new Article16NaberSkill(),
				17   => new Article17AirdalinSkill(),
				18   => new Article18SillianSkill(),
				19   => new Article19AlbionSkill(),
				20   => new Article20VelganosSkill(),
				21   => new Article21AzakielSkill(),
				22   => new Article22YuzSkill(),
				23   => new Article23KaysarSkill(),
				24   => new Article24TianSkill(),
				25   => new Article25RahartSkill(),
				26   => new Article26GustoSkill(),
				27   => new Article27SienSkill(),
				28   => new Article28AzenaSkill(),
				29   => new Article29LaurielSkill(),
				30   => new Article30BahunTurSkill(),
				31   => new Article31PahuSkill(),
				32   => new Article32GuardianLuSkill(),
				33   => new Article33GalaturSkill(),
				34   => new Article34BeatriceSkill(),
				35   => new Article35PriestAmanSkill(),
				36   => new Article36LuteranSkill(),
				37   => new Article37AzenaInannaSkill(),
				38   => new Article38KadanSkill(),
				39   => new Article39ShandiSkill(),
				40   => new Article40NinavSkill(),
				41   => new Article41EvergraceSkill(),
				42   => new Article42DemonAmanSkill(),
				1026 => new Article26GustoBasicAttack(),
				1027 => new Article27SienBasicAttack(),
				1028 => new Article28AzenaBasicAttack(),
				1029 => new Article29LaurielBasicAttack(),
				1030 => new Article30BahunTurBasicAttack(),
				1031 => new Article31PahuBasicAttack(),
				1032 => new Article32GuardianLuBasicAttack(),
				1033 => new Article33GalaturBasicAttack(),
				1034 => new Article34BeatriceBasicAttack(),
				1035 => new Article35PriestAmanBasicAttack(),
				1036 => new Article36LuteranBasicAttack(),
				1037 => new Article37AzenaInannaBasicAttack(),
				1038 => new Article38KadanBasicAttack(),
				1039 => new Article39ShandiBasicAttack(),
				1040 => new Article40NinavBasicAttack(),
				1041 => new Article41EvergraceBasicAttack(),
				1042 => new Article42DemonAmanBasicAttack(),
				1001 => new Article1NiaBasicAttack(),
				1002 => new Article2SashaBasicAttack(),
				1003 => new BasicAttackSkill(DamageArea.SingleTarget, Stat.StatType.Strength, Battle.DamageType.Physical),
				1004 => new Article4MysticBasicAttack(),
				1005 => new Article5NeriaBasicAttack(),
				1006 => new Article6ZakraBasicAttack(),
				1007 => new Article7LumerusBasicAttack(),
				1008 => new Article8GinBasicAttack(),
				1010 => new Article10GingerwhaleBasicAttack(),
				1009 => new Article9WeiBasicAttack(),
				1011 => new Article11InannaBasicAttack(),
				1012 => new Article12DohwagaBasicAttack(),
				1013 => new Article13LangeBasicAttack(),
				1014 => new Article14AkatesBasicAttack(),
				1015 => new Article15PriunaBasicAttack(),
				1016 => new Article16NaberBasicAttack(),
				1022 => new Article22YuzBasicAttack(),
				_    => null,
			};
		}

		internal SkillObjBase Spawn(SkillObjBase prefab, Vector3 position, Quaternion rotation) {
			if (prefab == null) {
				return null;
			}

			string prefabName = prefab.name;
			if (string.IsNullOrWhiteSpace(prefabName)) {
				Debug.LogError("이름이 없는 스킬 프리팹은 풀에 등록할 수 없습니다.", prefab);
				return null;
			}

			if (registeredPrefabs.TryGetValue(prefabName, out SkillObjBase registeredPrefab) &&
			    registeredPrefab != prefab) {
				Debug.LogError(
				               $"서로 다른 스킬 프리팹이 같은 이름 '{prefabName}'을 사용하고 있습니다. " +
				               "스킬 오브젝트 프리팹 이름은 고유해야 합니다.",
				               prefab);
				return null;
			}

			if (!objectPools.TryGetValue(prefabName, out ObjectPooler<SkillObjBase> pool)) {
				pool = new ObjectPooler<SkillObjBase>(prefab, poolRoot);
				objectPools.Add(prefabName, pool);
				registeredPrefabs.Add(prefabName, prefab);
			}

			SkillObjBase skillObject = pool.Get();
			skillObject.PrepareForSpawn(poolRoot, position, rotation);
			return skillObject;
		}

		public void Clear() {
			foreach (ObjectPooler<SkillObjBase> pool in objectPools.Values) {
				pool.Clear();
			}

			objectPools.Clear();
			registeredPrefabs.Clear();
		}
	}
}
