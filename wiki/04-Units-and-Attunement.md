# 04. Units, Archetypes & FFT Attunement

Battles in **Elemental Hex Tactics 3D** are designed around tight skirmishes (2v2 demo up to 4v4 in campaign). Instead of managing 20 identical foot-soldiers, each unit represents a distinct archetype with asymmetrical physical presence.

---

## 3-Tier Unit Size Taxonomy (Small, Normal, Big)

Units are divided into 3 physical size tiers, which dictates how they interact with push physics, hazard tiles, and how many equipment slots they have:

| Size | Archetypes | In-Game Height | Gear Slots | Physics & Hazard Behavior | Tactical & Domain Role |
| :--- | :--- | :---: | :---: | :--- | :--- |
| **`Small`** | **Minions & Demi-Humans**<br/>*(Kobolds, Imps, Slimes, Dark Elf Scouts)* | ~1.0m - 1.2m | **1–2 Aux Slots**<br/>*(Trinkets / Work Tools)* | **Lightweight:** Takes extra displacement on shoves. Very vulnerable to lava & deep water. | Agile harasser, trap planter, or assigned to the *Mana Mine & Farm* as domain workforce. |
| **`Normal`** | **Humanoid Shapers**<br/>*(Commander, Holy Inquisitors)* | ~1.7m - 1.8m | **5 Full RPG Slots**<br/>*(Weapon, Armor, Boots, Helm, Ring)* | **Standard:** Displaced 1 hex by shoves. Needs gear (like *Lava Walkers*) to cross hazards safely. | Tactical terraformer. Infuses elements, manipulates turn order, executes kinetic push combos. |
| **`Big`** | **Colossal Titans**<br/>*(Magma Behemoth, Leviathan)* | ~2.5m - 3.5m | **3 Specialist Slots**<br/>*(1 Relic + 2 Scrolls)* | **Unshovable Heavyweight:** Immune to normal shoves and native hazards (Magma Titan walks on lava). | Heavy frontline anchor. Consumes land (*Siphon*), drops screen-wiping ultimates. Requires daily Food upkeep. |

> **Dev Note on Equipment Design:**  
> The 5 slots on Normal humanoid units aren't for generic "+5 Attack" stat inflation. They solve tactical puzzles: **Weapons** decide which element you infuse on attack, **Boots** let you walk on magma/ice, and **Helms/Goggles** let you aim through steam smokescreens!

---

## Active Skirmish Roster (Current Demo)

| Unit | Faction | Archetype | Affinity | HP | ATK | Move | Role & Summary |
| :--- | :--- | :--- | :--- | :---: | :---: | :---: | :--- |
| **Commander** | Player | **Commander** | None | 10 | 3 | 3 | Tactical caster. Master of elemental spells (`Fireball`, `Water`, `Earth Spire`), shoves, and siphoning. |
| **Magma Titan** | Player | **Titan** | Fire | 18 | 4 | 2 | Colossal behemoth. Immune to Magma, Deep Water, and Mud. Uses `Titan Strike`, `Tail Shove`, and `Magma Cataclysm`. |
| **Dracomancer** | Enemy | **Commander** | Fire | 10 | 3 | 3 | Enemy pyromancer boss. Fires ranged Fireballs and advances aggressively. |
| **Demon Slime** | Enemy | **Minion** | None | 8 | 2 | 3 | Mobile frontliner. Uses melee strikes and shoves to knock player units off high ground. |

---

## Unit Abilities & Commands

### Commander Abilities
* **`Fireball` (Range 3, Dmg 3):** Fires a projectile. Chars dry ground to Scorched Earth, or boils water into Steam.
* **`Water Surge` (Range 3, Dmg 2):** Quenches Magma (triggering a 3-hex steam eruption!) and Scorched Earth.
* **`Earth Spire` (Range 3, Dmg 2):** Raises a solid **`StonePillar`** on flat earth, or turns water into a **`Mud`** quagmire.
* **`Push` (Range 1, Dmg 1):** Shoves target 1 hex backward. Triggers **Wall-Slam collisions** if blocked.
* **`Siphon Land`:** Absorbs active elemental ground (`Magma`, `Scorched`, or `Water`), resetting the hex to barren earth and granting **+1 Elemental Core (`★`)**.

### Titan Abilities
* **`Titan Strike` (Range 1, Dmg 4):** Heavy physical smash.
* **`Tail Shove` (Range 1, Dmg 1):** Heavy knockback tail swipe.
* **`Siphon Land`:** Harvests adjacent elemental tiles for Cores.
* **`🌋 Magma Cataclysm` (Ultimate):**
  * **Cost:** 1 Elemental Core (`★`) | **Range:** 2 Hexes.
  * **Area of Effect:** Center hex + all 6 surrounding neighbor hexes (7 hexes total!).
  * **Damage:** **8 Massive Damage** to all enemies in the blast radius!
  * **Terrain Effect:** Turns neutral tiles into Scorched Earth and upgrades Scorched to molten Magma.
  * *(Dev Note: 8 damage on 7 hexes sounds broken, but it requires harvesting a core, surviving in range, and positioning carefully. When it hits, it SHOULD feel like an extinction event!)*

---

## FFT Geomancer-Style Attunement

Units standing on active elemental tiles dynamically gain passive buffs via `TacticalUnit3D.UpdateAttunement()`:

* **🔥 Flame Surge:** Stand on `Scorched Earth` or `Magma` $\rightarrow$ **+2 Bonus Attack Damage** on all strikes! Base ring glows fiery orange (`#FF841A`).
* **💧 Aqua Surge:** Stand in `Water` $\rightarrow$ **+1 Bonus Move Range**! Base ring glows electric cyan (`#33D9FF`).
* **💨 Vapor Shroud:** Stand inside a `Steam` cloud $\rightarrow$ Billowing mist obscures sightlines and grants damage evasion.
