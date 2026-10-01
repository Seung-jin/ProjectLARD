using UnityEngine;

namespace LARD.Shop {
    [CreateAssetMenu(fileName = "ShopConstModel", menuName = "Data/Shop/Constants")]
    public sealed class ShopConstModel : ScriptableObject {
        public int Id;

        [Min(0)] public int PurchaseCost;
    }
}
