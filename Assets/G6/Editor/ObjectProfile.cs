using System.Collections.Generic;
using UnityEngine;

namespace G6.EditorTools
{
    /// <summary>Takip edilecek nesne seçimi. Assets/G6/Data/NesneAyarlari.asset içinden değiştirilir.</summary>
    public enum TrackedObjectKind
    {
        ArduinoMega2560,
        Zimba,
    }

    /// <summary>Bir parçanın tanımı: ad, metinler, renk, patlatma ve etiket yönü, geometri.</summary>
    public class PartDef
    {
        public string Id, Title, Description, FunFact;
        public Color Color;
        public bool Primary = true;   // ana bölge: etiketi baştan görünür
        public Vector3 Explode, LabelDir;
        public string RealMaterial;
        public List<(PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale)> Shapes = new();
        public List<(Mesh mesh, Vector3 pos)> Meshes = new();
    }

    /// <summary>Nesneye özgü her şey: adı, parçaları, Vuforia veritabanı adları, kılavuz açıları, simülasyon kamerası.</summary>
    public class ObjectProfile
    {
        public string DisplayName;   // arayüzde: "Arduino Mega", "Zımba"
        public string DatasetName;   // Vuforia veritabanı dosya adı
        public string TargetName;    // veritabanındaki hedef adı
        public string FileStem;      // ModelTarget/ altındaki dosya adı
        public Color BodyColor;      // DAE önizleme rengi
        public float SimDistance, SimPitch, SimYaw;
        public (string name, string title, Vector3 dir)[] GuideViews;
        public List<PartDef> Parts;

        // İsteğe bağlı zengin özellikler (yalnızca Arduino'da dolu).
        public List<G6.PinMap.Pin> Pins;
        public G6.EnergyFlow.Path[] EnergyPaths;
        public G6.EnergyFlow.Path BlinkPath;
        public Vector3 Pin13, PinGnd;
        public bool HasBlink;
    }
}
