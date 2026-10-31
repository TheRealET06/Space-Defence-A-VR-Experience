using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    /// Per-wave audio overrides. Wave numbers are 1-based and count manual waves first,
    /// then generated waves (same numbering the HUD uses).
    [Serializable]
    public class WaveAudioEntry
    {
        [Min(1)] public int waveNumber = 1;
        [Tooltip("Uses the Boss Music below unless 'Music' is set on this entry.")]
        public bool isBossWave;
        [Tooltip("Music for this wave. Overrides the default / boss music. Leave empty to use those.")]
        public AudioClip music;
        [Tooltip("Played when this wave starts. Leave empty to use the default start sound.")]
        public AudioClip startSfx;
        [Tooltip("Played when this wave is cleared. Leave empty to use the default end sound.")]
        public AudioClip endSfx;
        [Tooltip("Music for the break AFTER this wave is cleared, before the next one starts. Leave empty to use the default interlude music.")]
        public AudioClip interludeAfter;
    }

    /// Plays all music and sound effects for the wave flow. Listens to WaveDirector events,
    /// so WaveDirector itself never touches audio.
    public class WaveAudioDirector : MonoBehaviour
    {
        public static WaveAudioDirector Instance { get; private set; }

        [Header("References")]
        [SerializeField] WaveDirector waveDirector;   // auto-found if empty
        [Tooltip("Looping music source. Created automatically if empty.")]
        [SerializeField] AudioSource musicSource;
        [Tooltip("One-shot sound effect source. Created automatically if empty.")]
        [SerializeField] AudioSource sfxSource;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] float sfxVolume = 1f;
        [SerializeField] float crossfadeSeconds = 1.5f;

        [Header("Default music")]
        [Tooltip("Plays at game start and between waves.")]
        [SerializeField] AudioClip interludeMusic;
        [Tooltip("Used for any wave that has no override music and isn't a boss wave.")]
        [SerializeField] AudioClip defaultWaveMusic;
        [Tooltip("Used for waves flagged as boss waves (unless that wave has its own music).")]
        [SerializeField] AudioClip bossMusic;

        [Header("Default wave sounds")]
        [SerializeField] AudioClip defaultWaveStartSfx;
        [SerializeField] AudioClip defaultWaveEndSfx;

        [Header("Game end")]
        [SerializeField] AudioClip gameWonSfx;
        [SerializeField] AudioClip gameLostSfx;
        [SerializeField] bool stopMusicOnGameEnd = true;

        [Header("Per-wave overrides (only add the waves you want to customize)")]
        [SerializeField] List<WaveAudioEntry> waves = new List<WaveAudioEntry>();

        Coroutine fadeRoutine;
        AudioClip currentMusic;
        bool gameEnded;

        // ---------- Lifecycle ----------

        void Awake()
        {
            Instance = this;

            // 2D (non-spatial) sources so music doesn't change with head position in VR.
            if (musicSource == null) musicSource = CreateSource("Music", true);
            if (sfxSource == null) sfxSource = CreateSource("SFX", false);
            musicSource.loop = true;
            musicSource.volume = 0f;
        }

        void Start()
        {
            // WaveDirector sets its Instance in Awake, so it's ready by Start.
            if (waveDirector == null) waveDirector = WaveDirector.Instance;
            if (waveDirector == null)
            {
                Debug.LogError("WaveAudioDirector: no WaveDirector found.", this);
                enabled = false;
                return;
            }

            waveDirector.WaveStarted += OnWaveStarted;
            waveDirector.WaveEnded += OnWaveEnded;
            waveDirector.ReadyForWave += OnReadyForWave;
            waveDirector.GameWon += PlayGameWon;

            PlayMusic(interludeMusic);   // calm music while the player sets up for wave 1
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (waveDirector == null) return;
            waveDirector.WaveStarted -= OnWaveStarted;
            waveDirector.WaveEnded -= OnWaveEnded;
            waveDirector.ReadyForWave -= OnReadyForWave;
            waveDirector.GameWon -= PlayGameWon;
        }

        AudioSource CreateSource(string childName, bool loop)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            return src;
        }

        // ---------- WaveDirector events ----------

        void OnWaveStarted(int waveNumber, int totalWaves)
        {
            if (gameEnded) return;
            var entry = Find(waveNumber);

            PlaySfx(Pick(entry?.startSfx, defaultWaveStartSfx));

            AudioClip music = entry != null && entry.music != null ? entry.music
                            : entry != null && entry.isBossWave ? Pick(bossMusic, defaultWaveMusic)
                            : defaultWaveMusic;
            PlayMusic(music);
        }

        // Fires the moment a wave is fully cleared (including the final wave).
        void OnWaveEnded(int waveNumber)
        {
            if (gameEnded) return;
            PlaySfx(Pick(Find(waveNumber)?.endSfx, defaultWaveEndSfx));
        }

        // Fires after a wave is cleared and the next one can be started.
        void OnReadyForWave(int nextWaveNumber)
        {
            if (gameEnded) return;
            // The wave that just ended is the one before the upcoming wave.
            PlayMusic(Pick(Find(nextWaveNumber - 1)?.interludeAfter, interludeMusic));
        }

        // ---------- Public API ----------

        public void PlayGameWon()
        {
            if (gameEnded) return;
            gameEnded = true;
            if (stopMusicOnGameEnd) PlayMusic(null);
            PlaySfx(gameWonSfx);
        }

        /// Call this from wherever your game is lost (e.g. GameManager's lose method).
        public void PlayGameLost()
        {
            if (gameEnded) return;
            gameEnded = true;
            if (stopMusicOnGameEnd) PlayMusic(null);
            PlaySfx(gameLostSfx);
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
        }

        /// Crossfades to a new track. Passing null fades the music out.
        public void PlayMusic(AudioClip clip)
        {
            if (clip == currentMusic && (clip == null || musicSource.isPlaying)) return;
            currentMusic = clip;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(Crossfade(clip));
        }

        // ---------- Helpers ----------

        WaveAudioEntry Find(int waveNumber)
        {
            foreach (var w in waves)
                if (w != null && w.waveNumber == waveNumber) return w;
            return null;
        }

        // Unity-safe "a ?? b" (the ?? operator doesn't understand Unity's null checks).
        static AudioClip Pick(AudioClip a, AudioClip b) => a != null ? a : b;

        IEnumerator Crossfade(AudioClip next)
        {
            // Fade the current track out.
            float startVol = musicSource.volume;
            if (musicSource.isPlaying && crossfadeSeconds > 0f)
            {
                for (float t = 0f; t < crossfadeSeconds; t += Time.unscaledDeltaTime)
                {
                    musicSource.volume = Mathf.Lerp(startVol, 0f, t / crossfadeSeconds);
                    yield return null;
                }
            }
            musicSource.volume = 0f;
            musicSource.Stop();

            if (next == null) { fadeRoutine = null; yield break; }

            // Fade the new track in.
            musicSource.clip = next;
            musicSource.Play();
            if (crossfadeSeconds > 0f)
            {
                for (float t = 0f; t < crossfadeSeconds; t += Time.unscaledDeltaTime)
                {
                    musicSource.volume = Mathf.Lerp(0f, musicVolume, t / crossfadeSeconds);
                    yield return null;
                }
            }
            musicSource.volume = musicVolume;
            fadeRoutine = null;
        }
    }
}