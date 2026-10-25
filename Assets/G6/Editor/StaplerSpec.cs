using UnityEngine;

namespace G6.EditorTools
{
    /// <summary>
    /// Zımba makinesinin ölçüleri (metre). Kendi zımbanızı cetvelle ölçüp bu değerleri girin,
    /// sonra G6 > Sahneyi Kur. Model Target için dijital ikiz gerçek nesneyle aynı boyutta olmalıdır.
    /// Varsayılanlar G6'nın zımbası: Gipta F0556 (mavi).
    /// </summary>
    [CreateAssetMenu(menuName = "G6/Zımba Ölçüleri", fileName = "ZimbaOlculeri")]
    public class StaplerSpec : ScriptableObject
    {
        [Tooltip("Önden arkaya toplam uzunluk")] public float length = 0.12f;
        [Tooltip("Soldan sağa genişlik")] public float width = 0.036f;
        [Tooltip("Masadan üst kolun tepesine yükseklik")] public float height = 0.05f;
        [Tooltip("Gövde rengi (simülasyondaki zımba ve Model Target önizlemesi)")] public Color bodyColor = new Color(0.08f, 0.35f, 0.85f);
    }
}
