using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ElementalHexTactics3D.Combat
{
    /// <summary>
    /// Pure procedural Particle VFX Manager for Elemental Hex Tactics 3D.
    /// Creates and manages particle textures, materials, and emission systems with zero external assets.
    /// Works with Unity 6 Universal Render Pipeline (URP) and Bloom post-processing.
    /// </summary>
    public class CombatVFXManager : MonoBehaviour
    {
        private static CombatVFXManager instance;
        public static CombatVFXManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<CombatVFXManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("CombatVFXManager");
                        instance = go.AddComponent<CombatVFXManager>();
                    }
                }
                return instance;
            }
        }

        private Texture2D softGlowTex;
        private Texture2D sharpSparkTex;
        private Texture2D cloudPuffTex;

        private Material glowMaterial;
        private Material sparkMaterial;
        private Material cloudMaterial;

        private void Awake()
        {
            if (instance == null) instance = this;
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureMaterials();
        }

        private void EnsureMaterials()
        {
            if (glowMaterial != null && sparkMaterial != null && cloudMaterial != null) return;
            GenerateProceduralTextures();
            GenerateProceduralMaterials();
        }

        private void GenerateProceduralTextures()
        {
            // 1. Soft Radial Glow (32x32)
            softGlowTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            softGlowTex.filterMode = FilterMode.Bilinear;
            softGlowTex.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2(15.5f, 15.5f);
            float maxR = 15.5f;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normDist = Mathf.Clamp01(dist / maxR);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - normDist), 2f);
                    softGlowTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            softGlowTex.Apply();

            // 2. Sharp Cross Spark (32x32)
            sharpSparkTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            sharpSparkTex.filterMode = FilterMode.Bilinear;
            sharpSparkTex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dx = Mathf.Abs(x - 15.5f) / 15.5f;
                    float dy = Mathf.Abs(y - 15.5f) / 15.5f;

                    // Diamond / 4-point star formula
                    float star = Mathf.Max(0f, 1f - (dx * 0.2f + dy * 2.8f)) +
                                 Mathf.Max(0f, 1f - (dy * 0.2f + dx * 2.8f));
                    float core = Mathf.Max(0f, 1f - (dx * 1.5f + dy * 1.5f));
                    float intensity = Mathf.Clamp01(star * 0.7f + core * 0.8f);

                    sharpSparkTex.SetPixel(x, y, new Color(1f, 1f, 1f, intensity));
                }
            }
            sharpSparkTex.Apply();

            // 3. Soft Cloud Puff (32x32)
            cloudPuffTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            cloudPuffTex.filterMode = FilterMode.Bilinear;
            cloudPuffTex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normDist = Mathf.Clamp01(dist / maxR);
                    // Smooth cubic falloff for cloudy smoke
                    float alpha = Mathf.SmoothStep(1f, 0f, normDist);
                    cloudPuffTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * 0.85f));
                }
            }
            cloudPuffTex.Apply();
        }

        private void GenerateProceduralMaterials()
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                 ?? Shader.Find("Particles/Standard Unlit")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit")
                                 ?? Shader.Find("Sprites/Default");

            // Glow Material (Additive-like bright unlit)
            glowMaterial = new Material(particleShader);
            glowMaterial.name = "Mat_Procedural_Glow";
            glowMaterial.mainTexture = softGlowTex;
            if (glowMaterial.HasProperty("_BaseMap")) glowMaterial.SetTexture("_BaseMap", softGlowTex);
            if (glowMaterial.HasProperty("_Surface")) glowMaterial.SetFloat("_Surface", 1f); // Transparent
            if (glowMaterial.HasProperty("_Blend")) glowMaterial.SetFloat("_Blend", 1f); // Additive
            glowMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glowMaterial.EnableKeyword("_BLENDMODE_ADD");
            glowMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            glowMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glowMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            glowMaterial.SetInt("_ZWrite", 0);

            // Spark Material
            sparkMaterial = new Material(particleShader);
            sparkMaterial.name = "Mat_Procedural_Spark";
            sparkMaterial.mainTexture = sharpSparkTex;
            if (sparkMaterial.HasProperty("_BaseMap")) sparkMaterial.SetTexture("_BaseMap", sharpSparkTex);
            if (sparkMaterial.HasProperty("_Surface")) sparkMaterial.SetFloat("_Surface", 1f);
            sparkMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            sparkMaterial.EnableKeyword("_BLENDMODE_ADD");
            sparkMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            sparkMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sparkMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            sparkMaterial.SetInt("_ZWrite", 0);

            // Cloud / Smoke Material (Alpha blended)
            cloudMaterial = new Material(particleShader);
            cloudMaterial.name = "Mat_Procedural_Cloud";
            cloudMaterial.mainTexture = cloudPuffTex;
            if (cloudMaterial.HasProperty("_BaseMap")) cloudMaterial.SetTexture("_BaseMap", cloudPuffTex);
            if (cloudMaterial.HasProperty("_Surface")) cloudMaterial.SetFloat("_Surface", 1f);
            if (cloudMaterial.HasProperty("_Blend")) cloudMaterial.SetFloat("_Blend", 0f); // Alpha blend
            cloudMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            cloudMaterial.EnableKeyword("_BLENDMODE_ALPHA");
            cloudMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            cloudMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            cloudMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            cloudMaterial.SetInt("_ZWrite", 0);
        }

        private ParticleSystem CreateParticleSystem(GameObject target)
        {
            ParticleSystem ps = target.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            return ps;
        }

        /// <summary>
        /// Plays impact sparks and debris when a unit takes damage.
        /// </summary>
        public void PlayHitSparks(Vector3 worldPos, Color color, int count = 16)
        {
            EnsureMaterials();
            GameObject vfxObj = new GameObject("VFX_HitSparks");
            vfxObj.transform.position = worldPos;

            ParticleSystem ps = CreateParticleSystem(vfxObj);
            ParticleSystemRenderer psRenderer = vfxObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = sparkMaterial;
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;

            var main = ps.main;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(color * 1.6f, Color.white * 1.8f);
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.gravityModifier = 0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ps.Play();
        }

        /// <summary>
        /// Plays wall slam impact VFX: sharp rock chips, sparks, and dust puff.
        /// Triggered when Into The Breach-style shove collides with a wall/cliff or occupied tile.
        /// </summary>
        public void PlayWallSlam(Vector3 worldPos, Vector3 impactNormal)
        {
            EnsureMaterials();
            GameObject vfxObj = new GameObject("VFX_WallSlam");
            vfxObj.transform.position = worldPos;

            // 1. Rock Chips / Debris burst
            ParticleSystem psDebris = CreateParticleSystem(vfxObj);
            ParticleSystemRenderer rDebris = vfxObj.GetComponent<ParticleSystemRenderer>();
            rDebris.material = sparkMaterial;

            var mainD = psDebris.main;
            mainD.loop = false;
            mainD.duration = 0.5f;
            mainD.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            mainD.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 6.5f);
            mainD.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            mainD.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.4f, 1.8f), new Color(0.85f, 0.65f, 0.45f, 1f));
            mainD.gravityModifier = 2.0f;
            mainD.stopAction = ParticleSystemStopAction.Destroy;

            var emD = psDebris.emission;
            emD.rateOverTime = 0;
            emD.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 24) });

            var shapeD = psDebris.shape;
            shapeD.shapeType = ParticleSystemShapeType.Cone;
            shapeD.angle = 45f;
            shapeD.radius = 0.2f;
            if (impactNormal != Vector3.zero)
            {
                vfxObj.transform.rotation = Quaternion.LookRotation(impactNormal);
            }

            var sizeD = psDebris.sizeOverLifetime;
            sizeD.enabled = true;
            AnimationCurve curveD = new AnimationCurve();
            curveD.AddKey(0f, 1f);
            curveD.AddKey(1f, 0.1f);
            sizeD.size = new ParticleSystem.MinMaxCurve(1f, curveD);

            // 2. Dust Puff Child
            GameObject dustObj = new GameObject("DustPuff");
            dustObj.transform.SetParent(vfxObj.transform, false);

            ParticleSystem psDust = CreateParticleSystem(dustObj);
            ParticleSystemRenderer rDust = dustObj.GetComponent<ParticleSystemRenderer>();
            rDust.material = cloudMaterial;

            var mainDust = psDust.main;
            mainDust.loop = false;
            mainDust.duration = 0.6f;
            mainDust.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            mainDust.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
            mainDust.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            mainDust.startColor = new Color(0.75f, 0.70f, 0.60f, 0.45f);
            mainDust.gravityModifier = -0.2f;

            var emDust = psDust.emission;
            emDust.rateOverTime = 0;
            emDust.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });

            var shapeDust = psDust.shape;
            shapeDust.shapeType = ParticleSystemShapeType.Sphere;
            shapeDust.radius = 0.25f;

            var sizeDust = psDust.sizeOverLifetime;
            sizeDust.enabled = true;
            AnimationCurve curveDust = new AnimationCurve();
            curveDust.AddKey(0f, 0.4f);
            curveDust.AddKey(1f, 1.3f);
            sizeDust.size = new ParticleSystem.MinMaxCurve(1f, curveDust);

            psDebris.Play();
            psDust.Play();
        }

        /// <summary>
        /// Plays fiery explosive burst when Fireball hits a tile or unit.
        /// </summary>
        public void PlayFireBurst(Vector3 worldPos)
        {
            EnsureMaterials();
            GameObject vfxObj = new GameObject("VFX_FireBurst");
            vfxObj.transform.position = worldPos + Vector3.up * 0.2f;

            ParticleSystem ps = CreateParticleSystem(vfxObj);
            ParticleSystemRenderer r = vfxObj.GetComponent<ParticleSystemRenderer>();
            r.material = glowMaterial;

            var main = ps.main;
            main.loop = false;
            main.duration = 0.45f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.65f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1.8f, 0.6f, 0.1f, 1f), new Color(2.0f, 0.9f, 0.2f, 1f));
            main.gravityModifier = -0.8f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 0.1f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ps.Play();
        }

        /// <summary>
        /// Plays fountain water splash when Water Surge hits.
        /// </summary>
        public void PlayWaterSplash(Vector3 worldPos)
        {
            EnsureMaterials();
            GameObject vfxObj = new GameObject("VFX_WaterSplash");
            vfxObj.transform.position = worldPos + Vector3.up * 0.1f;

            ParticleSystem ps = CreateParticleSystem(vfxObj);
            ParticleSystemRenderer r = vfxObj.GetComponent<ParticleSystemRenderer>();
            r.material = glowMaterial;

            var main = ps.main;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.2f, 0.7f, 1.8f, 0.85f), new Color(0.5f, 0.9f, 1.8f, 0.95f));
            main.gravityModifier = 1.8f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 22) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.2f;
            vfxObj.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // Shoot upward

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 0.2f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ps.Play();
        }

        /// <summary>
        /// Plays billowing steam cloud when water cools magma or fire boils water.
        /// </summary>
        public void PlaySteamCloud(Vector3 worldPos)
        {
            EnsureMaterials();
            GameObject vfxObj = new GameObject("VFX_SteamCloud");
            vfxObj.transform.position = worldPos + Vector3.up * 0.2f;

            ParticleSystem ps = CreateParticleSystem(vfxObj);
            ParticleSystemRenderer r = vfxObj.GetComponent<ParticleSystemRenderer>();
            r.material = cloudMaterial;

            var main = ps.main;
            main.loop = false;
            main.duration = 1.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
            main.startColor = new Color(0.92f, 0.95f, 1.0f, 0.55f);
            main.gravityModifier = -0.45f; // Rises into sky
            main.stopAction = ParticleSystemStopAction.Destroy;

            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.5f);
            curve.AddKey(1f, 1.8f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ps.Play();
        }

        /// <summary>
        /// Attaches a continuous glowing ember particle system to a Magma tile.
        /// </summary>
        public GameObject AttachLavaEmbers(Transform tileParent)
        {
            EnsureMaterials();
            GameObject embersObj = new GameObject("LavaEmbers");
            embersObj.transform.SetParent(tileParent, false);
            embersObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            ParticleSystem ps = CreateParticleSystem(embersObj);
            ParticleSystemRenderer r = embersObj.GetComponent<ParticleSystemRenderer>();
            r.material = glowMaterial;

            var main = ps.main;
            main.loop = true;
            main.duration = 2.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(2.0f, 0.7f, 0.1f, 1f), new Color(1.8f, 0.3f, 0.05f, 1f));
            main.gravityModifier = -0.15f; // Drift gently upward

            var em = ps.emission;
            em.rateOverTime = 3.5f; // Subtle, gentle ambient embers

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.65f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.6f);
            curve.AddKey(0.4f, 1.2f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ps.Play();
            return embersObj;
        }

        /// <summary>
        /// Attaches a continuous swirling violet-black portal vortex to the Abyssal Rift tile.
        /// </summary>
        public GameObject AttachRiftVortex(Transform riftParent)
        {
            EnsureMaterials();
            GameObject vortexObj = new GameObject("RiftVortexParticles");
            vortexObj.layer = LayerMask.NameToLayer("Ignore Raycast");
            vortexObj.transform.SetParent(riftParent, false);
            vortexObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            ParticleSystem ps = CreateParticleSystem(vortexObj);
            ParticleSystemRenderer r = vortexObj.GetComponent<ParticleSystemRenderer>();
            r.material = glowMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;

            var main = ps.main;
            main.loop = true;
            main.duration = 2.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.1f, 1.5f, 0.9f), new Color(0.3f, 0.02f, 0.6f, 0.9f));
            main.gravityModifier = -0.2f;

            var em = ps.emission;
            em.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.7f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.4f);
            curve.AddKey(0.5f, 1.2f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ps.Play();
            return vortexObj;
        }

        /// <summary>
        /// Plays a violent upward void vortex and shockwave when an enemy is sacrificed into the Rift.
        /// </summary>
        public void PlayRiftSacrifice(Vector3 worldPos)
        {
            EnsureMaterials();
            GameObject vfxObj = new GameObject("VFX_RiftSacrifice");
            vfxObj.transform.position = worldPos + Vector3.up * 0.1f;

            ParticleSystem ps = CreateParticleSystem(vfxObj);
            ParticleSystemRenderer r = vfxObj.GetComponent<ParticleSystemRenderer>();
            r.material = sparkMaterial;

            var main = ps.main;
            main.loop = false;
            main.duration = 0.8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 6.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1.0f, 0.2f, 2.0f, 1f), new Color(0.2f, 0.0f, 0.5f, 1f));
            main.gravityModifier = -0.5f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 32) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.3f;
            vfxObj.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // shoot upward

            ps.Play();
        }

        /// <summary>
        /// Plays an explosive magma shockwave ring when the Titan erupts through the Rift.
        /// </summary>
        public void PlayTitanShockwave(Vector3 worldPos)
        {
            EnsureMaterials();
            GameObject vfxObj = new GameObject("VFX_TitanShockwave");
            vfxObj.transform.position = worldPos + Vector3.up * 0.1f;

            ParticleSystem ps = CreateParticleSystem(vfxObj);
            ParticleSystemRenderer r = vfxObj.GetComponent<ParticleSystemRenderer>();
            r.material = glowMaterial;

            var main = ps.main;
            main.loop = false;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4.0f, 8.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(2.5f, 0.8f, 0.1f, 1f), new Color(2.0f, 0.2f, 0.05f, 1f));
            main.stopAction = ParticleSystemStopAction.Destroy;

            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 36) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            ps.Play();
        }

        private void OnDestroy()
        {
            if (softGlowTex != null) Destroy(softGlowTex);
            if (sharpSparkTex != null) Destroy(sharpSparkTex);
            if (cloudPuffTex != null) Destroy(cloudPuffTex);

            if (glowMaterial != null) Destroy(glowMaterial);
            if (sparkMaterial != null) Destroy(sparkMaterial);
            if (cloudMaterial != null) Destroy(cloudMaterial);
        }
    }
}
