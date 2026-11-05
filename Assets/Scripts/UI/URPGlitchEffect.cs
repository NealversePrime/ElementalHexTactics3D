using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.CameraControl;

namespace ElementalHexTactics3D.UI
{
    /// <summary>
    /// Implements the URPGlitch (Analog Glitch + Digital Glitch) effect based on Keijiro Takahashi / saimarei's URPGlitch:
    /// - Analog Glitch: Scan Line Jitter, Vertical Jump, Horizontal Shake, and RGB Color Drift (Chromatic Split).
    /// - Digital Glitch: Real-time 64x32 blocky strip noise texture driving UV block displacement & channel corruption.
    /// - Synchronized 3D URP Volume Post-Processing (Chromatic Aberration + Lens Distortion) and procedural digital static audio.
    /// </summary>
    public class URPGlitchEffect : MonoBehaviour
    {
        private const int NoiseWidth = 64;
        private const int NoiseHeight = 32;

        private Texture2D noiseTexture;
        private Color[] noiseColors;
        private Material imageGlitchMat;
        private Material overlayGlitchMat;
        private AudioClip proceduralGlitchClip;
        private AudioSource glitchAudioSource;

        private float verticalJumpTime;
        private Coroutine activeGlitchRoutine;

        // Shader Property IDs
        private static readonly int PropNoiseTex = Shader.PropertyToID("_NoiseTex");
        private static readonly int PropScanLineJitter = Shader.PropertyToID("_ScanLineJitter");
        private static readonly int PropVerticalJump = Shader.PropertyToID("_VerticalJump");
        private static readonly int PropHorizontalShake = Shader.PropertyToID("_HorizontalShake");
        private static readonly int PropColorDrift = Shader.PropertyToID("_ColorDrift");
        private static readonly int PropDigitalIntensity = Shader.PropertyToID("_DigitalIntensity");
        private static readonly int PropOverlayMode = Shader.PropertyToID("_OverlayMode");

        private void Awake()
        {
            InitializeResources();
        }

        private void InitializeResources()
        {
            if (noiseTexture == null)
            {
                noiseTexture = new Texture2D(NoiseWidth, NoiseHeight, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.DontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };
                noiseColors = new Color[NoiseWidth * NoiseHeight];
                UpdateNoiseTexture(1f);
            }

            if (imageGlitchMat == null || overlayGlitchMat == null)
            {
                Shader glitchShader = Resources.Load<Shader>("Shaders/URPGlitchUI")
                                      ?? Shader.Find("Hidden/ElementalHex/URPGlitchUI");
                if (glitchShader != null)
                {
                    imageGlitchMat = new Material(glitchShader) { hideFlags = HideFlags.DontSave };
                    imageGlitchMat.SetTexture(PropNoiseTex, noiseTexture);
                    imageGlitchMat.SetFloat(PropOverlayMode, 0f);

                    overlayGlitchMat = new Material(glitchShader) { hideFlags = HideFlags.DontSave };
                    overlayGlitchMat.SetTexture(PropNoiseTex, noiseTexture);
                    overlayGlitchMat.SetFloat(PropOverlayMode, 1f);
                }
            }

            if (proceduralGlitchClip == null)
            {
                proceduralGlitchClip = CreateProceduralGlitchClip();
            }

            if (glitchAudioSource == null)
            {
                glitchAudioSource = gameObject.GetComponent<AudioSource>();
                if (glitchAudioSource == null)
                {
                    glitchAudioSource = gameObject.AddComponent<AudioSource>();
                    glitchAudioSource.playOnAwake = false;
                    glitchAudioSource.spatialBlend = 0f;
                }
            }
        }

        /// <summary>
        /// Updates the 64x32 blocky digital noise texture using horizontal strip persistence
        /// (matching URPGlitch / KinoGlitch's DigitalGlitch noise generator).
        /// </summary>
        private void UpdateNoiseTexture(float changeChance = 0.85f)
        {
            if (noiseTexture == null || noiseColors == null) return;

            Color c = RandomGlitchColor();
            for (int y = 0; y < NoiseHeight; y++)
            {
                for (int x = 0; x < NoiseWidth; x++)
                {
                    if (Random.value > changeChance)
                    {
                        c = RandomGlitchColor();
                    }
                    noiseColors[y * NoiseWidth + x] = c;
                }
            }

            noiseTexture.SetPixels(noiseColors);
            noiseTexture.Apply(false);
        }

        private static Color RandomGlitchColor()
        {
            return new Color(Random.value, Random.value, Random.value, Random.value);
        }

        private void ApplyShaderParams(
            Material mat,
            float scanLineJitter,
            float verticalJump,
            float horizontalShake,
            float colorDrift,
            float digitalIntensity)
        {
            if (mat == null) return;

            // Scan line jitter: x = displacement, y = threshold
            float slDisp = 0.002f + Mathf.Pow(scanLineJitter, 3f) * 0.08f;
            float slThresh = Mathf.Clamp01(1.0f - scanLineJitter * 1.2f);
            mat.SetVector(PropScanLineJitter, new Vector4(slDisp, slThresh, 0f, 0f));

            // Vertical jump: x = amount, y = accumulated time
            verticalJumpTime += Time.unscaledDeltaTime * verticalJump * 11.3f;
            mat.SetVector(PropVerticalJump, new Vector4(verticalJump, verticalJumpTime, 0f, 0f));

            // Horizontal shake
            mat.SetFloat(PropHorizontalShake, horizontalShake * 0.22f);

            // Color drift (RGB Chromatic Split): x = drift amount, y = time phase
            float cdAmount = colorDrift * 0.045f;
            mat.SetVector(PropColorDrift, new Vector4(cdAmount, Time.unscaledTime * 606.11f, 0f, 0f));

            // Digital glitch block intensity
            mat.SetFloat(PropDigitalIntensity, digitalIntensity);
            mat.SetTexture(PropNoiseTex, noiseTexture);
        }

        public void TriggerBurst(
            Image backgroundArtImage,
            Image portraitLeft,
            Image portraitRight,
            Image glitchOverlayImage,
            RectTransform dialogueBoxRect,
            float duration = 0.72f,
            float intensity = 0.85f)
        {
            InitializeResources();
            if (activeGlitchRoutine != null)
            {
                StopCoroutine(activeGlitchRoutine);
            }
            activeGlitchRoutine = StartCoroutine(GlitchBurstRoutine(
                backgroundArtImage,
                portraitLeft,
                portraitRight,
                glitchOverlayImage,
                dialogueBoxRect,
                duration,
                intensity));
        }

        private IEnumerator GlitchBurstRoutine(
            Image backgroundArtImage,
            Image portraitLeft,
            Image portraitRight,
            Image glitchOverlayImage,
            RectTransform dialogueBoxRect,
            float duration,
            float peakIntensity)
        {
            // Play digital glitch audio burst
            if (glitchAudioSource != null && proceduralGlitchClip != null)
            {
                bool muted = SoundManager3D.Instance != null && SoundManager3D.Instance.IsMuted;
                float vol = SoundManager3D.Instance != null ? SoundManager3D.Instance.SfxVolume : 0.6f;
                if (!muted)
                {
                    glitchAudioSource.pitch = Random.Range(0.88f, 1.18f);
                    glitchAudioSource.PlayOneShot(proceduralGlitchClip, Mathf.Clamp01(vol * 0.85f));
                }
            }

            // Shake 3D tactical camera if visible behind dialogue
            TacticalCameraController.Instance?.Shake(0.28f * peakIntensity, duration * 0.8f);

            // Hook URP Global Volume ChromaticAberration & LensDistortion for 3D scene glitching
            Volume globalVolume = FindAnyObjectByType<Volume>();
            ChromaticAberration chroma = null;
            LensDistortion lens = null;
            bool origChromaActive = false;
            float origChromaVal = 0f;
            bool origLensActive = false;
            float origLensVal = 0f;

            if (globalVolume != null && globalVolume.profile != null)
            {
                if (globalVolume.profile.TryGet(out chroma))
                {
                    origChromaActive = chroma.active;
                    origChromaVal = chroma.intensity.value;
                    chroma.active = true;
                    chroma.intensity.overrideState = true;
                }
                if (globalVolume.profile.TryGet(out lens))
                {
                    origLensActive = lens.active;
                    origLensVal = lens.intensity.value;
                    lens.active = true;
                    lens.intensity.overrideState = true;
                }
            }

            // Assign URPGlitch materials to UI images
            if (backgroundArtImage != null && imageGlitchMat != null)
                backgroundArtImage.material = imageGlitchMat;
            if (portraitLeft != null && imageGlitchMat != null)
                portraitLeft.material = imageGlitchMat;
            if (portraitRight != null && imageGlitchMat != null)
                portraitRight.material = imageGlitchMat;

            if (glitchOverlayImage != null)
            {
                glitchOverlayImage.gameObject.SetActive(true);
                glitchOverlayImage.color = Color.white;
                if (overlayGlitchMat != null)
                {
                    glitchOverlayImage.material = overlayGlitchMat;
                }
            }

            Vector2 origBoxPos = dialogueBoxRect != null ? dialogueBoxRect.anchoredPosition : Vector2.zero;
            Vector2 origLeftPos = portraitLeft != null ? portraitLeft.rectTransform.anchoredPosition : Vector2.zero;
            Vector2 origRightPos = portraitRight != null ? portraitRight.rectTransform.anchoredPosition : Vector2.zero;

            float elapsed = 0f;
            int step = 0;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Envelope: intense start + erratic spikes + sharp decay at end
                float envelope = Mathf.Sin((1f - progress * 0.7f) * Mathf.PI * 0.5f);
                float spike = (Random.value > 0.35f) ? Random.Range(0.75f, 1.15f) : Random.Range(0.25f, 0.55f);
                float cur = Mathf.Clamp01(peakIntensity * envelope * spike);

                if (step % 2 == 0)
                {
                    UpdateNoiseTexture(Random.Range(0.68f, 0.90f));
                }
                step++;

                float scanJitter = Mathf.Clamp01(cur * 0.95f);
                float vertJump = (Random.value > 0.65f) ? cur * 0.38f : 0f;
                float horizShake = cur * 0.45f;
                float colorDrift = Mathf.Clamp01(cur * 1.10f);
                float digitalInt = Mathf.Clamp01(cur * 0.65f);

                ApplyShaderParams(imageGlitchMat, scanJitter, vertJump, horizShake, colorDrift, digitalInt);
                ApplyShaderParams(overlayGlitchMat, scanJitter, vertJump, horizShake, colorDrift, digitalInt);

                // Drive 3D URP Post-Processing Volume simultaneously
                if (chroma != null)
                {
                    chroma.intensity.value = Mathf.Clamp01(cur * 1.0f);
                }
                if (lens != null)
                {
                    lens.intensity.value = Random.Range(-0.35f, 0.35f) * cur;
                }

                // Jitter Dialogue Box & Portraits
                if (dialogueBoxRect != null)
                {
                    dialogueBoxRect.anchoredPosition = origBoxPos + new Vector2(
                        Random.Range(-16f, 16f) * cur,
                        Random.Range(-9f, 9f) * cur);
                }
                if (portraitLeft != null && portraitLeft.gameObject.activeSelf)
                {
                    portraitLeft.rectTransform.anchoredPosition = origLeftPos + new Vector2(
                        Random.Range(-22f, 22f) * cur,
                        Random.Range(-10f, 10f) * cur);
                }
                if (portraitRight != null && portraitRight.gameObject.activeSelf)
                {
                    portraitRight.rectTransform.anchoredPosition = origRightPos + new Vector2(
                        Random.Range(-22f, 22f) * cur,
                        Random.Range(-10f, 10f) * cur);
                }

                yield return new WaitForSecondsRealtime(0.033f);
            }

            // Restore clean state
            if (backgroundArtImage != null) backgroundArtImage.material = null;
            if (portraitLeft != null)
            {
                portraitLeft.material = null;
                portraitLeft.rectTransform.anchoredPosition = origLeftPos;
            }
            if (portraitRight != null)
            {
                portraitRight.material = null;
                portraitRight.rectTransform.anchoredPosition = origRightPos;
            }
            if (dialogueBoxRect != null)
            {
                dialogueBoxRect.anchoredPosition = origBoxPos;
            }
            if (glitchOverlayImage != null)
            {
                glitchOverlayImage.material = null;
                glitchOverlayImage.gameObject.SetActive(false);
            }

            if (chroma != null)
            {
                chroma.intensity.value = origChromaVal;
                chroma.active = origChromaActive;
            }
            if (lens != null)
            {
                lens.intensity.value = origLensVal;
                lens.active = origLensActive;
            }

            activeGlitchRoutine = null;
        }

        private static AudioClip CreateProceduralGlitchClip()
        {
            int sampleRate = 44100;
            float lengthSec = 0.55f;
            int sampleCount = Mathf.RoundToInt(sampleRate * lengthSec);
            float[] samples = new float[sampleCount];

            System.Random rng = new System.Random(1337);
            float phase = 0f;
            float heldSample = 0f;
            int holdCounter = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float env = Mathf.Sin(t * Mathf.PI);

                // Bit-crushed digital tear + square-wave frequency jumps
                if (holdCounter <= 0)
                {
                    holdCounter = rng.Next(4, 28);
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                    float freq = (i % 3200 < 1600) ? 140f : 620f;
                    phase += freq / sampleRate;
                    float sq = Mathf.Sin(phase * Mathf.PI * 2f) >= 0f ? 0.6f : -0.6f;
                    heldSample = Mathf.Clamp((noise * 0.7f + sq * 0.4f), -1f, 1f);
                }
                holdCounter--;

                // Gated stutter slices
                float gate = ((i / 1100) % 3 == 1) ? 0.15f : 1.0f;
                samples[i] = heldSample * env * gate * 0.45f;
            }

            AudioClip clip = AudioClip.Create("Procedural_URPGlitch_Burst", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (noiseTexture != null) Destroy(noiseTexture);
            if (imageGlitchMat != null) Destroy(imageGlitchMat);
            if (overlayGlitchMat != null) Destroy(overlayGlitchMat);
            if (proceduralGlitchClip != null) Destroy(proceduralGlitchClip);
        }
    }
}
