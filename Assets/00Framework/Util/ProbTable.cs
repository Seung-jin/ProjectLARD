using System;
using System.Collections.Generic;
using System.Linq;

namespace LSJ.Util {
	public class ProbTable<T> {
		private readonly List<T> items = new List<T>();
		private readonly Func<T, int> weightSelector;
		private readonly Random random = new Random();

		public ProbTable(Func<T, int> weightSelector) {
			this.weightSelector = weightSelector ??
			                      throw new ArgumentNullException(nameof(weightSelector));
		}

		public void Add(T item) {
			if (item == null) {
				throw new ArgumentNullException(nameof(item));
			}

			items.Add(item);
		}

		public void AddRange(IEnumerable<T> items) {
			if (items == null) {
				throw new ArgumentNullException(nameof(items));
			}

			foreach (var item in items) {
				Add(item);
			}
		}

		public void Clear() {
			items.Clear();
		}

		public T Pick() {
			if (items.Count == 0) {
				throw new InvalidOperationException("Cannot pick from an empty table.");
			}

			int totalWeight = items.Sum(weightSelector);
			if (totalWeight <= 0) {
				throw new InvalidOperationException("Total weight must be greater than 0.");
			}

			int randomValue = random.Next(totalWeight);
			int currentWeight = 0;

			foreach (var item in items) {
				currentWeight += weightSelector(item);
				if (randomValue < currentWeight) {
					return item;
				}
			}

			return items[items.Count - 1];
		}

		public List<T> PickMultiple(int count, bool allowDuplicates = false) {
			if (count <= 0) {
				throw new ArgumentException("Count must be greater than 0.", nameof(count));
			}

			if (items.Count == 0) {
				throw new InvalidOperationException("Cannot pick from an empty table.");
			}

			var result = new List<T>();
			if (allowDuplicates) {
				for (int i = 0; i < count; i++) {
					result.Add(Pick());
				}
			} else {
				if (count > items.Count) {
					throw new ArgumentException($"Cannot pick {count} unique items from a table with only {items.Count} items.", nameof(count));
				}

				var availableItems = new List<T>(items);
				var availableWeights = new List<int>(items.Select(weightSelector));

				for (int i = 0; i < count; i++) {
					int totalWeight = availableWeights.Sum();
					if (totalWeight <= 0) {
						throw new InvalidOperationException("Total weight must be greater than 0.");
					}

					int randomValue = random.Next(totalWeight);
					int currentWeight = 0;
					int selectedIndex = -1;

					for (int j = 0; j < availableItems.Count; j++) {
						currentWeight += availableWeights[j];
						if (randomValue < currentWeight) {
							selectedIndex = j;
							break;
						}
					}

					if (selectedIndex == -1) {
						selectedIndex = availableItems.Count - 1;
					}

					result.Add(availableItems[selectedIndex]);
					availableItems.RemoveAt(selectedIndex);
					availableWeights.RemoveAt(selectedIndex);
				}
			}

			return result;
		}

		public int Count => items.Count;

		public IReadOnlyList<T> Items => items.AsReadOnly();
	}
}