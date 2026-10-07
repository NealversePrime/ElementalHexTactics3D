using UnityEngine;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Combat;

namespace ElementalHexTactics3D.Grid
{
    /// <summary>
    /// Represents an interactive 3D hexagonal tile pillar in the tactical battlefield.
    /// Manages coordinates, elevation, elemental state, materials, and occupant references.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class HexTile3D : MonoBehaviour
    {
        [Header("Grid Position & Elevation")]
        [SerializeField] private HexCoordinates coordinates;
        [SerializeField] private int elevation = 0;

        [Header("Elemental State")]
        [SerializeField] private TileState state = TileState.Barren;
        [SerializeField] private int tierLevel = 0;

        [Header("Rendering Components")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private MeshCollider meshCollider;

        private Material currentTopMaterial;
        private Material currentSideMaterial;
        private MaterialPropertyBlock propBlock;
        private GameObject ambientVfxObj;
        private bool isHovered = false;
        private bool isSelected = false;
        private bool isReachable = false;

        private Vector3 restingLocalPosition;
        private Vector3 originalBasePosition;

        public HexCoordinates Coordinates => coordinates;
        public int Elevation => elevation;
        public TileState State => state;
        public int TierLevel => tierLevel;
        public bool IsHovered => isHovered;
        public bool IsSelected => isSelected;
        public bool IsReachable => isReachable;

        /// <summary>
        /// Unit currently occupying this 3D hex tile.
        /// </summary>
        public Component CurrentOccupant { get; set; }
        public bool IsOccupied => GetOccupant() != null;

        public TacticalUnit3D GetOccupant()
        {
            if (CurrentOccupant is TacticalUnit3D unit)
            {
                if (unit != null && unit.gameObject.activeInHierarchy && unit.CurrentHealth > 0)
                {
                    return unit;
                }
                CurrentOccupant = null;
            }

            // Proximity fallback: find any active living unit standing directly on this tile's top position
            TacticalUnit3D[] allUnits = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsSortMode.None);
            Vector3 myPos = transform.position;
            foreach (var u in allUnits)
            {
                if (u == null || !u.gameObject.activeInHierarchy || u.CurrentHealth <= 0) continue;
                if (u.CurrentTile == this || Vector3.Distance(new Vector3(u.transform.position.x, 0f, u.transform.position.z), new Vector3(myPos.x, 0f, myPos.z)) < 0.65f)
                {
                    CurrentOccupant = u;
                    return u;
                }
            }
            return null;
        }

        private void Awake()
        {
            EnsureComponents();
            originalBasePosition = transform.localPosition;
            restingLocalPosition = transform.localPosition;
            propBlock = new MaterialPropertyBlock();
        }

        private void EnsureComponents()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
        }

        /// <summary>
        /// Initializes the 3D tile with coordinates, elevation, mesh, and materials.
        /// </summary>
        public void Initialize(HexCoordinates coords, int elev, Mesh mesh, Material topMat, Material sideMat)
        {
            EnsureComponents();
            coordinates = coords;
            elevation = elev;

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;

            currentTopMaterial = topMat;
            currentSideMaterial = sideMat;

            if (currentTopMaterial != null)
            {
                currentTopMaterial.EnableKeyword("_EMISSION");
            }

            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterials = new Material[] { currentTopMaterial, currentSideMaterial };
            }

            originalBasePosition = transform.localPosition;
            restingLocalPosition = transform.localPosition;
            UpdateVisuals();
        }

        private void Start()
        {
            UpdateAmbientVFX();
        }

        private void UpdateAmbientVFX()
        {
            if (state == TileState.Magma)
            {
                if (ambientVfxObj == null && CombatVFXManager.Instance != null)
                {
                    ambientVfxObj = CombatVFXManager.Instance.AttachLavaEmbers(transform);
                }
            }
            else
            {
                if (ambientVfxObj != null)
                {
                    Destroy(ambientVfxObj);
                    ambientVfxObj = null;
                }
            }
        }

        /// <summary>
        /// Changes the elemental state and tier of the tile, updating to the correct elemental material.
        /// </summary>
        public void SetState(TileState newState, int newTier)
        {
            state = newState;
            tierLevel = newTier;

            if (originalBasePosition == Vector3.zero && transform.localPosition != Vector3.zero)
            {
                originalBasePosition = transform.localPosition;
            }

            // Visually pop Stone Pillar up by 1 unit elevation
            if (state == TileState.StonePillar)
            {
                restingLocalPosition = originalBasePosition + Vector3.up * 1.0f;
                transform.localPosition = restingLocalPosition;
            }
            else
            {
                restingLocalPosition = originalBasePosition;
                transform.localPosition = restingLocalPosition;
            }

            // Keep unit standing on top surface so they are never swallowed or buried inside the tile
            TacticalUnit3D occupant = GetOccupant();
            if (occupant != null)
            {
                occupant.transform.position = GetTopCenterPosition();
            }

            if (newState != TileState.Barren && newState != TileState.StonePillar)
            {
                customBaseTint = null;
                customBaseEmission = null;
            }

            if (HexGrid3D.Instance != null)
            {
                Material mat = HexGrid3D.Instance.GetTopMaterial(state, tierLevel);
                SetTopMaterial(mat);
            }
            else
            {
                UpdateVisuals();
            }

            UpdateAmbientVFX();
        }

        private Color? customBaseTint = null;
        private Color? customBaseEmission = null;

        /// <summary>
        /// Sets a custom aesthetic tint and optional emission for interior / thematic tiles (e.g., royal red carpet, dark marble).
        /// </summary>
        public void SetCustomTint(Color tint, Color? emission = null)
        {
            customBaseTint = tint;
            customBaseEmission = emission;
            UpdateVisuals();
        }

        public void ClearCustomTint()
        {
            customBaseTint = null;
            customBaseEmission = null;
            UpdateVisuals();
        }

        /// <summary>
        /// Projects a continuous top-down battlemap texture across the hex surface in world space,
        /// ensuring seamless carpets, borders, and marble floors without per-hex tiling repetition.
        /// </summary>
        public void ApplyWorldProjectedUVs(Material projectedMaterial)
        {
            ApplyWorldProjectedUVs(projectedMaterial, -5.5f, 5.5f, -5.0f, 5.0f);
        }

        public void ApplyWorldProjectedUVs(Material projectedMaterial, float minX, float maxX, float minZ, float maxZ)
        {
            EnsureComponents();
            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            Mesh instancedMesh = Instantiate(meshFilter.sharedMesh);
            instancedMesh.name = $"{meshFilter.sharedMesh.name}_WorldUV_{coordinates.Q}_{coordinates.R}";

            Vector3[] verts = instancedMesh.vertices;
            Vector2[] uvs = instancedMesh.uv;

            // Top cap comprises vertices 0..6 (center + 6 outer corners)
            int topVertCount = Mathf.Min(7, verts.Length);
            for (int i = 0; i < topVertCount; i++)
            {
                Vector3 worldV = transform.TransformPoint(verts[i]);
                float u = Mathf.Clamp01((worldV.x - minX) / (maxX - minX));
                float v = Mathf.Clamp01((worldV.z - minZ) / (maxZ - minZ));
                uvs[i] = new Vector2(u, v);
            }

            instancedMesh.uv = uvs;
            meshFilter.sharedMesh = instancedMesh;
            if (meshCollider != null) meshCollider.sharedMesh = instancedMesh;

            ClearCustomTint();
            if (projectedMaterial != null)
            {
                SetTopMaterial(projectedMaterial);
            }
        }

        public void SetTopMaterial(Material topMat)
        {
            if (topMat == null || meshRenderer == null) return;
            currentTopMaterial = topMat;
            currentTopMaterial.EnableKeyword("_EMISSION");
            meshRenderer.sharedMaterials = new Material[] { currentTopMaterial, currentSideMaterial };
            UpdateVisuals();
        }

        private bool isTutorialHighlighted = false;
        public bool IsTutorialHighlighted => isTutorialHighlighted;

        public void SetTutorialHighlighted(bool highlighted)
        {
            if (isTutorialHighlighted == highlighted) return;
            isTutorialHighlighted = highlighted;
            UpdateVisuals();
        }

        public void SetReachable(bool reachable)
        {
            if (isReachable == reachable) return;
            isReachable = reachable;
            UpdateVisuals();
        }

        /// <summary>
        /// Updates hover, reachable, and selection visual highlights using MaterialPropertyBlock (no GC or material cloning).
        /// </summary>
        public void UpdateVisuals()
        {
            if (meshRenderer == null) return;
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            meshRenderer.GetPropertyBlock(propBlock, 0); // Submesh 0 (Top Cap)

            Color tintColor = customBaseTint.HasValue ? customBaseTint.Value : Color.white;
            Color emissionColor = customBaseEmission.HasValue ? customBaseEmission.Value : Color.black;

            if (state == TileState.Mud)
            {
                tintColor = new Color(0.42f, 0.28f, 0.16f, 1f); // Rich muddy clay brown
                emissionColor = new Color(0.06f, 0.04f, 0.02f, 1f);
            }
            else if (state == TileState.StonePillar)
            {
                tintColor = new Color(0.62f, 0.64f, 0.68f, 1f); // Solid granite gray
                emissionColor = new Color(0.12f, 0.12f, 0.14f, 1f);
            }

            if (isTutorialHighlighted)
            {
                tintColor = new Color(2.1f, 1.7f, 0.4f, 1f); // Radiant gold highlight
                emissionColor = new Color(0.65f, 0.48f, 0.08f, 1f);
            }
            else if (isSelected)
            {
                tintColor = new Color(1.8f, 1.6f, 0.5f, 1f); // Golden tactical tint
                emissionColor = new Color(0.45f, 0.35f, 0.05f, 1f); // Warm golden emission
            }
            else if (isHovered)
            {
                if (isReachable)
                {
                    tintColor = new Color(0.5f, 1.6f, 2.4f, 1f); // Bright electric cyan on hover
                    emissionColor = new Color(0.2f, 0.65f, 1.0f, 1f);
                }
                else
                {
                    if (state == TileState.Mud || state == TileState.StonePillar)
                        tintColor *= 1.35f;
                    else if (customBaseTint.HasValue)
                        tintColor = customBaseTint.Value * 1.35f;
                    else
                        tintColor = new Color(1.35f, 1.35f, 1.35f, 1f);

                    emissionColor = new Color(0.12f, 0.12f, 0.12f, 1f);
                }
            }
            else if (isReachable)
            {
                tintColor = new Color(0.25f, 1.15f, 2.0f, 1f); // Vivid tactical cyan
                emissionColor = new Color(0.08f, 0.38f, 0.80f, 1f); // Glowing tactical field
            }

            propBlock.SetColor("_BaseColor", tintColor);
            propBlock.SetColor("_Color", tintColor);
            propBlock.SetColor("_EmissionColor", emissionColor);
            meshRenderer.SetPropertyBlock(propBlock, 0);
        }

        public void SetHovered(bool hovered)
        {
            if (isHovered == hovered) return;
            isHovered = hovered;

            // Subtle vertical pop on hover
            float hoverLift = isHovered ? 0.12f : 0f;
            transform.localPosition = restingLocalPosition + Vector3.up * hoverLift;

            UpdateVisuals();
        }

        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;
            isSelected = selected;
            UpdateVisuals();
        }

        /// <summary>
        /// Returns the top surface center position in world space (where units stand).
        /// </summary>
        public Vector3 GetTopCenterPosition()
        {
            return transform.position;
        }

        private void OnDestroy()
        {
            if (ambientVfxObj != null)
            {
                Destroy(ambientVfxObj);
            }
        }
    }
}
