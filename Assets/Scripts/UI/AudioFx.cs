// ============================================================
//  AudioFx.cs  -  Sonidos sencillos generados por código
//  (no necesitas archivos de audio). Si luego tienen sonidos
//  de verdad, solo cambien los clips de aquí.
// ============================================================
using UnityEngine;

namespace AstroJunior
{
    public static class AudioFx
    {
        static AudioSource src;
        static AudioClip clip_click, clip_good, clip_bad, clip_alarm;
        public static bool muted = false;

        public static void Init(GameObject host)
        {
            src = host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            clip_click = Tone(700f, 700f, 0.06f);
            clip_good = Tone(520f, 880f, 0.22f);
            clip_bad = Tone(300f, 140f, 0.30f);
            clip_alarm = Tone(900f, 500f, 0.45f);
        }

        static AudioClip Tone(float f0, float f1, float dur)
        {
            int rate = 22050;
            int n = (int)(rate * dur);
            float[] d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                float f = Mathf.Lerp(f0, f1, k);
                phase += 2f * Mathf.PI * f / rate;
                d[i] = Mathf.Sin(phase) * (1f - k) * 0.25f;
            }
            AudioClip c = AudioClip.Create("fx", n, 1, rate, false);
            c.SetData(d, 0);
            return c;
        }

        static void Play(AudioClip c)
        {
            if (muted || src == null || c == null) return;
            src.PlayOneShot(c, 0.8f);
        }

        public static void Click() { Play(clip_click); }
        public static void Good() { Play(clip_good); }
        public static void Bad() { Play(clip_bad); }
        public static void Alarm() { Play(clip_alarm); }
    }
}
