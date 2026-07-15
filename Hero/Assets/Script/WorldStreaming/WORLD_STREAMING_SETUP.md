# Hero2D world streaming setup

This system keeps a large, premade 2D world performant without adding a networking dependency.

## One-time scene setup

1. Select the Player GameObject.
2. Run `Tools > Hero2D > World Streaming > Setup Manager And Selected Player`.
3. Split the premade map hierarchy into sensible rectangular chunk roots, for example `Chunk_0_0`, `Chunk_0_1`, etc.
4. Keep every `Chunk_*` root active. The streamer must remain able to control it.
5. Add `WorldChunk` to each chunk root (the second Tools menu command can do this).
6. Create child roots inside each chunk and assign them:
   - **Presentation Roots:** ground sprites, decorations and local terrain colliders.
   - **Simulation Roots:** enemies, AI, spawners and gameplay-only objects.
   - **Shared Roots:** objects required whenever either channel is active.
7. Set the chunk `Size` so its cyan Scene-view gizmo covers that part of the map.

Never place the Player, Canvas, camera, inventory/equipment systems, save managers, audio manager or the `WorldChunkStreamer` under a streamed root.

## Distance rules

`Load Distance` is smaller than `Unload Distance`. The gap is intentional hysteresis and prevents objects rapidly switching on/off at a boundary.

Start with the defaults and ensure chunks load before the camera can see their edges. Increase presentation distances for a larger camera or faster movement.

## Future co-op roles

- Local client player: Presentation ON, Simulation ON.
- Remote player represented on a client: Presentation OFF, Simulation ON (when the client owns relevant simulation).
- Dedicated server target: Presentation OFF, Simulation ON.

A future networking adapter can call `WorldStreamingTarget.ConfigureChannels`. World state such as opened chests, defeated bosses and unique pickups should be keyed by `WorldChunk.ChunkId`; the streaming system only sleeps objects and does not itself decide authoritative saved state.
