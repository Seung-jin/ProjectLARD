using Cysharp.Threading.Tasks;

namespace LARD.Data {
	/// <summary>
	/// 게임 데이터셋이 비동기 로딩에 참여하기 위한 공통 계약이다.
	/// </summary>
	public interface IDataSet {
		/// <summary>데이터셋의 에셋 또는 런타임 데이터를 준비한다.</summary>
		UniTask Load();
	}
}
