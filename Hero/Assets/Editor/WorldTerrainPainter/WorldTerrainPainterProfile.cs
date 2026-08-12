#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum WorldTerrainLayer
{
    Ground,
    Paths,
    Water,
    Decorations,
    Obstacles,
    Collision
}

[Serializable]
public sealed class WorldTerrainWeightedTile
{
    public TileBase tile;
    [Min(0.01f)] public float weight = 1f;
}

[Serializable]
public sealed class WorldTerrainType
{
    public string displayName = "New Terrain";
    public Color mapColor = Color.green;
    public WorldTerrainLayer targetLayer = WorldTerrainLayer.Ground;
    public List<WorldTerrainWeightedTile> paintTiles = new List<WorldTerrainWeightedTile>();

    [Header("Optional Decoration Scatter")]
    [Range(0f, 1f)] public float decorationChance;
    public List<WorldTerrainWeightedTile> decorationTiles = new List<WorldTerrainWeightedTile>();

    [Header("Optional Collision")]
    public bool paintCollision;
    public TileBase collisionTile;

    public TileBase PickPaintTile(System.Random random)
    {
        return PickWeighted(paintTiles, random);
    }

    public TileBase PickDecorationTile(System.Random random)
    {
        return PickWeighted(decorationTiles, random);
    }

    private static TileBase PickWeighted(List<WorldTerrainWeightedTile> entries, System.Random random)
    {
        if (entries == null || entries.Count == 0) return null;

        double total = 0d;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].tile != null)
                total += Math.Max(0.01f, entries[i].weight);
        }

        if (total <= 0d) return null;

        double roll = random.NextDouble() * total;
        for (int i = 0; i < entries.Count; i++)
        {
            WorldTerrainWeightedTile entry = entries[i];
            if (entry == null || entry.tile == null) continue;

            roll -= Math.Max(0.01f, entry.weight);
            if (roll <= 0d) return entry.tile;
        }

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i] != null && entries[i].tile != null)
                return entries[i].tile;
        }

        return null;
    }
}

[CreateAssetMenu(fileName = "World Terrain Profile", menuName = "Hero2D/World Terrain Painter Profile")]
public sealed class WorldTerrainPainterProfile : ScriptableObject
{
    [Tooltip("Suggested world size for a 32px tile imported at 100 Pixels Per Unit is 0.32 x 0.32.")]
    public Vector2 cellSize = new Vector2(0.32f, 0.32f);

    [Range(0f, 0.25f)]
    [Tooltip("Maximum RGB distance accepted by the color-map importer.")]
    public float colorTolerance = 0.03f;

    public List<WorldTerrainType> terrains = new List<WorldTerrainType>();

    public WorldTerrainType FindClosestTerrain(Color color)
    {
        WorldTerrainType best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < terrains.Count; i++)
        {
            WorldTerrainType terrain = terrains[i];
            if (terrain == null) continue;

            Color mapped = terrain.mapColor;
            float distance = Mathf.Sqrt(
                Mathf.Pow(color.r - mapped.r, 2f) +
                Mathf.Pow(color.g - mapped.g, 2f) +
                Mathf.Pow(color.b - mapped.b, 2f));

            if (distance < bestDistance)
            {
                best = terrain;
                bestDistance = distance;
            }
        }

        return bestDistance <= colorTolerance ? best : null;
    }

    private void OnValidate()
    {
        cellSize.x = Mathf.Max(0.001f, cellSize.x);
        cellSize.y = Mathf.Max(0.001f, cellSize.y);
    }
}
#endif
