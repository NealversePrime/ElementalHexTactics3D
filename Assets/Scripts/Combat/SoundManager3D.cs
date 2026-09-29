using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ElementalHexTactics3D.Combat
{
    /// <summary>
    /// Central 3D Sound Manager supporting loaded audio clips (.mp3, .ogg)
    /// with zero-dependency procedural audio synthesis as automatic fallback.
    /// Provides skill SFX (fireball, water surge, earth spire, kinetic push, void sacrifice, etc.),
    /// impact/hurt sounds, victory/defeat stingers, and smooth BGM crossfading.
    /// </summary>
    public class SoundManager3D : MonoBehaviour
    {
        private static SoundManager3D instance;
        public static SoundManager3D Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<SoundManager3D>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SoundManager3D");
                        instance = go.AddComponent<SoundManager3D>();
                    }
                }
                return instance;
            }
        }

        [Header("Audio Settings")]
        [SerializeField] private bool isMuted = false;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.80f;
        [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.45f;

        [Header("Spell Audio (MP3)")]
        [SerializeField] private AudioClip[] fireballClips;
        [SerializeField] private AudioClip[] waterballClips;
        [SerializeField] private AudioClip[] earthmagicClips;
        [SerializeField] private AudioClip airblastClip;

        [Header("Combat & Ability Audio (MP3)")]
        [SerializeField] private AudioClip coreAbsorbClip;
        [SerializeField] private AudioClip magmaBlastClip;
        [SerializeField] private AudioClip[] monsterAttackClips;
        [SerializeField] private AudioClip tailwhipClip;
        [SerializeField] private AudioClip riftOpenClip;
        [SerializeField] private AudioClip vanguardEmergenceClip;
        [SerializeField] private AudioClip riftSacrificeClip;
        [SerializeField] private AudioClip wallSlamClip;
        [SerializeField] private AudioClip mudTrapClip;
        [SerializeField] private AudioClip getHitClip;

        [Header("Jingles & Stings (MP3)")]
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;

        [Header("Background Music (OGG)")]
        [SerializeField] private AudioClip hubBgmClip;
        [SerializeField] private AudioClip battleBgmClip;

        private AudioSource sfxSource;
        private AudioSource bgmSource;
        private Coroutine bgmFadeCoroutine;

        // Procedural Audio Clips (Generated at runtime as resilient fallbacks)
        private AudioClip fireWhooshClip;
        private AudioClip waterSplashClip;
        private AudioClip impactThudClip;
        private AudioClip siphonChimeClip;
        private AudioClip cataclysmBoomClip;
        private AudioClip buttonClickClip;

        public bool IsMuted
        {
            get => isMuted;
            set
            {
                isMuted = value;
                if (sfxSource != null) sfxSource.mute = isMuted;
                if (bgmSource != null) bgmSource.mute = isMuted;
            }
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                if (sfxSource != null) sfxSource.volume = sfxVolume;
            }
        }

        public float BgmVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = Mathf.Clamp01(value);
                if (bgmSource != null && bgmFadeCoroutine == null) bgmSource.volume = bgmVolume;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            SetupAudioSources();
            GenerateProceduralAudioClips();
            LoadAudioClips();
        }

        private void OnValidate()
        {
            LoadAudioClips();
        }

        private void SetupAudioSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.volume = sfxVolume;
                sfxSource.mute = isMuted;
            }

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
                bgmSource.volume = bgmVolume;
                bgmSource.mute = isMuted;
            }
        }

        public void LoadAudioClips()
        {
#if UNITY_EDITOR
            if (fireballClips == null || fireballClips.Length == 0 || fireballClips[0] == null)
            {
                fireballClips = new AudioClip[]
                {
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/fireball1.mp3"),
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/fireball2.mp3"),
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/fireball3.mp3")
                };
            }

            if (waterballClips == null || waterballClips.Length == 0 || waterballClips[0] == null)
            {
                waterballClips = new AudioClip[]
                {
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/waterball1.mp3"),
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/waterball2.mp3")
                };
            }

            if (earthmagicClips == null || earthmagicClips.Length == 0 || earthmagicClips[0] == null)
            {
                earthmagicClips = new AudioClip[]
                {
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/earthmagic1.mp3"),
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/earthmagic2.mp3")
                };
            }

            if (airblastClip == null)
                airblastClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Airblast.mp3");

            if (coreAbsorbClip == null)
                coreAbsorbClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Coreabsorb.mp3");

            if (magmaBlastClip == null)
                magmaBlastClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/magmablast.mp3");

            if (monsterAttackClips == null || monsterAttackClips.Length == 0 || monsterAttackClips[0] == null)
            {
                monsterAttackClips = new AudioClip[]
                {
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/monsterattack2.mp3"),
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/monsterattack3.mp3"),
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/monsterbite.mp3")
                };
            }

            if (tailwhipClip == null)
                tailwhipClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/tailwhip.mp3");

            if (riftOpenClip == null)
                riftOpenClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/riftopen.mp3");

            if (vanguardEmergenceClip == null)
                vanguardEmergenceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/comingoutfromrift.mp3");

            if (riftSacrificeClip == null)
                riftSacrificeClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/enemygeteatenbyrift.mp3");

            if (wallSlamClip == null)
                wallSlamClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/bodyimpact.mp3");

            if (mudTrapClip == null)
                mudTrapClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/mudsound.mp3");

            if (getHitClip == null)
                getHitClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/gethit.mp3");

            if (victoryClip == null)
                victoryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/victory.mp3");

            if (defeatClip == null)
                defeatClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/defeat.mp3");

            if (hubBgmClip == null)
                hubBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/Drama_-_Bittersweet_Reunion.ogg");

            if (battleBgmClip == null)
                battleBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/Drama_-_Suspenseful_Search.ogg");
#endif
        }

        // ================= SFX PLAYBACK METHODS ================= //

        private void PlayRandomClip(AudioClip[] clips, AudioClip fallback, float volumeMultiplier = 1f)
        {
            if (isMuted || sfxSource == null) return;

            if (clips != null && clips.Length > 0)
            {
                int startIdx = Random.Range(0, clips.Length);
                for (int i = 0; i < clips.Length; i++)
                {
                    int idx = (startIdx + i) % clips.Length;
                    if (clips[idx] != null)
                    {
                        sfxSource.PlayOneShot(clips[idx], sfxVolume * volumeMultiplier);
                        return;
                    }
                }
            }

            if (fallback != null)
            {
                sfxSource.PlayOneShot(fallback, sfxVolume * volumeMultiplier);
            }
        }

        private void PlaySingleClip(AudioClip clip, AudioClip fallback, float volumeMultiplier = 1f)
        {
            if (isMuted || sfxSource == null) return;

            AudioClip toPlay = clip != null ? clip : fallback;
            if (toPlay != null)
            {
                sfxSource.PlayOneShot(toPlay, sfxVolume * volumeMultiplier);
            }
        }

        public void PlayFireball() => PlayRandomClip(fireballClips, fireWhooshClip, 1.0f);
        public void PlayWaterSurge() => PlayRandomClip(waterballClips, waterSplashClip, 1.0f);
        public void PlayEarthSpire() => PlayRandomClip(earthmagicClips, impactThudClip, 1.0f);
        public void PlayKineticPush() => PlaySingleClip(airblastClip, fireWhooshClip, 1.0f);
        public void PlayTailShove() => PlaySingleClip(tailwhipClip != null ? tailwhipClip : airblastClip, fireWhooshClip, 1.0f);
        public void PlayCoreAbsorb() => PlaySingleClip(coreAbsorbClip, siphonChimeClip, 1.1f);
        public void PlayConsumeLand() => PlayCoreAbsorb();
        public void PlayCataclysm() => PlaySingleClip(magmaBlastClip, cataclysmBoomClip, 1.3f);
        public void PlayTitanStrike() => PlayRandomClip(monsterAttackClips, impactThudClip, 1.15f);
        public void PlayMonsterAttack() => PlayRandomClip(monsterAttackClips, impactThudClip, 1.0f);
        public void PlayRiftOpen() => PlaySingleClip(riftOpenClip, waterSplashClip, 1.15f);
        public void PlayVanguardEmergence() => PlaySingleClip(vanguardEmergenceClip, siphonChimeClip, 1.1f);
        public void PlayRiftDevour() => PlaySingleClip(riftSacrificeClip, cataclysmBoomClip, 1.25f);
        public void PlayWallSlam(float volumeScale = 1.0f) => PlaySingleClip(wallSlamClip, impactThudClip, volumeScale);
        public void PlayMudTrap() => PlaySingleClip(mudTrapClip, waterSplashClip, 1.0f);
        public void PlayGetHit() => PlaySingleClip(getHitClip, impactThudClip, 0.95f);
        public void PlayVictory() => PlaySingleClip(victoryClip, siphonChimeClip, 1.2f);
        public void PlayDefeat() => PlaySingleClip(defeatClip, cataclysmBoomClip, 1.2f);

        public void PlaySpellCast(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire:
                    PlayFireball();
                    break;
                case ElementType.Water:
                    PlayWaterSurge();
                    break;
                case ElementType.Earth:
                    PlayEarthSpire();
                    break;
                case ElementType.Wind:
                    PlayKineticPush();
                    break;
                default:
                    PlayFireball();
                    break;
            }
        }

        public void PlaySpellCast(bool isFire)
        {
            if (isFire) PlayFireball();
            else PlayWaterSurge();
        }

        public void PlaySlam(float volumeScale = 1.0f)
        {
            PlayWallSlam(volumeScale);
        }

        public void PlayButtonClick()
        {
            if (isMuted || sfxSource == null) return;
            sfxSource.PlayOneShot(buttonClickClip, sfxVolume * 0.5f);
        }

        // ================= BACKGROUND MUSIC (BGM) METHODS ================= //

        public void PlayBGM(AudioClip clip, float fadeDuration = 0.8f)
        {
            if (clip == null || bgmSource == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(CrossfadeBGMRoutine(clip, fadeDuration));
        }

        public void PlayHubBGM()
        {
            if (hubBgmClip != null) PlayBGM(hubBgmClip, 1.0f);
        }

        public void PlayBattleBGM()
        {
            if (battleBgmClip != null) PlayBGM(battleBgmClip, 1.0f);
        }

        public void StopBGM(float fadeDuration = 0.5f)
        {
            if (bgmSource == null || !bgmSource.isPlaying) return;
            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(FadeOutBGMRoutine(fadeDuration));
        }

        private IEnumerator CrossfadeBGMRoutine(AudioClip newClip, float duration)
        {
            float startVol = bgmSource.volume;
            float halfDuration = Mathf.Max(0.05f, duration * 0.5f);

            if (bgmSource.isPlaying && bgmSource.volume > 0.01f)
            {
                float timer = 0f;
                while (timer < halfDuration)
                {
                    timer += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(startVol, 0f, timer / halfDuration);
                    yield return null;
                }
            }

            bgmSource.clip = newClip;
            bgmSource.volume = 0f;
            bgmSource.Play();

            float targetVol = bgmVolume;
            float inTimer = 0f;
            while (inTimer < halfDuration)
            {
                inTimer += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVol, inTimer / halfDuration);
                yield return null;
            }

            bgmSource.volume = targetVol;
            bgmFadeCoroutine = null;
        }

        private IEnumerator FadeOutBGMRoutine(float duration)
        {
            float startVol = bgmSource.volume;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, timer / duration);
                yield return null;
            }
            bgmSource.Stop();
            bgmSource.clip = null;
            bgmFadeCoroutine = null;
        }

        // ================= PROCEDURAL SYNTHESIS ENGINE ================= //

        private void GenerateProceduralAudioClips()
        {
            fireWhooshClip = CreateSweepClip("SFX_FireWhoosh", 0.32f, 440f, 160f, isNoise: true);
            waterSplashClip = CreateSweepClip("SFX_WaterSplash", 0.28f, 600f, 300f, isNoise: true);
            impactThudClip = CreateThudClip("SFX_ImpactThud", 0.25f, 120f);
            siphonChimeClip = CreateChimeClip("SFX_SiphonChime", 0.40f);
            cataclysmBoomClip = CreateCataclysmClip("SFX_CataclysmBoom", 0.75f);
            buttonClickClip = CreateClickClip("SFX_ButtonClick", 0.04f, 1200f);
        }

        private AudioClip CreateSweepClip(string name, float duration, float startFreq, float endFreq, bool isNoise)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float phase = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * currentFreq / sampleRate;

                float envelope = Mathf.Sin(t * Mathf.PI);
                float tone = Mathf.Sin(phase);
                float noise = (Random.value * 2f - 1f) * 0.35f;

                samples[i] = envelope * (isNoise ? (tone * 0.65f + noise) : tone);
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateThudClip(string name, float duration, float baseFreq)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float phase = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float currentFreq = Mathf.Lerp(baseFreq, 35f, t);
                phase += 2f * Mathf.PI * currentFreq / sampleRate;

                float decay = Mathf.Exp(-t * 8f);
                float noise = (Random.value * 2f - 1f) * 0.25f * decay;

                samples[i] = (Mathf.Sin(phase) * 0.75f + noise) * decay;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateChimeClip(string name, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float[] notes = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
            int stepSamples = totalSamples / notes.Length;

            for (int n = 0; n < notes.Length; n++)
            {
                float freq = notes[n];
                int start = n * stepSamples;
                int end = (n == notes.Length - 1) ? totalSamples : (n + 1) * stepSamples;
                float phase = 0f;

                for (int i = start; i < end; i++)
                {
                    phase += 2f * Mathf.PI * freq / sampleRate;
                    float localT = (float)(i - start) / (totalSamples - start);
                    float env = Mathf.Exp(-localT * 3.5f);
                    samples[i] = Mathf.Sin(phase) * env * 0.6f;
                }
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateCataclysmClip(string name, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float phase = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float freq = Mathf.Lerp(90f, 25f, t);
                phase += 2f * Mathf.PI * freq / sampleRate;

                float decay = Mathf.Exp(-t * 2.8f);
                float noise = (Random.value * 2f - 1f) * 0.45f;

                samples[i] = (Mathf.Sin(phase) * 0.6f + noise) * decay;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateClickClip(string name, float duration, float freq)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float phase = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                phase += 2f * Mathf.PI * freq / sampleRate;
                float env = 1f - t;
                samples[i] = Mathf.Sin(phase) * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
