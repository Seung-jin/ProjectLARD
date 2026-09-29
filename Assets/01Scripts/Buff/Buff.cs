using System;
using System.Collections.Generic;
using LAD.Stat;
using UnityEngine;

namespace LAD.Buff {
    [Serializable]
    public sealed class Buff {
        [SerializeField] private string id;
        [SerializeField] private BuffType type;
        [SerializeField, Min(0f)] private float duration;
        [SerializeField] private List<StatModifier> statModifiers = new List<StatModifier>();
        [SerializeField] private LAD.Effects.CrowdControlType crowdControl;

        public string Id => id;
        public BuffType Type => type;
        public float Duration => duration;
        public IReadOnlyList<StatModifier> StatModifiers => statModifiers;
        public LAD.Effects.CrowdControlType CrowdControl => crowdControl;

        public Buff(string id, BuffType type, float duration, IEnumerable<StatModifier> statModifiers,
                    LAD.Effects.CrowdControlType crowdControl = LAD.Effects.CrowdControlType.None) {
            this.crowdControl = crowdControl;
            this.id = id;
            this.type = type;
            this.duration = Mathf.Max(0f, duration);
            this.statModifiers = statModifiers != null
                ? new List<StatModifier>(statModifiers)
                : new List<StatModifier>();
        }
    }
}
