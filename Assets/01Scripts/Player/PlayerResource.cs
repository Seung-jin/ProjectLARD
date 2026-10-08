using System;
using LARD.Stat;

namespace LARD.Player {
    public sealed class PlayerResource {
        public long Silver { get; private set; } = 100;
        public long Gold { get; private set; } = 10;
        public event Action<ResourceType, long> BalanceChanged;

        public long Get(ResourceType type) {
            return type switch {
                ResourceType.Silver => Silver,
                ResourceType.Gold => Gold,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }

        public void Add(ResourceType type, long amount) {
            if (amount < 0) {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Set(type, checked(Get(type) + amount));
        }

        public bool TrySpend(ResourceType type, long amount) {
            if (amount < 0) {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            long balance = Get(type);
            if (balance < amount) {
                return false;
            }

            Set(type, balance - amount);
            return true;
        }

        private void Set(ResourceType type, long amount) {
            if (Get(type) == amount) {
                return;
            }

            switch (type) {
                case ResourceType.Silver:
                    Silver = amount;
                    break;
                case ResourceType.Gold:
                    Gold = amount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            BalanceChanged?.Invoke(type, amount);
        }
    }
}
