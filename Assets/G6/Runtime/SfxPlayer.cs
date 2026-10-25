using UnityEngine;

namespace G6
{
    /// <summary>
    /// Geri bildirim sesleri. Ses dosyası kullanılmaz; sesler sinüs dalgalarından çalışma anında üretilir,
    /// böylece lisans sorunu da olmaz.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        const int Rate = 44100;

        AudioSource source;
        AudioClip found, tap, correct, wrong, whoosh, lost;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            found = Tones("found", 0.09f, 523f, 659f, 784f, 1047f);
            tap = Tones("tap", 0.05f, 880f);
            correct = Tones("correct", 0.1f, 784f, 1175f);
            wrong = Tones("wrong", 0.16f, 220f, 165f);
            lost = Tones("lost", 0.1f, 659f, 440f);
            whoosh = Sweep("whoosh", 0.35f, 180f, 900f);
        }

        public void Found() => source.PlayOneShot(found, 0.6f);
        public void Tap() => source.PlayOneShot(tap, 0.5f);
        public void Correct() => source.PlayOneShot(correct, 0.6f);
        public void Wrong() => source.PlayOneShot(wrong, 0.6f);
        public void Lost() => source.PlayOneShot(lost, 0.4f);
        public void Whoosh() => source.PlayOneShot(whoosh, 0.5f);

        /// <summary>Art arda çalınan kısa notalar (arpej).</summary>
        static AudioClip Tones(string name, float noteLength, params float[] freqs)
        {
            int perNote = (int)(Rate * noteLength);
            var data = new float[perNote * freqs.Length];
            for (int n = 0; n < freqs.Length; n++)
            for (int i = 0; i < perNote; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, i / 200f) * (1f - (float)i / perNote); // tık sesini önleyen zarf
                data[n * perNote + i] = Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * env * 0.5f;
            }
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Sweep(string name, float length, float from, float to)
        {
            int count = (int)(Rate * length);
            var data = new float[count];
            float phase = 0f;
            var rng = new System.Random(6);
            for (int i = 0; i < count; i++)
            {
                float k = (float)i / count;
                phase += 2f * Mathf.PI * Mathf.Lerp(from, to, k * k) / Rate;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                float env = Mathf.Sin(Mathf.PI * k);
                data[i] = (Mathf.Sin(phase) * 0.3f + noise * 0.15f) * env;
            }
            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
