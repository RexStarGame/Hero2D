# Hero2D Regional Enemy Spawning

This system replaces the old global enemy mixture with physical RPG regions.
It does not modify the paused Orc enemy.

## One-time setup

1. In Unity, run:
   `Hero2D > Enemies > Regional Spawning > Create Regional Spawn Director`.
2. Disable the old global `EnemySpawn` component when the first regional
   zones are ready. Do not run both systems for the same normal enemies.
3. Create an `EnemyRegionProfile` asset with:
   `Create > Hero2D > Enemies > Enemy Region Profile`.

## Add a zone to a streamed chunk

1. Select that `WorldChunk` object's **Simulation** root.
2. Run:
   `Hero2D > Enemies > Regional Spawning >
   Add Spawn Zone To Selected Simulation Root`.
3. Assign the region profile on the new `EnemySpawnZone`.

The resulting hierarchy is:

```text
WorldChunk
└── Simulation
    └── EnemySpawnZone
        ├── DynamicEnemies
        └── Enemy 1 Spawn Area
```

Because both the zone and `DynamicEnemies` are beneath Simulation, distant
chunks pause their spawner, AI, animation and physics together.

## Add one independently configured enemy

1. Select `EnemySpawnZone`.
2. Run:
   `Hero2D > Enemies > Regional Spawning >
   Add Enemy Entry Area To Selected Zone`.
3. Resize the new trigger `BoxCollider2D` to paint where that exact enemy may
   appear. A `PolygonCollider2D` may be used instead.
4. On `EnemySpawnZone > Enemies`, assign:
   - Enemy Prefab
   - Desired Population
   - Minimum/Maximum Respawn Time
   - Minimum/Maximum Distance From Players
   - Allowed Ground Layers when ground uses colliders
   - Blocked Environment Layers
   - Optional Blocked Tags

Every entry has its own collider and independent timer. Two enemy types in one
region can therefore use different habitats and respawn speeds.

## Walls and ground

- Put walls, rocks, trees and environmental blockers on dedicated layers such
  as `Walls` and `OtherStuff`.
- Include those layers in each entry's **Blocked Environment Layers**.
- Prefer layers over tags because physics layer checks are faster.
- Optional Blocked Tags are an additional safety check.
- If walkable grass/path colliders exist, enable **Require Allowed Ground** and
  select their layers. If the ground is purely visual and has no collider,
  leave this option disabled; the spawn-area collider remains the allowed map.
- **Enemy Clearance Radius** keeps the enemy body away from obstacle edges.
- Safe zones are rejected automatically.

## Respawn and streaming behaviour

- A killed/destroyed enemy starts only its own entry's respawn timer.
- A chunk becoming inactive does not count as an enemy death.
- Sleeping enemies stay registered in the global budget.
- When the chunk returns, its existing enemies resume instead of duplicates
  being created elsewhere.

## Future co-op / online authority

Single-player requires no authority component.

For networking, create a MonoBehaviour implementing:

```csharp
public interface IRegionalSpawnAuthority
{
    bool HasRegionalSpawnAuthority { get; }
}
```

Assign it to the director's **Authority Provider**. It should return `true`
only on the host/server. All registered `WorldStreamingTarget` players are
used for distance safety, so a candidate is rejected if it appears too close
to any player and must remain near at least one simulation target.

This prevents duplicate client-side spawning without tying the project to a
specific networking package.
