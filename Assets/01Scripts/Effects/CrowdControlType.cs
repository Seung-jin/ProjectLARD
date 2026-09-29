using System;

namespace LAD.Effects {
    [Flags]
    public enum CrowdControlType {
        None = 0,
        Slow = 1,
        Stun = 2,
        Root = 4,
    }
}
