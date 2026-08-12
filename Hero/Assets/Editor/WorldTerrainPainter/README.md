# Hero2D World Terrain Painter

Open the tool from **Tools > Hero2D > World Terrain Painter** (`Ctrl/Cmd + Shift + T`).

## First-time setup

1. Create a terrain profile from the painter window.
2. Select the profile asset and add Tile or Rule Tile assets to each terrain's weighted paint list.
3. Use **Create Layered Tilemaps** for a new setup, or assign existing Tilemaps.
4. Enable Scene Painting and paint in the Scene view.

The default Grid cell size is `0.32 x 0.32` world units, matching a 32-pixel tile imported at 100 Pixels Per Unit. Change the profile cell size before creating layers if your tile imports differ.

If your artwork is still only Sprite assets, select the sprites or sliced sprite sheet and use **Tools > Hero2D > Terrain Painter > Create Tile Assets From Selected Sprites**.

## Professional workflow

- Use ordinary Tile assets for weighted natural variation.
- Use Rule Tile assets when a terrain needs automatic edges and corners.
- Configure decoration chance and weighted decoration tiles for rocks, flowers, cacti, and similar details.
- Set Minimum Decoration Scale and Maximum Decoration Scale on each terrain to give scattered decorations a fixed or randomized size. For example, `1.5` and `2` creates decorations between 150% and 200% of their normal size. Existing profiles remain at `1` and `1` until changed.
- Scale is applied when a decoration is painted. Repaint existing decoration cells if you want them to receive the new range.
- Enable collision on terrain types that should paint into the hidden collision Tilemap.
- Hold Shift while painting to erase temporarily.
- Every brush stroke, rectangle, fill, import, and layer-creation action supports Undo.

The tool never converts or deletes the existing manually placed SpriteRenderer environment. Layer creation and painting happen only after an explicit button press or Scene-view input.

## Color maps

Assign a texture in the optional importer. Every opaque pixel maps to one Tilemap cell. RGB colors are matched against each terrain's Map Color using the profile tolerance. Transparent or unmatched pixels are skipped.

Recommended colors:

- Green: grass
- Yellow: sand/desert
- Brown: roads/paths
- Blue: water

The importer does not modify the texture's Read/Write import setting.
