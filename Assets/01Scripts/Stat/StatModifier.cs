using System;
using UnityEngine;

namespace LAD.Stat {
    [Serializable]
    public struct StatModifier {
        [SerializeField] private StatType type;
        [SerializeField] private float flatValue;
        [SerializeField] private float percentValue;

        public StatType Type => type;
        public float FlatValue => flatValue;
        public float PercentValue => percentValue;

        public StatModifier(StatType type, float flatValue, float percentValue = 0f) {
            this.type = type;
            this.flatValue = flatValue;
            this.percentValue = percentValue;
        }
    }
}
