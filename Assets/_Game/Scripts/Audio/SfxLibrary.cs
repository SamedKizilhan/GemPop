using UnityEngine;
using static GemPop.Audio.Synth;

namespace GemPop.Audio
{
    public enum Sfx
    {
        CubePop, SpecialCreated, RocketWhoosh, TntBoom, StoneCrack, VaseCrack, VaseBreak,
        DoorBreak, ChaliceClink, ChaliceBoxBreak, Click, Invalid, Win, Fail
    }

    /// <summary>Every sound effect, synthesized once at startup. Each entry is a few lines of signal math.</summary>
    public static class SfxLibrary
    {
        /// <summary>Relative loudness; clips are peak-normalized so this is where the mix is balanced.</summary>
        public static float Volume(Sfx sfx) => sfx switch
        {
            Sfx.CubePop => 0.55f,
            Sfx.SpecialCreated => 0.7f,
            Sfx.RocketWhoosh => 0.8f,
            Sfx.TntBoom => 1.0f,
            Sfx.StoneCrack => 1.0f,
            Sfx.VaseCrack => 0.7f,
            Sfx.VaseBreak => 0.85f,
            Sfx.DoorBreak => 0.9f,
            Sfx.ChaliceClink => 0.7f,
            Sfx.ChaliceBoxBreak => 0.9f,
            Sfx.Click => 0.5f,
            Sfx.Invalid => 0.5f,
            Sfx.Win => 0.9f,
            _ => 0.9f,
        };

        public static AudioClip Build(Sfx sfx) => sfx switch
        {
            Sfx.CubePop         => CubePop(),
            Sfx.SpecialCreated  => Chime("SpecialCreated", 88, 95),           // E6 -> B6
            Sfx.RocketWhoosh    => Whoosh(),
            Sfx.TntBoom         => Boom(),
            Sfx.StoneCrack      => StoneCrack(),
            Sfx.VaseCrack       => Crack("VaseCrack", 0.12f, 0.04f, 0.9f),
            Sfx.VaseBreak       => VaseBreak(),
            Sfx.DoorBreak       => Crack("DoorBreak", 0.3f, 0.1f, 0.25f),
            Sfx.ChaliceClink    => Clink("ChaliceClink", 2637f, 3951f, 0.35f),
            Sfx.ChaliceBoxBreak => Clink("ChaliceBoxBreak", 1760f, 2637f, 0.6f),
            Sfx.Click           => Click(),
            Sfx.Invalid         => Thud(),
            Sfx.Win             => WinJingle(),
            _                   => FailDirge(),
        };

        /// <summary>Soft "plop": a sine whose pitch slides down quickly.</summary>
        private static AudioClip CubePop()
        {
            float phase = 0f;
            return Render("CubePop", 0.16f, (t, i) =>
            {
                float hz = 760f * Mathf.Exp(-t * 7f) + 180f;
                phase += hz / SampleRate;
                return Osc(Wave.Sine, phase) * Decay(t, 0.13f);
            });
        }

        /// <summary>Two quick bell notes.</summary>
        private static AudioClip Chime(string name, int midiA, int midiB)
        {
            float a = MidiToHz(midiA), b = MidiToHz(midiB);
            return Render(name, 0.45f, (t, i) =>
            {
                float s = Osc(Wave.Triangle, t * a) * Decay(t, 0.25f);
                if (t > 0.09f) s += Osc(Wave.Triangle, (t - 0.09f) * b) * Decay(t - 0.09f, 0.35f);
                return s;
            });
        }

        /// <summary>Filtered noise that opens up and fades: a rocket passing by.</summary>
        private static AudioClip Whoosh()
        {
            float low = 0f;
            return Render("RocketWhoosh", 0.45f, (t, i) =>
            {
                float cutoff = Mathf.Lerp(0.04f, 0.5f, t / 0.45f);      // one-pole low-pass opening over time
                low += cutoff * (Noise() - low);
                float env = Mathf.Pow(Mathf.Sin(Mathf.PI * t / 0.45f), 1.5f);
                return low * env;
            });
        }

        /// <summary>Bomb: sharp crack transient, deep pitch-dropping thump, long rumble, soft-clipped for punch.</summary>
        private static AudioClip Boom()
        {
            float phase = 0f, low = 0f, mid = 0f;
            return Render("TntBoom", 1.2f, (t, i) =>
            {
                float n = Noise();
                float crack = n * Decay(t, 0.03f) * 1.5f;                          // initial snap
                float hz = 38f + 140f * Mathf.Exp(-t * 11f);                       // deep thump sliding down
                phase += hz / SampleRate;
                float thump = Osc(Wave.Sine, phase) * Decay(t, 0.55f) * 1.4f;
                low += 0.04f * (n - low);                                          // rumble
                mid += 0.2f * (n - mid);                                           // body
                float rumble = low * Decay(t, 0.9f) * 3f + mid * Decay(t, 0.25f) * 0.8f;
                float s = crack + thump + rumble;
                return (float)System.Math.Tanh(s * 1.6);                           // soft clip = loud and fat
            });
        }

        /// <summary>Rock shattering: a crack, then a quick cascade of smaller debris hits with a stony mid-range tone.</summary>
        private static AudioClip StoneCrack()
        {
            float[] hits = { 0f, 0.035f, 0.06f, 0.095f, 0.13f, 0.18f, 0.24f };   // debris landing
            float[] gains = { 1.5f, 0.9f, 1.1f, 0.7f, 0.8f, 0.5f, 0.4f };
            float low = 0f, band = 0f, phase = 0f;
            return Render("StoneCrack", 0.45f, (t, i) =>
            {
                float n = Noise();
                low += 0.05f * (n - low);                  // rumble (lower corner = deeper)
                band += 0.35f * (n - band);                // stony mid-range
                float mid = band - low;

                float env = 0f;
                for (int k = 0; k < hits.Length; k++)
                    if (t >= hits[k]) env += gains[k] * Decay(t - hits[k], 0.045f);

                float hz = 90f + 50f * Mathf.Exp(-t * 25f);
                phase += hz / SampleRate;
                float thud = Osc(Wave.Sine, phase) * Decay(t, 0.14f) * 1.2f;

                float s = mid * env * 1.3f + low * Decay(t, 0.42f) * 2.4f + thud;
                return (float)System.Math.Tanh(s * 1.4);
            });
        }

        /// <summary>Noise burst with a second smaller burst: stone, vase crack, wooden door. 'brightness' = how much high end.</summary>
        private static AudioClip Crack(string name, float length, float decay, float brightness)
        {
            float low = 0f;
            return Render(name, length, (t, i) =>
            {
                float n = Noise();
                low += 0.15f * (n - low);
                float s = Mathf.Lerp(low, n, brightness);
                float env = Decay(t, decay) + (t > 0.05f ? 0.5f * Decay(t - 0.05f, decay * 0.7f) : 0f);
                return s * env;
            });
        }

        /// <summary>Shatter: bright noise plus three ringing shards.</summary>
        private static AudioClip VaseBreak()
        {
            float low = 0f;
            float[] shards = { 2400f, 3100f, 4150f };
            float[] starts = { 0f, 0.03f, 0.07f };
            return Render("VaseBreak", 0.4f, (t, i) =>
            {
                float n = Noise();
                low += 0.3f * (n - low);
                float s = (n - low) * Decay(t, 0.14f) * 1.2f;
                for (int k = 0; k < shards.Length; k++)
                    if (t > starts[k]) s += Osc(Wave.Sine, (t - starts[k]) * shards[k]) * Decay(t - starts[k], 0.22f) * 0.35f;
                return s;
            });
        }

        /// <summary>Metallic ping: two partials, slightly detuned.</summary>
        private static AudioClip Clink(string name, float hzA, float hzB, float decay)
        {
            return Render(name, decay + 0.1f, (t, i) =>
                Osc(Wave.Sine, t * hzA) * Decay(t, decay) +
                Osc(Wave.Sine, t * hzB * 1.003f) * Decay(t, decay * 0.7f) * 0.6f +
                Osc(Wave.Sine, t * hzA * 2.01f) * Decay(t, decay * 0.4f) * 0.3f);
        }

        private static AudioClip Click() =>
            Render("Click", 0.06f, (t, i) => (Noise() * 0.5f + Osc(Wave.Sine, t * 1100f)) * Decay(t, 0.025f));

        private static AudioClip Thud() =>
            Render("Invalid", 0.12f, (t, i) => Osc(Wave.Sine, t * 150f) * Decay(t, 0.08f));

        /// <summary>Royal fanfare: brassy saw/square voices, dotted rhythm, ends on a bright major chord.</summary>
        private static AudioClip WinJingle()
        {
            var brass = new Voice(Wave.Saw, 0.03f, 0.2f, 0f, 0.5f);
            var brassBody = new Voice(Wave.Square, 0.03f, 0.2f, 0f, 0.3f);
            var melody = Melody(0f,
                (67, 0.75f), (67, 0.25f), (67, 0.5f), (72, 0.5f),      // G G G C
                (76, 0.75f), (76, 0.25f), (79, 1.0f),                  // E E G
                (84, 2.0f));                                           // high C
            var harmony = new[]
            {
                new Note(60, 0f, 2f), new Note(64, 0f, 2f),            // C E under the first bar
                new Note(60, 2f, 2f), new Note(67, 2f, 2f),            // C G under the second
                new Note(60, 4f, 2f), new Note(64, 4f, 2f), new Note(67, 4f, 2f), new Note(72, 4f, 2f),   // final chord
            };
            var bass = Melody(0f, (48, 2f), (55, 2f), (48, 2f));
            return Sequence("Win", 120f, 6.5f,
                (melody, brass), (melody, brassBody),
                (harmony, new Voice(Wave.Saw, 0.05f, 0.3f, 0f, 0.22f)),
                (bass, new Voice(Wave.Triangle, 0.02f, 0.3f, 0f, 0.5f)));
        }

        /// <summary>Casual-game fail: three soft descending bell notes landing on a minor chord.</summary>
        private static AudioClip FailDirge()
        {
            // Casual-game "fail" bell: E C A. Three layers make it ring hard rather than soft:
            // the triangle body, a square-wave strike that dies in 60 ms, and an inharmonic
            // partial (+17 semitones ~ 2.6x) that gives the metallic "clang" of a real bell.
            var body   = new Voice(Wave.Triangle, 0.002f, 0.35f, 0.55f, 0.8f);
            var strike = new Voice(Wave.Square,   0.001f, 0.05f, 0.06f, 0.5f);
            var clang  = new Voice(Wave.Sine,     0.001f, 0.2f,  0.25f, 0.3f);
            var melody = Melody(0f, (76, 0.5f), (72, 0.5f), (69, 1.5f));
            var partial = new Note[melody.Length];
            for (int i = 0; i < melody.Length; i++)
                partial[i] = new Note(melody[i].Midi + 17, melody[i].StartBeat, melody[i].Beats);
            var chord = new[] { new Note(57, 1f, 1.6f), new Note(60, 1f, 1.6f), new Note(64, 1f, 1.6f) };   // A minor
            return Sequence("Fail", 120f, 3f,
                (melody, body), (melody, strike), (partial, clang),
                (chord, new Voice(Wave.Sine, 0.05f, 0.5f, 0f, 0.3f)));
        }
    }
}
