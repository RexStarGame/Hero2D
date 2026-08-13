#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public sealed class WorldTerrainPainterTargets
{
    public Grid grid;
    public Tilemap ground;
    public Tilemap paths;
    public Tilemap water;
    public Tilemap decorations;
    public Tilemap obstacles;
    public Tilemap collision;

    public Tilemap Get(WorldTerrainLayer layer)
    {
        switch (layer)
        {
            case WorldTerrainLayer.Ground: return ground;
            case WorldTerrainLayer.Paths: return paths;
            case WorldTerrainLayer.Water: return water;
            case WorldTerrainLayer.Decorations: return decorations;
            case WorldTerrainLayer.Obstacles: return obstacles;
            case WorldTerrainLayer.Collision: return collision;
            default: return null;
        }
    }

    public bool HasAnyTilemap()
    {
        return ground != null || paths != null || water != null || decorations != null ||
               obstacles != null || collision != null;
    }

    public void Clear()
    {
        grid = null;
        ground = null;
        paths = null;
        water = null;
        decorations = null;
        obstacles = null;
        collision = null;
    }
}
#endif
