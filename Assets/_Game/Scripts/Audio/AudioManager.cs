using System.Collections.Generic;
using GemPop.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GemPop.Audio
{
    /// <summary>
    /// Creates itself before the first scene, survives scene loads, and owns the two audio channels:
    /// looping music (picked by scene) and one-shot effects. All clips are synthesized at startup.
    /// Gameplay code only says WHAT happened (AudioManager.Play(Sfx.CrateBreak)); nothing else leaks in.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const float MusicVolume = 0.28f;
        private const float SfxVolume = 0.85f;
        private const float RepeatGuardSeconds = 0.05f;   // the same effect at most once per 50 ms (Bomb kills 20 cubes at once)

        private static AudioManager _instance;

        private readonly Dictionary<Sfx, AudioClip> _sfx = new();
        private readonly Dictionary<Music, AudioClip> _music = new();
        private readonly Dictionary<Sfx, float> _lastPlayed = new();
        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private Music _current = Music.None;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.volume = MusicVolume;
            _musicSource.playOnAwake = false;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            foreach (Sfx sfx in System.Enum.GetValues(typeof(Sfx))) _sfx[sfx] = SfxLibrary.Build(sfx);
            foreach (Music music in System.Enum.GetValues(typeof(Music)))
            {
                AudioClip clip = MusicLibrary.Build(music);
                if (clip != null) _music[music] = clip;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _sfxSource.Stop();                                   // a new scene never inherits the old scene's jingle
            if (scene.name == SceneLoader.MainScene) PlayMusic(Music.Menu);
            else if (scene.name == SceneLoader.LevelScene) PlayMusic(Music.Level);
        }

        // ------------------------------------------------------------------ public API

        public static void Play(Sfx sfx, float pitch = 1f)
        {
            if (_instance == null) return;
            _instance.PlayInternal(sfx, pitch);
        }

        public static void PlayMusic(Music music)
        {
            if (_instance == null) return;
            _instance.PlayMusicInternal(music);
        }

        public static void StopMusic() => PlayMusic(Music.None);

        /// <summary>Cuts any playing effect (e.g. the fail jingle when "Try Again" is pressed).</summary>
        public static void StopSfx()
        {
            if (_instance != null) _instance._sfxSource.Stop();
        }

        // ------------------------------------------------------------------ internals

        private void PlayInternal(Sfx sfx, float pitch)
        {
            if (!_sfx.TryGetValue(sfx, out AudioClip clip)) return;
            if (_lastPlayed.TryGetValue(sfx, out float last) && Time.unscaledTime - last < RepeatGuardSeconds) return;
            _lastPlayed[sfx] = Time.unscaledTime;

            _sfxSource.pitch = pitch * Random.Range(0.96f, 1.04f);   // tiny variation so repeats don't sound robotic
            _sfxSource.PlayOneShot(clip, SfxVolume * SfxLibrary.Volume(sfx));
        }

        private void PlayMusicInternal(Music music)
        {
            if (music == _current && _musicSource.isPlaying) return;
            _current = music;
            _musicSource.Stop();
            if (music == Music.None || !_music.TryGetValue(music, out AudioClip clip)) return;
            _musicSource.clip = clip;
            _musicSource.Play();
        }
    }
}
