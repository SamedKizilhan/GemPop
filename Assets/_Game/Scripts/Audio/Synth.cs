using System;
using UnityEngine;

namespace GemPop.Audio
{
    /// <summary>
    /// A tiny software synthesizer: generates AudioClips from code so the project needs no audio files.
    /// Everything is plain math over a sample buffer (44.1 kHz, mono).
    /// </summary>
    public static class Synth
    {
        public const int SampleRate = 44100;
        private static readonly System.Random Rng = new System.Random(1234);   // deterministic noise

        public enum Wave { Sine, Triangle, Square, Saw }

        // ------------------------------------------------------------ primitives

        public static float Osc(Wave wave, float phase)   // phase in cycles (0..1 repeating)
        {
            float p = phase - Mathf.Floor(phase);
            switch (wave)
            {
                case Wave.Square:   return p < 0.5f ? 0.6f : -0.6f;
                case Wave.Saw:      return (p * 2f - 1f) * 0.7f;
                case Wave.Triangle: return 1f - 4f * Mathf.Abs(p - 0.5f);
                default:            return Mathf.Sin(p * Mathf.PI * 2f);
            }
        }

        public static float Noise() => (float)(Rng.NextDouble() * 2.0 - 1.0);

        /// <summary>Exponential decay envelope: 1 at t=0, ~0 after 'decay' seconds.</summary>
        public static float Decay(float t, float decay) => Mathf.Exp(-5f * t / decay);

        /// <summary>Attack-release envelope.</summary>
        public static float AR(float t, float attack, float total, float release)
        {
            if (t < attack) return t / attack;
            if (t > total - release) return Mathf.Max(0f, (total - t) / release);
            return 1f;
        }

        public static float MidiToHz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        // ------------------------------------------------------------ clip builders

        /// <summary>Renders 'seconds' of audio from a function of time. 'state' lets the function keep filter memory.</summary>
        public static AudioClip Render(string name, float seconds, Func<float, int, float> sampleAt)
        {
            int count = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[count];
            float peak = 0f;
            for (int i = 0; i < count; i++)
            {
                data[i] = sampleAt(i / (float)SampleRate, i);
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            if (peak > 0.001f)
                for (int i = 0; i < count; i++) data[i] *= 0.9f / peak;   // normalize

            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>One note of a melody.</summary>
        public readonly struct Note
        {
            public readonly int Midi;          // 0 = rest
            public readonly float StartBeat;
            public readonly float Beats;
            public Note(int midi, float startBeat, float beats) { Midi = midi; StartBeat = startBeat; Beats = beats; }
        }

        /// <summary>A voice = waveform mix + envelope shape used to render notes.</summary>
        public readonly struct Voice
        {
            public readonly Wave Wave;
            public readonly float Attack;      // seconds
            public readonly float Release;     // seconds after the note ends
            public readonly float Decay;       // exponential decay during the note (0 = sustain)
            public readonly float Gain;
            public Voice(Wave wave, float attack, float release, float decay, float gain)
            { Wave = wave; Attack = attack; Release = release; Decay = decay; Gain = gain; }
        }

        /// <summary>Renders a sequence of notes (additively, so chords and bass lines can overlap) into a loopable clip.</summary>
        public static AudioClip Sequence(string name, float bpm, float totalBeats, params (Note[] notes, Voice voice)[] tracks)
        {
            float secondsPerBeat = 60f / bpm;
            float seconds = totalBeats * secondsPerBeat;
            int count = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[count];

            foreach (var (notes, voice) in tracks)
            {
                foreach (Note n in notes)
                {
                    if (n.Midi <= 0) continue;
                    float hz = MidiToHz(n.Midi);
                    float start = n.StartBeat * secondsPerBeat;
                    float length = n.Beats * secondsPerBeat;
                    int from = Mathf.FloorToInt(start * SampleRate);
                    int to = Mathf.Min(count, Mathf.CeilToInt((start + length + voice.Release) * SampleRate));
                    for (int i = from; i < to; i++)
                    {
                        float t = i / (float)SampleRate - start;
                        float env = AR(t, voice.Attack, length + voice.Release, voice.Release);
                        if (voice.Decay > 0f) env *= Decay(t, voice.Decay);
                        data[i] += Osc(voice.Wave, t * hz) * env * voice.Gain;
                    }
                }
            }

            float peak = 0f;
            foreach (float s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
            if (peak > 0.001f) for (int i = 0; i < count; i++) data[i] *= 0.85f / peak;

            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Parses "midi:startBeat:beats" tokens separated by spaces (see TurkishMarchScore).</summary>
        public static Note[] ParseScore(string score)
        {
            string[] tokens = score.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var notes = new Note[tokens.Length];
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            for (int i = 0; i < tokens.Length; i++)
            {
                string[] parts = tokens[i].Split(':');
                notes[i] = new Note(int.Parse(parts[0], culture), float.Parse(parts[1], culture), float.Parse(parts[2], culture));
            }
            return notes;
        }

        /// <summary>Helper to write melodies compactly: a list of (midi, beats) consumed left to right.</summary>
        public static Note[] Melody(float startBeat, params (int midi, float beats)[] steps)
        {
            var notes = new Note[steps.Length];
            float beat = startBeat;
            for (int i = 0; i < steps.Length; i++)
            {
                notes[i] = new Note(steps[i].midi, beat, steps[i].beats);
                beat += steps[i].beats;
            }
            return notes;
        }
    }
}
