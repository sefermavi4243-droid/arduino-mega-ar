using System.Collections.Generic;
using UnityEngine;

namespace G6.EditorTools
{
    /// <summary>
    /// Arduino Mega 2560 Rev3 dijital ikizi. Parça konumları Arduino'nun açık kaynak tasarım dosyasından
    /// (Arduino_MEGA_2560-Rev3.brd, arduino-mega2560_R3-reference-design.zip) milimetre olarak okundu.
    /// Kart koordinatı: x 0..101.6 (USB tarafı 0), y 0..53.34 (güç/analog pinleri 0), z kartın üstünden yükseklik.
    /// Unity'de kart masada yatar: X = x - 50.8, Z = y - 26.67 (+Z = PWM pinlerinin olduğu kenar), Y = yükseklik.
    /// Bileşen gövde ölçüleri üretici veri sayfalarından yaklaşık alınmıştır.
    /// </summary>
    static class ArduinoMegaModel
    {
        const float Pcb = 1.6f;        // kart kalınlığı (mm)
        const float Header = 8.5f;     // dişi pin başlığı yüksekliği (mm)

        public static ObjectProfile Profile() => new ObjectProfile
        {
            DisplayName = "Arduino Mega",
            DatasetName = "G6_ArduinoMega",
            TargetName = "ArduinoMega",
            FileStem = "arduino_mega",
            BodyColor = new Color(0.0f, 0.45f, 0.58f),
            SimDistance = 0.17f, SimPitch = 55f, SimYaw = 12f,
            // Kart yassı olduğu için kamera hep yukarıdan, eğik bakar.
            GuideViews = new[]
            {
                ("UstOn", "Üstten, pinler karşıda", new Vector3(0f, 1.6f, -1f)),
                ("UsbTarafi", "USB tarafından", new Vector3(-1f, 1.3f, -0.2f)),
                ("PinTarafi", "Dijital pin tarafından", new Vector3(1f, 1.3f, 0.2f)),
                ("UstArka", "Üstten, pinler önde", new Vector3(0f, 1.6f, 1f)),
                ("Capraz", "Çapraz, USB köşesinden", new Vector3(-0.8f, 1.4f, -0.8f)),
            },
            Parts = Parts(),
            Pins = ArduinoMegaPins.All(),
            EnergyPaths = ArduinoMegaPins.EnergyPaths(),
            BlinkPath = ArduinoMegaPins.BlinkPath(),
            Pin13 = ArduinoMegaPins.Pin13,
            PinGnd = ArduinoMegaPins.PinGnd,
            HasBlink = true,
        };

        // Başta etiketi görünen ana bölgeler; diğerleri dokununca açılır.
        static readonly string[] PrimaryIds = { "mcu", "usb_b", "dc_jak", "dijital", "pwm", "analog", "guc_pin" };

        static Vector3 Center(float x0, float x1, float y0, float y1, float h, float z0 = Pcb) =>
            new Vector3(((x0 + x1) / 2f - 50.8f) * 0.001f, (z0 + h / 2f) * 0.001f, ((y0 + y1) / 2f - 26.67f) * 0.001f);

        static Vector3 Size(float x0, float x1, float y0, float y1, float h) =>
            new Vector3((x1 - x0) * 0.001f, h * 0.001f, (y1 - y0) * 0.001f);

        static void Box(PartDef p, float x0, float x1, float y0, float y1, float h, float z0 = Pcb) =>
            p.Shapes.Add((PrimitiveType.Cube, Center(x0, x1, y0, y1, h, z0), Vector3.zero, Size(x0, x1, y0, y1, h)));

        /// <summary>Dikey silindir (elektrolitik kondansatör).</summary>
        static void Can(PartDef p, float x, float y, float diameter, float h) =>
            p.Shapes.Add((PrimitiveType.Cylinder, Center(x, x, y, y, h), Vector3.zero,
                new Vector3(diameter * 0.001f, h * 0.0005f, diameter * 0.001f)));

        static PartDef Part(string id, string title, string desc, string fact, Color color, string mat) => new PartDef
        {
            Id = id, Title = title, Description = desc, FunFact = fact, Color = color, RealMaterial = mat
        };

        static List<PartDef> Parts()
        {
            var parts = new List<PartDef>();

            var mcu = Part("mcu", "ATmega2560",
                "Kartın beyni: 256 KB flash, 8 KB SRAM ve 4 KB EEPROM belleğe sahip 8 bitlik mikrodenetleyici. Yazdığın kod burada çalışır.",
                "16 MHz saatle çalışır; saniyede yaklaşık 16 milyon komut işleyebilir.",
                new Color(0.25f, 0.9f, 1f), "Siyah");
            Box(mcu, 43.8f, 57.8f, 21.35f, 35.35f, 1.2f);
            parts.Add(mcu);

            var usb = Part("usb_b", "USB-B Girişi",
                "Bilgisayara bağlanan kare uçlu giriş. Hem kodu karta yükler hem de karta 5 V güç verir.",
                "Kartın üzerindeki 500 mA'lik sigorta, bilgisayarın USB portunu kısa devreye karşı korur.",
                new Color(1f, 0.75f, 0.25f), "Metal");
            Box(usb, -6.35f, 9.9f, 32.1f, 44.1f, 10.9f);
            parts.Add(usb);

            var dc = Part("dc_jak", "Güç Jakı",
                "Harici adaptör girişi. 7–12 V önerilir; kart üzerindeki regülatör bunu 5 V'a düşürür.",
                "USB ve adaptör birlikte takılıysa kart, karşılaştırıcı devre sayesinde otomatik olarak adaptörü seçer.",
                new Color(1f, 0.5f, 0.25f), "Siyah");
            Box(dc, -1.8f, 12.4f, 3.9f, 12.9f, 11f);
            parts.Add(dc);

            var u2 = Part("usb_cip", "USB-Seri Çip",
                "USB ile ana işlemci arasında tercüman: bilgisayarın USB sinyallerini seri haberleşmeye çevirir. Orijinal kartta ATmega16U2, çoğu klon kartta CH340G çipi bulunur.",
                "Klon kartlar için bilgisayara CH340 sürücüsü kurmak gerekebilir; orijinal kart sürücüsüz tanınır.",
                new Color(0.65f, 0.5f, 1f), "Siyah");
            Box(u2, 17.44f, 22.44f, 32.3f, 37.3f, 1f);
            parts.Add(u2);

            var dig = Part("dijital", "Dijital Pinler",
                "Çift sıra dijital giriş/çıkış pinleri. LED, düğme, röle gibi açık/kapalı çalışan elemanlar buraya bağlanır.",
                "Mega'da toplam 54 dijital pin var; Uno'da yalnızca 14.",
                new Color(0.55f, 1f, 0.35f), "Siyah");
            Box(dig, 92.71f, 97.79f, 6.35f, 52.07f, Header);
            parts.Add(dig);

            var pwm = Part("pwm", "PWM / I2C",
                "2–13 numaralı pinler PWM ile ayarlanabilir çıkış verir (LED parlaklığı, motor hızı). SDA/SCL ise I2C haberleşme hattıdır.",
                "Mega'da 15 PWM pini vardır; analogWrite() yalnızca bu pinlerde çalışır.",
                new Color(1f, 0.35f, 0.85f), "Siyah");
            Box(pwm, 17.53f, 42.93f, 49.53f, 52.07f, Header);
            Box(pwm, 44.45f, 64.77f, 49.53f, 52.07f, Header);
            parts.Add(pwm);

            var serial = Part("seri", "Seri Portlar",
                "TX/RX pin çiftleri. Mega'nın 4 ayrı donanım seri portu var: Serial, Serial1, Serial2, Serial3.",
                "Pin 0 ve 1 USB ile paylaşılır; kod yüklerken bunlara bir şey bağlıysa yükleme başarısız olabilir.",
                new Color(0.4f, 0.65f, 1f), "Siyah");
            Box(serial, 67.31f, 87.63f, 49.53f, 52.07f, Header);
            parts.Add(serial);

            var analog = Part("analog", "Analog Girişler",
                "16 analog giriş (A0–A15). Sensörden gelen 0–5 V gerilimi analogRead() ile sayıya çevirir.",
                "10 bit çözünürlük: 0–5 V aralığını 1024 basamağa böler, yani yaklaşık 4,9 mV hassasiyet.",
                new Color(0.25f, 1f, 0.75f), "Siyah");
            Box(analog, 49.53f, 69.85f, 1.27f, 3.81f, Header);
            Box(analog, 72.39f, 92.71f, 1.27f, 3.81f, Header);
            parts.Add(analog);

            var power = Part("guc_pin", "Güç Pinleri",
                "Devrene enerji dağıtan pinler: 5V, 3.3V, GND, Vin, IOREF ve RESET.",
                "3.3 V çıkışı en fazla 50 mA verir; güçlü modüller için yetmeyebilir.",
                new Color(1f, 0.3f, 0.35f), "Siyah");
            Box(power, 26.67f, 46.99f, 1.27f, 3.81f, Header);
            parts.Add(power);

            var reset = Part("reset", "Reset Düğmesi",
                "Basınca program en baştan başlar. Kod silinmez, yalnızca yeniden çalışır.",
                "Seri monitörü açmak da kartı otomatik olarak resetler.",
                new Color(1f, 0.9f, 0.4f), "MetalParlak");
            Box(reset, 72.4f, 75.9f, 24.9f, 31f, 2.5f);
            parts.Add(reset);

            var reg = Part("guc_devre", "Regülatör",
                "NCP1117 regülatörü adaptör gerilimini sabit 5 V'a indirir; yanındaki iki büyük kondansatör dalgalanmayı süzer.",
                "12 V üstü girişte regülatör ısınır: fazla gerilim ısıya dönüşür.",
                new Color(0.9f, 0.55f, 1f), "Siyah");
            Box(reg, 6.9f, 10.4f, 14.5f, 21f, 1.6f);
            Can(reg, 18.415f, 9.144f, 6.3f, 7.7f);
            Can(reg, 25.527f, 9.144f, 6.3f, 7.7f);
            parts.Add(reg);

            var xtal = Part("kristal", "16 MHz Kristal",
                "ATmega16U2'nin saat kaynağı. Ana işlemcinin yanında da daha küçük bir 16 MHz seramik rezonatör bulunur.",
                "delay(1000) gibi zaman fonksiyonları bu saat sinyalinin vuruşlarını sayarak çalışır.",
                new Color(0.75f, 0.85f, 1f), "Metal");
            Box(xtal, 12.7f, 24.2f, 23.7f, 28.4f, 3.6f);
            parts.Add(xtal);

            var icsp = Part("icsp", "ICSP Pinleri",
                "Bilgisayar programı olmadan, doğrudan programlama için 6 pinli başlık. Bootloader yüklemek için kullanılır.",
                "Kartta iki tane var: biri ATmega2560, diğeri ATmega16U2 için.",
                new Color(0.5f, 1f, 1f), "Siyah");
            Box(icsp, 62.36f, 67.44f, 24.13f, 31.75f, 2.5f);
            Box(icsp, 14.48f, 22.1f, 43.69f, 48.77f, 2.5f);
            parts.Add(icsp);

            var led = Part("led", "Durum LED'leri",
                "ON: kart enerjili. L: 13. pine bağlıdır, Blink örneğinde yanıp söner. TX/RX: seri veri giderken ve gelirken yanar.",
                "L LED'i kendi kodunla da kontrol edilebilir: digitalWrite(13, HIGH).",
                new Color(1f, 1f, 0.5f), "MetalParlak");
            Box(led, 75.6f, 76.8f, 35.8f, 37.9f, 0.8f);
            Box(led, 27.3f, 28.6f, 33.2f, 43.2f, 0.8f);
            parts.Add(led);

            var pcb = Part("pcb", "PCB Kartı",
                "Bileşenleri taşıyan ve bakır yollarla birbirine bağlayan kart. Boyutu 101,6 × 53,3 mm.",
                "Arduino'nun tasarım dosyaları açık kaynaktır; bu uygulamadaki 3B modelin parça konumları da o dosyadan okundu.",
                new Color(0.3f, 0.75f, 1f), "PCB");
            Box(pcb, 0f, 101.6f, 0f, 53.34f, Pcb, 0f);
            parts.Add(pcb);

            // Patlatma ve etiket yönleri: parçalar kartın merkezinden dışarı ve farklı yüksekliklere kalkar.
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                p.Primary = System.Array.IndexOf(PrimaryIds, p.Id) >= 0;
                Vector3 c = p.Shapes[0].pos;
                if (p.Id == "pcb")
                {
                    p.Explode = new Vector3(0, -0.02f, 0);
                    p.LabelDir = new Vector3(-1f, -0.3f, -0.3f);
                    continue;
                }
                p.Explode = new Vector3(c.x * 0.35f, 0.03f + 0.006f * (i % 4), c.z * 0.35f);
                p.LabelDir = new Vector3(c.x, 0.03f, c.z).normalized + Vector3.up * 0.4f;
            }
            return parts;
        }
    }
}
