using System.Collections.Generic;

namespace LSJ.Util {
	public class Counter {
		public static Counter Instance;

		public static int Next(string key) {
			if (Instance == null) {
				Instance = new Counter();
			}

			return Instance.GetNext(key);
		}

		// instance
		private readonly Dictionary<string, int> _dic = new Dictionary<string, int>();

		public int GetNext(string key) {
			if (_dic.ContainsKey(key) == false) {
				_dic[key] = 1;
				return 1;
			}

			_dic[key]++;
			return _dic[key];
		}
	}
}