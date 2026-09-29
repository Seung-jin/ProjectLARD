using System;
using UnityEngine;

namespace LAD.Stat {
    [Serializable]
    public struct StatData {
        [SerializeField, Min(0f)] private float grade1;
        [SerializeField, Min(0f)] private float grade2;
        [SerializeField, Min(0f)] private float grade3;

        public float Grade1 => grade1;
        public float Grade2 => grade2;
        public float Grade3 => grade3;

        public StatData(float grade1, float grade2, float grade3) {
            this.grade1 = Mathf.Max(0f, grade1);
            this.grade2 = Mathf.Max(0f, grade2);
            this.grade3 = Mathf.Max(0f, grade3);
        }

        public float GetValue(int grade) {
            return grade switch {
                2 => grade2,
                3 => grade3,
                _ => grade1,
            };
        }
    }
}
