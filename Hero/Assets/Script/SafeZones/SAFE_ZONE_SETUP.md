# Hero2D safe-zone setup

1. Create an empty GameObject named `SafeZone`.
2. Add a `BoxCollider2D`, `CircleCollider2D`, `CapsuleCollider2D` or `PolygonCollider2D` matching the desired protected area.
3. Add `SafeZone2D`. It automatically makes the collider a trigger and adds/configures a kinematic `Rigidbody2D`.
4. Leave `Enemy Layers` at Everything unless the project later uses a dedicated enemy-only filter.
5. Keep all four rule toggles enabled for a complete non-combat zone.

No tag or extra component is required on current enemies. `EnemyHealth` and `BossHealth` are recognised automatically.

The player's body position decides whether an attack is allowed. A player cannot stand inside and reach an enemy outside with the sword. Player damage is rejected centrally in `PlayerHealth`, so boss AoE and future damage sources are covered too.

For future multiplayer, the server/host should assign `SafeZone2D.HasSimulationAuthority` so only the authoritative simulation expels enemies. Clients may still display the zone, but should not independently decide networked enemy positions.
