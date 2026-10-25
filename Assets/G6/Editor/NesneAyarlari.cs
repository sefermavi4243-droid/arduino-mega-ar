using UnityEngine;

namespace G6.EditorTools
{
    /// <summary>
    /// Hangi nesnenin takip edileceğini seçer. Değiştirdikten sonra: G6 > Vuforia > Hepsini hazırla,
    /// ardından Tools/ModelTargetUret.ps1 ile yeni Model Target üretin.
    /// </summary>
    [CreateAssetMenu(menuName = "G6/Nesne Ayarları", fileName = "NesneAyarlari")]
    public class NesneAyarlari : ScriptableObject
    {
        public TrackedObjectKind nesne = TrackedObjectKind.ArduinoMega2560;
    }
}
