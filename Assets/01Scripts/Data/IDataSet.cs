using Cysharp.Threading.Tasks;

namespace LARD.Data {
	public interface IDataSet {
		UniTask Load();
	}
}