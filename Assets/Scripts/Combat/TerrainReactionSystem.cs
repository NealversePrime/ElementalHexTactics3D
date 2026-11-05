using UnityEngine;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.Units;

namespace ElementalHexTactics3D.Combat
{
    /// <summary>
    /// Result data returned when predicting or applying a terrain reaction.
    /// Reused by both actual spell casting and the Ghost UI preview to prevent desyncs.
    /// </summary>
    public struct TerrainReactionResult
    {
        public TileState ResultingState;
        public int ResultingTier;
        public string ReactionName;
        public bool TriggeredReaction;
        public string Description;
    }

    /// <summary>
    /// Core system evaluating elemental interactions on hex terrain.
    /// Follows the 3-step resolution:
    ///   1. Does the spell OPPOSE the tile's current state? (trigger reset / elemental reaction)
    ///   2. Does the spell MATCH the tile's current state? (upgrade TierLevel)
    ///   3. Is the tile neutral? (set new elemental state)
    /// </summary>
    public static class TerrainReactionSystem
    {
        /// <summary>
        /// Pure prediction method: calculates the outcome without modifying the tile.
        /// Powers the Ghost UI preview.
        /// </summary>
        public static TerrainReactionResult PredictReaction(TileState currentState, int currentTier, ElementType spell)
        {
            TerrainReactionResult result = new TerrainReactionResult
            {
                ResultingState = currentState,
                ResultingTier = currentTier,
                ReactionName = "No Reaction",
                TriggeredReaction = false,
                Description = "Terrain unaffected."
            };

            switch (spell)
            {
                case ElementType.Fire:
                    // 1. OPPOSE Check: Fire on Water -> STEAM CLOUD!
                    if (currentState == TileState.Water)
                    {
                        result.ResultingState = TileState.Steam;
                        result.ResultingTier = 1;
                        result.ReactionName = "Steam Cloud";
                        result.TriggeredReaction = true;
                        result.Description = "Scalding Steam Cloud erupted! Water boiled away into vapor!";
                        return result;
                    }

                    // 2. MATCH Check: Fire matches Scorched Earth -> upgrades to Magma
                    if (currentState == TileState.Scorched)
                    {
                        // In Tutorial 1 (Throne Room), keep burnt carpet at Scorched Earth (Tier 1) without erupting molten magma!
                        bool isTutorial1 = Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsStage1Active;
                        if (isTutorial1)
                        {
                            result.ResultingState = TileState.Scorched;
                            result.ResultingTier = 1;
                            result.ReactionName = "Persistent Holy Blaze";
                            result.TriggeredReaction = false;
                            result.Description = "The scorched velvet carpet continues to sizzle with fiery embers!";
                            return result;
                        }

                        result.ResultingState = TileState.Magma;
                        result.ResultingTier = 2;
                        result.ReactionName = "Magma Surge";
                        result.TriggeredReaction = true;
                        result.Description = "Scorched earth melts and intensifies into molten Magma (Tier 2)!";
                        return result;
                    }
                    if (currentState == TileState.Magma)
                    {
                        result.ResultingState = TileState.Magma;
                        result.ResultingTier = 2;
                        result.ReactionName = "Intense Heat";
                        result.TriggeredReaction = false;
                        result.Description = "Magma roars with continuous flame!";
                        return result;
                    }

                    // 3. NEUTRAL Check: Fire infuses Barren/Grass/Steam -> Scorched Earth
                    if (currentState == TileState.Barren || currentState == TileState.Grass || currentState == TileState.Steam)
                    {
                        result.ResultingState = TileState.Scorched;
                        result.ResultingTier = 1;
                        result.ReactionName = "Scorched Earth";
                        result.TriggeredReaction = true;
                        result.Description = "Earth is charred into Scorched Earth (Tier 1)!";
                        return result;
                    }

                    // 4. Mud + Fire -> Bakes into Scorched Earth!
                    if (currentState == TileState.Mud)
                    {
                        result.ResultingState = TileState.Scorched;
                        result.ResultingTier = 1;
                        result.ReactionName = "Baked Mud";
                        result.TriggeredReaction = true;
                        result.Description = "Intense heat baked the wet mud into dry scorched ground!";
                        return result;
                    }
                    break;

                case ElementType.Water:
                    // 1. OPPOSE Check: Water on Magma / Scorched -> Quench & Cool!
                    if (currentState == TileState.Magma)
                    {
                        result.ResultingState = TileState.Scorched;
                        result.ResultingTier = 1;
                        result.ReactionName = "Steam Cataclysm & Magma Cool";
                        result.TriggeredReaction = true;
                        result.Description = "Molten magma cooled to Scorched Earth, violently erupting steam across 3 surrounding hexes!";
                        return result;
                    }
                    if (currentState == TileState.Scorched)
                    {
                        result.ResultingState = TileState.Steam;
                        result.ResultingTier = 1;
                        result.ReactionName = "Steam Eruption";
                        result.TriggeredReaction = true;
                        result.Description = "Scorched earth quenched into a scalding Steam Cloud!";
                        return result;
                    }
                    if (currentState == TileState.Mud)
                    {
                        result.ResultingState = TileState.Water;
                        result.ResultingTier = 1;
                        result.ReactionName = "Flooded Mud";
                        result.TriggeredReaction = true;
                        result.Description = "Mud flooded into Shallow Water.";
                        return result;
                    }

                    // 2. MATCH Check: Water matches Water -> upgrades to Deep Water (Tier 2)
                    if (currentState == TileState.Water)
                    {
                        result.ResultingState = TileState.Water;
                        result.ResultingTier = 2;
                        result.ReactionName = "Deep Water Surge";
                        result.TriggeredReaction = true;
                        result.Description = "Water level deepens into treacherous Deep Water (Tier 2)!";
                        return result;
                    }

                    // 3. NEUTRAL Check: Water infuses Barren/Grass/Steam -> Shallow Water (Tier 1)
                    if (currentState == TileState.Barren || currentState == TileState.Grass || currentState == TileState.Steam)
                    {
                        result.ResultingState = TileState.Water;
                        result.ResultingTier = 1;
                        result.ReactionName = "Water Inundation";
                        result.TriggeredReaction = true;
                        result.Description = "Ground flooded with Shallow Water (Tier 1).";
                        return result;
                    }
                    break;

                case ElementType.Earth:
                    // 1. Water + Earth -> MUD TRAP!
                    if (currentState == TileState.Water)
                    {
                        result.ResultingState = TileState.Mud;
                        result.ResultingTier = 1;
                        result.ReactionName = "Quagmire Mud Trap";
                        result.TriggeredReaction = true;
                        result.Description = "Earth collapsed into water, creating a sticky Mud trap!";
                        return result;
                    }

                    // 2. Fire + Earth -> Smother & cool
                    if (currentState == TileState.Scorched || currentState == TileState.Magma)
                    {
                        result.ResultingState = TileState.Barren;
                        result.ResultingTier = 0;
                        result.ReactionName = "Earth Smother";
                        result.TriggeredReaction = true;
                        result.Description = "Earth smothered the fiery embers into solid ground.";
                        return result;
                    }

                    // 3. Mud + Earth -> Fills mud back to solid dry ground
                    if (currentState == TileState.Mud)
                    {
                        result.ResultingState = TileState.Barren;
                        result.ResultingTier = 0;
                        result.ReactionName = "Earth Fill";
                        result.TriggeredReaction = true;
                        result.Description = "Solid earth filled the mud quagmire into stable ground.";
                        return result;
                    }

                    // 4. Neutral + Earth -> STONE PILLAR OBSTACLE!
                    if (currentState == TileState.Barren || currentState == TileState.Grass || currentState == TileState.Steam)
                    {
                        result.ResultingState = TileState.StonePillar;
                        result.ResultingTier = 2;
                        result.ReactionName = "Earth Spire";
                        result.TriggeredReaction = true;
                        result.Description = "Erected a solid Stone Pillar obstacle for Wall-Slam setups!";
                        return result;
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// Executes a spell on a target hex tile:
        /// 1. Updates terrain state and tier.
        /// 2. Damages occupant unit.
        /// </summary>
        public static TerrainReactionResult ApplySpell(HexTile3D tile, ElementType spell, int damage = 3)
        {
            if (tile == null) return default;

            TileState prevState = tile.State;

            // 1. Evaluate and apply terrain reaction
            TerrainReactionResult reaction = PredictReaction(tile.State, tile.TierLevel, spell);
            tile.SetState(reaction.ResultingState, reaction.ResultingTier);

            // Special: Water on Magma -> 3-hex random adjacent Steam Eruption!
            if (spell == ElementType.Water && prevState == TileState.Magma && HexGrid3D.Instance != null)
            {
                System.Collections.Generic.List<HexTile3D> neighbors = HexGrid3D.Instance.GetNeighbors(tile.Coordinates);
                System.Collections.Generic.List<HexTile3D> eligible = new System.Collections.Generic.List<HexTile3D>();
                foreach (var n in neighbors)
                {
                    if (n != null && n.State != TileState.Magma && n.State != TileState.Steam)
                    {
                        eligible.Add(n);
                    }
                }
                if (eligible.Count < 3)
                {
                    foreach (var n in neighbors)
                    {
                        if (n != null && !eligible.Contains(n) && n.State != TileState.Magma)
                        {
                            eligible.Add(n);
                        }
                    }
                }

                // Random shuffle
                for (int i = 0; i < eligible.Count; i++)
                {
                    int rnd = Random.Range(i, eligible.Count);
                    var temp = eligible[i];
                    eligible[i] = eligible[rnd];
                    eligible[rnd] = temp;
                }

                int count = Mathf.Min(3, eligible.Count);
                for (int i = 0; i < count; i++)
                {
                    HexTile3D steamNeighbor = eligible[i];
                    steamNeighbor.SetState(TileState.Steam, 1);
                    CombatVFXManager.Instance?.PlaySteamCloud(steamNeighbor.GetTopCenterPosition());
                }

                Debug.Log($"<color=#00E5FF><b>[Steam Cataclysm]</b></color> Magma at {tile.Coordinates} quenched, erupting steam across {count} adjacent hexes!");
            }

            // Trigger elemental reaction VFX
            if (reaction.ResultingState == TileState.Steam)
            {
                CombatVFXManager.Instance?.PlaySteamCloud(tile.GetTopCenterPosition());
            }
            else if (spell == ElementType.Fire)
            {
                CombatVFXManager.Instance?.PlayFireBurst(tile.GetTopCenterPosition());
            }
            else if (spell == ElementType.Water)
            {
                CombatVFXManager.Instance?.PlayWaterSplash(tile.GetTopCenterPosition());
            }
            else if (spell == ElementType.Earth)
            {
                CombatVFXManager.Instance?.PlayWallSlam(tile.GetTopCenterPosition(), Vector3.up);
            }

            // 2. Damage unit occupying the tile & refresh attunement
            TacticalUnit3D occupant = tile.CurrentOccupant as TacticalUnit3D;
            if (occupant != null)
            {
                occupant.UpdateAttunement();
                occupant.TakeDamage(damage);
                Debug.Log($"<color=#FF5252><b>[Direct Hit!]</b></color> {spell} dealt <b>{damage}</b> damage to <b>{occupant.UnitName}</b>!");
            }
            else
            {
                Debug.Log($"<color=#BDBDBD>[Spell Infusion]</color> {spell} struck empty tile {tile.Coordinates}.");
            }

            Debug.Log($"<color=#FFA726><b>[Spell Cast]</b></color> {spell} on {tile.Coordinates} → " +
                      $"<b>{reaction.ReactionName}</b> ({tile.State}, Tier {tile.TierLevel}). {reaction.Description}");

            return reaction;
        }
    }
}

