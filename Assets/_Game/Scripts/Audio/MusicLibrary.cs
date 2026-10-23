using UnityEngine;
using static GemPop.Audio.Synth;

namespace GemPop.Audio
{
    public enum Music { None, Menu, Level }

    /// <summary>
    /// Loopable music rendered from note data. Mozart's Rondo alla Turca is public domain; this is our own
    /// synthesized rendition (no recording is used), so there is nothing to license.
    /// </summary>
    public static class MusicLibrary
    {
        public static AudioClip Build(Music music) => music switch
        {
            Music.Menu  => MenuWaltz(),
            Music.Level => TurkishMarch(),
            _           => null,
        };

        /// <summary>Cheerful farm tune in G major, 4/4 at 132 bpm, oom-pah bass (8 bars, loops).</summary>
        private static AudioClip MenuWaltz()
        {
            var lead = new Voice(Wave.Triangle, 0.01f, 0.12f, 0.6f, 0.6f);
            var leadBright = new Voice(Wave.Square, 0.01f, 0.12f, 0.6f, 0.25f);
            var oom = new Voice(Wave.Triangle, 0.005f, 0.08f, 0.25f, 0.5f);
            var pah = new Voice(Wave.Square, 0.005f, 0.06f, 0.15f, 0.22f);

            const float q = 1f, e = 0.5f;
            var melody = Melody(0f,
                (79, e), (81, e), (83, q), (79, q), (83, e), (84, e),          // G A B  G  B C
                (86, q), (83, q), (79, 2f),                                    // D  B  G -
                (81, e), (83, e), (84, q), (81, q), (84, e), (86, e),          // A B C  A  C D
                (88, q), (86, q), (83, 2f),                                    // E  D  B -
                (79, e), (81, e), (83, q), (86, q), (83, e), (81, e),          // G A B  D  B A
                (79, q), (76, q), (74, 2f),                                    // G  E  D -
                (74, e), (76, e), (78, q), (81, q), (78, e), (76, e),          // D E F# A F# E
                (79, 2f), (0, 2f));                                            // G -  (rest)

            // bass: root on beats 1 and 3, chord "pah" on 2 and 4   (G C G D | G C D G)
            int[] roots = { 55, 48, 55, 50, 55, 48, 50, 55 };
            var bassLine = new Note[roots.Length * 2];
            var chords = new Note[roots.Length * 4];
            for (int bar = 0; bar < roots.Length; bar++)
            {
                float b = bar * 4f;
                bassLine[bar * 2] = new Note(roots[bar] - 12, b, 1f);
                bassLine[bar * 2 + 1] = new Note(roots[bar] - 12 + 7, b + 2f, 1f);
                chords[bar * 4] = new Note(roots[bar] + 4, b + 1f, 0.5f);
                chords[bar * 4 + 1] = new Note(roots[bar] + 7, b + 1f, 0.5f);
                chords[bar * 4 + 2] = new Note(roots[bar] + 4, b + 3f, 0.5f);
                chords[bar * 4 + 3] = new Note(roots[bar] + 7, b + 3f, 0.5f);
            }
            return Sequence("MenuTune", 132f, 32f, (melody, lead), (melody, leadBright), (bassLine, oom), (chords, pah));
        }

        /// <summary>Rondo alla Turca, bars 1-24 from the MIDI score, harpsichord-like voices, seamless 48-beat loop.</summary>
        private static AudioClip TurkishMarch()
        {
            var lead = new Voice(Wave.Square, 0.004f, 0.04f, 0.4f, 0.45f);
            var leadBody = new Voice(Wave.Triangle, 0.004f, 0.04f, 0.4f, 0.55f);
            var bassVoice = new Voice(Wave.Triangle, 0.004f, 0.04f, 0.35f, 0.5f);

            Note[] right = ParseScore(TurkishMarchScore.RightHand);
            Note[] left = ParseScore(TurkishMarchScore.LeftHand);
            return Sequence("TurkishMarch", TurkishMarchScore.Bpm, TurkishMarchScore.LoopBeats,
                (right, lead), (right, leadBody), (left, bassVoice));
        }
    }
}
