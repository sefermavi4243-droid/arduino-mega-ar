using System.Collections.Generic;
using UnityEngine;

namespace G6.EditorTools
{
    /// <summary>
    /// Arduino Mega 2560 pin başlıklarının her pini: adı, işlevine göre rengi ve kart üzerindeki konumu.
    /// Başlık merkezleri ve yönleri Arduino_MEGA_2560-Rev3.brd dosyasından, pin sırası kartın üzerindeki
    /// baskı yazılarından alınmıştır. Pin aralığı 2,54 mm.
    /// </summary>
    static class ArduinoMegaPins
    {
        const float Pitch = 2.54f;
        const float Top = 1.6f + 8.5f; // kart + dişi başlık yüksekliği (mm)

        static readonly Color Power = new Color(1f, 0.35f, 0.35f);
        static readonly Color Ground = new Color(0.75f, 0.78f, 0.82f);
        static readonly Color Analog = new Color(0.3f, 1f, 0.6f);
        static readonly Color Pwm = new Color(1f, 0.45f, 0.9f);
        static readonly Color Serial = new Color(0.45f, 0.75f, 1f);
        static readonly Color I2C = new Color(1f, 0.72f, 0.3f);
        static readonly Color Digital = new Color(0.75f, 1f, 0.45f);
        static readonly Color Other = Color.white;

        /// <summary>Kart koordinatını (mm) ikizin yerel uzayına (m) çevirir. Bkz. ArduinoMegaModel.</summary>
        public static Vector3 Board(float x, float y, float h) =>
            new Vector3((x - 50.8f) * 0.001f, h * 0.001f, (y - 26.67f) * 0.001f);

        public static Vector3 Pin13 => Board(30.226f + (4 - 4.5f) * Pitch, 50.8f, Top);
        public static Vector3 PinGnd => Board(30.226f + (3 - 4.5f) * Pitch, 50.8f, Top);

        public static List<G6.PinMap.Pin> All()
        {
            var pins = new List<G6.PinMap.Pin>();

            void Row(string partId, float cx, float y, Vector3 outward, params (string name, Color color)[] names)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.IsNullOrEmpty(names[i].name)) continue; // bağlantısız pin
                    pins.Add(new G6.PinMap.Pin
                    {
                        name = names[i].name,
                        partId = partId,
                        position = Board(cx + (i - (names.Length - 1) / 2f) * Pitch, y, Top),
                        outward = outward,
                        color = names[i].color,
                    });
                }
            }

            var up = Vector3.forward;    // kartın üst kenarı (+Z)
            var down = Vector3.back;

            // Üst kenar, USB tarafından başlayarak.
            Row("pwm", 30.226f, 50.8f, up,
                ("SCL", I2C), ("SDA", I2C), ("AREF", Other), ("GND", Ground), ("13", Pwm),
                ("12", Pwm), ("11", Pwm), ("10", Pwm), ("9", Pwm), ("8", Pwm));
            Row("pwm", 54.61f, 50.8f, up,
                ("7", Pwm), ("6", Pwm), ("5", Pwm), ("4", Pwm), ("3", Pwm), ("2", Pwm), ("TX0 1", Serial), ("RX0 0", Serial));
            Row("seri", 77.47f, 50.8f, up,
                ("14 TX3", Serial), ("15 RX3", Serial), ("16 TX2", Serial), ("17 RX2", Serial),
                ("18 TX1", Serial), ("19 RX1", Serial), ("20 SDA", I2C), ("21 SCL", I2C));

            // Alt kenar.
            Row("guc_pin", 36.83f, 2.54f, down,
                ("", Other), ("IOREF", Other), ("RESET", Other), ("3.3V", Power), ("5V", Power), ("GND", Ground), ("GND", Ground), ("VIN", Power));
            Row("analog", 59.69f, 2.54f, down,
                ("A0", Analog), ("A1", Analog), ("A2", Analog), ("A3", Analog), ("A4", Analog), ("A5", Analog), ("A6", Analog), ("A7", Analog));
            Row("analog", 82.55f, 2.54f, down,
                ("A8", Analog), ("A9", Analog), ("A10", Analog), ("A11", Analog), ("A12", Analog), ("A13", Analog), ("A14", Analog), ("A15", Analog));

            // Sağ kenardaki çift sıra: üstte 5V, altta GND, arada 22–53. İç sütun çift, dış sütun tek numaralı.
            for (int k = 0; k < 18; k++)
            {
                float y = 50.8f - k * Pitch;
                (string, Color) inner, outer;
                if (k == 0) { inner = ("5V", Power); outer = ("5V", Power); }
                else if (k == 17) { inner = ("GND", Ground); outer = ("GND", Ground); }
                else { inner = ((22 + 2 * (k - 1)).ToString(), Digital); outer = ((23 + 2 * (k - 1)).ToString(), Digital); }
                pins.Add(new G6.PinMap.Pin { name = inner.Item1, partId = "dijital", position = Board(93.98f, y, Top), outward = Vector3.left, color = inner.Item2 });
                pins.Add(new G6.PinMap.Pin { name = outer.Item1, partId = "dijital", position = Board(96.52f, y, Top), outward = Vector3.right, color = outer.Item2 });
            }
            return pins;
        }

        static Vector3 Trace(float x, float y) => Board(x, y, 3.1f); // kartın 1,5 mm üstünden akar

        /// <summary>Akım yolları: girişlerden işlemciye, işlemciden çıkışlara. Bileşen konumları .brd dosyasından.</summary>
        public static G6.EnergyFlow.Path[] EnergyPaths() => new[]
        {
            // USB 5 V: USB-B -> sigorta F1 -> anahtar T1 -> işlemci
            new G6.EnergyFlow.Path { color = new Color(0.25f, 0.9f, 1f), delay = 0f, points = new[]
                { Trace(3.81f, 38.1f), Trace(4.445f, 30f), Trace(4.445f, 26.67f), Trace(9.144f, 25.9f), Trace(30f, 25.9f), Trace(43.8f, 28.3f) } },
            // Adaptör: güç jakı -> diyot D1 -> regülatör IC1 -> anahtar T1
            new G6.EnergyFlow.Path { color = new Color(1f, 0.55f, 0.2f), delay = 0.3f, points = new[]
                { Trace(5.334f, 8.382f), Trace(14f, 4.5f), Trace(21.59f, 2.8f), Trace(15f, 12f), Trace(8.636f, 17.78f), Trace(9.144f, 25.9f) } },
            // İşlemciden 5V pinine
            new G6.EnergyFlow.Path { color = new Color(0.4f, 1f, 0.5f), delay = 1f, points = new[]
                { Trace(50.8f, 21.3f), Trace(45f, 10f), Trace(38.1f, 4f) } },
            // İşlemciden dijital pinlere
            new G6.EnergyFlow.Path { color = new Color(0.75f, 1f, 0.45f), delay = 1.2f, points = new[]
                { Trace(57.8f, 28.3f), Trace(85f, 29f), Trace(93.98f, 29.2f) } },
            BlinkPath(),
        };

        /// <summary>İşlemciden 13. pine (Blink LED'i).</summary>
        public static G6.EnergyFlow.Path BlinkPath() => new G6.EnergyFlow.Path
        {
            color = new Color(1f, 0.35f, 0.25f), delay = 0f,
            points = new[] { Trace(48f, 35.4f), Trace(35f, 45f), Trace(28.956f, 49.5f) },
        };
    }
}
