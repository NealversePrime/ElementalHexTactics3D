using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Effekseer;
using ElementalHexTactics3D.CameraControl;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ElementalHexTactics3D.Combat
{
    /// <summary>
    /// Coordinates spell visual effects and audio in Elemental Hex Tactics 3D.
    /// Integrates the EVFX Blast Effekseer package alongside procedural particles from CombatVFXManager.
    /// Ensures distinct visual aesthetics and punchy audio for Radiant Smite, Hellfire Blast, Cleave, and Imp Spit.
    /// </summary>
    public class SpellVFXManager3D : MonoBehaviour
    {
        private static SpellVFXManager3D instance;
        public static SpellVFXManager3D Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<SpellVFXManager3D>();
                    if (instance == null)
                    {
                        var prefab = Resources.Load<GameObject>("SpellVFXManager3D");
                        if (prefab != null)
                        {
                            var go = Object.Instantiate(prefab);
                            go.name = "SpellVFXManager3D";
                            instance = go.GetComponent<SpellVFXManager3D>();
                        }
                        if (instance == null)
                        {
                            GameObject go = new GameObject("SpellVFXManager3D");
                            instance = go.AddComponent<SpellVFXManager3D>();
                        }
                    }
                }
                return instance;
            }
        }

        [Header("EVFX Blast Effect Assets")]
        [SerializeField] private EffekseerEffectAsset radiantSmiteAsset;
        [SerializeField] private EffekseerEffectAsset hellfireBlastAsset;
        [SerializeField] private EffekseerEffectAsset impSpitAsset;
        [SerializeField] private EffekseerEffectAsset chainBlastAsset;
        [SerializeField] private EffekseerEffectAsset wallSlamSmokeAsset;
        [SerializeField] private EffekseerEffectAsset decimatingBeamAsset;

        private bool initialized = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureAssetsLoaded();
        }

        private void EnsureAssetsLoaded()
        {
            if (initialized) return;
            initialized = true;

            if (radiantSmiteAsset == null)
                radiantSmiteAsset = Resources.Load<EffekseerEffectAsset>("VFX/EVFX_Blast/EVFX06_05_EnergyBeam");
            if (hellfireBlastAsset == null)
                hellfireBlastAsset = Resources.Load<EffekseerEffectAsset>("VFX/EVFX_Blast/EVFX06_04_OverloadedBlast");
            if (impSpitAsset == null)
                impSpitAsset = Resources.Load<EffekseerEffectAsset>("VFX/EVFX_Blast/EVFX06_01_SingleBlast");
            if (chainBlastAsset == null)
                chainBlastAsset = Resources.Load<EffekseerEffectAsset>("VFX/EVFX_Blast/EVFX06_03_ChainBlast");
            if (wallSlamSmokeAsset == null)
                wallSlamSmokeAsset = Resources.Load<EffekseerEffectAsset>("VFX/EVFX_Blast/EVFX06_02_SmokeBlast");
            if (decimatingBeamAsset == null)
                decimatingBeamAsset = Resources.Load<EffekseerEffectAsset>("VFX/EVFX_Blast/EVFX06_10_DecimatingBeam");

#if UNITY_EDITOR
            if (radiantSmiteAsset == null)
                radiantSmiteAsset = LoadEffectAsset("Assets/Resources/VFX/EVFX_Blast/EVFX06_05_EnergyBeam.asset");
            if (hellfireBlastAsset == null)
                hellfireBlastAsset = LoadEffectAsset("Assets/Resources/VFX/EVFX_Blast/EVFX06_04_OverloadedBlast.asset");
            if (impSpitAsset == null)
                impSpitAsset = LoadEffectAsset("Assets/Resources/VFX/EVFX_Blast/EVFX06_01_SingleBlast.asset");
            if (chainBlastAsset == null)
                chainBlastAsset = LoadEffectAsset("Assets/Resources/VFX/EVFX_Blast/EVFX06_03_ChainBlast.asset");
            if (wallSlamSmokeAsset == null)
                wallSlamSmokeAsset = LoadEffectAsset("Assets/Resources/VFX/EVFX_Blast/EVFX06_02_SmokeBlast.asset");
            if (decimatingBeamAsset == null)
                decimatingBeamAsset = LoadEffectAsset("Assets/Resources/VFX/EVFX_Blast/EVFX06_10_DecimatingBeam.asset");
#endif

            RegisterAsset(radiantSmiteAsset);
            RegisterAsset(hellfireBlastAsset);
            RegisterAsset(impSpitAsset);
            RegisterAsset(chainBlastAsset);
            RegisterAsset(wallSlamSmokeAsset);
            RegisterAsset(decimatingBeamAsset);
        }

        private void RegisterAsset(EffekseerEffectAsset asset)
        {
            if (asset != null && EffekseerSystem.IsValid)
            {
                EffekseerSystem.Instance.LoadEffect(asset);
            }
        }

#if UNITY_EDITOR
        private EffekseerEffectAsset LoadEffectAsset(string path)
        {
            return AssetDatabase.LoadAssetAtPath<EffekseerEffectAsset>(path);
        }
#endif

        /// <summary>
        /// Plays Paladin's Holy Radiant Smite VFX (Golden Energy Beam + bright procedural sparks + sound).
        /// </summary>
        public void PlayRadiantSmiteVFX(Vector3 targetPos, Vector3? casterPos = null)
        {
            EnsureAssetsLoaded();
            Vector3 spawnPos = targetPos + Vector3.up * 0.1f;

            // 1. Play Effekseer Energy Beam
            PlayEffekseerEffect(radiantSmiteAsset, spawnPos, Quaternion.Euler(-90f, 0f, 0f), Vector3.one * 1.2f);

            // 2. Procedural radiant particles
            CombatVFXManager.Instance?.PlayHitSparks(spawnPos + Vector3.up * 0.5f, new Color(1.8f, 1.6f, 0.4f), 24);

            // 3. Audio & Camera Juice
            SoundManager3D.Instance?.PlayRadiantSmite(1.2f);
            TacticalCameraController.Instance?.Shake(0.35f, 0.35f);
        }

        /// <summary>
        /// Plays Demon Lord's Hellfire Blast VFX (Overloaded Volcanic Blast + procedural fire burst + sound).
        /// </summary>
        public void PlayHellfireBlastVFX(Vector3 targetPos)
        {
            EnsureAssetsLoaded();
            Vector3 spawnPos = targetPos + Vector3.up * 0.15f;

            // 1. Play Effekseer Overloaded Blast
            PlayEffekseerEffect(hellfireBlastAsset, spawnPos, Quaternion.identity, Vector3.one * 1.5f);

            // 2. Procedural fiery explosive burst
            CombatVFXManager.Instance?.PlayFireBurst(spawnPos);

            // 3. Audio & Camera Juice
            SoundManager3D.Instance?.PlayHellfireBlast(1.3f);
            TacticalCameraController.Instance?.Shake(0.40f, 0.40f);
        }

        /// <summary>
        /// Plays Magma Imp's Fire Spit VFX (Single Fast Blast + mini fire burst + sound).
        /// </summary>
        public void PlayImpSpitVFX(Vector3 targetPos)
        {
            EnsureAssetsLoaded();
            Vector3 spawnPos = targetPos + Vector3.up * 0.15f;

            // 1. Play Effekseer Single Blast
            PlayEffekseerEffect(impSpitAsset, spawnPos, Quaternion.identity, Vector3.one * 0.9f);

            // 2. Procedural sparks
            CombatVFXManager.Instance?.PlayFireBurst(spawnPos);

            // 3. Audio
            SoundManager3D.Instance?.PlayImpSpit(1.0f);
        }

        /// <summary>
        /// Plays Demon Lord's Abyssal Cleave VFX (Chain Blast Arc + heavy slash sparks + sound).
        /// </summary>
        public void PlayCleaveVFX(Vector3 targetPos)
        {
            EnsureAssetsLoaded();
            Vector3 spawnPos = targetPos + Vector3.up * 0.2f;

            // 1. Play Effekseer Chain Blast
            PlayEffekseerEffect(chainBlastAsset, spawnPos, Quaternion.identity, Vector3.one * 1.3f);

            // 2. Procedural crimson sparks
            CombatVFXManager.Instance?.PlayHitSparks(spawnPos + Vector3.up * 0.4f, new Color(2.0f, 0.4f, 0.1f), 20);

            // 3. Audio & Camera Juice
            SoundManager3D.Instance?.PlayChainBlast(1.1f);
            TacticalCameraController.Instance?.Shake(0.45f, 0.45f);
        }

        /// <summary>
        /// Plays Wall Slam impact VFX (Smoke Blast + rock chips + dust puff + sound).
        /// </summary>
        public void PlayWallSlamVFX(Vector3 targetPos, Vector3 impactNormal)
        {
            EnsureAssetsLoaded();
            Vector3 spawnPos = targetPos + Vector3.up * 0.3f;

            // 1. Play Effekseer Smoke Blast
            Quaternion rot = impactNormal != Vector3.zero ? Quaternion.LookRotation(impactNormal) : Quaternion.identity;
            PlayEffekseerEffect(wallSlamSmokeAsset, spawnPos, rot, Vector3.one * 0.18f, speed: 1.8f, tint: new Color(1f, 1f, 1f, 0.45f));

            // 2. Procedural wall slam debris & dust
            CombatVFXManager.Instance?.PlayWallSlam(spawnPos, impactNormal);

            // 3. Audio & Camera Juice
            SoundManager3D.Instance?.PlayWallSlam(1.3f);
            TacticalCameraController.Instance?.Shake(0.50f, 0.50f);
        }

        /// <summary>True when the EVFX Blast projectile effect is loaded, so the plain sphere orb can be skipped.</summary>
        public bool HasProjectileEffect
        {
            get
            {
                EnsureAssetsLoaded();
                return impSpitAsset != null;
            }
        }

        /// <summary>
        /// Flies an Effekseer blast (EVFX06_01_SingleBlast, tinted with the spell colour) along the same arc the
        /// old sphere orb used. Returns false immediately if Effekseer could not play it so the caller can fall back.
        /// </summary>
        public IEnumerator PlayProjectileVFX(Vector3 start, Vector3 target, Color tint, float duration)
        {
            EnsureAssetsLoaded();
            EffekseerHandle handle = default;
            bool ok = false;
            try
            {
                if (impSpitAsset != null && EffekseerSystem.IsValid)
                {
                    EffekseerSystem.Instance.LoadEffect(impSpitAsset);
                    handle = EffekseerSystem.PlayEffect(impSpitAsset, start);
                    ok = handle.enabled || handle.exists;
                    if (ok)
                    {
                        handle.SetScale(Vector3.one * 0.8f);
                        handle.SetAllColor(tint);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[SpellVFXManager3D] Projectile VFX failed: " + ex.Message);
            }

            if (!ok)
            {
                Debug.LogWarning("[SpellVFXManager3D] Effekseer projectile could not be played (asset/system missing).");
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 pos = Vector3.Lerp(start, target, t);
                pos.y = Mathf.Lerp(start.y, target.y, t) + Mathf.Sin(t * Mathf.PI) * 0.8f;
                handle.SetLocation(pos);
                yield return null;
            }
        }

        private void PlayEffekseerEffect(EffekseerEffectAsset asset, Vector3 pos, Quaternion rotation, Vector3 scale, float speed = 1f, Color? tint = null)
        {
            if (asset == null)
            {
                Debug.LogWarning("[SpellVFXManager3D] Effekseer asset is null - effect skipped (using procedural fallback only).");
                return;
            }

            try
            {
                if (EffekseerSystem.IsValid)
                {
                    EffekseerSystem.Instance.LoadEffect(asset);
                    EffekseerHandle handle = EffekseerSystem.PlayEffect(asset, pos);
                    if (handle.enabled || handle.exists)
                    {
                        handle.SetRotation(rotation);
                        handle.SetScale(scale);
                        if (speed != 1f) handle.speed = speed;
                        if (tint.HasValue) handle.SetAllColor(tint.Value);
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[SpellVFXManager3D] EffekseerSystem.PlayEffect fallback: " + ex.Message);
            }

            // Fallback via transient EffekseerEmitter component
            SpawnEmitterFallback(asset, pos, rotation, scale);
        }

        private void SpawnEmitterFallback(EffekseerEffectAsset asset, Vector3 pos, Quaternion rotation, Vector3 scale)
        {
            GameObject emitterObj = new GameObject("EffekseerEmitter_" + asset.name);
            emitterObj.transform.position = pos;
            emitterObj.transform.rotation = rotation;
            emitterObj.transform.localScale = scale;

            EffekseerEmitter emitter = emitterObj.AddComponent<EffekseerEmitter>();
            emitter.effectAsset = asset;
            emitter.playOnStart = true;
            emitter.isLooping = false;
            emitter.Play();

            // Destroy object after 3 seconds
            Destroy(emitterObj, 3.0f);
        }
    }
}

