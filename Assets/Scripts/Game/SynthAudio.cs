using UnityEngine;

namespace Warashibe.Game
{
    /// <summary>
    /// Procedurally-synthesised placeholder audio (T-050). Like UiKit's generated rounded sprites, the
    /// sounds are built in code at runtime — no asset files, no .meta, build-safe, fully owned. Tones
    /// follow docs/04 §5's hints (仮素材; a sound designer swaps these for real SE/BGM later).
    /// </summary>
    public static class SynthAudio
    {
        const int Rate = 44100;

        // ---- public: one clip per docs/04 §5 id ----

        public static AudioClip Se(string id)
        {
            switch (id)
            {
                case "se_accept": return AcceptChime();
                case "se_decline": return Decline();
                case "se_stair": return StairTone(0);
                case "se_get": return Pop();
                case "se_bun": return Wing();
                default: return null;
            }
        }

        /// <summary>se_stair rises one scale note per stop cleared (docs/04 §5: 6段＋クリア和音).</summary>
        public static AudioClip StairNote(int step) => StairTone(step);

        public static AudioClip Bgm(string id)
        {
            switch (id)
            {
                case "bgm_michi": return Loop("bgm_michi", MichiMelody());
                case "bgm_ichi": return Loop("bgm_ichi", IchiMelody());
                default: return null;
            }
        }

        // ---- synth core ----

        // Add a decaying tone (sum of harmonics) into buf at the given start sample.
        static void AddTone(float[] buf, int start, float freq, float durSec, float amp, float[] partials, float decay)
        {
            if (start < 0) start = 0;
            int n = Mathf.Min(buf.Length - start, (int)(durSec * Rate));
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-decay * t);        // exponential decay
                float attack = Mathf.Clamp01(t / 0.005f); // 5ms attack (declick)
                float s = 0f;
                for (int h = 0; h < partials.Length; h++)
                    s += partials[h] * Mathf.Sin(2f * Mathf.PI * freq * (h + 1) * t);
                buf[start + i] += s * env * attack * amp;
            }
        }

        static AudioClip FromBuffer(string name, float[] buf)
        {
            for (int i = 0; i < buf.Length; i++) buf[i] = Mathf.Clamp(buf[i], -1f, 1f); // soft clip
            var clip = AudioClip.Create(name, buf.Length, 1, Rate, false);
            clip.SetData(buf, 0);
            return clip;
        }

        static float[] Buffer(float durSec) => new float[Mathf.Max(1, (int)(durSec * Rate))];

        // ---- SE builders (docs/04 §5) ----

        // se_accept 0.8s: 鈴＋琴のアルペジオ上昇（bell-ish rising arpeggio）
        static AudioClip AcceptChime()
        {
            var buf = Buffer(0.85f);
            var bell = new[] { 1f, 0.5f, 0.35f, 0.2f };                 // bell-like harmonics
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };     // C5 E5 G5 C6 (major, up)
            for (int k = 0; k < notes.Length; k++)
                AddTone(buf, (int)(k * 0.12f * Rate), notes[k], 0.5f, 0.5f, bell, 6f);
            return FromBuffer("se_accept", buf);
        }

        // se_decline 0.4s: 低い木魚系（low muted knock — not too negative）
        static AudioClip Decline()
        {
            var buf = Buffer(0.4f);
            AddTone(buf, 0, 146.83f, 0.35f, 0.6f, new[] { 1f, 0.3f }, 22f); // D3, fast decay
            return FromBuffer("se_decline", buf);
        }

        // se_stair 0.5s: 旅の進行で1音ずつ上がる（pentatonic rising per step）
        static AudioClip StairTone(int step)
        {
            float[] scale = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 987.77f, 1046.5f }; // C D E G A B C
            float freq = scale[Mathf.Clamp(step, 0, scale.Length - 1)];
            var buf = Buffer(0.5f);
            AddTone(buf, 0, freq, 0.45f, 0.5f, new[] { 1f, 0.4f, 0.15f }, 7f);
            return FromBuffer("se_stair", buf);
        }

        // se_get 0.3s: ポン(鼓)（drum pop）
        static AudioClip Pop()
        {
            var buf = Buffer(0.3f);
            AddTone(buf, 0, 196f, 0.25f, 0.7f, new[] { 1f, 0.5f }, 30f); // G3 quick thump
            return FromBuffer("se_get", buf);
        }

        // se_bun 0.3s: 羽音を可愛く加工（cute wing flutter = tremolo buzz）
        static AudioClip Wing()
        {
            var buf = Buffer(0.3f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-8f * t) * Mathf.Clamp01(t / 0.005f);
                float trem = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 40f * t); // 40Hz flutter
                buf[i] = Mathf.Sin(2f * Mathf.PI * 320f * t) * trem * env * 0.4f;
            }
            return FromBuffer("se_bun", buf);
        }

        // ---- BGM (short looping placeholder; real 60-90s tracks come later) ----

        static AudioClip Loop(string name, float[] buf)
        {
            var clip = AudioClip.Create(name, buf.Length, 1, Rate, false);
            clip.SetData(buf, 0);
            return clip;
        }

        static float[] MichiMelody() // 道中: gentle pentatonic (flute-ish)
        {
            float[] mel = { 523.25f, 587.33f, 659.25f, 587.33f, 523.25f, 440f, 523.25f, 587.33f };
            return Melody(mel, 0.6f, new[] { 1f, 0.15f }, 0.18f);
        }

        static float[] IchiMelody() // 出会い/市: brighter (shamisen-ish)
        {
            float[] mel = { 659.25f, 587.33f, 523.25f, 587.33f, 659.25f, 783.99f, 659.25f, 523.25f };
            return Melody(mel, 0.5f, new[] { 1f, 0.5f, 0.25f }, 0.16f);
        }

        static float[] Melody(float[] notes, float noteDur, float[] partials, float amp)
        {
            var buf = new float[(int)(notes.Length * noteDur * Rate)];
            for (int k = 0; k < notes.Length; k++)
                AddTone(buf, (int)(k * noteDur * Rate), notes[k], noteDur * 0.95f, amp, partials, 2.5f);
            return buf;
        }
    }
}
