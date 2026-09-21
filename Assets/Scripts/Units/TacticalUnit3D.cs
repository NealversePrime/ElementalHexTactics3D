using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.Turn;
using ElementalHexTactics3D.CameraControl;

namespace ElementalHexTactics3D.Units
{
    public enum UnitFaction
    {
        Player,
        Enemy
    }

    public enum UnitArchetype
    {
        Commander,
        Titan,
        Minion
    }

    public enum ElementalAffinity
    {
        None,
        Fire,
        Water
    }

    /// <summary>
    /// Represents an interactive 2.5D tactical unit on the 3D hexagonal battlefield.
    /// Manages unit stats, grid positioning, smooth hop movement across elevations,
    /// camera-facing billboarding, selection base ring indicators, turn action economy,
    /// and dynamic FFT-style elemental tile attunement.
    /// </summary>
    public class TacticalUnit3D : MonoBehaviour
    {
        [Header("Identity & Faction")]
        [SerializeField] private string unitName = "Tactical Unit";
        [SerializeField] private UnitFaction faction = UnitFaction.Player;
        [SerializeField] private UnitArchetype archetype = UnitArchetype.Commander;
        [SerializeField] private ElementalAffinity affinity = ElementalAffinity.None;

        [Header("Stats")]
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private int currentHealth = 10;
        [SerializeField] private int moveRange = 3;
        [SerializeField] private int baseAttackDamage = 3;

        [Header("Turn Action Economy")]
        [SerializeField] private bool hasMovedThisTurn = false;
        [SerializeField] private bool hasActedThisTurn = false;

        [Header("Grid Positioning")]
        [SerializeField] private HexCoordinates coordinates;
        [SerializeField] private HexTile3D currentTile;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Billboard25D billboard;
        [SerializeField] private GameObject baseRing;
        [SerializeField] private SpriteRenderer baseRingRenderer;

        private bool isMoving = false;
        private bool isSelected = false;

        public string UnitName => unitName;
        public UnitFaction Faction => faction;
        public UnitArchetype Archetype => archetype;
        public ElementalAffinity Affinity => affinity;
        public int MaxHealth => maxHealth;
        public int CurrentHealth
        {
            get => currentHealth;
            set => currentHealth = Mathf.Clamp(value, 0, maxHealth);
        }

        public void Revive(int hp = -1)
        {
            currentHealth = (hp > 0) ? Mathf.Min(hp, maxHealth) : maxHealth;
            ClearMobilityDebuffs();
        }

        public int MoveRange => moveRange;
        public int BaseAttackDamage => baseAttackDamage;

        // Attunement Properties (FFT Geomancer System)
        public string CurrentAttunementName { get; private set; } = "None";
        public int BonusAttackDamage { get; private set; } = 0;
        public int BonusMoveRange { get; private set; } = 0;
        public bool IsMagmaImmune { get; private set; } = false;

        [Header("Mobility Debuffs")]
        [SerializeField] private int immobilizeTurns = 0;
        [SerializeField] private int crippleTurns = 0;

        public int ImmobilizeTurns => immobilizeTurns;
        public int CrippleTurns => crippleTurns;
        public bool IsImmobilized => immobilizeTurns > 0;
        public bool IsCrippled => crippleTurns > 0;

        public int EffectiveAttackDamage => baseAttackDamage + BonusAttackDamage;
        public int EffectiveMoveRange
        {
            get
            {
                if (IsImmobilized) return 0;
                int normal = moveRange + BonusMoveRange;
                if (IsCrippled) return Mathf.Min(1, normal);
                return normal;
            }
        }

        [Header("Land Consumption Economy")]
        [SerializeField] private int elementalCores = 0;
        public int ElementalCores => elementalCores;

        public void AddElementalCore(int amount = 1)
        {
            elementalCores += amount;
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.SpawnDamageText(transform.position, $"<color=#FFD54F>+{amount} ELEMENTAL CORE!</color>", new Color(1f, 0.85f, 0.2f), 1.5f);
            }
        }

        public bool ConsumeElementalCore(int amount = 1)
        {
            if (elementalCores >= amount)
            {
                elementalCores -= amount;
                return true;
            }
            return false;
        }

        public bool HasMovedThisTurn
        {
            get => hasMovedThisTurn;
            set
            {
                hasMovedThisTurn = value;
                UpdateBaseRingVisuals();
            }
        }

        public bool HasActedThisTurn
        {
            get => hasActedThisTurn;
            set
            {
                hasActedThisTurn = value;
                UpdateBaseRingVisuals();
            }
        }

        public bool IsExhausted => hasMovedThisTurn && hasActedThisTurn;

        public void ResetTurnActions()
        {
            hasMovedThisTurn = false;
            hasActedThisTurn = false;
            UpdateAttunement();
            UpdateBaseRingVisuals();
        }

        public void ApplyMobilityDebuff(int immobilizeRounds, int crippleRounds)
        {
            immobilizeTurns = Mathf.Max(immobilizeTurns, immobilizeRounds);
            crippleTurns = Mathf.Max(crippleTurns, crippleRounds);
            UpdateAttunement();
            UpdateBaseRingVisuals();
        }

        public void ClearMobilityDebuffs()
        {
            if (immobilizeTurns > 0 || crippleTurns > 0)
            {
                immobilizeTurns = 0;
                crippleTurns = 0;
                UpdateAttunement();
                UpdateBaseRingVisuals();
            }
        }

        public void OnTurnEnd()
        {
            if (immobilizeTurns > 0)
            {
                immobilizeTurns--;
                if (immobilizeTurns == 0 && crippleTurns > 0)
                {
                    CombatFeedbackManager.Instance?.SpawnDamageText(transform.position, "CRIPPLED (Move: 1)", new Color(1.0f, 0.7f, 0.2f), 1.3f);
                }
            }
            else if (crippleTurns > 0)
            {
                crippleTurns--;
            }

            UpdateAttunement();
            UpdateBaseRingVisuals();
        }

        public HexCoordinates Coordinates => coordinates;
        public HexTile3D CurrentTile
        {
            get
            {
                if (currentTile == null)
                {
                    ReacquireCurrentTile();
                }
                return currentTile;
            }
            set
            {
                currentTile = value;
                if (currentTile != null)
                {
                    coordinates = currentTile.Coordinates;
                }
            }
        }
        public bool IsMoving => isMoving;
        public bool IsSelected => isSelected;

        public static Color PlayerColor = new Color(0.15f, 0.60f, 1.0f, 0.85f);
        public static Color EnemyColor = new Color(0.95f, 0.25f, 0.20f, 0.85f);
        public static Color SelectedColor = new Color(1.0f, 0.85f, 0.20f, 1.0f);
        public static Color ExhaustedColor = new Color(0.35f, 0.40f, 0.45f, 0.70f);

        private void Awake()
        {
            EnsureComponents();
        }

        private void Start()
        {
            ReacquireCurrentTile();
            EnsureCollider();
            UpdateAttunement();
            UpdateBaseRingVisuals();
        }

        public void Initialize(
            string name,
            UnitFaction unitFaction,
            Sprite sprite,
            HexTile3D startTile,
            int hp = 10,
            int range = 3,
            UnitArchetype unitArchetype = UnitArchetype.Commander,
            ElementalAffinity unitAffinity = ElementalAffinity.None,
            int baseAtk = 3)
        {
            unitName = name;
            faction = unitFaction;
            maxHealth = hp;
            currentHealth = hp;
            moveRange = range;
            archetype = unitArchetype;
            affinity = unitAffinity;
            baseAttackDamage = baseAtk;

            EnsureComponents();

            if (spriteRenderer != null && sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            if (startTile != null)
            {
                SnapToTile(startTile);
            }

            UpdateAttunement();
            UpdateBaseRingVisuals();
        }

        private void EnsureComponents()
        {
            EnsureCollider();
            if (billboard == null) billboard = GetComponentInChildren<Billboard25D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Setup base ring indicator if not present
            if (baseRing == null)
            {
                Transform ringTrans = transform.Find("BaseRing");
                if (ringTrans != null)
                {
                    baseRing = ringTrans.gameObject;
                    baseRingRenderer = baseRing.GetComponent<SpriteRenderer>();
                }
            }
        }

        private void EnsureCollider()
        {
            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<CapsuleCollider>();
            }
            col.center = new Vector3(0f, 0.75f, 0f);
            col.radius = 0.45f;
            col.height = 1.6f;
        }

        /// <summary>
        /// Snaps the unit directly onto a tile's top center surface.
        /// </summary>
        public void SnapToTile(HexTile3D tile)
        {
            if (tile == null) return;

            if (currentTile != null && currentTile.CurrentOccupant == this)
            {
                currentTile.CurrentOccupant = null;
            }

            currentTile = tile;
            coordinates = tile.Coordinates;
            currentTile.CurrentOccupant = this;

            Vector3 topPos = tile.GetTopCenterPosition();
            transform.position = topPos;
            UpdateAttunement();
        }

        public void ReacquireCurrentTile()
        {
            HexGrid3D grid = HexGrid3D.Instance ?? Object.FindFirstObjectByType<HexGrid3D>();
            if (grid != null)
            {
                // 1. Try to find tile by exact coordinates
                currentTile = grid.GetTile(coordinates);

                // 2. Proximity fallback: find nearest tile by world position
                if (currentTile == null)
                {
                    currentTile = grid.GetTile(HexCoordinates.FromWorldPosition(transform.position, grid.HexRadius));
                }

                if (currentTile != null)
                {
                    coordinates = currentTile.Coordinates;
                    currentTile.CurrentOccupant = this;
                    UpdateAttunement();
                }
            }
        }

        public void SnapToNearestTileIfUnassigned()
        {
            if (currentTile != null) return;
            ReacquireCurrentTile();
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateBaseRingVisuals();
        }

        /// <summary>
        /// Evaluates active environmental Attunement based on current tile state and unit affinity.
        /// (FFT Geomancer-style dynamic passive buff machine).
        /// </summary>
        public void UpdateAttunement()
        {
            BonusAttackDamage = 0;
            BonusMoveRange = 0;
            IsMagmaImmune = false;
            CurrentAttunementName = "None";

            if (CurrentTile == null) return;

            TileState state = CurrentTile.State;

            // 1. Fire Attunement: Standing on Scorched Earth or Magma
            if (state == TileState.Scorched || state == TileState.Magma)
            {
                if (affinity == ElementalAffinity.Fire || archetype == UnitArchetype.Titan)
                {
                    BonusAttackDamage = 2;
                    // Only colossal Titans have the molten scales to be immune to Magma damage!
                    // Humanoid casters/minions gain +2 ATK but still take burn damage if immersed in molten magma!
                    IsMagmaImmune = (archetype == UnitArchetype.Titan);
                    CurrentAttunementName = (state == TileState.Magma)
                        ? (IsMagmaImmune ? "🔥 Magma Surge (+2 ATK, Magma Immune)" : "🔥 Magma Surge (+2 ATK, Burn Hazard!)")
                        : "🔥 Flame Surge (+2 ATK)";
                }
            }
            // 2. Aqua Attunement: Standing in Shallow or Deep Water
            else if (state == TileState.Water)
            {
                if (affinity == ElementalAffinity.Water || archetype == UnitArchetype.Titan)
                {
                    BonusMoveRange = 1;
                    CurrentAttunementName = "💧 Aqua Surge (+1 Move)";
                }
            }
            // 3. Vapor Shroud: Standing in Steam cloud
            else if (state == TileState.Steam)
            {
                CurrentAttunementName = "💨 Vapor Shroud (Mist Cover)";
            }
            // 4. Mud: Sticky quagmire
            else if (state == TileState.Mud)
            {
                CurrentAttunementName = "💩 Mud (Quagmire)";
            }

            if (IsImmobilized)
            {
                CurrentAttunementName += " | ⛓️ IMMOBILIZED";
            }
            else if (IsCrippled)
            {
                CurrentAttunementName += " | 🦶 CRIPPLED (Move 1)";
            }

            UpdateBaseRingVisuals();
        }

        public void UpdateBaseRingVisuals()
        {
            if (baseRingRenderer != null && baseRing != null)
            {
                if (isSelected)
                {
                    baseRingRenderer.color = SelectedColor;
                    baseRing.transform.localScale = Vector3.one * 1.25f;
                }
                else if (faction == UnitFaction.Player && IsExhausted)
                {
                    baseRingRenderer.color = ExhaustedColor;
                    baseRing.transform.localScale = Vector3.one * 0.90f;
                }
                else if (IsImmobilized)
                {
                    // Entangled / Submerged / Mud trapped ring indicator
                    baseRingRenderer.color = new Color(0.75f, 0.45f, 0.20f, 0.95f);
                    baseRing.transform.localScale = Vector3.one * 0.95f;
                }
                else if (BonusAttackDamage > 0)
                {
                    // Flame Attuned: Radiant fiery orange/gold tactical ring!
                    baseRingRenderer.color = new Color(1.0f, 0.52f, 0.10f, 0.95f);
                    baseRing.transform.localScale = Vector3.one * 1.15f;
                }
                else if (BonusMoveRange > 0)
                {
                    // Aqua Attuned: Glowing azure/cyan tactical ring!
                    baseRingRenderer.color = new Color(0.20f, 0.85f, 1.0f, 0.95f);
                    baseRing.transform.localScale = Vector3.one * 1.15f;
                }
                else
                {
                    baseRingRenderer.color = (faction == UnitFaction.Player) ? PlayerColor : EnemyColor;
                    baseRing.transform.localScale = Vector3.one * 1.0f;
                }
            }
        }

        /// <summary>
        /// Smoothly moves the unit along a path of hex tiles with parabolic elevation hops.
        /// </summary>
        public IEnumerator MoveAlongPath(List<HexTile3D> path, float stepDuration = 0.22f)
        {
            if (this == null || currentHealth <= 0 || path == null || path.Count == 0 || isMoving) yield break;

            isMoving = true;

            // Clear current tile occupancy
            if (currentTile != null && currentTile.CurrentOccupant == this)
            {
                currentTile.CurrentOccupant = null;
            }

            for (int i = 0; i < path.Count; i++)
            {
                if (this == null || currentHealth <= 0) yield break;
                HexTile3D nextTile = path[i];
                Vector3 startPos = transform.position;
                Vector3 targetPos = nextTile.GetTopCenterPosition();

                if (billboard != null)
                {
                    billboard.FaceDirection(targetPos);
                }

                float elapsed = 0f;
                while (elapsed < stepDuration)
                {
                    if (this == null || currentHealth <= 0) yield break;
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / stepDuration);

                    // Parabolic hop: XZ lerp + vertical arc
                    Vector3 currentXZ = Vector3.Lerp(startPos, targetPos, t);
                    float arcY = Mathf.Sin(t * Mathf.PI) * 0.35f;
                    float currentY = Mathf.Lerp(startPos.y, targetPos.y, t) + arcY;

                    transform.position = new Vector3(currentXZ.x, currentY, currentXZ.z);
                    yield return null;
                }

                if (this == null || currentHealth <= 0) yield break;
                transform.position = targetPos;
                currentTile = nextTile;
                coordinates = nextTile.Coordinates;
            }

            if (this == null || currentHealth <= 0) yield break;

            // Secure occupancy at final destination
            currentTile.CurrentOccupant = this;
            isMoving = false;
            UpdateAttunement();

            Debug.Log($"<color=#66BB6A><b>[Unit Move]</b></color> <b>{unitName}</b> reached {coordinates} (Tile State: {currentTile.State}).");

            // Environmental Hazard Check on Landing!
            ResolveTileHazardOnLanding(currentTile);
        }

        /// <summary>
        /// Applies hazard burn/submersion damage or mobility debuffs if unit lands on a hazardous tile.
        /// </summary>
        public void ResolveTileHazardOnLanding(HexTile3D tile)
        {
            if (tile == null || currentHealth <= 0) return;

            // 1. Molten Magma Hazard
            if (tile.State == TileState.Magma && !IsMagmaImmune)
            {
                Debug.Log($"<color=#FF3D00><b>[Hazard Burn!]</b></color> {unitName} stepped into molten Magma! Took 3 burn damage.");
                TakeDamage(3, "🔥 MAGMA BURN! -3");
                TacticalCameraController.Instance?.Shake(0.32f, 0.35f);
                SoundManager3D.Instance?.PlaySpellCast(isFire: true);
            }
            // 2. Deep Water Submersion (Tier 2) -> Turn 1 Immobilize, Turn 2 Cripple
            else if (tile.State == TileState.Water && tile.TierLevel >= 2)
            {
                // Water-attuned units and colossal Titans are immune!
                if (affinity != ElementalAffinity.Water && archetype != UnitArchetype.Titan)
                {
                    ApplyMobilityDebuff(1, 1);
                    Debug.Log($"<color=#0288D1><b>[Deep Water Submerged!]</b></color> {unitName} plunged into Deep Water! Immobilized 1 round.");
                    CombatFeedbackManager.Instance?.SpawnDamageText(transform.position, "🌊 SUBMERGED! (Immobilized)", new Color(0.2f, 0.8f, 1.0f), 1.5f);
                    TacticalCameraController.Instance?.Shake(0.2f, 0.25f);
                    SoundManager3D.Instance?.PlaySpellCast(isFire: false);
                }
            }
            // 3. Mud Quagmire Trap -> Turn 1 Immobilize, Turn 2 Cripple
            else if (tile.State == TileState.Mud)
            {
                // Titans are immune to mud traps!
                if (archetype != UnitArchetype.Titan)
                {
                    ApplyMobilityDebuff(1, 1);
                    Debug.Log($"<color=#8D6E63><b>[Mud Quagmire Trap!]</b></color> {unitName} caught in sticky Mud! Immobilized 1 round.");
                    CombatFeedbackManager.Instance?.SpawnDamageText(transform.position, "💩 MUD TRAP! (Immobilized)", new Color(0.75f, 0.55f, 0.35f), 1.5f);
                    TacticalCameraController.Instance?.Shake(0.18f, 0.20f);
                }
            }
            // 4. Safe / Dry ground outside hazards -> Immediately frees unit from fluid traps!
            else
            {
                if (immobilizeTurns > 0 || crippleTurns > 0)
                {
                    ClearMobilityDebuffs();
                    Debug.Log($"<color=#66BB6A><b>[Freed!]</b></color> {unitName} reached safe ground ({tile.State})! Mobility debuff removed.");
                    CombatFeedbackManager.Instance?.SpawnDamageText(transform.position, "FREED! (Move Restored)", new Color(0.4f, 0.9f, 0.5f), 1.3f);
                }
            }
        }

        /// <summary>
        /// Visually lunges the sprite standee toward a target position during an attack or shove, then springs back.
        /// Modifies only local visual position so root transform and grid movement are never corrupted.
        /// </summary>
        public IEnumerator PlayAttackLunge(Vector3 targetWorldPos, float duration = 0.22f)
        {
            Transform visualTrans = (spriteRenderer != null) ? spriteRenderer.transform : transform;
            Vector3 baseLocalPos = new Vector3(0f, 0.75f, 0f);

            Vector3 worldDir = (targetWorldPos - transform.position).normalized;
            Vector3 localDir = transform.InverseTransformDirection(worldDir);
            Vector3 lungeOffset = new Vector3(localDir.x, 0f, localDir.z).normalized * 0.45f;

            float half = duration * 0.5f;
            float elapsed = 0f;

            // Thrust forward
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                visualTrans.localPosition = Vector3.Lerp(baseLocalPos, baseLocalPos + lungeOffset, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            // Snap back
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                visualTrans.localPosition = Vector3.Lerp(baseLocalPos + lungeOffset, baseLocalPos, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            visualTrans.localPosition = baseLocalPos;
        }

        /// <summary>
        /// Quickly shudders the sprite standee left/right and flashes red.
        /// Modifies only local visual position so root transform and grid movement are never corrupted.
        /// </summary>
        public IEnumerator PlayHurtShudder(float duration = 0.25f)
        {
            Transform visualTrans = (spriteRenderer != null) ? spriteRenderer.transform : transform;
            Vector3 baseLocalPos = new Vector3(0f, 0.75f, 0f);
            Color originalColor = (spriteRenderer != null) ? spriteRenderer.color : Color.white;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1.0f, 0.35f, 0.35f, 1f); // Red flash
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float shakeX = Mathf.Sin(elapsed * 45f) * 0.08f;
                visualTrans.localPosition = baseLocalPos + Vector3.right * shakeX;
                yield return null;
            }

            visualTrans.localPosition = baseLocalPos;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }

        public void TakeDamage(int amount, string customLabel = null)
        {
            // 1. Magma Hazard Immunity check via Flame Attunement
            if (IsMagmaImmune && !string.IsNullOrEmpty(customLabel) && customLabel.IndexOf("Magma", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.Log($"<color=#FFA726><b>[Immunity]</b></color> {unitName} resisted Magma hazard via Flame Attunement!");
                if (CombatFeedbackManager.Instance != null)
                {
                    CombatFeedbackManager.Instance.SpawnDamageText(transform.position, "IMMUNE", new Color(1.0f, 0.75f, 0.2f));
                }
                return;
            }

            // 2. Vapor Shroud mitigation in Steam mist
            if (CurrentTile != null && CurrentTile.State == TileState.Steam && amount > 1)
            {
                amount = Mathf.Max(1, amount - 1);
                Debug.Log($"<color=#B0BEC5><b>[Vapor Shroud]</b></color> {unitName} mitigated 1 dmg in Steam mist!");
            }

            currentHealth = Mathf.Max(0, currentHealth - amount);
            Debug.Log($"<color=#EF5350><b>[Damage]</b></color> {unitName} took {amount} dmg! HP: {currentHealth}/{maxHealth}");

            // Trigger floating combat text
            string label = !string.IsNullOrEmpty(customLabel) ? customLabel : $"-{amount}";
            Color textColor = (faction == UnitFaction.Player)
                ? new Color(1.0f, 0.25f, 0.2f)   // Red for player hurt
                : new Color(1.0f, 0.85f, 0.15f); // Orange-Gold for enemy hit

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.SpawnDamageText(transform.position, label, textColor);
            }

            // Trigger procedural particle hit sparks
            CombatVFXManager.Instance?.PlayHitSparks(transform.position + Vector3.up * 0.75f, textColor);

            // Visual shudder
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(PlayHurtShudder());
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"<color=#D32F2F><b>[Defeated]</b></color> {unitName} has fallen in battle!");
            if (currentTile != null && currentTile.CurrentOccupant == this)
            {
                currentTile.CurrentOccupant = null;
            }

            // Player Titans retreat back to Citadel Reserve across the Rift instead of permanent Destroy!
            if (faction == UnitFaction.Player && archetype == UnitArchetype.Titan)
            {
                if (CombatFeedbackManager.Instance != null)
                {
                    CombatFeedbackManager.Instance.SpawnDamageText(transform.position, "RETREATED TO CITADEL", new Color(0.7f, 0.3f, 0.9f), 2f);
                }
                currentTile = null;
                gameObject.SetActive(false);
                if (TurnManager3D.Instance != null)
                {
                    TurnManager3D.Instance.CheckBattleConditions();
                }
                return;
            }

            Destroy(gameObject);

            if (TurnManager3D.Instance != null)
            {
                TurnManager3D.Instance.CheckBattleConditions();
            }
        }
    }
}

