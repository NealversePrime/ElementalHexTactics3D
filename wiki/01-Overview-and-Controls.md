# 01. Overview, Camera & Tactical Controls

## Concept & The Core Pitch

**Elemental Hex Tactics 3D** is a turn-based tactical RPG built around spatial puzzles and elemental chemistry. The design is basically my love letter to a few specific games:

* **The Spatial Puzzle of *Into the Breach*:** Every turn is about positioning, angles, and kinetic shoves. Smashing an enemy into a stone pillar or knocking them off high ground is usually way more effective than just hitting them with a basic attack.
* **The Systemic Chemistry of *Divinity: Original Sin*:** Elements aren't just damage numbers—they physically reshape the hex tiles. Fire boils water into blinding steam, water quenches magma, and earth turns puddles into sticky mud quagmires.
* **The Environmental Attunement of *Final Fantasy Tactics*:** Geomancer-inspired passive buffs. If you stand in fire with fire affinity, you hit harder. If you stand in water, you move faster.
* **The HD-2D Diorama Aesthetic of *Triangle Strategy*:** 2.5D pixel standees placed on elevated 3D hex pillars with tilt-shift depth-of-field and radiant bloom.

---

## Tactical Camera Controls

The camera controller lives in [`TacticalCameraController.cs`](https://github.com/NealversePrime/ElementalHexTactics3D/blob/main/Assets/Scripts/Camera/TacticalCameraController.cs).

### Key Bindings

| Key / Input | Action | What it actually does |
| :--- | :--- | :--- |
| **`W` `A` `S` `D`** | **Pan Camera** | Moves the camera target relative to wherever the camera is currently facing. |
| **`Q`** | **Rotate Left 60°** | Snaps 60° counter-clockwise so the camera always lines up with the hex grid edges. |
| **`E`** | **Rotate Right 60°** | Snaps 60° clockwise to align with the hex edges. |
| **`Mouse Scroll`** | **Zoom In / Out** | Smoothly zooms between 5.0m (close-up) and 25.0m (strategic battlefield overview). |
| **Middle Mouse Drag** | **Free Pan** | Quick drag if you don't wanna use WASD. |

> **Dev Note (Why 60° snaps?):**  
> Originally I had free 360-degree camera rotation like a standard RTS, but playtesters kept getting disoriented trying to figure out which way was "forward" on pointy-topped hexes. Locking the rotation to crisp 60° increments using `Mathf.SmoothDampAngle` completely solved the disorientation while keeping the camera rotation buttery smooth.

---

## Turn Structure & Action Economy

Turn management is handled by [`TurnManager3D.cs`](https://github.com/NealversePrime/ElementalHexTactics3D/blob/main/Assets/Scripts/Turn/TurnManager3D.cs). Combat runs on alternating faction phases (Player Phase $\rightarrow$ Enemy Phase $\rightarrow$ Round Advance).

### Round Progression

1. **Player Phase:**
   * All player units refresh their action flags (`ResetTurnActions()`).
   * Environmental hazard checks resolve for tiles units are standing on (magma burn, deep water/mud turn-denial).
   * You can command units in any order you want (no rigid initiative lock).
   * Click **`END TURN`** when done.
2. **Enemy Phase:**
   * Enemy units activate sequentially.
   * AI calcualtes nearest targets, paths around cliffs, advances, and uses spells or shoves.
3. **Round Advance:**
   * Status debuffs tick down (`OnTurnEnd()`).
   * Round counter increments and control flips back to the player.

### Unit Action Economy (Move + Act)

Every unit gets two flags per turn:
* **`HasMovedThisTurn`:** Move up to your `EffectiveMoveRange` along valid hex paths.
* **`HasActedThisTurn`:** Cast a spell, make a melee strike, push, or siphon land.
* **`IsExhausted`:** When a unit does both, its base selection ring dims to gray so you know it's done for the round.

---

## Tactical HUD & Interface

UI is rendered thru [`HexGridInteraction3D.cs`](https://github.com/NealversePrime/ElementalHexTactics3D/blob/main/Assets/Scripts/InputHandling/HexGridInteraction3D.cs) using solid slate panels so it stays readable against glowing magma and particle effects:

1. **Top-Left Status HUD:** Displays round counter, phase banner, unit HP/ATK, elemental attunements, and stored **Elemental Cores**. Hovering any hex displays its cube coordinates, elevation, and terrain state.
2. **Top Turn Announcement Banner:** Shows phase changes (`⚔️ ENEMY PHASE`, `ROUND X - PLAYER TURN`).  
   *(Dev Note: Clamped this to `minX = 430f` on smaller screens because it kept overlapping the top-left unit panel—fixed now!)*
3. **Bottom Action Bar:** Move toggle, spell buttons (`Fireball`, `Water`, `Earth Spire`), kinetic abilities (`Push`, `Tail Shove`), and Titan ultimates (`Magma Cataclysm`).
4. **Predictive Ghost UI Preview:** When you hover a spell over a hex tile, a preview card pops up in the bottom-right showing the predicted reaction name, resulting tile state, and description *before* you click to confirm.
