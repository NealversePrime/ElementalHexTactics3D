using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.CameraControl;

namespace ElementalHexTactics3D.InputHandling
{
    public partial class HexGridInteraction3D
    {
        private void ExecuteAction(HexTile3D targetTile)
        {
            string activeSkillId = PendingSkillId;
            int customDmg = PendingSkillDamage;
            int manaCost = PendingSkillManaCost;

            if (manaCost > 0 && currentSelectedUnit != null && currentMode != UnitActionMode.Move && currentMode != UnitActionMode.None)
            {
                currentSelectedUnit.ConsumeMana(manaCost);
            }

            switch (currentMode)
            {
                case UnitActionMode.Move:
                    ExecuteMoveAction(targetTile);
                    break;

                case UnitActionMode.Fireball:
                    int fireDmg = customDmg > 0 ? customDmg : ((currentSelectedUnit != null && currentSelectedUnit.MaxHealth >= 500) ? 780 : 3);
                    Color fireCol = (activeSkillId == "JudgmentBeam") ? new Color(1.0f, 0.95f, 0.45f) : new Color(1.0f, 0.45f, 0.1f);
                    TriggerElementalSpell(targetTile, ElementType.Fire, fireDmg, fireCol, activeSkillId);
                    break;

                case UnitActionMode.WaterSurge:
                    int waterDmg = customDmg > 0 ? customDmg : ((currentSelectedUnit != null && currentSelectedUnit.MaxHealth >= 500) ? 520 : 2);
                    TriggerElementalSpell(targetTile, ElementType.Water, waterDmg, new Color(0.2f, 0.75f, 1.0f), activeSkillId);
                    break;

                case UnitActionMode.EarthSpire:
                    int earthDmg = customDmg > 0 ? customDmg : ((currentSelectedUnit != null && currentSelectedUnit.MaxHealth >= 500) ? 640 : 2);
                    TriggerElementalSpell(targetTile, ElementType.Earth, earthDmg, new Color(0.85f, 0.65f, 0.30f), activeSkillId);
                    break;

                case UnitActionMode.KineticPush:
                    TriggerKineticPush(targetTile);
                    break;

                case UnitActionMode.TitanStrike:
                    TriggerTitanStrike(targetTile);
                    break;

                case UnitActionMode.ConsumeLand:
                    TriggerConsumeLand(targetTile);
                    break;

                case UnitActionMode.MagmaCataclysm:
                    TriggerMagmaCataclysm(targetTile);
                    break;

                case UnitActionMode.DeployCommander:
                    TriggerDeployCommander(targetTile);
                    break;

                case UnitActionMode.SummonTitan:
                    TriggerSummonTitan(targetTile);
                    break;

                case UnitActionMode.TearRift:
                    TriggerTearRift(targetTile);
                    break;
            }
        }

        private void ExecuteMoveAction(HexTile3D targetTile)
        {
            if (currentSelectedUnit.HasMovedThisTurn)
            {
                Debug.LogWarning($"[Move] {currentSelectedUnit.UnitName} has already moved this turn!");
                return;
            }

            if (currentSelectedUnit.EffectiveMoveRange <= 0)
            {
                Debug.LogWarning($"[Move] {currentSelectedUnit.UnitName} is immobilized and cannot move!");
                ClearTargetHighlights();
                SetActionMode(UnitActionMode.None);
                return;
            }

            if (!activeTargetTiles.Contains(targetTile))
            {
                Debug.LogWarning($"[Move] Target tile {targetTile.Coordinates} is not in reachable range!");
                return;
            }

            HexTile3D startTile = ResolveUnitCurrentTile(currentSelectedUnit);
            var path = HexPathfinder3D.FindPath(HexGrid3D.Instance, startTile, targetTile);

            if (path != null && path.Count > 0 && path.Count <= currentSelectedUnit.EffectiveMoveRange)
            {
                currentSelectedUnit.HasMovedThisTurn = true;
                ClearTargetHighlights();
                StartCoroutine(currentSelectedUnit.MoveAlongPath(path));
                SetActionMode(UnitActionMode.None);
            }
            else
            {
                Debug.LogWarning($"[Move] No valid path within range found to {targetTile.Coordinates} (Path steps: {path?.Count}, MaxAllowed: {currentSelectedUnit.EffectiveMoveRange})");
            }
        }

        private void TriggerElementalSpell(HexTile3D targetTile, ElementType element, int damage, Color projColor, string skillId = null)
        {
            currentSelectedUnit.HasActedThisTurn = true;
            ClearTargetHighlights();
            StartCoroutine(ExecutePlayerSpell(targetTile, element, damage, projColor, skillId));
            SetActionMode(UnitActionMode.None);
        }

        private void TriggerKineticPush(HexTile3D targetTile)
        {
            TacticalUnit3D targetUnit = targetTile.CurrentOccupant as TacticalUnit3D;
            if (targetUnit == null) return;

            currentSelectedUnit.HasActedThisTurn = true;
            ClearTargetHighlights();
            StartCoroutine(ExecutePlayerPush(targetUnit));
            SetActionMode(UnitActionMode.None);
        }

        private void TriggerTitanStrike(HexTile3D targetTile)
        {
            TacticalUnit3D strikeTarget = targetTile.CurrentOccupant as TacticalUnit3D;
            if (strikeTarget == null || strikeTarget.Faction == currentSelectedUnit.Faction) return;

            currentSelectedUnit.HasActedThisTurn = true;
            ClearTargetHighlights();
            StartCoroutine(ExecuteTitanStrike(strikeTarget));
            SetActionMode(UnitActionMode.None);
        }

        private void TriggerConsumeLand(HexTile3D targetTile)
        {
            if (!IsTileConsumable(targetTile)) return;

            currentSelectedUnit.HasSiphonedThisTurn = true;
            ClearTargetHighlights();
            StartCoroutine(ExecuteConsumeLand(targetTile));
            SetActionMode(UnitActionMode.None);
        }

        private void TriggerMagmaCataclysm(HexTile3D targetTile)
        {
            currentSelectedUnit.HasActedThisTurn = true;
            ClearTargetHighlights();
            StartCoroutine(ExecuteMagmaCataclysm(targetTile));
            SetActionMode(UnitActionMode.None);
        }

        private IEnumerator ExecuteConsumeLand(HexTile3D targetTile)
        {
            if (currentSelectedUnit == null || targetTile == null) yield break;

            if (targetTile != currentSelectedUnit.CurrentTile)
            {
                yield return currentSelectedUnit.PlayAttackLunge(targetTile.transform.position, 0.18f);
            }

            SoundManager3D.Instance?.PlayConsumeLand();
            TacticalCameraController.Instance?.Shake(0.18f, 0.18f);

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.StartCoroutine(
                    CombatFeedbackManager.Instance.SpawnShockwaveEffect(targetTile.transform.position, new Color(1f, 0.85f, 0.2f), maxRadius: 1.4f, duration: 0.35f));
            }

            TileState prevState = MutateHarvestedTileState(targetTile);

            currentSelectedUnit.HasSiphonedThisTurn = true;
            currentSelectedUnit.AddElementalCore(1);

            TacticalUnit3D occupant = targetTile.GetOccupant();
            if (occupant != null) occupant.UpdateAttunement();
            currentSelectedUnit.UpdateAttunement();

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner("⚡ LAND HARVESTED!", $"{currentSelectedUnit.UnitName} harvested energy from {prevState} into an Elemental Core!", 1.3f, new Color(1.0f, 0.85f, 0.2f));
            }

            yield return new WaitForSeconds(0.2f);
        }

        private TileState MutateHarvestedTileState(HexTile3D targetTile)
        {
            TileState prevState = targetTile.State;
            if (prevState == TileState.Magma)
            {
                targetTile.SetState(TileState.Scorched, 1);
            }
            else
            {
                targetTile.SetState(TileState.Barren, 0);
            }
            return prevState;
        }

        private IEnumerator ExecuteMagmaCataclysm(HexTile3D centerTile)
        {
            if (currentSelectedUnit == null || centerTile == null) yield break;
            if (!currentSelectedUnit.ConsumeElementalCore(1)) yield break;

            yield return currentSelectedUnit.PlayAttackLunge(centerTile.transform.position, 0.28f);

            TacticalCameraController.Instance?.Shake(0.65f, 0.55f);
            SoundManager3D.Instance?.PlayCataclysm();

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner("🌋 TITAN ULTIMATE: MAGMA CATACLYSM!", "A subterranean cataclysm ruptures the earth with molten fury!", 1.6f, new Color(1.0f, 0.35f, 0.1f));
                CombatFeedbackManager.Instance.StartCoroutine(
                    CombatFeedbackManager.Instance.SpawnShockwaveEffect(centerTile.transform.position, new Color(1.0f, 0.45f, 0.1f), maxRadius: 3.2f, duration: 0.55f));
            }

            HashSet<TacticalUnit3D> hitEnemies = EruptCataclysmTilesAndCollectTargets(centerTile);

            yield return new WaitForSeconds(0.12f);

            foreach (var enemy in hitEnemies)
            {
                if (enemy != null && enemy.CurrentHealth > 0)
                {
                    enemy.TakeDamage(8);
                }
            }

            yield return new WaitForSeconds(0.3f);
        }

        private HashSet<TacticalUnit3D> EruptCataclysmTilesAndCollectTargets(HexTile3D centerTile)
        {
            HexGrid3D grid = HexGrid3D.Instance;
            List<HexCoordinates> blastCoords = centerTile.Coordinates.GetRange(1);
            HashSet<TacticalUnit3D> hitEnemies = new HashSet<TacticalUnit3D>();

            foreach (var coord in blastCoords)
            {
                HexTile3D tile = grid != null ? grid.GetTile(coord) : null;
                if (tile == null) continue;

                if (tile.State == TileState.Scorched)
                {
                    tile.SetState(TileState.Magma, 2);
                }
                else if (tile.State != TileState.Magma)
                {
                    tile.SetState(TileState.Scorched, 1);
                }

                TacticalUnit3D occupant = tile.GetOccupant();
                if (occupant != null && occupant.Faction != currentSelectedUnit.Faction)
                {
                    hitEnemies.Add(occupant);
                }
            }

            return hitEnemies;
        }

        private IEnumerator ExecuteTitanStrike(TacticalUnit3D targetEnemy)
        {
            if (currentSelectedUnit == null || targetEnemy == null) yield break;

            if (currentSelectedUnit.IsRangedAttack)
            {
                yield return PlayRangedBasicAttackFeedback(targetEnemy);
            }
            else
            {
                yield return PlayMeleeBasicAttackFeedback(targetEnemy);
            }

            int totalDmg = currentSelectedUnit.EffectiveAttackDamage;
            targetEnemy.TakeDamage(totalDmg);
        }

        private IEnumerator PlayRangedBasicAttackFeedback(TacticalUnit3D targetEnemy)
        {
            yield return currentSelectedUnit.PlayAttackLunge(targetEnemy.transform.position, 0.16f);
            TacticalCameraController.Instance?.Shake(0.18f, 0.20f);

            string lower = currentSelectedUnit.UnitName.ToLowerInvariant();
            Vector3 hitPos = targetEnemy.transform.position + Vector3.up * 0.75f;

            if (lower.Contains("archer") || lower.Contains("bow") || lower.Contains("ranger"))
            {
                SoundManager3D.Instance?.PlayKineticPush();
                CombatVFXManager.Instance?.PlayHitSparks(hitPos, new Color(1f, 0.85f, 0.3f));
            }
            else if (lower.Contains("mage") || lower.Contains("wizard") || lower.Contains("sorcerer"))
            {
                SoundManager3D.Instance?.PlayFireball();
                CombatVFXManager.Instance?.PlayHitSparks(hitPos, new Color(0.4f, 0.8f, 1f));
            }
            else
            {
                SoundManager3D.Instance?.PlayTitanStrike();
                CombatVFXManager.Instance?.PlayHitSparks(hitPos, new Color(1f, 0.5f, 0.2f));
            }
        }

        private IEnumerator PlayMeleeBasicAttackFeedback(TacticalUnit3D targetEnemy)
        {
            yield return currentSelectedUnit.PlayAttackLunge(targetEnemy.transform.position, 0.22f);
            TacticalCameraController.Instance?.Shake(0.25f, 0.25f);
            SoundManager3D.Instance?.PlayTitanStrike();

            if (currentSelectedUnit.Affinity == ElementalAffinity.Fire || currentSelectedUnit.UnitName.Contains("Dragon"))
            {
                CombatVFXManager.Instance?.PlayFireBurst(targetEnemy.transform.position + Vector3.up * 0.5f);
            }
        }

        private IEnumerator ExecutePlayerSpell(HexTile3D targetTile, ElementType element, int dmg, Color projColor, string skillId = null)
        {
            TacticalUnit3D caster = currentSelectedUnit;
            if (caster == null || targetTile == null) yield break;

            yield return caster.PlayAttackLunge(targetTile.transform.position, 0.22f);
            if (caster == null || targetTile == null) yield break;

            Vector3 casterPos = caster.transform.position + Vector3.up * 0.8f;
            Vector3 targetPos = targetTile.GetTopCenterPosition() + Vector3.up * 0.4f;

            if (CombatFeedbackManager.Instance != null)
            {
                yield return CombatFeedbackManager.Instance.SpawnSpellProjectile(casterPos, targetPos, projColor, 0.26f);
            }

            PlayCasterSpellVFX(caster, element, casterPos, targetPos, skillId);

            TacticalUnit3D targetOccupant = targetTile.GetOccupant();
            TerrainReactionResult reaction = TerrainReactionSystem.PredictReaction(targetTile.State, targetTile.TierLevel, element);

            if (element == ElementType.Earth && targetOccupant != null && reaction.ResultingState == TileState.StonePillar)
            {
                yield return ExecuteEarthPillarEjectionRoutine(caster, targetOccupant, targetTile, dmg);
            }
            else
            {
                TerrainReactionSystem.ApplySpell(targetTile, element, damage: dmg);
            }
        }

        private void PlayCasterSpellVFX(TacticalUnit3D caster, ElementType element, Vector3 casterPos, Vector3 targetPos, string skillId = null)
        {
            if (caster == null) return;

            if (skillId == "JudgmentBeam")
            {
                SpellVFXManager3D.Instance?.PlayJudgmentBeamVFX(targetPos, casterPos);
                return;
            }
            if (skillId == "HeavensPillar" || skillId == "SacredBulwark")
            {
                SpellVFXManager3D.Instance?.PlayHeavensPillarVFX(targetPos);
                return;
            }
            if (skillId == "SanctifyingWave" || skillId == "CleansingFlood")
            {
                SpellVFXManager3D.Instance?.PlaySanctifyingWaveVFX(targetPos);
                return;
            }

            string lowerName = caster.UnitName.ToLower();
            if (lowerName.Contains("paladin") && element == ElementType.Fire)
            {
                SpellVFXManager3D.Instance?.PlayRadiantSmiteVFX(targetPos, casterPos);
            }
            else if ((lowerName.Contains("demon") || lowerName.Contains("malakor")) && element == ElementType.Fire)
            {
                SpellVFXManager3D.Instance?.PlayHellfireBlastVFX(targetPos);
            }
            else if (element == ElementType.Earth)
            {
                SpellVFXManager3D.Instance?.PlayHeavensPillarVFX(targetPos);
            }
            else if (element == ElementType.Water)
            {
                SpellVFXManager3D.Instance?.PlaySanctifyingWaveVFX(targetPos);
            }
            else
            {
                SoundManager3D.Instance?.PlaySpellCast(element);
            }
        }

        private IEnumerator ExecuteEarthPillarEjectionRoutine(TacticalUnit3D caster, TacticalUnit3D occupant, HexTile3D targetTile, int damage)
        {
            if (occupant == null || targetTile == null || HexGrid3D.Instance == null) yield break;

            HexGrid3D grid = HexGrid3D.Instance;
            HexCoordinates targetCoords = targetTile.Coordinates;

            // 1. Initial Earth Pillar Eruption Impact & Damage
            occupant.TakeDamage(damage);
            TacticalCameraController.Instance?.Shake(0.38f, 0.38f);
            SoundManager3D.Instance?.PlayEarthSpire();
            SoundManager3D.Instance?.PlayWallSlam(1.2f);
            CombatVFXManager.Instance?.PlayWallSlam(targetTile.GetTopCenterPosition(), Vector3.up);

            if (occupant == null || occupant.CurrentHealth <= 0)
            {
                targetTile.SetState(TileState.StonePillar, 2);
                yield break;
            }

            // 2. Detach occupant & elevate tile into a Stone Pillar
            if (targetTile.CurrentOccupant == occupant) targetTile.CurrentOccupant = null;
            occupant.CurrentTile = null;
            targetTile.SetState(TileState.StonePillar, 2);

            // 3. Determine Ejection Destination
            HexTile3D primaryTile = GetPrimaryEjectionTile(grid, caster, targetCoords);
            bool primaryBlocked = IsEjectionTileBlocked(primaryTile, targetTile);

            HexTile3D finalDestTile;
            if (!primaryBlocked)
            {
                finalDestTile = primaryTile;
            }
            else
            {
                yield return TriggerSeismicWallSlamRoutine(occupant, primaryTile);
                if (occupant == null || occupant.CurrentHealth <= 0) yield break;

                finalDestTile = FindOpenNeighborForEjection(grid, targetTile);
            }

            // 4. Displace occupant to destination
            yield return DisplaceOccupantToEjectionTile(occupant, targetTile, finalDestTile);
            yield return new WaitForSeconds(0.15f);
        }

        private HexTile3D GetPrimaryEjectionTile(HexGrid3D grid, TacticalUnit3D caster, HexCoordinates targetCoords)
        {
            HexCoordinates primaryDir = (caster != null)
                ? PushMechanic3D.GetPushDirection(caster.Coordinates, targetCoords)
                : HexCoordinates.Directions[0];

            return grid.GetTile(targetCoords + primaryDir);
        }

        private bool IsEjectionTileBlocked(HexTile3D candidateTile, HexTile3D originTile)
        {
            return (candidateTile == null) ||
                   candidateTile.IsOccupied ||
                   candidateTile.State == TileState.StonePillar ||
                   (Mathf.Abs(candidateTile.Elevation - originTile.Elevation) > 1);
        }

        private IEnumerator TriggerSeismicWallSlamRoutine(TacticalUnit3D occupant, HexTile3D blockedTile)
        {
            Debug.Log($"<color=#EF5350><b>[Seismic Wall Slam!]</b></color> {occupant.UnitName} was thrown into an obstacle by the erupting pillar!");
            yield return new WaitForSeconds(0.1f);
            TacticalCameraController.Instance?.Shake(0.35f, 0.35f);
            SoundManager3D.Instance?.PlayWallSlam(1.3f);

            Vector3 slamVfxPos = blockedTile != null ? blockedTile.GetTopCenterPosition() : occupant.transform.position + Vector3.up * 0.5f;
            CombatVFXManager.Instance?.PlayWallSlam(slamVfxPos, Vector3.up);
            int slamDmg = (occupant != null && occupant.MaxHealth >= 500) ? 550 : 2;
            occupant.TakeDamage(slamDmg);
        }

        private HexTile3D FindOpenNeighborForEjection(HexGrid3D grid, HexTile3D originTile)
        {
            List<HexTile3D> validNeighbors = new List<HexTile3D>();
            foreach (var dir in HexCoordinates.Directions)
            {
                HexTile3D nTile = grid.GetTile(originTile.Coordinates + dir);
                if (nTile != null && !nTile.IsOccupied && nTile.State != TileState.StonePillar)
                {
                    if (Mathf.Abs(nTile.Elevation - originTile.Elevation) <= 1)
                    {
                        validNeighbors.Add(nTile);
                    }
                }
            }

            return validNeighbors.Count > 0
                ? validNeighbors[Random.Range(0, validNeighbors.Count)]
                : originTile;
        }

        private IEnumerator DisplaceOccupantToEjectionTile(TacticalUnit3D occupant, HexTile3D originTile, HexTile3D destTile)
        {
            if (destTile != null && destTile != originTile)
            {
                CombatFeedbackManager.Instance?.SpawnDamageText(
                    occupant.transform.position + Vector3.up * 1.0f,
                    "⛰️ SEISMIC LAUNCH!",
                    new Color(0.85f, 0.55f, 0.25f),
                    1.6f
                );

                yield return occupant.MoveAlongPath(new List<HexTile3D> { destTile }, stepDuration: 0.16f);
                occupant.SnapToTile(destTile);
                occupant.ResolveTileHazardOnLanding(destTile);
            }
            else
            {
                occupant.SnapToTile(originTile);
            }
        }

        private IEnumerator ExecutePlayerPush(TacticalUnit3D targetUnit)
        {
            if (currentSelectedUnit == null || targetUnit == null) yield break;

            SoundManager3D.Instance?.PlayKineticPush();
            yield return currentSelectedUnit.PlayAttackLunge(targetUnit.transform.position, 0.22f);
            yield return PushMechanic3D.ExecutePushRoutine(currentSelectedUnit, targetUnit, HexGrid3D.Instance);
        }
    }
}
